namespace Dan200.Core.Input
{
    public class TwoAxisJoystick : IJoystick
    {
        private IAxis m_xAxis;
        private IAxis m_yAxis;

        public float X
        {
            get
            {
                return m_xAxis.Value;
            }
        }

        public float Y
        {
            get
            {
                return m_yAxis.Value;
            }
        }

        public TwoAxisJoystick(IAxis xAxis, IAxis yAxis)
        {
            m_xAxis = xAxis;
            m_yAxis = yAxis;
        }
    }
}

