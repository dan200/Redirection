using Dan200.Core.Computer.APIs;
using Dan200.Core.Lua;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Dan200.Core.Computer.Devices.DiskDrive
{
    [LuaType("mount")]
    public class LuaMount : LuaObject
    {
        public readonly IMount Mount;
        public readonly IWritableMount WritableMount;
        public bool Connected;

        public LuaMount(IMount mount)
        {
            Mount = mount;
            if (Mount is IWritableMount && !((IWritableMount)Mount).ReadOnly)
            {
                WritableMount = (IWritableMount)Mount;
            }
        }

        public override void Dispose()
        {
        }

        [LuaMethod]
        public LuaArgs isConnected(LuaArgs args)
        {
            return new LuaArgs(Connected);
        }

        [LuaMethod]
        public LuaArgs getName(LuaArgs args)
        {
            return new LuaArgs(Mount.Name);
        }

        [LuaMethod]
        public LuaArgs isReadOnly(LuaArgs args)
        {
            return new LuaArgs(WritableMount == null);
        }

        [LuaMethod]
        public LuaArgs getCapacity(LuaArgs args)
        {
            CheckConnected();
            return new LuaArgs(Mount.Capacity);
        }

        [LuaMethod]
        public LuaArgs getFreeSpace(LuaArgs args)
        {
            CheckConnected();
            return new LuaArgs(
                Math.Max(Mount.Capacity - Mount.UsedSpace, 0)
            );
        }

        [LuaMethod]
        public LuaArgs list(LuaArgs args)
        {
            var path = Sanitize(args.GetString(0));
            try
            {
                CheckConnected();
                CheckDirExists(path);
                var results = Mount.List(path);
                var table = new LuaTable(results.Length);
                for (int i = 0; i < results.Length; ++i)
                {
                    table[i + 1] = new LuaValue(results[i]);
                }
                return new LuaArgs(table);
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs exists(LuaArgs args)
        {
            var path = Sanitize(args.GetString(0));
            try
            {
                CheckConnected();
                return new LuaArgs(Mount.Exists(path));
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }


        [LuaMethod]
        public LuaArgs getSize(LuaArgs args)
        {
            var path = Sanitize(args.GetString(0));
            try
            {
                CheckConnected();
                CheckFileExists(path);
                return new LuaArgs(Mount.GetSize(path));
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs getModifiedTime(LuaArgs args)
        {
            var path = Sanitize(args.GetString(0));
            try
            {
                CheckConnected();
                CheckFileExists(path);
                var date = Mount.GetModifiedTime(path);
                var seconds = OSAPI.TimeFromDate(date);
                return new LuaArgs(seconds);
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs isDir(LuaArgs args)
        {
            var path = Sanitize(args.GetString(0));
            try
            {
                CheckConnected();
                return new LuaArgs(Mount.Exists(path) && Mount.IsDir(path));
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs makeDir(LuaArgs args)
        {
            var path = Sanitize(args.GetString(0));
            try
            {
                CheckConnected();
                CheckWritable();
                CheckFileDoesNotExist(path);
                if (!Mount.Exists(path))
                {
                    WritableMount.MakeDir(path);
                }
                return LuaArgs.Empty;
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs move(LuaArgs args)
        {
            var path = Sanitize(args.GetString(0));
            var dest = Sanitize(args.GetString(1));
            try
            {
                CheckConnected();
                CheckWritable();
                CheckEitherExists(path);
                CheckEitherDoesNotExist(dest);
                CheckParentDirExists(dest);
                CheckPathsNotOverlapping(path, dest);
                WritableMount.Move(path, dest);
                return LuaArgs.Empty;
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs copy(LuaArgs args)
        {
            var path = Sanitize(args.GetString(0));
            var dest = Sanitize(args.GetString(1));
            try
            {
                CheckConnected();
                CheckWritable();
                CheckEitherExists(path);
                CheckEitherDoesNotExist(dest);
                CheckParentDirExists(dest);
                CheckPathsNotOverlapping(path, dest);
                WritableMount.Copy(path, dest);
                return LuaArgs.Empty;
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs delete(LuaArgs args)
        {
            var path = Sanitize(args.GetString(0));
            try
            {
                CheckConnected();
                CheckWritable();
                if (path.IsEmpty)
                {
                    throw new IOException("Cannot delete mount root");
                }
                if (Mount.Exists(path))
                {
                    WritableMount.Delete(path);
                }
                return LuaArgs.Empty;
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs open(LuaArgs args)
        {
            var path = Sanitize(args.GetString(0));
            var mode = args.IsNil(1) ? "r" : args.GetString(1);
            try
            {
                CheckConnected();
                if (mode == "r")
                {
                    CheckFileExists(path);
                    return new LuaArgs(new LuaFile(Mount.OpenForRead(path)));
                }
                else if (mode == "rb")
                {
                    CheckFileExists(path);
                    return new LuaArgs(new LuaFile(Mount.OpenForBinaryRead(path), LuaFileOpenMode.Read));
                }
                else if (mode == "w")
                {
                    CheckWritable();
                    CheckDirDoesNotExist(path);
                    CheckParentDirExists(path);
                    return new LuaArgs(new LuaFile(WritableMount.OpenForWrite(path, false)));
                }
                else if (mode == "wb")
                {
                    CheckWritable();
                    CheckDirDoesNotExist(path);
                    CheckParentDirExists(path);
                    return new LuaArgs(new LuaFile(WritableMount.OpenForBinaryWrite(path, false), LuaFileOpenMode.Write));
                }
                else if (mode == "a")
                {
                    CheckWritable();
                    CheckDirDoesNotExist(path);
                    CheckParentDirExists(path);
                    return new LuaArgs(new LuaFile(WritableMount.OpenForWrite(path, true)));
                }
                else if (mode == "ab")
                {
                    CheckWritable();
                    CheckDirDoesNotExist(path);
                    CheckParentDirExists(path);
                    return new LuaArgs(new LuaFile(WritableMount.OpenForBinaryWrite(path, true), LuaFileOpenMode.Write));
                }
                else
                {
                    throw new LuaError("Unsupported mode: " + mode);
                }
            }
            catch (FileNotFoundException)
            {
                return LuaArgs.Nil;
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs find(LuaArgs args)
        {
            var wildcard = Sanitize(args.GetString(0));
            try
            {
                CheckConnected();
                var dir = wildcard.GetDir();
                var namePattern = wildcard.GetName();
                var nameRegex = new Regex("^" + Regex.Escape(namePattern).Replace("\\*", ".*") + "$");
                var table = new LuaTable();
                int i = 0;
                if (Mount.Exists(dir) && Mount.IsDir(dir))
                {
                    foreach (var name in Mount.List(dir))
                    {
                        if (nameRegex.IsMatch(name))
                        {
                            table[++i] = FilePath.Combine(dir, name).ToString();
                        }
                    }
                }
                return new LuaArgs(table);
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        private void CheckConnected()
        {
            if (!Connected)
            {
                throw new LuaError(string.Format("Access Denied: Mount {0} is disconnected", Mount.Name));
            }
        }

        private void CheckWritable()
        {
            if (WritableMount == null)
            {
                throw new LuaError(string.Format("Access Denied: Mount {0} is read only", Mount.Name));
            }
        }

        private void CheckEitherExists(FilePath path)
        {
            if (!Mount.Exists(path))
            {
                throw new FileNotFoundException(string.Format("No such path: {0}", path));
            }
        }

        private void CheckEitherDoesNotExist(FilePath path)
        {
            if (Mount.Exists(path))
            {
                throw new IOException(string.Format("Path {0} already exists", path));
            }
        }

        private void CheckFileExists(FilePath path)
        {
            if (!Mount.Exists(path) || Mount.IsDir(path))
            {
                throw new FileNotFoundException(string.Format("No such file: {0}", path));
            }
        }

        private void CheckFileDoesNotExist(FilePath path)
        {
            if (Mount.Exists(path) && !Mount.IsDir(path))
            {
                throw new IOException(string.Format("File {0} already exists", path));
            }
        }

        private void CheckParentDirExists(FilePath path)
        {
            CheckDirExists(path.GetDir());
        }

        private void CheckDirExists(FilePath path)
        {
            if (!Mount.Exists(path) || !Mount.IsDir(path))
            {
                throw new FileNotFoundException(string.Format("No such directory: {0}", path));
            }
        }

        private void CheckDirDoesNotExist(FilePath path)
        {
            if (Mount.Exists(path) && Mount.IsDir(path))
            {
                throw new IOException(string.Format("Directory {0} already exists", path));
            }
        }

        private void CheckPathsNotOverlapping(FilePath src, FilePath dest)
        {
            if (src.IsParentOf(dest) || dest.IsParentOf(src))
            {
                throw new IOException(string.Format("Cannot move or copy path {0} inside itself", src));
            }
        }

        private FilePath Sanitize(string path)
        {
            var result = new FilePath(path);
            if (result.IsBackPath)
            {
                throw new IOException(string.Format("Invalid path: {0}", path));
            }
            return result.UnRoot();
        }
    }
}
