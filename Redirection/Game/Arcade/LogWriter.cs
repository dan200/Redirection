using Dan200.Core.Main;
using System.Text;

namespace Dan200.Game.Arcade
{
    public class LogWriter : System.IO.TextWriter
    {
        private LogLevel m_level;
        private StringBuilder m_pendingLine;

        public override Encoding Encoding
        {
            get
            {
                return Encoding.UTF8;
            }
        }

        public LogWriter(LogLevel level)
        {
            m_level = level;
            m_pendingLine = new StringBuilder();
        }

        public override void Write(char value)
        {
            if (value == '\n')
            {
                Emit();
            }
            else if (value != '\r')
            {
                m_pendingLine.Append(value);
            }
        }

        private void Emit()
        {
            App.Log(m_pendingLine.ToString(), m_level);
            m_pendingLine.Clear();
        }
    }
}

