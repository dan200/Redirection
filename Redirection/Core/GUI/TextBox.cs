
using Dan200.Core.Input;
using OpenTK;

namespace Dan200.Core.GUI
{
    public class TextBox : Box
    {
        private string m_text;
        private Text m_textElement;

        private bool m_focus;
        private bool m_hover;

        public string Text
        {
            get
            {
                return m_text;
            }
            set
            {
                m_text = value;
                UpdateText();
            }
        }

        public bool Focus
        {
            get
            {
                return m_focus;
            }
            set
            {
                if (m_focus != value)
                {
                    m_focus = value;
                    UpdateText();
                }
            }
        }

        public TextBox(float width, float height) : base(Core.Render.Texture.Get("gui/textbox.png", true), width, height)
        {
            m_text = "";
            m_textElement = new Text(UIFonts.Default, m_text, UIColours.Text, TextAlignment.Center);
            m_textElement.ParseImages = false;
            m_focus = false;
            m_hover = false;
            UpdateText();
        }

        public override void Dispose()
        {
            m_textElement.Dispose();
            m_textElement = null;

            base.Dispose();
        }

        protected override void OnInit()
        {
            m_textElement.Init(Screen);
        }

        private void OnClick()
        {
            Focus = TestMouse();
        }

        private void Backspace()
        {
            if (Text.Length > 0)
            {
                if (char.IsLowSurrogate(Text[Text.Length - 1]))
                {
                    Text = Text.Substring(0, Text.Length - 2);
                }
                else
                {
                    Text = Text.Substring(0, Text.Length - 1);
                }
            }
        }

        private void Char(int codepoint)
        {
            var newText = m_text + char.ConvertFromUtf32(codepoint);
            var width = m_textElement.Font.Measure(newText, false);
            if (width <= Width - 12.0f)
            {
                Text = newText;
            }
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            if (Screen.Mouse.Buttons[MouseButton.Left].Pressed)
            {
                OnClick();
            }

            if (Focus)
            {
                var text = Screen.Keyboard.Text;
                for (int i = 0; i < text.Length; ++i)
                {
                    if (text[i] == '\b')
                    {
                        Backspace();
                    }
                    else if (char.IsHighSurrogate(text, i) && (i + 1 < text.Length && char.IsLowSurrogate(text, i + 1)))
                    {
                        Char(char.ConvertToUtf32(text, i));
                    }
                    else if (!char.IsSurrogate(text, i))
                    {
                        Char(text[i]);
                    }
                }
            }
        }

        private void UpdateHover()
        {
            bool hover = TestMouse();
            if (hover != m_hover)
            {
                m_hover = hover;
                UpdateText();
            }
        }

        protected override void OnDraw()
        {
            base.OnDraw();
            m_textElement.Draw();
        }

        protected override void OnRebuild()
        {
            base.OnRebuild();
            m_textElement.Anchor = Anchor;
            m_textElement.LocalPosition = LocalPosition + new Vector2(0.5f * Width, 0.5f * (Height - m_textElement.Font.Height));
            m_textElement.RequestRebuild();
        }

        private bool TestMouse()
        {
            Vector2 mouseLocal = Screen.MousePosition - Position;
            if (mouseLocal.X >= 0.0f && mouseLocal.X < Width &&
                mouseLocal.Y >= 0.0f && mouseLocal.Y < Height)
            {
                return true;
            }
            return false;
        }

        private void UpdateText()
        {
            m_textElement.Colour = (!m_focus && m_hover) ? UIColours.Hover : UIColours.Text;
            m_textElement.String = m_focus ? (m_text + "_") : m_text;
        }
    }
}