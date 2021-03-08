using System;

namespace Dan200.Game.Level
{
    public class TimeMachine
    {
        private const float FIXED_TIMESTEP = 1.0f / 60.0f;

        private float m_time;
        private float m_realTime;
        private float m_rate;
        private float m_pendingTime;
        private float? m_limit;

        public float Time
        {
            get
            {
                return m_time;
            }
        }

        public float RealTime
        {
            get
            {
                return m_realTime;
            }
        }

        public float Rate
        {
            get
            {
                return m_rate;
            }
            set
            {
                m_rate = value;
            }
        }

        public float? Limit
        {
            get
            {
                return m_limit;
            }
            set
            {
                m_limit = value;
            }
        }

        public TimeMachine()
        {
            m_time = 0.0f;
            m_realTime = 0.0f;
            m_rate = 1.0f;
            m_pendingTime = 0.0f;
            m_limit = null;
        }

        public void Update(float dt)
        {
            m_pendingTime += dt;
        }

        public bool Step()
        {
            float rate = m_rate;
            if (rate < 0.0f)
            {
                // Step backwards in time in one go
                if (m_pendingTime > 0.0f)
                {
                    var limit = m_limit.HasValue ? m_limit.Value : 0.0f;
                    m_time = Math.Max(m_time + rate * m_pendingTime, limit);
                    m_realTime += m_pendingTime;
                    m_pendingTime = 0.0f;
                    return true;
                }
                return false;
            }
            else if (rate > 0.0f)
            {
                // Go forward in time by one fixed timestep
                var step = FIXED_TIMESTEP;
                if (m_limit.HasValue)
                {
                    step = Math.Min(FIXED_TIMESTEP, m_limit.Value - m_time);
                }
                if (m_pendingTime * rate >= FIXED_TIMESTEP)
                {
                    m_time += step;
                    m_realTime += FIXED_TIMESTEP / rate;
                    m_pendingTime -= FIXED_TIMESTEP / rate;
                    return true;
                }
                return false;
            }
            else
            {
                // Stand still in time
                m_realTime += m_pendingTime;
                m_pendingTime = 0.0f;
                return false;
            }
        }
    }
}

