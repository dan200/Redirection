using Dan200.Core.Util;
using System;
using System.Collections.Generic;

namespace Dan200.Core.Input.Null
{
    public class NullKeyboard : IKeyboard
    {
        private IReadOnlyDictionary<Key, IButton> m_keys;

        public IReadOnlyDictionary<Key, IButton> Keys
        {
            get
            {
                return m_keys;
            }
        }

        public string Text
        {
            get
            {
                return "";
            }
        }

        public NullKeyboard()
        {
            var keys = new Dictionary<Key, IButton>();
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (key != Key.None)
                {
                    keys.Add(key, NullButton.Instance);
                }
            }
            m_keys = keys.ToReadOnly();
        }
    }
}

