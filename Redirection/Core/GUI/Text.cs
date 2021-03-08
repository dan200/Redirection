
using Dan200.Core.Assets;
using Dan200.Core.Render;
using OpenTK;

namespace Dan200.Core.GUI
{
    public enum TextAlignment
    {
        Left,
        Center,
        Right
    }

    public enum TextStyle
    {
        Default,
        UpperCase,
        LowerCase,
    }

    public static class TextStyleExtensions
    {
        public static string Apply(this TextStyle style, string text, Language language)
        {
            switch (style)
            {
                case TextStyle.UpperCase:
                    {
                        return text.ToUpper(language.Culture);
                    }
                case TextStyle.LowerCase:
                    {
                        return text.ToLower(language.Culture);
                    }
                default:
                    {
                        return text;
                    }
            }
        }
    }

    public class Text : Element
    {
        private Font m_font;
        private Vector4 m_colour;
        private TextAlignment m_alignment;
        private TextStyle m_style;
        private bool m_parseImages;

        private Geometry[] m_geometry;
        private Texture[] m_textures;
        private string m_string;

        private float m_scale;

        public string String
        {
            get
            {
                return m_string;
            }
            set
            {
                if (m_string != value)
                {
                    m_string = value;
                    RequestRebuild();
                }
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
                if (m_style != value)
                {
                    m_style = value;
                    RequestRebuild();
                }
            }
        }

        public Vector4 Colour
        {
            get
            {
                return m_colour;
            }
            set
            {
                m_colour = value;
            }
        }

        public TextAlignment Alignment
        {
            get
            {
                return m_alignment;
            }
            set
            {
                if (m_alignment != value)
                {
                    m_alignment = value;
                    RequestRebuild();
                }
            }
        }

        public bool ParseImages
        {
            get
            {
                return m_parseImages;
            }
            set
            {
                if (m_parseImages != value)
                {
                    m_parseImages = value;
                    RequestRebuild();
                }
            }
        }

        public float Width
        {
            get
            {
                if (Screen != null)
                {
                    var styledString = m_style.Apply(m_string, Screen.Language);
                    return m_font.Measure(styledString, m_parseImages) * m_scale;
                }
                else
                {
                    return m_font.Measure(m_string, m_parseImages) * m_scale;
                }
            }
        }

        public float Height
        {
            get
            {
                return m_font.Height * m_scale;
            }
        }

        public Font Font
        {
            get
            {
                return m_font;
            }
        }

        public float Scale
        {
            get
            {
                return m_scale;
            }
            set
            {
                if (m_scale != value)
                {
                    m_scale = value;
                    RequestRebuild();
                }
            }
        }

        public Text(Font font, string text, Vector4 colour, TextAlignment alignment)
        {
            m_font = font;
            m_geometry = new Geometry[m_font.PageCount + 4];
            m_textures = new Texture[m_geometry.Length];
            for (int i = 0; i < m_geometry.Length; ++i)
            {
                m_geometry[i] = new Geometry(Primitive.Triangles, text.Length * 4, text.Length * 6);
                m_textures[i] = null;
            }
            m_string = text;
            m_style = TextStyle.Default;
            m_colour = colour;
            m_alignment = alignment;
            m_parseImages = true;
            m_scale = 1.0f;
        }

        public override void Dispose()
        {
            base.Dispose();
            for (int i = 0; i < m_geometry.Length; ++i)
            {
                m_geometry[i].Dispose();
            }
            m_geometry = null;
            m_textures = null;
        }

        protected override void OnInit()
        {
        }

        protected override void OnUpdate(float dt)
        {
        }

        protected override void OnDraw()
        {
            for (int i = 0; i < m_geometry.Length; ++i)
            {
                if (m_geometry[i].IndexCount > 0 && m_textures[i] != null)
                {
                    Screen.Effect.Colour = (i < m_font.PageCount) ? m_colour : Vector4.One;
                    Screen.Effect.Texture = m_textures[i];
                    Screen.Effect.Bind();
                    m_geometry[i].Draw();
                }
            }
        }

        protected override void OnRebuild()
        {
            var styledString = m_style.Apply(m_string, Screen.Language);
            float startX = Position.X;
            float startY = Position.Y;
            switch (m_alignment)
            {
                case TextAlignment.Center:
                    {
                        startX -= 0.5f * m_font.Measure(styledString, m_parseImages) * m_scale;
                        break;
                    }
                case TextAlignment.Right:
                    {
                        startX -= m_font.Measure(styledString, m_parseImages) * m_scale;
                        break;
                    }
            }
            m_font.Render(styledString, startX, startY, m_geometry, m_textures, m_parseImages, m_scale);
        }
    }
}
