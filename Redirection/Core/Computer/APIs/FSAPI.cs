using Dan200.Core.Computer.Devices.DiskDrive;
using Dan200.Core.Lua;
using System.IO;

namespace Dan200.Core.Computer.APIs
{
    public class FSAPI : LuaAPI
    {
        private FileSystem m_fileSystem;

        public FSAPI(Computer computer, FileSystem fileSystem) : base("fs")
        {
            m_fileSystem = fileSystem;
        }

        [LuaMethod]
        public LuaArgs list(LuaArgs args)
        {
            var path = new FilePath(args.GetString(0));
            try
            {
                var results = m_fileSystem.List(path);
                var table = new LuaTable();
                for (int i = 0; i < results.Length; ++i)
                {
                    table[i + 1] = results[i];
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
            var path = new FilePath(args.GetString(0));
            try
            {
                return new LuaArgs(m_fileSystem.Exists(path));
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs getName(LuaArgs args)
        {
            var path = new FilePath(args.GetString(0));
            return new LuaArgs(path.GetName());
        }

        [LuaMethod]
        public LuaArgs getDir(LuaArgs args)
        {
            var path = new FilePath(args.GetString(0));
            return new LuaArgs(path.GetDir().ToString());
        }

        [LuaMethod]
        public LuaArgs getExtension(LuaArgs args)
        {
            var path = new FilePath(args.GetString(0));
            return new LuaArgs(path.GetExtension());
        }

        [LuaMethod]
        public LuaArgs getNameWithoutExtension(LuaArgs args)
        {
            var path = new FilePath(args.GetString(0));
            return new LuaArgs(path.GetNameWithoutExtension());
        }

        [LuaMethod]
        public LuaArgs match(LuaArgs args)
        {
            var path = new FilePath(args.GetString(0));
            var wildcard = new FilePath(args.GetString(1));
            return new LuaArgs(path.Matches(wildcard));
        }

        [LuaMethod]
        public LuaArgs getSize(LuaArgs args)
        {
            var path = new FilePath(args.GetString(0));
            try
            {
                var size = m_fileSystem.GetSize(path);
                return new LuaArgs(size);
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs getModifiedTime(LuaArgs args)
        {
            var path = new FilePath(args.GetString(0));
            try
            {
                var date = m_fileSystem.GetModifiedTime(path);
                var seconds = OSAPI.TimeFromDate(date);
                return new LuaArgs(seconds);
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs combine(LuaArgs args)
        {
            var a = new FilePath(args.GetString(0));
            var b = new FilePath(args.GetString(1));
            return new LuaArgs(FilePath.Combine(a, b).ToString());
        }

        [LuaMethod]
        public LuaArgs isDir(LuaArgs args)
        {
            var path = new FilePath(args.GetString(0));
            try
            {
                return new LuaArgs(m_fileSystem.IsDir(path));
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs isReadOnly(LuaArgs args)
        {
            var path = new FilePath(args.GetString(0));
            return new LuaArgs(m_fileSystem.IsReadOnly(path));
        }

        [LuaMethod]
        public LuaArgs makeDir(LuaArgs args)
        {
            var path = new FilePath(args.GetString(0));
            try
            {
                m_fileSystem.MakeDir(path);
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
            var path = new FilePath(args.GetString(0));
            var dest = new FilePath(args.GetString(1));
            try
            {
                m_fileSystem.Move(path, dest);
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
            var path = new FilePath(args.GetString(0));
            var dest = new FilePath(args.GetString(1));
            try
            {
                m_fileSystem.Copy(path, dest);
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
            var path = new FilePath(args.GetString(0));
            try
            {
                m_fileSystem.Delete(path);
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
            var path = new FilePath(args.GetString(0));
            var mode = args.IsNil(1) ? "r" : args.GetString(1);
            try
            {
                if (mode == "r")
                {
                    return new LuaArgs(new LuaFile(m_fileSystem.OpenForRead(path)));
                }
                else if (mode == "rb")
                {
                    return new LuaArgs(new LuaFile(m_fileSystem.OpenForBinaryRead(path), LuaFileOpenMode.Read));
                }
                else if (mode == "w")
                {
                    return new LuaArgs(new LuaFile(m_fileSystem.OpenForWrite(path, false)));
                }
                else if (mode == "wb")
                {
                    return new LuaArgs(new LuaFile(m_fileSystem.OpenForBinaryWrite(path, false), LuaFileOpenMode.Write));
                }
                else if (mode == "a")
                {
                    return new LuaArgs(new LuaFile(m_fileSystem.OpenForWrite(path, true)));
                }
                else if (mode == "ab")
                {
                    return new LuaArgs(new LuaFile(m_fileSystem.OpenForBinaryWrite(path, true), LuaFileOpenMode.Write));
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
            var wildcard = new FilePath(args.GetString(0));
            try
            {
                var results = m_fileSystem.Find(wildcard);
                var table = new LuaTable();
                for (int i = 0; i < results.Length; ++i)
                {
                    table[i + 1] = results[i].ToString();
                }
                return new LuaArgs(table);
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs getMountLocations(LuaArgs args)
        {
            var mounts = m_fileSystem.GetMountLocations();
            var table = new LuaTable(mounts.Length);
            for (int i = 0; i < mounts.Length; ++i)
            {
                table[i + 1] = mounts[i].ToString();
            }
            return new LuaArgs(table);
        }

        [LuaMethod]
        public LuaArgs getMount(LuaArgs args)
        {
            var path = new FilePath(args.GetString(0));
            try
            {
                FilePath subPath;
                bool readOnly;
                var mount = m_fileSystem.GetMount(path, out subPath, out readOnly);
                if (mount != null)
                {
                    return new LuaArgs(mount, subPath.ToString(), readOnly);
                }
                else
                {
                    return LuaArgs.Nil;
                }
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs mount(LuaArgs args)
        {
            try
            {
                var mount = args.GetObject<LuaMount>(0);
                var path = new FilePath(args.GetString(1));
                var subPath = args.IsNil(2) ? FilePath.Empty : new FilePath(args.GetString(2));
                var readOnly = args.IsNil(3) ? false : args.GetBool(3);
                m_fileSystem.Mount(mount, path, subPath, readOnly);
                return LuaArgs.Empty;
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs unmount(LuaArgs args)
        {
            try
            {
                if (args.IsString(0))
                {
                    var path = new FilePath(args.GetString(0));
                    m_fileSystem.Unmount(path);
                }
                else
                {
                    var mount = args.GetObject<LuaMount>(0);
                    m_fileSystem.Unmount(mount);
                }
                return LuaArgs.Empty;
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }
    }
}

