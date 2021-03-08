using System;

namespace Dan200.Core.Lua
{
    [AttributeUsage(AttributeTargets.Class)]
    public class LuaTypeAttribute : Attribute
    {
        public readonly string CustomName;
        public readonly bool ExposeType;

        public LuaTypeAttribute(string customName = null, bool exposeType = true)
        {
            CustomName = customName;
            ExposeType = exposeType;
        }
    }
}

