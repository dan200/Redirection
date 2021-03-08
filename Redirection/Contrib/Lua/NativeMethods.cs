using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Lua
{
	public static unsafe class NativeMethods
	{
		const string LIBNAME = "lua53";

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_gc (IntPtr L, int what, int data);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr lua_typename (IntPtr L, int type);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_error(IntPtr L);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr luaL_newstate ();

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_close (IntPtr L);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void luaL_openlibs (IntPtr L);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int luaL_loadstring (IntPtr L, byte* chunk);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int luaL_loadbufferx(IntPtr L, byte* chunk, IntPtr size, byte* name, byte* mode);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_createtable (IntPtr L, int narr, int nrec);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_gettable (IntPtr L, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_rawget(IntPtr L, int index);

#if LUA_32BITS
		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_rawgeti (IntPtr L, int index, int integer);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_rawseti (IntPtr L, int index, int integer);
#else
        [DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_rawgeti (IntPtr L, int index, long integer);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_rawseti(IntPtr L, int index, long integer);
#endif

        [DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_settop (IntPtr L, int newTop);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_rotate (IntPtr L, int index, int n);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_copy (IntPtr L, int fromindex, int toindex);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_settable (IntPtr L, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_rawset(IntPtr L, int index);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_setmetatable (IntPtr L, int objIndex);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_getmetatable (IntPtr L, int objIndex);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_pushvalue (IntPtr L, int index);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_gettop (IntPtr L);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_type (IntPtr L, int index);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr lua_newuserdata (IntPtr L, IntPtr size);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr lua_touserdata (IntPtr L, int index);

        [DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr lua_tothread (IntPtr L, int index);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_isstring (IntPtr L, int index);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_isinteger (IntPtr L, int index);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_iscfunction (IntPtr L, int index);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_pushnil (IntPtr L);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_pcallk (IntPtr L, int nArgs, int nResults, int msgh, IntPtr ctx, IntPtr kFunction);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_callk (IntPtr L, int nArgs, int nResults, IntPtr ctx, IntPtr kFunction);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int lua_yieldk(IntPtr L, int nResults, IntPtr ctx, IntPtr kFunction);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_isyieldable(IntPtr L);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr lua_tocfunction (IntPtr L, int index);

#if LUA_32BITS
		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern float lua_tonumberx (IntPtr L, int index, IntPtr isnum);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_tointegerx (IntPtr L, int index, IntPtr isnum);
#else
        [DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern double lua_tonumberx (IntPtr L, int index, IntPtr isnum);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern long lua_tointegerx (IntPtr L, int index, IntPtr isnum);
		#endif

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_toboolean (IntPtr L, int index);

#if LUA_32BITS
		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_pushnumber (IntPtr L, float number);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_pushinteger (IntPtr L, int integer);
#else
        [DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_pushnumber (IntPtr L, double number);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_pushinteger (IntPtr L, long integer);
		#endif

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_pushboolean (IntPtr L, int value);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr lua_tolstring (IntPtr L, int index, out IntPtr strLen);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr luaL_tolstring(IntPtr L, int index, out IntPtr strLen);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr lua_pushlstring (IntPtr L, byte* str, IntPtr size);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int luaL_newmetatable (IntPtr L, byte* meta);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_getfield (IntPtr L, int stackPos, byte* meta);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int luaL_getmetafield (IntPtr L, int stackPos, byte* field);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_checkstack (IntPtr L, int extra);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_next (IntPtr L, int index);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_pushlightuserdata (IntPtr L, IntPtr udata);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_getglobal (IntPtr L, byte* name);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_setglobal (IntPtr L, byte* name);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void lua_setfield(IntPtr L, int index, byte* name);

        [DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr lua_newstate (IntPtr alloc, IntPtr ud);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr lua_pushcclosure (IntPtr L, IntPtr function, int upvalues);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_sethook (IntPtr L, IntPtr func, int mask, int count);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_getstack (IntPtr L, int level, ref Lua.lua_Debug ar);

		[DllImport (LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_getinfo (IntPtr L, byte* what, ref Lua.lua_Debug ar);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void luaL_where(IntPtr L, int level);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_concat(IntPtr L, int n);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_absindex(IntPtr L, int index);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr lua_newthread(IntPtr L);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_status(IntPtr L);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int lua_resume(IntPtr L, IntPtr from, int nargs);

		[DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void lua_xmove(IntPtr from, IntPtr to, int n);

        [DllImport(LIBNAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr lua_atpanic(IntPtr L, IntPtr panicf);
    }
}
