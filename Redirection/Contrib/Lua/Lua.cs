using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Lua
{
	public static unsafe class Lua
	{
        // Constants
        public const int LUA_VERSION_MAJOR = 5;
        public const int LUA_VERSION_MINOR = 3;

#if LUA_32BITS
		public const int LUA_32BITS = 1;
        public const int LUA_MININTEGER = int.MinValue;
        public const int LUA_MAXINTEGER = int.MaxValue;
#else
        public const int LUA_32BITS = 0;
        public const long LUA_MININTEGER = long.MinValue;
        public const long LUA_MAXINTEGER = long.MaxValue;
#endif

        public const int LUAI_MAXSTACK = 1000000;
        public const int LUA_REGISTRYINDEX = (-LUAI_MAXSTACK - 1000);
        public const int LUA_RIDX_MAINTHREAD = 1;
        public const int LUA_RIDX_GLOBALS = 2;

        public const int LUA_OK = 0;
        public const int LUA_YIELD = 1;
        public const int LUA_ERRRUN = 2;
        public const int LUA_ERRSYNTAX = 3;
        public const int LUA_ERRMEM = 4;
        public const int LUA_ERRGCMM = 5;
        public const int LUA_ERRERR = 6;

        public const int LUA_HOOKCALL = 0;
        public const int LUA_HOOKRET = 1;
        public const int LUA_HOOKLINE = 2;
        public const int LUA_HOOKCOUNT = 3;
        public const int LUA_HOOKTAILCALL = 4;

        public const int LUA_MASKCALL = (1 << LUA_HOOKCALL);
        public const int LUA_MASKRET = (1 << LUA_HOOKRET);
        public const int LUA_MASKLINE = (1 << LUA_HOOKLINE);
        public const int LUA_MASKCOUNT = (1 << LUA_HOOKCOUNT);

        public const int LUA_MULTRET = -1;

        public const int LUA_TNONE = -1;
        public const int LUA_TNIL = 0;
        public const int LUA_TBOOLEAN = 1;
        public const int LUA_TLIGHTUSERDATA = 2;
        public const int LUA_TNUMBER = 3;
        public const int LUA_TSTRING = 4;
        public const int LUA_TTABLE = 5;
        public const int LUA_TFUNCTION = 6;
        public const int LUA_TUSERDATA = 7;
        public const int LUA_TTHREAD = 8;

        public const int LUA_GCSTOP = 0;
        public const int LUA_GCRESTART = 1;
        public const int LUA_GCCOLLECT = 2;
        public const int LUA_GCCOUNT = 3;
        public const int LUA_GCCOUNTB = 4;
        public const int LUA_GCSTEP = 5;
        public const int LUA_GCSETPAUSE = 6;
        public const int LUA_GCSETSTEPMUL = 7;
        public const int LUA_GCISRUNNING = 9;

        public const int LUA_IDSIZE = 60;
        
        // Types
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int lua_CFunction(IntPtr L);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int lua_KFunction(IntPtr L, int status, IntPtr ctx);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr lua_Alloc(IntPtr ud, IntPtr ptr, IntPtr osize, IntPtr nsize);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void lua_Hook(IntPtr state, ref lua_Debug ar);

        [StructLayout(LayoutKind.Sequential)]
        public struct lua_Debug
        {
            // Public part
            public readonly int eventCode;
            private readonly IntPtr pname;
            private readonly IntPtr pnamewhat;
            private readonly IntPtr pwhat;
            private readonly IntPtr psource;
            public readonly int currentline;
            public readonly int linedefined;
            public readonly int lastlinedefined;
            public readonly byte nups;
            public readonly byte nparams;
            public readonly sbyte isvararg;
            public readonly sbyte istailcall;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = LUA_IDSIZE)]
            private readonly byte[] pshortsrc;

            // Private part
            private readonly IntPtr i_ci;

            public string name
            {
                get
                {
                    return new CharPtr(pname).Decode();
                }
            }

            public string namewhat
            {
                get
                {
                    return new CharPtr(pnamewhat).Decode();
                }
            }

            public string source
            {
                get
                {
                    return new CharPtr(psource).Decode();
                }
            }

            public string short_src
            {
                get
                {
                    int zeroPos = -1;
                    for (int i = 0; i < pshortsrc.Length; ++i)
                    {
                        if (pshortsrc[i] == 0)
                        {
                            zeroPos = i;
							break;
                        }
                    }
                    if (zeroPos >= 0)
                    {
                        return Encoding.UTF8.GetString(pshortsrc, 0, zeroPos);
                    }
                    else
                    {
                        return Encoding.UTF8.GetString(pshortsrc);
                    }
                }
            }
        }

        // Functions
		public static int lua_gc (IntPtr L, int what, int data)
		{
			return NativeMethods.lua_gc (L, what, data);
		}

		public static CharPtr lua_typename (IntPtr L, int type)
		{
			return new CharPtr (
				NativeMethods.lua_typename (L, type)
			);
		}

		public static int luaL_error (IntPtr L, string message, int level=1)
		{
			int size;
			fixed (byte* pBytes = Encode(message, out size))
			{
				if (level != 0)
				{
					NativeMethods.luaL_where(L, level); // 1
					NativeMethods.lua_pushlstring(L, pBytes, new IntPtr(size)); // 2
					NativeMethods.lua_concat(L, 2); // 1
				}
				else
				{
					NativeMethods.lua_pushlstring(L, pBytes, new IntPtr(size)); // 1
				}
				return NativeMethods.lua_error(L); // 0
			}
		}
			
		public static int lua_error (IntPtr L)
		{
			return NativeMethods.lua_error (L);
		}

		public static IntPtr luaL_newstate ()
		{
			return NativeMethods.luaL_newstate ();
		}
			
		public static void lua_close (IntPtr L)
		{
			NativeMethods.lua_close (L);
		}

		public static void luaL_openlibs (IntPtr L)
		{
			NativeMethods.luaL_openlibs (L);
		}

		public static int luaL_loadstring (IntPtr L, string chunk)
		{
            fixed( byte* pBytes = Encode(chunk) )
            {
                return NativeMethods.luaL_loadstring(L, pBytes);
            }
		}

		public static int luaL_loadbufferx(IntPtr L, string chunk, string name, string mode)
		{
			int size;
			int nameOffset;
			int modeOffset;
			fixed ( byte* pBytes = Encode(chunk, out size),
		       pNameBytes = EncodeExtra(name, out nameOffset),
		       pModeBytes = EncodeExtra(mode, out modeOffset)
			)
			{
				return NativeMethods.luaL_loadbufferx(L, pBytes, new IntPtr(size), pNameBytes + nameOffset, pModeBytes + modeOffset);
			}
		}

		public static void lua_createtable (IntPtr L, int narr, int nrec)
		{
			NativeMethods.lua_createtable (L, narr, nrec);
		}

		public static int lua_gettable (IntPtr L, int index)
		{
			return NativeMethods.lua_gettable (L, index);
		}

		public static int lua_rawget(IntPtr L, int index)
		{
			return NativeMethods.lua_rawget(L, index);
		}

