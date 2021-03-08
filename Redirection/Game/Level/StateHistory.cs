using System.Collections.Generic;

namespace Dan200.Game.Level
{
    public class StateHistory<TState> where TState : class
    {
        private struct StateRecord
        {
            public readonly float TimeStamp;
            public readonly TState State;

            public StateRecord(float time, TState state)
            {
                TimeStamp = time;
                State = state;
            }
        }

        public float InitialTimeStamp
        {
            get
            {
                return m_initialTimeStamp;
            }
        }

        public float LatestTimeStamp
        {
            get
            {
                return m_latestTimeStamp;
            }
        }

        public TState CurrentState
        {
            get
            {
                if (m_history.Count > 0)
                {
                    return m_history.Peek().State;
                }
                return default(TState);
            }
        }

        private Stack<StateRecord> m_history;
        private float m_initialTimeStamp;
        private float m_latestTimeStamp;

        public StateHistory(float initialTimeStamp)
        {
            m_history = new Stack<StateRecord>();
            m_initialTimeStamp = initialTimeStamp;
            m_latestTimeStamp = initialTimeStamp;
        }

        public void PushState(TState state)
        {
            if (state != CurrentState)
            {
                m_history.Push(new StateRecord(m_latestTimeStamp, state));
            }
        }

        public void Update(float timeStamp)
        {
            float now = timeStamp;
            if (now < m_latestTimeStamp)
            {
                while (m_history.Count > 0 && m_history.Peek().TimeStamp > now)
                {
                    m_history.Pop();
                }
            }
            m_latestTimeStamp = now;
        }

        public void Reset()
        {
            m_history.Clear();
            m_latestTimeStamp = m_initialTimeStamp;
        }
    }
}

