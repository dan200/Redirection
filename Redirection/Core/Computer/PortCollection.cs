using Dan200.Core.Util;
using System.Collections.Generic;

namespace Dan200.Core.Computer
{
    public class PortCollection : IReadOnlyCollection<IDevicePort>
    {
        private List<IDevicePort> m_ports;

        public int Count
        {
            get
            {
                return m_ports.Count;
            }
        }

        public PortCollection()
        {
            m_ports = new List<IDevicePort>();
        }

        public void Add(IDevicePort port)
        {
            if (!m_ports.Contains(port))
            {
                m_ports.Add(port);
            }
        }

        public void Remove(IDevicePort port)
        {
            m_ports.Remove(port);
        }

        public void Clear()
        {
            m_ports.Clear();
        }

        public IEnumerator<IDevicePort> GetEnumerator()
        {
            return m_ports.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_ports.GetEnumerator();
        }
    }
}
