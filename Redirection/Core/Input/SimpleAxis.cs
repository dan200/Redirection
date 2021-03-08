using System;

namespace Dan200.Core.Input
{
    public class SimpleAxis : IAxis
    {
        private float m_value;
        private float m_deadzone;

        public float Value
        {
            get
            {
                if (Math.Abs(m_value) >= m_deadzone)
                {
                    return m_value;
                }
                return 0.0f;
            }
            set
            {
                m_value = value;
            }
        }

        public float DeadZone
        {
            get
            {
                return m_deadzone;
            }
            set
            {
                m_deadzone = value;
            }
        }

        public SimpleAxis()
        {
            m_value = 0.0f;
            m_deadzone = 0.0f;
        }
    }
}