#if LUA_32BITS
        public static int lua_rawgeti (IntPtr L, int index, int integer)
        {
            return NativeMethods.lua_rawgeti (L, index, integer);
        }

        public static void lua_rawseti (IntPtr L, int index, int integer)
        {
            NativeMethods.lua_rawseti (L, index, integer);
        }
#else
        public static int lua_rawgeti (IntPtr L, int index, long integer)
		{
			return NativeMethods.lua_rawgeti (L, index, integer);
		}

        public static void lua_rawseti(IntPtr L, int index, long integer)
        {
            NativeMethods.lua_rawseti(L, index, integer);
        }
#endif

        public static void lua_settop (IntPtr L, int newTop)
		{
			NativeMethods.lua_settop (L, newTop);
		}

		public static void lua_pop (IntPtr L, int n)
		{
			NativeMethods.lua_settop (L, -(n)-1);
		}			

		public static void lua_rotate (IntPtr L, int index, int n)
		{
			NativeMethods.lua_rotate (L, index, n);
		}

		public static void lua_copy (IntPtr L, int fromidx, int toidx)
		{
			NativeMethods.lua_copy( L, fromidx, toidx );
		}

		public static void lua_insert (IntPtr L, int index)
		{
			NativeMethods.lua_rotate (L, index, 1);
		}

		public static void lua_remove (IntPtr L, int index)
		{
			NativeMethods.lua_rotate (L, index, -1);
			NativeMethods.lua_settop(L, -2);
		}
			
		public static void lua_settable (IntPtr L, int index)
		{
			NativeMethods.lua_settable (L, index);
		}

		public static void lua_rawset(IntPtr L, int index)
		{
			NativeMethods.lua_rawset(L, index);
		}

		public static void lua_setmetatable (IntPtr L, int objIndex)
		{
			NativeMethods.lua_setmetatable (L, objIndex);
		}

		public static int lua_getmetatable (IntPtr L, int objIndex)
		{
			return NativeMethods.lua_getmetatable (L, objIndex);
		}

		public static void lua_pushvalue (IntPtr L, int index)
		{
			NativeMethods.lua_pushvalue (L, index);
		}

		public static void lua_replace (IntPtr L, int index)
		{
			NativeMethods.lua_copy (L, -1, index);
			NativeMethods.lua_settop(L, -2);
		}

		public static int lua_gettop (IntPtr L)
		{
			return NativeMethods.lua_gettop (L);
		}

		public static int lua_type (IntPtr L, int index)
		{
			return NativeMethods.lua_type (L, index);
		}

		public static IntPtr lua_newuserdata (IntPtr L, IntPtr size)
		{
			return NativeMethods.lua_newuserdata (L, size);
		}
		
		public static IntPtr lua_touserdata (IntPtr L, int index)
		{
			return NativeMethods.lua_touserdata (L, index);
		}

        public static IntPtr lua_tothread (IntPtr L, int index)
        {
            return NativeMethods.lua_tothread (L, index);
        }

		public static bool lua_isinteger (IntPtr L, int index)
		{
			return NativeMethods.lua_isinteger (L, index) != 0;
		}

		public static void lua_pushnil (IntPtr L)
		{
			NativeMethods.lua_pushnil (L);
		}

		public static lua_CFunction lua_tocfunction (IntPtr L, int index)
		{
			IntPtr ptr = NativeMethods.lua_tocfunction (L, index);
            if (ptr != IntPtr.Zero)
            {
                return Marshal.GetDelegateForFunctionPointer(ptr, typeof(lua_CFunction)) as lua_CFunction;
            }
			return null;
		}

