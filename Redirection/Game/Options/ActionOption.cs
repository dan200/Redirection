
using Dan200.Core.Assets;
using System;

namespace Dan200.Game.Options
{
    public class ActionOption : IOption
    {
        private string m_key;
        private Action m_action;

        public ActionOption(string key, Action action)
        {
            m_key = key;
            m_action = action;
        }

        public string ToString(Language language)
        {
            return language.Translate(m_key);
        }

        public void Click()
        {
            m_action.Invoke();
        }
    }

}
