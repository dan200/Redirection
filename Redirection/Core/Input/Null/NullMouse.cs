using Dan200.Core.Util;
using System;
using System.Collections.Generic;

namespace Dan200.Core.Input.Null
{
    public class NullMouse : IMouse
    {
        private IReadOnlyDictionary<MouseButton, IButton> m_buttons;

        public int X
        {
            get
            {
                return -99;
            }
        }

        public int Y
        {
            get
            {
                return -99;
            }
        }

        public int DX
        {
            get
            {
                return 0;
            }
        }

        public int DY
        {
            get
            {
                return 0;
            }
        }

        public int Wheel
        {
            get
            {
                return 0;
            }
        }

        public Dan200.Core.Util.IReadOnlyDictionary<MouseButton, IButton> Buttons
        {
            get
            {
                return m_buttons;
            }
        }

        public NullMouse()
        {
            var buttons = new Dictionary<MouseButton, IButton>();
            foreach (MouseButton button in Enum.GetValues(typeof(MouseButton)))
            {
                if (button != MouseButton.None)
                {
                    buttons.Add(button, NullButton.Instance);
                }
            }
            m_buttons = buttons.ToReadOnly();
        }
    }
}