#if LUA_32BITS
		public static double lua_tonumber (IntPtr L, int index)
		{
			return NativeMethods.lua_tonumberx (L, index, IntPtr.Zero);
		}

		public static int lua_tointeger (IntPtr L, int index)
		{
			return NativeMethods.lua_tointegerx (L, index, IntPtr.Zero);
		}
#else
        public static double lua_tonumber (IntPtr L, int index)
		{
			return NativeMethods.lua_tonumberx (L, index, IntPtr.Zero);
		}

		public static long lua_tointeger (IntPtr L, int index)
		{
			return NativeMethods.lua_tointegerx (L, index, IntPtr.Zero);
		}
#endif

		public static bool lua_toboolean (IntPtr L, int index)
		{ 
			return NativeMethods.lua_toboolean (L, index) != 0;
		}

		public static CharPtr lua_tostring (IntPtr L, int index)
		{
			IntPtr len;
			var ptr = NativeMethods.lua_tolstring (L, index, out len);
			return new CharPtr (ptr, (int)len);
		}

		public static CharPtr luaL_tostring(IntPtr L, int index)
		{
			IntPtr len;
			var ptr = NativeMethods.luaL_tolstring(L, index, out len);
			return new CharPtr(ptr, (int)len);
		}

#if LUA_32BITS
		public static void lua_pushnumber (IntPtr L, float number)
		{
			NativeMethods.lua_pushnumber (L, number);
		}

		public static void lua_pushinteger (IntPtr L, int integer)
		{
			NativeMethods.lua_pushinteger (L, integer);
		}
