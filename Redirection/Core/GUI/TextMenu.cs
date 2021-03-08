using Dan200.Core.Audio;
using Dan200.Core.Input;
using Dan200.Core.Render;
using OpenTK;
using System;

namespace Dan200.Core.GUI
{
    public class TextMenuClickedEventArgs : EventArgs
    {
        public readonly int Index;

        public TextMenuClickedEventArgs(int index)
        {
            Index = index;
        }
    }

    public enum MenuDirection
    {
        Horizontal,
        Vertical
    }

    public class TextMenu : Element
    {
        public const float MARGIN = 16.0f;
        public const float HGAP = 8.0f;

        private TextAlignment m_alignment;
        private MenuDirection m_direction;
        private float m_width;
        private float m_minimumWidth;

        private Vector4 m_textColour;
        private Vector4 m_highlightColour;
        private Font m_font;
        private Text[] m_options;
        private TextMenuOptionSet m_optionSet;
        private TextStyle m_style;

        private bool m_mouseHighlight;
        private int m_highlight;

        private bool m_showBackground;
        private Geometry m_backgroundGeometry;
        private ITexture m_backgroundTexture;

        public event EventHandler<TextMenuClickedEventArgs> OnClicked;

        public class TextMenuOptionSet
        {
            private TextMenu m_owner;

            public TextMenuOptionSet(TextMenu owner)
            {
                m_owner = owner;
            }

            public int Count
            {
                get
                {
                    return m_owner.m_options.Length;
                }
            }

            public string this[int i]
            {
                get
                {
                    if (i >= 0 && i < m_owner.m_options.Length)
                    {
                        return m_owner.m_options[i].String;
                    }
                    return null;
                }
                set
                {
                    if (i >= 0 && i < m_owner.m_options.Length)
                    {
                        m_owner.m_options[i].String = value;
                        m_owner.RecalculateWidth();
                        m_owner.RepositionOptions();
                        m_owner.RequestRebuild();
                    }
                }
            }
        }

        public TextMenuOptionSet Options
        {
            get
            {
                return m_optionSet;
            }
        }

        public Vector4 TextColour
        {
            get
            {
                return m_textColour;
            }
            set
            {
                m_textColour = value;
                UpdateHoverColours();
            }
        }

        public Font Font
        {
            get
            {
                return m_font;
            }
        }

        public TextStyle Style
        {
            get
            {
                return m_style;
            }
            set
            {
                if (value != m_style)
                {
                    m_style = value;
                    for (int i = 0; i < m_options.Length; ++i)
                    {
                        var option = m_options[i];
                        option.Style = m_style;
                    }
                    RecalculateWidth();
                    RepositionOptions();
                    RequestRebuild();
                }
            }
        }

        public float Height
        {
            get
            {
                if (m_direction == MenuDirection.Vertical)
                {
                    return m_font.Height * m_options.Length;
                }
                else
                {
                    return m_font.Height;
                }
            }
        }

        public float Width
        {
            get
            {
                return m_width;
            }
        }

        public float MinimumWidth
        {
            get
            {
                return m_minimumWidth;
            }
            set
            {
                if (value != m_minimumWidth)
                {
                    m_minimumWidth = value;
                    RecalculateWidth();
                    RepositionOptions();
                    RequestRebuild();
                }
            }
        }

        protected float Margin
        {
            get
            {
                return m_showBackground ? MARGIN : 0.0f;
            }
        }

        public Vector4 HighlightColour
        {
            get
            {
                return m_highlightColour;
            }
            set
            {
                m_highlightColour = value;
                UpdateHoverColours();
            }
        }

        public bool ShowBackground
        {
            get
            {
                return m_showBackground;
            }
            set
            {
                if (value != m_showBackground)
                {
                    m_showBackground = value;
                    RecalculateWidth();
                    RepositionOptions();
                    RequestRebuild();
                }
            }
        }

        public bool Enabled;
        public bool MouseOnly;

        public int Focus
        {
            get
            {
                return m_highlight;
            }
        }

        public TextMenu(Font font, string[] options, TextAlignment alignment, MenuDirection direction, int columns = 1)
        {
            m_font = font;
            m_style = TextStyle.Default;
            m_textColour = UIColours.Text;
            m_highlightColour = UIColours.Hover;
            m_options = new Text[options.Length];
            m_optionSet = new TextMenuOptionSet(this);

            m_alignment = alignment;
            m_direction = direction;
            for (int i = 0; i < m_options.Length; ++i)
            {
                var option = new Text(m_font, options[i], m_textColour, TextAlignment.Left);
                option.Style = m_style;
                m_options[i] = option;
            }
            Enabled = true;
            MouseOnly = false;

            m_highlight = -1;
            m_mouseHighlight = false;
            m_minimumWidth = 0.0f;

            m_showBackground = false;
            m_backgroundGeometry = new Geometry(Primitive.Triangles, 4 * options.Length, 6 * options.Length);
            m_backgroundTexture = Texture.Get("gui/inset_button.png", true);
            RecalculateWidth();
        }

