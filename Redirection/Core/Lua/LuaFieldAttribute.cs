using System;

namespace Dan200.Core.Lua
{
    [AttributeUsage(AttributeTargets.Field)]
    public class LuaFieldAttribute : Attribute
    {
        public readonly String CustomName;

        public LuaFieldAttribute(string customName = null)
        {
            CustomName = customName;
        }
    }
}

