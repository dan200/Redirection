using Dan200.Core.GUI;
using Dan200.Core.Render;
using OpenTK;

namespace Dan200.Game.GUI
{
    public class Terminal : Element
    {
        private Font m_font;
        private Text[] m_lines;
        private float m_width;

        public float Width
        {
            get
            {
                return m_width;
            }
        }

        public float Height
        {
            get
            {
                return m_font.Height * m_lines.Length;
            }
        }

        public int Lines
        {
            get
            {
                return m_lines.Length;
            }
        }

        public Terminal(Font font, Vector4 textColour, float width, int numLines)
        {
            m_font = font;
            m_lines = new Text[numLines];
            m_width = width;
            for (int i = 0; i < m_lines.Length; ++i)
            {
                m_lines[i] = new Text(m_font, "", textColour, TextAlignment.Left);
            }
        }

        protected override void OnInit()
        {
            for (int i = 0; i < m_lines.Length; ++i)
            {
                var line = m_lines[i];
                line.Init(Screen);
            }
        }

        public override void Dispose()
        {
            for (int i = 0; i < m_lines.Length; ++i)
            {
                var line = m_lines[i];
                line.Dispose();
            }
        }

        public void SetLine(int i, string text)
        {
            if (i >= 0 && i < m_lines.Length)
            {
                var line = m_lines[i];
                var len = m_font.WordWrap(text, true, m_width);
                if (len < text.Length)
                {
                    text = text.Substring(0, len);
                }
                line.String = text;
            }
        }

        public void SetAlignment(int i, TextAlignment alignment)
        {
            if (i >= 0 && i < m_lines.Length)
            {
                var line = m_lines[i];
                if (line.Alignment != alignment)
                {
                    line.Alignment = alignment;
                    RequestRebuild();
                }
            }
        }

        public int WordWrap(string text, int start)
        {
            return m_font.WordWrap(text, start, true, m_width);
        }

        public string GetLine(int i)
        {
            if (i >= 0 && i < m_lines.Length)
            {
                var line = m_lines[i];
                return line.String;
            }
            return "";
        }

        public void Clear()
        {
            for (int i = 0; i < m_lines.Length; ++i)
            {
                var line = m_lines[i];
                line.String = "";
                if (line.Alignment != TextAlignment.Left)
                {
                    line.Alignment = TextAlignment.Left;
                    RequestRebuild();
                }
            }
        }

        public void Scroll(int n)
        {
            if (n > 0)
            {
                for (int i = 0; i < m_lines.Length; ++i)
                {
                    var line = m_lines[i];
                    var src = i + n;
                    if (src < m_lines.Length)
                    {
                        line.String = m_lines[src].String;
                        line.Alignment = m_lines[src].Alignment;
                    }
                    else
                    {
                        line.String = "";
                        line.Alignment = TextAlignment.Left;
                    }
                }
                RequestRebuild();
            }
            else if (n < 0)
            {
                for (int i = 0; i < m_lines.Length; ++i)
                {
                    var line = m_lines[i];
                    var src = i + n;
                    if (src >= 0)
                    {
                        line.String = m_lines[src].String;
                    }
                    else
                    {
                        line.String = "";
                    }
                }
                RequestRebuild();
            }
        }

        protected override void OnUpdate(float dt)
        {
            for (int i = 0; i < m_lines.Length; ++i)
            {
                var line = m_lines[i];
                line.Update(dt);
            }
        }

        protected override void OnRebuild()
        {
            for (int i = 0; i < m_lines.Length; ++i)
            {
                var line = m_lines[i];
                line.Anchor = Anchor;
                if (line.Alignment == TextAlignment.Left)
                {
                    line.LocalPosition = LocalPosition + new Vector2(0.0f, (float)i * m_font.Height);
                }
                else if (line.Alignment == TextAlignment.Right)
                {
                    line.LocalPosition = LocalPosition + new Vector2(Width, (float)i * m_font.Height);
                }
                else
                {
                    line.LocalPosition = LocalPosition + new Vector2(0.5f * Width, (float)i * m_font.Height);
                }
                line.RequestRebuild();
            }
        }

        protected override void OnDraw()
        {
            for (int i = 0; i < m_lines.Length; ++i)
            {
                var line = m_lines[i];
                line.Draw();
            }
        }
    }
}
