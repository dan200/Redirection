using Dan200.Core.Assets;

namespace Dan200.Game.Options
{
    public class CycleOption : IOption
    {
        private string m_key;
        private IOptionValue<int> m_value;
        private int m_minimum;
        private int m_maximum;
        private int m_step;

        public CycleOption(string key, IOptionValue<int> value, int minimum, int maximum, int step = 1)
        {
            m_key = key;
            m_value = value;
            m_minimum = minimum;
            m_maximum = maximum;
            m_step = step;
        }

        public string ToString(Language language)
        {
            return language.Translate(m_key) + " - " + m_value.Value;
        }

        public void Click()
        {
            int newValue = m_value.Value + m_step;
            if (newValue > m_maximum)
            {
                newValue = m_minimum;
            }
            m_value.Value = newValue;
        }
    }

}
