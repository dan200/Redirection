using System;

namespace Dan200.Core.Lua
{
    public class MemoryTracker
    {
        private long m_totalMemory;
        private long m_usedMemory;
        private Action m_gcAction;

        public long TotalMemory
        {
            get
            {
                return m_totalMemory;
            }
            set
            {
                m_totalMemory = value;
            }
        }

        public long UsedMemory
        {
            get
            {
                return m_usedMemory;
            }
        }

        public long FreeMemory
        {
            get
            {
                return Math.Max(TotalMemory - m_usedMemory, 0);
            }
        }

        public MemoryTracker(long totalMemory, Action gcAction = null)
        {
            m_totalMemory = totalMemory;
            m_usedMemory = 0;
            m_gcAction = gcAction;
        }

        public void Reset()
        {
            m_usedMemory = 0;
        }

        public void ForceAlloc(long bytes)
        {
            if (bytes < 0)
            {
                throw new InvalidOperationException();
            }
            m_usedMemory += bytes;
        }

        public bool Alloc(long bytes, bool gc = true)
        {
            if (bytes < 0)
            {
                throw new InvalidOperationException();
            }
            if (m_usedMemory + bytes <= m_totalMemory)
            {
                m_usedMemory += bytes;
                return true;
            }
            else
            {
                if (gc && m_gcAction != null)
                {
                    m_gcAction.Invoke();
                    if (m_usedMemory + bytes <= m_totalMemory)
                    {
                        m_usedMemory += bytes;
                        return true;
                    }
                }
                return false;
            }
        }

        public void Free(long bytes)
        {
            if (bytes < 0 || bytes > m_usedMemory)
            {
                throw new InvalidOperationException();
            }
            m_usedMemory = m_usedMemory - bytes;
        }
    }
}
