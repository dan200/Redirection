
using Dan200.Core.Render;
using OpenTK;

namespace Dan200.Core.GUI
{
    public class Image : Element
    {
        private ITexture m_texture;
        private Quad m_area;
        private Geometry m_geometry;
        private Vector4 m_colour;
        private float m_width;
        private float m_height;
        private bool m_stretch;

        public ITexture Texture
        {
            get
            {
                return m_texture;
            }
            set
            {
                if (m_texture != value)
                {
                    m_texture = value;
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

        public float Width
        {
            get
            {
                return m_width;
            }
            set
            {
                m_width = value;
                RequestRebuild();
            }
        }

        public float Height
        {
            get
            {
                return m_height;
            }
            set
            {
                m_height = value;
                RequestRebuild();
            }
        }

        public Quad Area
        {
            get
            {
                return m_area;
            }
            set
            {
                m_area = value;
                RequestRebuild();
            }
        }

        public bool Stretch
        {
            get
            {
                return m_stretch;
            }
            set
            {
                m_stretch = value;
                RequestRebuild();
            }
        }

        public Image(ITexture texture, float width, float height) : this(texture, Quad.UnitSquare, width, height)
        {
        }

        public Image(ITexture texture, Quad area, float width, float height)
        {
            m_texture = texture;
            m_colour = Vector4.One;
            m_area = area;
            m_geometry = new Geometry(Primitive.Triangles, 6, 4);
            m_width = width;
            m_height = height;
            m_stretch = false;
        }

        public override void Dispose()
        {
            base.Dispose();
            m_geometry.Dispose();
            m_geometry = null;
        }

        protected override void OnInit()
        {
        }

        protected override void OnUpdate(float dt)
        {
        }

        protected override void OnDraw()
        {
            Screen.Effect.Colour = m_colour;
            Screen.Effect.Texture = m_texture;
            Screen.Effect.Bind();
            m_geometry.Draw();
        }

        protected override void OnRebuild()
        {
            // Rebuild self
            m_geometry.Clear();

            Vector2 origin = Position;
            if (m_stretch)
            {
                m_geometry.Add2DQuad(
                    origin,
                    origin + new Vector2(Width, Height),
                    m_area
                );
            }
            else
            {
                float aspect = Width / Height;
                float textureAspect = (m_area.Width * m_texture.Width) / (m_area.Height * m_texture.Height);
                if (textureAspect > aspect)
                {
                    float cutoff = ((textureAspect - aspect) * 0.5f) / textureAspect;
                    m_geometry.Add2DQuad(
                        origin,
                        origin + new Vector2(Width, Height),
                        m_area.Sub(cutoff, 0.0f, 1.0f - 2.0f * cutoff, 1.0f)
                    );
                }
                else if (textureAspect < aspect)
                {
                    float invAspect = 1.0f / aspect;
                    float invTextureAspect = 1.0f / textureAspect;
                    float cutoff = ((invTextureAspect - invAspect) * 0.5f) / invTextureAspect;
                    m_geometry.Add2DQuad(
                        origin,
                        origin + new Vector2(Width, Height),
                        m_area.Sub(0.0f, cutoff, 1.0f, 1.0f - 2.0f * cutoff)
                    );
                }
                else
                {
                    m_geometry.Add2DQuad(
                        origin,
                        origin + new Vector2(Width, Height),
                        m_area
                    );
                }
            }

            m_geometry.Rebuild();
        }
    }
}

