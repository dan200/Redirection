using System;
using System.Threading;

namespace Dan200.Core.Computer.Devices.GPU
{
    public class ChangeListener
    {
        [ThreadStatic]
        private static Random s_random = new Random();

        private int m_version;

        public int Version
        {
            get
            {
                return m_version;
            }
        }

        public ChangeListener()
        {
            m_version = s_random.Next();
        }

        public void Change()
        {
            Interlocked.Increment(ref m_version);
        }
    }
}

