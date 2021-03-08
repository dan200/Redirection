using System;

namespace Dan200.Core.Lua
{
    public class LuaError : Exception
    {
        public readonly LuaValue Value;
        public readonly int Level;

        public LuaError(string message, int level = 1) : base(message)
        {
            Value = new LuaValue(message);
            Level = level;
        }

        public LuaError(LuaValue message, int level = 1) : base(message.ToString())
        {
            Value = message;
            Level = level;
        }
    }
}
