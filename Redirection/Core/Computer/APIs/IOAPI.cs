using Dan200.Core.Lua;
using System.IO;

namespace Dan200.Core.Computer.APIs
{
    public class IOAPI : LuaAPI
    {
        private FileSystem m_fileSystem;
        private LuaObjectRef<LuaFile> m_input;
        private LuaObjectRef<LuaFile> m_output;

        [LuaField]
        public LuaValue stderr;

        [LuaField]
        public LuaValue stdin;

        [LuaField]
        public LuaValue stdout;

        public IOAPI(Computer computer, FileSystem fileSystem) : base("io")
        {
            m_fileSystem = fileSystem;

            stdin = new LuaFile(TextReader.Null, true);
            stdout = new LuaFile(computer.Output, true);
            stderr = new LuaFile(computer.ErrorOutput, true);

            m_input = new LuaObjectRef<LuaFile>(stdin.GetObject<LuaFile>());
            m_output = new LuaObjectRef<LuaFile>(stdout.GetObject<LuaFile>());
        }

        public override void Init(LuaMachine machine)
        {
            base.Init(machine);
            machine.DoString(@"
				-- Locals
				local type = type
				local error = error
				local function expect( value, sExpectedType, index )
				    local sFoundType = type( value )
				    if sExpectedType and sFoundType ~= sExpectedType then
			            error( ""Expected "" .. sExpectedType .. "" at argument #"" .. index .. "", got "" .. sFoundType, 3 )
				    end
				    return value
				end

				-- IO API
				local io_open = io.open
				local io_input = io.input
				local io_output = io.output
				local tostring = tostring
				local load = load

				function loadfile( sPath, sMode, _env )
					if sPath ~= nil then
						expect( sPath, ""string"", 1 )
					end
					local file, sChunkName
					if sPath then
						file = io_open( sPath )
						sChunkName = ""@"" .. sPath
					else
						file = io_input()
						sChunkName = ""=stdin""
					end
					if file then
						local sFile = file:read( ""a"" )
						file:close()
				        if _env ~= nil then
				            return load( sFile, sChunkName, sMode, _env )
				        else
				            return load( sFile, sChunkName, sMode )
				        end
					end
					return nil, ""File not found: "" .. sPath
				end

				local loadfile = loadfile
				function dofile( sPath )
					if sPath ~= nil then
						expect( sPath, ""string"", 1 )
					end
					local fnFile, sError = loadfile( sPath )
					if fnFile then
						return fnFile()
					else
						error( sError, 2 )
					end
				end

				local select = select
				function print( ... )
					local nArgs = select( ""#"", ... )
					local output = io_output()
					for n=1,nArgs do
						local arg = select( n, ... )
						output:write( tostring(arg) )
						if n<nArgs then
							output:write( ""\t"" )
						end
					end
					output:write( ""\n"" )
				end",
                "=IOAPI.Init"
            );
        }

        [LuaMethod]
        public LuaArgs close(LuaArgs args)
        {
            if (args.IsNil(0))
            {
                return m_output.Value.close(LuaArgs.Empty);
            }
            else
            {
                var file = args.GetObject<LuaFile>(0);
                return file.close(LuaArgs.Empty);
            }
        }

        [LuaMethod]
        public LuaArgs flush(LuaArgs args)
        {
            return m_output.Value.flush(LuaArgs.Empty);
        }

        [LuaMethod]
        public LuaArgs input(LuaArgs args)
        {
            if (args.IsNil(0))
            {
                return new LuaArgs(m_input.Value);
            }
            else if (args.IsObject<LuaFile>(0))
            {
                m_input.Value = args.GetObject<LuaFile>(0);
                return LuaArgs.Empty;
            }
            else
            {
                var path = new FilePath(args.GetString(0));
                try
                {
                    m_input.Value = new LuaFile(m_fileSystem.OpenForRead(path));
                    return LuaArgs.Empty;
                }
                catch (IOException e)
                {
                    throw new LuaError(e.Message);
                }
            }
        }

        [LuaMethod]
        public LuaArgs lines(LuaArgs args)
        {
            if (args.IsNil(0))
            {
                return m_input.Value.lines(LuaArgs.Empty);
            }
            else
            {
                var sPath = args.GetString(0);
                var results = open(new LuaArgs(sPath, "r"));
                if (results.IsObject<LuaFile>(0))
                {
                    return results.GetObject<LuaFile>(0).Lines(args.Select(1), true);
                }
                else
                {
                    throw new LuaError("File not found: " + sPath);
                }
            }
        }

        [LuaMethod]
        public LuaArgs open(LuaArgs args)
        {
            if (args.IsFunction(0))
            {
                // Open a function
                var function = args.GetFunction(0);
                var mode = args.IsNil(1) ? "r" : args.GetString(1);
                if (mode == "r" || mode == "rb")
                {
                    var stream = new LuaFunctionStream(function, LuaFileOpenMode.Read);
                    return new LuaArgs(new LuaFile(stream, LuaFileOpenMode.Read));
                }
                else if (mode == "w" || mode == "a" || mode == "wb" || mode == "ab")
                {
                    var stream = new LuaFunctionStream(function, LuaFileOpenMode.Write);
                    return new LuaArgs(new LuaFile(stream, LuaFileOpenMode.Write));
                }
                else
                {
                    throw new LuaError("Unsupported mode: " + mode);
                }
            }
            else
            {
                // Open a file
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
        }

        [LuaMethod]
        public LuaArgs output(LuaArgs args)
        {
            if (args.IsNil(0))
            {
                return new LuaArgs(m_output.Value);
            }
            else if (args.IsObject<LuaFile>(0))
            {
                m_output.Value = args.GetObject<LuaFile>(0);
                return LuaArgs.Empty;
            }
            else
            {
                var path = new FilePath(args.GetString(0));
                try
                {
                    m_output.Value = new LuaFile(m_fileSystem.OpenForWrite(path, false));
                    return LuaArgs.Empty;
                }
                catch (IOException e)
                {
                    throw new LuaError(e.Message);
                }
            }
        }

        [LuaMethod]
        public LuaArgs popen(LuaArgs args)
        {
            args.GetString(0);
            if (!args.IsNil(1))
            {
                args.GetString(1);
            }
            return LuaArgs.Nil;
        }

        [LuaMethod]
        public LuaArgs read(LuaArgs args)
        {
            return m_input.Value.read(args);
        }

        [LuaMethod]
        public LuaArgs tmpfile(LuaArgs args)
        {
            try
            {
                FilePath path;
                int i = 0;
                while (m_fileSystem.Exists(path = new FilePath("tmp/" + i + ".tmp")))
                {
                    ++i;
                }
                m_fileSystem.MakeDir(path.GetDir());
                var file = m_fileSystem.OpenForWrite(path, false);
                return new LuaArgs(new LuaFile(file));
            }
            catch (IOException e)
            {
                throw new LuaError(e.Message);
            }
        }

        [LuaMethod]
        public LuaArgs type(LuaArgs args)
        {
            if (args.IsObject<LuaFile>(0))
            {
                var file = args.GetObject<LuaFile>(0);
                if (file.IsOpen)
                {
                    return new LuaArgs("file");
                }
                else
                {
                    return new LuaArgs("closed file");
                }
            }
            return LuaArgs.Nil;
        }

        [LuaMethod]
        public LuaArgs write(LuaArgs args)
        {
            return m_output.Value.write(args);
        }
    }
}

