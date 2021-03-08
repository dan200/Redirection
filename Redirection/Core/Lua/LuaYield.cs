using System;

namespace Dan200.Core.Lua
{
    public delegate LuaArgs LuaContinuation(LuaArgs resumeArgs);

    public class LuaYield : Exception
    {
        public readonly LuaArgs Results;
        public readonly LuaContinuation Continuation;

        public LuaYield(LuaArgs results, LuaContinuation continuation) : base("Unhandled lua yield")
        {
            Results = results;
            Continuation = continuation;
        }
    }

    internal class LuaAsyncCall : Exception
    {
        public readonly LuaFunction Function;
        public readonly LuaArgs Arguments;
        public readonly LuaContinuation Continuation;

        public LuaAsyncCall(LuaFunction function, LuaArgs arguments, LuaContinuation continuation) : base("Unhandled lua async call")
        {
            Function = function;
            Arguments = arguments;
            Continuation = continuation;
        }
    }
}
