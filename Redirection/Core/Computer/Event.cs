using Dan200.Core.Lua;

namespace Dan200.Core.Computer
{
    public struct Event
    {
        public readonly string Name;
        public readonly LuaArgs Arguments;

        internal Event(string name, LuaArgs args)
        {
            Name = name;
            Arguments = args;
        }
    }
}

