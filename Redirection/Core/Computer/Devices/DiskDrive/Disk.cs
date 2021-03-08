using Dan200.Core.Lua;

namespace Dan200.Core.Computer.Devices.DiskDrive
{
    public class Disk
    {
        private LuaObjectRef<LuaMount> m_mount;

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

        public Disk(IMount mount)
        {
            m_mount = new LuaObjectRef<LuaMount>(
                new LuaMount(mount)
            );
        }
    }
}

