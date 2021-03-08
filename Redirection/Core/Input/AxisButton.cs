namespace Dan200.Core.Input
{
    public class AxisButton : IButton
    {
        private IAxis m_axis;
        private float m_threshold;

        private bool m_held;
        private bool m_pressed;
        private bool m_released;

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
            get { return false; }
        }

        public AxisButton(IAxis axis, float threshold)
        {
            m_axis = axis;
            m_threshold = threshold;
            m_held = false;
            m_pressed = false;
            m_released = false;
        }

        public void Update()
        {
            bool held = (m_threshold > 0.0f) ?
                (m_axis.Value >= m_threshold) :
                (m_axis.Value <= m_threshold);

            m_pressed = (held && !m_held);
            m_released = (!held && m_held);
            m_held = held;
        }

        public void Disconnect()
        {
            m_pressed = false;
            m_released = false;
            m_held = false;
        }
    }
}

