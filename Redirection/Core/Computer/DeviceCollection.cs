using Dan200.Core.Util;
using System.Collections.Generic;

namespace Dan200.Core.Computer
{
    public class DeviceCollection : IReadOnlyCollection<Device>
    {
        private List<Device> m_orderedDevices;
        private Dictionary<string, Device> m_nameToDevice;
        private Dictionary<Device, string> m_deviceToName;

        public int Count
        {
            get
            {
                return m_orderedDevices.Count;
            }
        }

        public Device this[string key]
        {
            get
            {
                if (m_nameToDevice.ContainsKey(key))
                {
                    return m_nameToDevice[key];
                }
                return null;
            }
        }

        public DeviceCollection()
        {
            m_orderedDevices = new List<Device>();
            m_nameToDevice = new Dictionary<string, Device>();
            m_deviceToName = new Dictionary<Device, string>();
        }

        public bool Contains(Device device)
        {
            return m_deviceToName.ContainsKey(device);
        }

        public string GetName(Device device)
        {
            if (m_deviceToName.ContainsKey(device))
            {
                return m_deviceToName[device];
            }
            return null;
        }

        internal void Add(Device device)
        {
            if (!m_deviceToName.ContainsKey(device))
            {
                var name = FindUniqueName(device.Type);
                m_orderedDevices.Add(device);
                m_nameToDevice.Add(name, device);
                m_deviceToName.Add(device, name);
            }
        }

        internal void Remove(Device device)
        {
            if (m_deviceToName.ContainsKey(device))
            {
                var name = m_deviceToName[device];
                m_orderedDevices.Remove(device);
                m_nameToDevice.Remove(name);
                m_deviceToName.Remove(device);
            }
        }

        internal void Clear()
        {
            m_orderedDevices.Clear();
            m_nameToDevice.Clear();
            m_deviceToName.Clear();
        }

        public IEnumerator<Device> GetEnumerator()
        {
            return m_orderedDevices.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_orderedDevices.GetEnumerator();
        }

        private string FindUniqueName(string desiredName)
        {
            if (m_nameToDevice.ContainsKey(desiredName))
            {
                int i = 2;
                string name = desiredName + i;
                while (m_nameToDevice.ContainsKey(name))
                {
                    ++i;
                    name = desiredName + i;
                }
                return name;
            }
            else
            {
                return desiredName;
            }
        }
    }
}