        public override void Dispose()
        {
            base.Dispose();
            for (int i = 0; i < m_options.Length; ++i)
            {
                m_options[i].Dispose();
            }
            m_backgroundGeometry.Dispose();
        }

        private void RecalculateWidth()
        {
            float width = 0.0f;
            if (m_direction == MenuDirection.Vertical)
            {
                for (int i = 0; i < m_options.Length; ++i)
                {
                    var option = m_options[i];
                    width = Math.Max(width, option.Width + 2.0f * Margin);
                }
            }
            else
            {
                for (int i = 0; i < m_options.Length; ++i)
                {
                    var option = m_options[i];
                    width += option.Width + 2.0f * Margin;
                    if (i < m_options.Length - 1)
                    {
                        width += HGAP;
                    }
                }
            }
            m_width = Math.Max(width, m_minimumWidth);
        }

        private void RepositionOptions()
        {
            float xPos;
            if (m_alignment == TextAlignment.Center)
            {
                xPos = -0.5f * m_width;
            }
            else if (m_alignment == TextAlignment.Right)
            {
                xPos = -m_width;
            }
            else
            {
                xPos = 0.0f;
            }
            float yPos = 0.0f;

            for (int i = 0; i < m_options.Length; ++i)
            {
                var option = m_options[i];
                option.Anchor = Anchor;
                if (m_direction == MenuDirection.Vertical)
                {
                    float elementXPos;
                    if (m_alignment == TextAlignment.Center)
                    {
                        elementXPos = xPos + 0.5f * m_width - 0.5f * option.Width;
                    }
                    else if (m_alignment == TextAlignment.Right)
                    {
                        elementXPos = xPos + (m_width - option.Width - Margin);
                    }
                    else
                    {
                        elementXPos = xPos + Margin;
                    }
                    option.LocalPosition = LocalPosition + new Vector2(elementXPos, yPos);
                    yPos += option.Height;
                }
                else
                {
                    option.LocalPosition = LocalPosition + new Vector2(xPos + Margin, 0.0f);
                    xPos += option.Width + 2.0f * Margin;
                    if (i < m_options.Length - 1)
                    {
                        xPos += HGAP;
                    }
                }
            }
        }

        protected override void OnInit()
        {
            RecalculateWidth();
            RepositionOptions();
            for (int i = 0; i < m_options.Length; ++i)
            {
                var option = m_options[i];
                option.Init(Screen);
            }

            if (Enabled && m_options.Length > 0)
            {
                if (Screen.InputMethod == InputMethod.Mouse)
                {
                    HighlightFromMouse();
                }
                else
                {
                    m_highlight = MouseOnly ? -1 : 0;
                    m_mouseHighlight = false;
                }
            }
            else
            {
                m_highlight = -1;
                m_mouseHighlight = false;
            }
            UpdateHoverColours();
        }

        private void UpdateHoverColours()
        {
            for (int i = 0; i < m_options.Length; ++i)
            {
                m_options[i].Colour = (i == m_highlight) ?
                    m_highlightColour : m_textColour;
            }
        }

        private int wrapIndex(int i)
        {
            return (i + m_options.Length) % m_options.Length;
        }

        private void Press(int option)
        {
            if (OnClicked != null)
            {
                OnClicked.Invoke(this, new TextMenuClickedEventArgs(option));
            }
        }

        private void HighlightFromMouse()
        {
            var highlight = TestMouse();
            m_highlight = highlight;
            m_mouseHighlight = true;
        }

