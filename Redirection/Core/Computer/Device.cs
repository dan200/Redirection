using Dan200.Core.Lua;
using System;

namespace Dan200.Core.Computer
{
    public enum DeviceUpdateResult
    {
        Continue,
        Shutdown,
        Reboot
    }

    public abstract class Device
    {
        public abstract string Type { get; }
        public abstract string Description { get; }

        protected Device()
        {
        }

        public virtual void Attach(Computer computer)
        {
        }

        public virtual void Detach()
        {
        }

        public virtual DeviceUpdateResult Boot()
        {
            return DeviceUpdateResult.Continue;
        }

        public virtual DeviceUpdateResult HandleEvent(Event e)
        {
            return DeviceUpdateResult.Continue;
        }

        public virtual DeviceUpdateResult Update(TimeSpan dt)
        {
            return DeviceUpdateResult.Continue;
        }

        public virtual void FreeUnusedMemory()
        {
        }

        [LuaMethod]
		public LuaArgs getType(LuaArgs args)
		{
			return new LuaArgs(Type);
		}

		[LuaMethod]
        public LuaArgs getDescription(LuaArgs args)
        {
            return new LuaArgs(Description);
        }
    }
}
