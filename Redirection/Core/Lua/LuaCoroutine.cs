namespace Dan200.Core.Lua
{
    public class LuaCoroutine
    {
        public readonly LuaMachine Machine;
        internal readonly int ID;

        public bool IsFinished
        {
            get
            {
                return Machine.Disposed || Machine.IsFinished(this);
            }
        }

        internal LuaCoroutine(LuaMachine machine, int id)
        {
            Machine = machine;
            ID = id;
        }

        ~LuaCoroutine()
        {
            if (!Machine.Disposed)
            {
                Machine.Release(this);
            }
        }

        public LuaArgs Resume(LuaArgs args)
        {
            if (Machine.Disposed)
            {
                throw new LuaError("Attempt to resume dead coroutine", 0);
            }
            return Machine.Resume(this, args);
        }
    }
}