        protected override void OnUpdate(float dt)
        {
            if (!Enabled || !Visible || Screen.ModalDialog != Parent)
            {
                var oldHighlight = m_highlight;
                if (Screen.Mouse.DX != 0 || Screen.Mouse.DY != 0 || Screen.InputMethod == InputMethod.Mouse)
                {
                    Screen.InputMethod = InputMethod.Mouse;
                    var highlight = TestMouse();
                    if (highlight != oldHighlight)
                    {
                        m_highlight = Visible ? -1 : highlight;
                        m_mouseHighlight = true;
                    }
                }
                if (m_highlight != oldHighlight)
                {
                    UpdateHoverColours();
                }
                return;
            }

            // Mouse hover
            if (Screen.Mouse.DX != 0 || Screen.Mouse.DY != 0 || Screen.InputMethod == InputMethod.Mouse)
            {
                Screen.InputMethod = InputMethod.Mouse;
                var oldHighlight = m_highlight;
                HighlightFromMouse();
                if (m_highlight != oldHighlight)
                {
                    if (m_highlight != -1)
                    {
                        PlayHighlightSound();
                    }
                    UpdateHoverColours();
                }
            }

            if (!MouseOnly)
            {
                // Navigation controls
                if ((m_direction == MenuDirection.Vertical) ? Screen.CheckUp() : Screen.CheckLeft())
                {
                    if (m_options.Length > 0)
                    {
                        m_highlight = (m_highlight >= 0 && !m_mouseHighlight) ?
                            wrapIndex(m_highlight - 1) :
                            m_options.Length - 1;
                        m_mouseHighlight = false;
                        PlayHighlightSound();
                    }
                    UpdateHoverColours();
                }
                if ((m_direction == MenuDirection.Vertical) ? Screen.CheckDown() : Screen.CheckRight())
                {
                    if (m_options.Length > 0)
                    {
                        m_highlight = (m_highlight >= 0 && !m_mouseHighlight) ?
                            wrapIndex(m_highlight + 1) :
                            0;
                        m_mouseHighlight = false;
                        PlayHighlightSound();
                    }
                    UpdateHoverColours();
                }
            }

            // Selection controls
            if (Screen.Mouse.Buttons[MouseButton.Left].Pressed)
            {
                Screen.InputMethod = InputMethod.Mouse;
                if (m_mouseHighlight && m_highlight >= 0)
                {
                    Press(m_highlight);
                    PlaySelectSound();
                }
            }
            if (!MouseOnly)
            {
                if (Screen.CheckSelect() && m_highlight >= 0)
                {
                    Press(m_highlight);
                    PlaySelectSound();
                }
            }
        }

        protected override void OnDraw()
        {
            // Draw background
            if (m_showBackground)
            {
                Screen.Effect.Colour = Vector4.One;
                Screen.Effect.Texture = m_backgroundTexture;
                Screen.Effect.Bind();
                m_backgroundGeometry.Draw();
            }

            // Draw text
            for (int i = 0; i < m_options.Length; ++i)
            {
                var option = m_options[i];
                option.Draw();
            }
        }

        protected override void OnRebuild()
        {
            // Rebuild text
            for (int i = 0; i < m_options.Length; ++i)
            {
                var option = m_options[i];
                option.RequestRebuild();
            }

            // Rebuild background
            m_backgroundGeometry.Clear();
            for (int i = 0; i < m_options.Length; ++i)
            {
                var option = m_options[i];
                float startX = (m_direction == MenuDirection.Vertical) ? 0.0f : (option.LocalPosition.X - LocalPosition.X - Margin);
                float width = (m_direction == MenuDirection.Vertical) ? m_width : (option.Width + 2.0f * Margin);
                float startY = (m_direction == MenuDirection.Vertical) ? (option.LocalPosition.Y - LocalPosition.Y) : 0.0f;
                float height = option.Height;
                if (m_direction == MenuDirection.Vertical && m_alignment == TextAlignment.Center)
                {
                    startX -= 0.5f * width;
                }
                else if (m_direction == MenuDirection.Vertical && m_alignment == TextAlignment.Right)
                {
                    startX -= width;
                }

                var origin = Position;
                var edgeWidth = 0.5f * (float)(m_backgroundTexture.Width / 4);
                var edgeHeight = 0.5f * (float)(m_backgroundTexture.Height / 4);
                m_backgroundGeometry.Add2DNineSlice(origin + new Vector2(startX, startY), origin + new Vector2(startX + width, startY + height), edgeWidth, edgeHeight, edgeWidth, edgeHeight);
            }
            m_backgroundGeometry.Rebuild();
        }

        public int TestMouse()
        {
            var position = Position;
            for (int line = 0; line < m_options.Length; ++line)
            {
                var option = m_options[line];
                float startX = (m_direction == MenuDirection.Vertical) ? 0.0f : (option.LocalPosition.X - LocalPosition.X - Margin);
                float width = (m_direction == MenuDirection.Vertical) ? m_width : (option.Width + 2.0f * Margin);
                float startY = (m_direction == MenuDirection.Vertical) ? (option.LocalPosition.Y - LocalPosition.Y) : 0.0f;
                float height = option.Height;
                if (m_direction == MenuDirection.Vertical && m_alignment == TextAlignment.Center)
                {
                    startX -= 0.5f * width;
                }
                else if (m_direction == MenuDirection.Vertical && m_alignment == TextAlignment.Right)
                {
                    startX -= width;
                }

                var mouseLocal = Screen.MousePosition - Position;
                if (mouseLocal.X >= startX && mouseLocal.X < startX + width &&
                    mouseLocal.Y >= startY && mouseLocal.Y < startY + height)
                {
                    return line;
                }
            }
            return -1;
        }

        private void PlayHighlightSound()
        {
            Screen.Audio.PlaySound("sound/menu_highlight.wav");
        }

        private void PlaySelectSound()
        {
            Screen.Audio.PlaySound("sound/menu_select.wav");
        }
    }
}

