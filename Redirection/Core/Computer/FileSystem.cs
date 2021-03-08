using Dan200.Core.Computer.Devices.DiskDrive;
using Dan200.Core.Lua;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Dan200.Core.Computer
{
    public class FileSystem : IDisposable
    {
        public const int BLOCK_SIZE = 256;

        public static long Measure(IMount mount)
        {
            return Measure(mount, FilePath.Empty);
        }

        public static long Measure(IMount mount, FilePath path)
        {
            path = path.UnRoot();
            if (mount.Exists(path))
            {
                long size = MeasureImpl(mount, path);
                if (path.IsEmpty && mount.IsDir(path))
                {
                    size -= BLOCK_SIZE; // Get the root directory for free
                }
                return size;
            }
            else
            {
                return 0;
            }
        }

        private static long MeasureImpl(IMount mount, FilePath path)
        {
            if (mount.IsDir(path))
            {
                long size = BLOCK_SIZE;
                var files = mount.List(path);
                foreach (var name in files)
                {
                    size += MeasureImpl(mount, FilePath.Combine(path, name));
                }
                return size;
            }
            else
            {
                long size = mount.GetSize(path);
                if ((size % BLOCK_SIZE) == 0)
                {
                    return size;
                }
                else
                {
                    return ((size / BLOCK_SIZE) + 1) * BLOCK_SIZE;
                }
            }
        }

        private class MountInfo : IDisposable
        {
            private readonly LuaObjectRef<LuaMount> m_luaMount;
            public readonly FilePath Location;
            public readonly FilePath SubPath;
            public readonly bool ReadOnly;

            public DateTime LastReadTime;
            public DateTime LastWriteTime;

            public LuaMount LuaMount
            {
                get
                {
                    return m_luaMount.Value;
                }
            }

            public IMount Mount
            {
                get
                {
                    return m_luaMount.Value.Mount;
                }
            }

            public IWritableMount WritableMount
            {
                get
                {
                    return ReadOnly ? null : m_luaMount.Value.WritableMount;
                }
            }

            public MountInfo(FilePath location, LuaMount mount, FilePath subPath, bool readOnly)
            {
                m_luaMount = new LuaObjectRef<LuaMount>(mount);
                Location = location;
                SubPath = subPath;
                ReadOnly = readOnly;

                LastReadTime = DateTime.MinValue;
                LastWriteTime = DateTime.MinValue;
            }

            public void Dispose()
            {
                m_luaMount.Dispose();
            }
        }

        private struct MountFilePath
        {
            public readonly MountInfo MountInfo;
            public readonly FilePath Path;

            public LuaMount LuaMount
            {
                get
                {
                    return MountInfo.LuaMount;
                }
            }

            public IMount Mount
            {
                get
                {
                    return MountInfo.Mount;
                }
            }

            public IWritableMount WritableMount
            {
                get
                {
                    return MountInfo.WritableMount;
                }
            }

            public bool ReadOnly
            {
                get
                {
                    return MountInfo.ReadOnly;
                }
            }

            public MountFilePath(MountInfo mount, FilePath path)
            {
                MountInfo = mount;
                Path = path;
            }

            public FilePath GetGlobalPath()
            {
                if (MountInfo.SubPath.IsEmpty)
                {
                    return FilePath.Combine(MountInfo.Location, Path);
                }
                else
                {
                    return FilePath.Combine(MountInfo.Location, Path.ToLocal(MountInfo.SubPath));
                }
            }
        }

        private MountInfo m_emptyRootMount;
        private List<MountInfo> m_mounts;

        public FileSystem()
        {
            var rootMount = new LuaMount(new EmptyMount("root"));
            rootMount.Connected = true;
            m_emptyRootMount = new MountInfo(FilePath.Empty, rootMount, FilePath.Empty, true);
            m_mounts = new List<MountInfo>();
        }

        public void Dispose()
        {
            UnmountAll();
            m_emptyRootMount.Dispose();
        }

        public FilePath[] GetMountLocations()
        {
            var locations = new FilePath[m_mounts.Count];
            for (int i = 0; i < m_mounts.Count; ++i)
            {
                locations[i] = m_mounts[i].Location;
            }
            return locations;
        }

        public void Mount(LuaMount mount, FilePath location, FilePath subPath = default(FilePath), bool readOnly = false)
        {
            // Check conditions
            if (location.IsBackPath)
            {
                throw new IOException(string.Format("Invalid location: {0}", location));
            }
            if (subPath.IsBackPath)
            {
                throw new IOException(string.Format("Invalid subPath: {0}", subPath));
            }
            location = location.UnRoot();
            subPath = subPath.UnRoot();

            // Create the mount
            var info = new MountInfo(location, mount, subPath, readOnly);
            CheckEitherExists(new MountFilePath(info, subPath));

            // Add the mount
            m_mounts.Add(info);
            for (int i = m_mounts.Count - 1; i >= 0; --i)
            {
                var mountInfo = m_mounts[i];
                if (mountInfo != info && mountInfo.Location == location)
                {
                    m_mounts.RemoveAt(i);
                    mountInfo.Dispose();
                    break;
                }
            }
        }

        public LuaMount GetMount(FilePath location, out FilePath o_subPath, out bool o_readOnly)
        {
            var file = Resolve(location);
            if (file.MountInfo != m_emptyRootMount)
            {
                o_subPath = file.Path;
                o_readOnly = file.ReadOnly;
                return file.LuaMount;
            }
            else
            {
                o_subPath = default(FilePath);
                o_readOnly = default(bool);
                return null;
            }
        }

        public void GetMountAccessTimes(LuaMount mount, out DateTime o_lastReadTime, out DateTime o_lastWriteTime)
        {
            o_lastReadTime = DateTime.MinValue;
            o_lastWriteTime = DateTime.MinValue;
            for (int i = m_mounts.Count - 1; i >= 0; --i)
            {
                var mountInfo = m_mounts[i];
                if (mountInfo.LuaMount == mount)
                {
                    if (mountInfo.LastReadTime > o_lastReadTime) o_lastReadTime = mountInfo.LastReadTime;
                    if (mountInfo.LastWriteTime > o_lastWriteTime) o_lastWriteTime = mountInfo.LastWriteTime;
                }
            }
        }

        public void Unmount(LuaMount mount)
        {
            // Remove the mount
            for (int i = m_mounts.Count - 1; i >= 0; --i)
            {
                var mountInfo = m_mounts[i];
                if (mountInfo.LuaMount == mount)
                {
                    m_mounts.RemoveAt(i);
                    mountInfo.Dispose();
                }
            }
        }

        public void Unmount(FilePath location)
        {
            // Remove the mount
            location = location.UnRoot();
            for (int i = m_mounts.Count - 1; i >= 0; --i)
            {
                var mountInfo = m_mounts[i];
                if (mountInfo.Location == location)
                {
                    m_mounts.RemoveAt(i);
                    mountInfo.Dispose();
                    return;
                }
            }
            throw new IOException(string.Format("No mount at location: {0}", location));
        }

        public void UnmountAll()
        {
            for (int i = 0; i < m_mounts.Count; ++i)
            {
                var mountInfo = m_mounts[i];
                mountInfo.Dispose();
            }
            m_mounts.Clear();
        }

        public string[] List(FilePath path)
        {
            var file = Resolve(path);
            CheckConnected(file);
            CheckDirExists(file);
            RecordRead(file);

            // Add matching files
            var files = new List<string>();
            files.AddRange(file.Mount.List(file.Path));

            // Add matching mounts
            foreach (var mount in m_mounts)
            {
                if (!mount.Location.IsRoot && mount.Location.GetDir() == path)
                {
                    var name = mount.Location.GetName();
                    if (!files.Contains(name))
                    {
                        files.Add(name);
                    }
                }
            }

            return files.ToArray();
        }

        public bool Exists(FilePath path)
        {
            var file = Resolve(path);
            CheckConnected(file);
            return file.Mount.Exists(file.Path);
        }

        public long GetSize(FilePath path)
        {
            var file = Resolve(path);
            CheckConnected(file);
            CheckFileExists(file);
            return file.Mount.GetSize(file.Path);
        }

        public DateTime GetModifiedTime(FilePath path)
        {
            var file = Resolve(path);
            CheckConnected(file);
            CheckFileExists(file);
            return file.Mount.GetModifiedTime(file.Path);
        }

        public bool IsDir(FilePath path)
        {
            var file = Resolve(path);
            CheckConnected(file);
            return file.Mount.Exists(file.Path) && file.Mount.IsDir(file.Path);
        }

        public bool IsReadOnly(FilePath path)
        {
            var file = Resolve(path);
            return file.WritableMount == null;
        }

        public void MakeDir(FilePath path)
        {
            var file = Resolve(path);
            CheckConnected(file);
            CheckWritable(file);
            CheckFileDoesNotExist(file);
            if (!file.Mount.Exists(file.Path))
            {
                RecordWrite(file);
                file.WritableMount.MakeDir(file.Path);
            }
        }

        public void Move(FilePath path, FilePath dest)
        {
            var srcFile = Resolve(path);
            var dstFile = Resolve(dest);
            CheckConnected(srcFile);
            CheckConnected(dstFile);
            CheckWritable(srcFile);
            CheckWritable(dstFile);
            CheckEitherExists(srcFile);
            CheckEitherDoesNotExist(dstFile);
            CheckPathsNotOverlapping(srcFile, dstFile);
            RecordRead(srcFile);
            RecordWrite(srcFile);
            RecordWrite(dstFile);
            if (srcFile.Mount == dstFile.Mount)
            {
                srcFile.WritableMount.Move(srcFile.Path, dstFile.Path);
            }
            else
            {
                CopyRecursive(srcFile, dstFile);
                dstFile.WritableMount.Delete(dstFile.Path);
            }
        }

        public void Copy(FilePath path, FilePath dest)
        {
            var srcFile = Resolve(path);
            var dstFile = Resolve(dest);
            CheckConnected(srcFile);
            CheckConnected(dstFile);
            CheckWritable(dstFile);
            CheckEitherExists(srcFile);
            CheckEitherDoesNotExist(dstFile);
            CheckPathsNotOverlapping(srcFile, dstFile);
            RecordRead(srcFile);
            RecordWrite(dstFile);
            if (srcFile.Mount == dstFile.Mount)
            {
                dstFile.WritableMount.Copy(srcFile.Path, dstFile.Path);
            }
            else
            {
                CopyRecursive(srcFile, dstFile);
            }
        }

        private void CopyRecursive(MountFilePath src, MountFilePath dst)
        {
            if (src.Mount.IsDir(src.Path))
            {
                dst.WritableMount.MakeDir(dst.Path);
                foreach (var file in src.Mount.List(src.Path))
                {
                    CopyRecursive(
                        new MountFilePath(src.MountInfo, FilePath.Combine(src.Path, file)),
                        new MountFilePath(dst.MountInfo, FilePath.Combine(dst.Path, file))
                    );
                }
            }
            else
            {
                using (var reader = src.Mount.OpenForBinaryRead(src.Path))
                {
                    using (var writer = dst.WritableMount.OpenForBinaryWrite(dst.Path, false))
                    {
                        reader.CopyTo(writer);
                    }
                }
            }
        }

        public void Delete(FilePath path)
        {
            var file = Resolve(path);
            CheckConnected(file);
            CheckWritable(file);
            if (file.Path == file.MountInfo.SubPath)
            {
                throw new IOException("Cannot delete mount root");
            }
            if (file.Mount.Exists(file.Path))
            {
                RecordWrite(file);
                file.WritableMount.Delete(file.Path);
            }
        }

        public TextReader OpenForRead(FilePath path)
        {
            var file = Resolve(path);
            CheckConnected(file);
            CheckFileExists(file);
            RecordRead(file);
            return file.Mount.OpenForRead(file.Path);
        }

        public Stream OpenForBinaryRead(FilePath path)
        {
            var file = Resolve(path);
            CheckConnected(file);
            CheckFileExists(file);
            RecordRead(file);
            return file.Mount.OpenForBinaryRead(file.Path);
        }

        public TextWriter OpenForWrite(FilePath path, bool append)
        {
            var file = Resolve(path);
            CheckConnected(file);
            CheckWritable(file);
            CheckDirDoesNotExist(file);
            CheckParentDirExists(file);
            RecordWrite(file);
            return file.WritableMount.OpenForWrite(file.Path, append);
        }

        public Stream OpenForBinaryWrite(FilePath path, bool append)
        {
            var file = Resolve(path);
            CheckConnected(file);
            CheckWritable(file);
            CheckDirDoesNotExist(file);
            CheckParentDirExists(file);
            RecordWrite(file);
            return file.WritableMount.OpenForBinaryWrite(file.Path, append);
        }

        public FilePath[] Find(FilePath wildcard)
        {
            wildcard = wildcard.UnRoot();
            var dir = wildcard.GetDir();
            var namePattern = wildcard.GetName();
            var nameRegex = new Regex("^" + Regex.Escape(namePattern).Replace("\\*", ".*") + "$");
            var results = new List<FilePath>();

            var dirFile = Resolve(dir);
            if (dirFile.LuaMount.Connected &&
                dirFile.Mount.Exists(dirFile.Path) &&
                dirFile.Mount.IsDir(dirFile.Path))
            {
                // Add matching files
                foreach (var filename in dirFile.Mount.List(dirFile.Path))
                {
                    if (nameRegex.IsMatch(filename))
                    {
                        results.Add(FilePath.Combine(dir, filename));
                    }
                }

                // Add matching mounts
                foreach (var mount in m_mounts)
                {
                    if (!mount.Location.IsRoot && mount.Location.GetDir() == dir)
                    {
                        if (nameRegex.IsMatch(mount.Location.GetName()))
                        {
                            if (!results.Contains(mount.Location))
                            {
                                results.Add(mount.Location);
                            }
                        }
                    }
                }
            }

            return results.ToArray();
        }

        private void CheckConnected(MountFilePath file)
        {
            if (!file.LuaMount.Connected)
            {
                throw new IOException(string.Format("Access Denied: Mount {0} is disconnected", file.Mount.Name));
            }
        }

        private void CheckWritable(MountFilePath file)
        {
            if (file.WritableMount == null)
            {
                throw new IOException(string.Format("Access Denied: Mount {0} is read only", file.Mount.Name));
            }
        }

        private void CheckEitherExists(MountFilePath file)
        {
            if (!file.Mount.Exists(file.Path))
            {
                throw new FileNotFoundException(string.Format("No such path: {0}", file.GetGlobalPath()));
            }
        }

        private void CheckEitherDoesNotExist(MountFilePath file)
        {
            if (file.Mount.Exists(file.Path))
            {
                throw new IOException(string.Format("Path {0} already exists", file.GetGlobalPath()));
            }
        }

        private void CheckFileExists(MountFilePath file)
        {
            if (!file.Mount.Exists(file.Path) || file.Mount.IsDir(file.Path))
            {
                throw new FileNotFoundException(string.Format("No such file: {0}", file.GetGlobalPath()));
            }
        }

        private void CheckFileDoesNotExist(MountFilePath file)
        {
            if (file.Mount.Exists(file.Path) && !file.Mount.IsDir(file.Path))
            {
                throw new IOException(string.Format("File {0} already exists", file.GetGlobalPath()));
            }
        }

        private void CheckParentDirExists(MountFilePath file)
        {
            CheckDirExists(new MountFilePath(file.MountInfo, file.Path.GetDir()));
        }

        private void CheckDirExists(MountFilePath file)
        {
            if (!file.Mount.Exists(file.Path) || !file.Mount.IsDir(file.Path))
            {
                throw new FileNotFoundException(string.Format("No such directory: {0}", file.GetGlobalPath()));
            }
        }

        private void CheckDirDoesNotExist(MountFilePath file)
        {
            if (file.Mount.Exists(file.Path) && file.Mount.IsDir(file.Path))
            {
                throw new IOException(string.Format("Directory {0} already exists", file.GetGlobalPath()));
            }
        }

        private void CheckPathsNotOverlapping(MountFilePath src, MountFilePath dst)
        {
            if (src.Mount == dst.Mount)
            {
                if (src.Path.IsParentOf(dst.Path) || dst.Path.IsParentOf(src.Path))
                {
                    throw new IOException(string.Format("Cannot move or copy path {0} inside itself", src.GetGlobalPath()));
                }
            }
        }

        private void RecordRead(MountFilePath file)
        {
            file.MountInfo.LastReadTime = DateTime.Now;
        }

        private void RecordWrite(MountFilePath file)
        {
            file.MountInfo.LastWriteTime = DateTime.Now;
        }

        private MountFilePath Resolve(FilePath path)
        {
            path = path.UnRoot();
            MountInfo longestMatch = null;
            int longestMatchLength = -1;
            for (int i = 0; i < m_mounts.Count; ++i)
            {
                MountInfo mount = m_mounts[i];
                if (mount.Location.Path.Length >= longestMatchLength &&
                    mount.Location.IsParentOf(path))
                {
                    longestMatch = mount;
                    longestMatchLength = mount.Location.Path.Length;
                }
            }
            if (longestMatch != null)
            {
                return new MountFilePath(
                    longestMatch,
                    FilePath.Combine(longestMatch.SubPath, path.ToLocal(longestMatch.Location))
                );
            }
            else
            {
                return new MountFilePath(
                    m_emptyRootMount,
                    path
                );
            }
        }
    }
}
