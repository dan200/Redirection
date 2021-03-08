namespace Dan200.Core.Input
{
    public class SimpleButton : IButton
    {
        private bool m_held;
        private bool m_pressed;
        private bool m_released;
        private bool m_repeated;
        private bool m_ignoring;
        private bool m_repeatQueued;

        public bool Held
        {
            get { return m_held; }
        }

        public bool Pressed
        {
            get { return m_pressed; }
        }

        public bool Released
        {
            get { return m_released; }
        }

        public bool Repeated
        {
            get { return m_repeated; }
        }

        public SimpleButton()
        {
            m_held = false;
            m_pressed = false;
            m_released = false;
            m_repeated = false;
        }

        public void Update(bool held)
        {
            if (m_ignoring && !held)
            {
                m_ignoring = false;
            }
            if (!m_ignoring)
            {
                m_pressed = (held && !m_held);
                m_released = (!held && m_held);
                m_held = held;
                m_repeated = m_repeatQueued && m_held && !m_pressed;
            }
            m_repeatQueued = false;
        }

        public void Repeat()
        {
            m_repeatQueued = true;
        }

        public void Disconnect()
        {
            m_pressed = false;
            m_released = false;
            m_held = false;
            m_repeated = false;
            m_ignoring = false;
        }

        public void IgnoreCurrentPress()
        {
            m_pressed = false;
            m_released = false;
            m_held = false;
            m_ignoring = true;
        }
    }
}

