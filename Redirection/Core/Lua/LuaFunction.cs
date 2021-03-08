namespace Dan200.Core.Lua
{
    public class LuaFunction
    {
        public readonly LuaMachine Machine;
        internal readonly int ID;

        internal LuaFunction(LuaMachine machine, int id)
        {
            Machine = machine;
            ID = id;
        }

        ~LuaFunction()
        {
            if (!Machine.Disposed)
            {
                Machine.Release(this);
            }
        }

        public LuaArgs Call(LuaArgs args)
        {
            if (Machine.Disposed)
            {
                throw new LuaError("Attempt to call dead function", 0);
            }
            return Machine.Call(this, args);
        }

		public LuaArgs CallAsync(LuaArgs args, LuaContinuation continuation=null)
        {
            if (Machine.Disposed)
            {
                throw new LuaError("Attempt to call dead function", 0);
            }
            return Machine.CallAsync(this, args, continuation);
        }
    }
}