#else
        public static void lua_pushnumber (IntPtr L, double number)
		{
			NativeMethods.lua_pushnumber (L, number);
		}

		public static void lua_pushinteger (IntPtr L, long integer)
		{
			NativeMethods.lua_pushinteger (L, integer);
		}
#endif

		public static void lua_pushboolean (IntPtr L, bool value)
		{
			NativeMethods.lua_pushboolean (L, value ? 1 : 0);
		}

		public static CharPtr lua_pushlstring (IntPtr L, string str)
		{
			int size;
            fixed( byte* pBytes = Encode(str, out size))
            {
                return new CharPtr(
                    NativeMethods.lua_pushlstring(L, pBytes, new IntPtr(size)),
                    size
                );
            }
		}

		public static CharPtr lua_pushlstring (IntPtr L, byte[] bytes)
		{
            fixed (byte* pBytes = bytes)
            {
                return new CharPtr(
                    NativeMethods.lua_pushlstring(L, pBytes, new IntPtr(bytes.Length)),
                    bytes.Length
                );
            }
		}
		
		public static int lua_getfield (IntPtr L, int stackPos, string meta)
		{
            fixed (byte* pBytes = Encode(meta))
            {
                return NativeMethods.lua_getfield(L, stackPos, pBytes);
            }
		}

		public static int luaL_getmetafield (IntPtr L, int stackPos, string field)
		{
            fixed (byte* pBytes = Encode(field))
            {
                return NativeMethods.luaL_getmetafield(L, stackPos, pBytes);
            }
		}

		public static int lua_checkstack (IntPtr L, int extra)
		{
			return NativeMethods.lua_checkstack (L, extra);
		}

		public static int lua_next (IntPtr L, int index)
		{
			return NativeMethods.lua_next (L, index);
		}

		public static void lua_pushlightuserdata (IntPtr L, IntPtr udata)
		{
			NativeMethods.lua_pushlightuserdata (L, udata);
		}

		public static int lua_pcall (IntPtr L, int nArgs, int nResults, int msgh)
		{
			return NativeMethods.lua_pcallk (L, nArgs, nResults, msgh, IntPtr.Zero, IntPtr.Zero);
		}

		public static void lua_call (IntPtr L, int nArgs, int nResults)
		{
			NativeMethods.lua_callk (L, nArgs, nResults, IntPtr.Zero, IntPtr.Zero);
		}

        public static int lua_pcallk(IntPtr L, int nArgs, int nResults, int msgh, IntPtr ctx, lua_KFunction k)
        {
            IntPtr funcK = (k == null) ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(k);
            return NativeMethods.lua_pcallk(L, nArgs, nResults, msgh, ctx, funcK);
        }

        public static void lua_callk(IntPtr L, int nArgs, int nResults, IntPtr ctx, lua_KFunction k)
        {
            IntPtr funcK = (k == null) ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(k);
            NativeMethods.lua_callk(L, nArgs, nResults, ctx, funcK);
        }

        public static int lua_yieldk(IntPtr L, int nResults, IntPtr ctx, lua_KFunction k)
        {
            IntPtr funcK = (k == null) ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(k);
            return NativeMethods.lua_yieldk(L, nResults, ctx, funcK);
        }

		public static bool lua_isyieldable(IntPtr L)
		{
			return NativeMethods.lua_isyieldable(L) != 0;
		}

        public static void lua_setglobal (IntPtr L, string name)
		{
            fixed (byte* pBytes = Encode(name))
            {
                NativeMethods.lua_setglobal(L, pBytes);
            }
		}

        public static void lua_setfield(IntPtr L, int index, string name)
        {
            fixed (byte* pBytes = Encode(name))
            {
                NativeMethods.lua_setfield(L, index, pBytes);
            }
        }

        public static int lua_getglobal (IntPtr L, string name)
		{
            fixed (byte* pBytes = Encode(name))
            {
                return NativeMethods.lua_getglobal(L, pBytes);
            }
		}

		public static void lua_pushglobaltable(IntPtr L)
		{
			NativeMethods.lua_rawgeti(L, LUA_REGISTRYINDEX, LUA_RIDX_GLOBALS);
		}

		public static IntPtr lua_newstate ( lua_Alloc f, IntPtr ud )
		{
			IntPtr funcAlloc = f == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate (f);
			return NativeMethods.lua_newstate( funcAlloc, ud );
		}

		public static void lua_pushcfunction (IntPtr L, lua_CFunction f)
		{
			lua_pushcclosure (L, f, 0);
		}

		public static void lua_pushcclosure (IntPtr L, lua_CFunction f, int count)
		{
			IntPtr pfunc = (f == null) ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate (f);
			NativeMethods.lua_pushcclosure (L, pfunc, count);
		}

		public static int lua_upvalueindex(int i)
		{
			return LUA_REGISTRYINDEX - i;
		}

		public static void lua_sethook (IntPtr L, lua_Hook func, int mask, int count)
		{
			IntPtr funcHook = func == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate (func);
			NativeMethods.lua_sethook (L, funcHook, mask, count);
		}

		public static int lua_getstack (IntPtr L, int level, ref lua_Debug ar)
		{
			return NativeMethods.lua_getstack (L, level, ref ar);
		}

		public static int lua_getinfo (IntPtr L, string what, ref lua_Debug ar)
		{
            fixed (byte* pBytes = Encode(what))
            {
                return NativeMethods.lua_getinfo(L, pBytes, ref ar);
            }
		}

		public static void luaL_where(IntPtr L, int level)
		{
			NativeMethods.luaL_where(L, level);
		}

		public static void lua_concat(IntPtr L, int n)
		{
			NativeMethods.lua_concat(L, n);
		}

		public static int lua_absindex(IntPtr L, int index)
		{
			return NativeMethods.lua_absindex(L, index);
		}

		public static IntPtr lua_newthread(IntPtr L)
		{
			return NativeMethods.lua_newthread(L);
		}

		public static int lua_status(IntPtr L)
		{
			return NativeMethods.lua_status(L);
		}

		public static int lua_resume(IntPtr L, IntPtr from, int nargs)
		{
			return NativeMethods.lua_resume(L, from, nargs);
		}

		public static void lua_xmove(IntPtr from, IntPtr to, int n)
		{
			NativeMethods.lua_xmove(from, to, n);
		}

        public static lua_CFunction lua_atpanic(IntPtr L, lua_CFunction panicf)
        {
            IntPtr pfunc = (panicf == null) ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(panicf);
            IntPtr poldfunc = NativeMethods.lua_atpanic(L, pfunc);
            if (poldfunc != IntPtr.Zero)
            {
                return Marshal.GetDelegateForFunctionPointer(poldfunc, typeof(lua_CFunction)) as lua_CFunction;
            }
            return null;
        }

        // Utility
        [ThreadStatic]
        private static byte[] s_smallByteBuffer = new byte[1024];

        [ThreadStatic]
        private static int s_smallByteBufferBytesUsed = 0;

        private static byte[] Encode(string str)
        {
            int unused;
            return Encode(str, out unused);
        }

        private static byte[] Encode(string str, out int o_size)
        {
            int unused;
            s_smallByteBufferBytesUsed = 0;
            return EncodeExtra(str, out unused, out o_size);
        }

        private static byte[] EncodeExtra(string str, out int o_start)
        {
            int unused;
            return EncodeExtra(str, out o_start, out unused);
        }

        private static byte[] EncodeExtra(string str, out int o_start, out int o_size)
        {
            byte[] buffer;
            int start;
            var lengthNeeded = Encoding.UTF8.GetMaxByteCount(str.Length) + 1;
            if (s_smallByteBuffer == null || lengthNeeded > (s_smallByteBuffer.Length - s_smallByteBufferBytesUsed))
            {
                buffer = new byte[lengthNeeded];
                start = 0;
            }
            else
            {
                buffer = s_smallByteBuffer;
                start = s_smallByteBufferBytesUsed;
            }

            int bytesWritten = Encoding.UTF8.GetBytes(str, 0, str.Length, buffer, start);
            buffer[start + bytesWritten] = 0;
            if (buffer == s_smallByteBuffer)
            {
                s_smallByteBufferBytesUsed += bytesWritten + 1;
            }

            o_start = start;
            o_size = bytesWritten;
            return buffer;
        }
    }
}

