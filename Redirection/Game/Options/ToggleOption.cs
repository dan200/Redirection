using Dan200.Core.Assets;
using Dan200.Game.Options;

namespace Dan200.Game.Game
{
    public class ToggleOption : IOption
    {
        private string m_key;
        private IOptionValue<bool> m_value;

        public ToggleOption(string key, IOptionValue<bool> value)
        {
            m_key = key;
            m_value = value;
        }

        public string ToString(Language language)
        {
            return language.Translate(m_key) + " - " + (m_value.Value ? "[gui/on.png]" : "[gui/off.png]");
        }

        public void Click()
        {
            m_value.Value = !m_value.Value;
        }
    }

}
