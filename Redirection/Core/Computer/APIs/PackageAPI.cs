using Dan200.Core.Lua;
using System.Text;

namespace Dan200.Core.Computer.APIs
{
    public class PackageAPI : LuaAPI
    {
        private FileSystem m_fileSystem;

        [LuaField]
        public LuaValue config = "/\n;\n?\n!\n-";

        [LuaField]
        public LuaValue cpath = "";

        [LuaField]
        public LuaValue loaded = new LuaTable();

        [LuaField]
        public LuaValue path =
            "?.lua;" +
            "?/init.lua";

        [LuaField]
        public LuaValue preload = new LuaTable();

        [LuaField]
        public LuaValue searchers = new LuaTable(); // This is populated in Init

        public PackageAPI(Computer computer, FileSystem fileSystem) : base("package")
        {
            m_fileSystem = fileSystem;
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

				-- Package API 
				local package_preload = package.preload
				local package_searchpath = package.searchpath
				local package_loaded = package.loaded
				local package_searchers = package.searchers
				local pcall = pcall
				local loadfile = loadfile

				table.insert(
					package.searchers,
					function( sName )
						-- Load from preload
						expect( sName, ""string"", 1 )
						if package_preload[ sName ] ~= nil then
							return package_preload[ sName ]
						else
							return ""no field package.preload['"" .. sName .. ""']""
						end
					end
				)

				table.insert(
					package.searchers,
					function( sName )
						-- Load from path
						expect( sName, ""string"", 1 )
						if type( package.path ) ~= ""string"" then
							error( ""package.path must be a string"", 0 )
						end
						local sPath, sError = package_searchpath( sName, package.path )
						if sPath ~= nil then
							local fnFile, sError = loadfile( sPath )
							if fnFile then
								return fnFile, sPath
							else
								return sError
							end
						else
							return sError
						end
					end
				)
				
				local require_in_progress = {}
				function require( sName )
					expect( sName, ""string"", 1 )
					if package_loaded[ sName ] ~= nil then
						return package_loaded[ sName ]
					end
					if require_in_progress[ sName ] then
						error( ""loop detected requiring '"" .. sName .. ""'"", 2 )
					end
					require_in_progress[ sName ] = true
					local ok, result = pcall( function() 
						local sFullError = ""module '"" .. sName .. ""' not found:""
						for n=1,#package_searchers do
							local fnSearcher = package_searchers[n]
							local fnLoader, sExtra = fnSearcher( sName )
							if type(fnLoader) == ""function"" then
								local result = fnLoader( sName, sExtra )
								if result ~= nil then
									package_loaded[ sName ] = result
									return result
								else
									package_loaded[ sName ] = true
									return true
								end
							elseif type(fnLoader) == ""string"" then
								sFullError = sFullError .. ""\n"" .. fnLoader
							end
						end
						error( sFullError, 0 )
					end )
					require_in_progress[ sName ] = false
					if ok then
						return result
					else
						error( result, 0 )
					end
				end",
                "=PackageAPI.Init"
            );
        }

        [LuaMethod]
        public LuaArgs loadlib(LuaArgs args)
        {
            args.GetString(0); // libname
            args.GetString(1); // funcname
                               // We don't support loading C libraries
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs searchpath(LuaArgs args)
        {
            var name = args.GetString(0);
            var path = args.GetString(1);
            var sep = args.IsNil(2) ? "." : args.GetString(2);
            var rep = args.IsNil(3) ? "/" : args.GetString(3);
            var fixedName = name.Replace(sep, rep);
            var fixedPath = path.Replace("?", fixedName);
            var candidates = fixedPath.Split(';');
            var errorBuilder = new StringBuilder();
            foreach (var candidate in candidates)
            {
                var candPath = new FilePath(candidate);
                if (m_fileSystem.Exists(candPath) && !m_fileSystem.IsDir(candPath))
                {
                    return new LuaArgs(candidate);
                }
                else
                {
                    if (errorBuilder.Length > 0)
                    {
                        errorBuilder.Append('\n');
                    }
                    errorBuilder.Append("no file '" + candidate + "'");
                }
            }
            return new LuaArgs(LuaValue.Nil, errorBuilder.ToString());
        }
    }
}
