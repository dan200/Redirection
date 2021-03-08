
using Dan200.Core.Assets;
using System;

namespace Dan200.Game.Options
{
    public class MultipleChoiceOption<T> : IOption
    {
        private string m_key;
        private IOptionValue<T> m_value;
        private T[] m_choices;
        private Func<T, string> m_translateFunction;

        public MultipleChoiceOption(string key, IOptionValue<T> value, T[] choices, Func<T, string> translateFunction)
        {
            m_key = key;
            m_value = value;
            m_choices = choices;
            m_translateFunction = translateFunction;
        }

        public string ToString(Language language)
        {
            string valueString = m_translateFunction.Invoke(m_value.Value);
            return language.Translate(m_key) + " - " + valueString;
        }

        public void Click()
        {
            int oldIndex = -1;
            var oldValue = m_value.Value;
            for (int i = 0; i < m_choices.Length; ++i)
            {
                if (object.Equals(m_choices[i], oldValue))
                {
                    oldIndex = i;
                    break;
                }
            }
            int newIndex = (oldIndex + 1) % m_choices.Length;
            m_value.Value = m_choices[newIndex];
        }
    }

}
