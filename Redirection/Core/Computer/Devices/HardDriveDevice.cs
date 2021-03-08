using Dan200.Core.Computer.Devices.DiskDrive;
using Dan200.Core.Lua;

namespace Dan200.Core.Computer.Devices
{
    public class HardDriveDevice : Device
    {
        private string m_description;
        private LuaObjectRef<LuaMount> m_mount;

        public override string Type
        {
            get
            {
                return "hdd";
            }
        }

        public override string Description
        {
            get
            {
                return m_description;
            }
        }

        internal LuaMount LuaMount
        {
            get
            {
                return m_mount.Value;
            }
        }

        public IMount Mount
        {
            get
            {
                return m_mount.Value.Mount;
            }
        }

        public HardDriveDevice(string description, IMount mount)
        {
            m_description = description;
            m_mount = new LuaObjectRef<LuaMount>(new LuaMount(mount));
        }

        public override void Attach(Computer computer)
        {
            m_mount.Value.Connected = true;
        }

        public override void Detach()
        {
            m_mount.Value.Connected = false;
        }

        [LuaMethod]
        public LuaArgs getCapacity(LuaArgs args)
        {
            return new LuaArgs(m_mount.Value.Mount.Capacity);
        }

        [LuaMethod]
        public LuaArgs getMount(LuaArgs args)
        {
            return new LuaArgs(m_mount.Value);
        }
    }
}

