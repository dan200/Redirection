
using OpenTK;

#if OPENGLES
using OpenTK.Graphics.ES20;
#else
#endif

using Dan200.Core.GUI;

namespace Dan200.Core.Render
{
    public class ScreenEffectInstance : EffectInstance
    {
        private Screen m_screen;
        private ITexture m_texture;
        private Vector4 m_fgColour;

        public ITexture Texture
        {
            get
            {
                return m_texture;
            }
            set
            {
                m_texture = value;
            }
        }

        public Vector4 Colour
        {
            get
            {
                return m_fgColour;
            }
            set
            {
                m_fgColour = value;
            }
        }

        public ScreenEffectInstance(Screen screen) : base("shaders/screen.effect")
        {
            m_screen = screen;
            m_texture = Dan200.Core.Render.Texture.White;
            m_fgColour = Vector4.One;
        }

        public override void Bind()
        {
            base.Bind();
            Set("texture", m_texture, 0);
            Set("fgColour", m_fgColour);
            Set("screenSize", new Vector2(m_screen.Width, m_screen.Height));
        }
    }
}
