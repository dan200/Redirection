using Dan200.Core.Computer.Devices.DiskDrive;
using Dan200.Core.Lua;

namespace Dan200.Core.Computer.Devices
{
    public class DiskDriveDevice : Device
    {
        private string m_description;

        private Disk m_disk;
        private Computer m_computer;

        public override string Type
        {
            get
            {
                return "drive";
            }
        }

        public override string Description
        {
            get
            {
                return m_description;
            }
        }

        public Disk Disk
        {
            get
            {
                return m_disk;
            }
            set
            {
                if (m_disk == value)
                {
                    return;
                }

                // Swap the disk
                var oldDisk = m_disk;
                m_disk = value;

                // Fire events
                if (m_computer != null)
                {
                    if (oldDisk != null)
                    {
                        oldDisk.LuaMount.Connected = false;
                    }
                    if (m_disk != null)
                    {
                        m_disk.LuaMount.Connected = true;
                    }
                    m_computer.Events.Queue(
                        "disk_changed", new LuaArgs(m_computer.Devices.GetName(this))
                    );
                }
            }
        }

        public DiskDriveDevice(string description)
        {
            m_description = description;
            m_disk = null;
        }

        public override void Attach(Computer computer)
        {
            m_computer = computer;
            if (m_disk != null)
            {
                m_disk.LuaMount.Connected = true;
            }
        }

        public override void Detach()
        {
            m_computer = null;
            if (m_disk != null)
            {
                m_disk.LuaMount.Connected = false;
            }
        }

        [LuaMethod]
        public LuaArgs eject(LuaArgs args)
        {
            // Remove the disk
            if (m_disk != null)
            {
                // Unmount the disk
                m_disk.LuaMount.Connected = false;
                m_disk = null;

                // Send the disk_changed event
                m_computer.Events.Queue(
                    "disk_changed", new LuaArgs(m_computer.Devices.GetName(this))
                );
            }
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs hasDisk(LuaArgs args)
        {
            return new LuaArgs(m_disk != null);
        }

        [LuaMethod]
        public LuaArgs getMount(LuaArgs args)
        {
            return new LuaArgs((m_disk != null) ? m_disk.LuaMount : null);
        }
    }
}

