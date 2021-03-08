using System;

namespace Dan200.Core.Input
{
    public class SimpleJoystick : IJoystick
    {
        private float m_x;
        private float m_y;
        private float m_deadzone;

        public float X
        {
            get
            {
                if (Math.Abs(m_x) >= m_deadzone)
                {
                    return m_x;
                }
                return 0.0f;
            }
            set
            {
                m_x = value;
            }
        }

        public float Y
        {
            get
            {
                if (Math.Abs(m_y) >= m_deadzone)
                {
                    return m_y;
                }
                return 0.0f;
            }
            set
            {
                m_y = value;
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

        public SimpleJoystick()
        {
            m_x = 0.0f;
            m_y = 0.0f;
            m_deadzone = 0.0f;
        }
    }
}

