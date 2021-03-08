using Dan200.Core.GUI;
using System;

namespace Dan200.Game.Arcade
{
    public class CheatCodeChecker
    {
        private const int MAX_CODE_LENGTH = 16;
        private Screen m_screen;
        private string m_code;

        public CheatCodeChecker(Screen screen)
        {
            m_screen = screen;
            m_code = "";
        }

        public void Update()
        {
            if (m_screen.ModalDialog == null)
            {
                if (m_screen.CheckUp())
                {
                    Append('U');
                }
                if (m_screen.CheckDown())
                {
                    Append('D');
                }
                if (m_screen.CheckLeft())
                {
                    Append('L');
                }
                if (m_screen.CheckRight())
                {
                    Append('R');
                }
            }
        }

        private void Append(char key)
        {
            if (m_code.Length == MAX_CODE_LENGTH)
            {
                m_code = m_code.Substring(1);
            }
            m_code = m_code + key;
        }

        public bool Check(string code)
        {
            if (m_code.EndsWith(code, StringComparison.InvariantCulture))
            {
                m_code = "";
                return true;
            }
            return false;
        }
    }
}

