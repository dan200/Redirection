using Dan200.Core.Lua;
using System.Collections.Generic;

namespace Dan200.Core.Computer
{
    public class EventQueue
    {
        private Queue<Event> m_events;
        private int m_sizeLimit;

        public EventQueue(int sizeLimit = 1024)
        {
            m_events = new Queue<Event>();
            m_sizeLimit = sizeLimit;
        }

        public void Clear()
        {
            m_events.Clear();
        }

        public bool Queue(string eventName)
        {
            return Queue(new Event(eventName, LuaArgs.Empty));
        }

        public bool Queue(string eventName, LuaArgs eventArgs)
        {
            return Queue(new Event(eventName, eventArgs));
        }

        public bool Queue(Event e)
        {
            if (m_events.Count < m_sizeLimit)
            {
                m_events.Enqueue(e);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Dequeue(out Event o_e)
        {
            if (m_events.Count > 0)
            {
                o_e = m_events.Dequeue();
                return true;
            }
            else
            {
                o_e = default(Event);
                return false;
            }
        }
    }
}
