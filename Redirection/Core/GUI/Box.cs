
using Dan200.Core.Render;
using OpenTK;

namespace Dan200.Core.GUI
{
    public class Box : Element
    {
        private Vector4 m_colour;
        private ITexture m_texture;
        private Geometry m_geometry;

        public readonly float Width;
        public readonly float Height;

        protected ITexture Texture
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

        protected Vector4 Colour
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

        public Box(ITexture texture, float width, float height)
        {
            m_colour = Vector4.One;
            m_texture = texture;
            m_geometry = new Geometry(Primitive.Triangles);
            Width = width;
            Height = height;
        }

        public override void Dispose()
        {
            base.Dispose();

            // Dispose self
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
            // Draw self
            Screen.Effect.Colour = m_colour;
            Screen.Effect.Texture = m_texture;
            Screen.Effect.Bind();
            m_geometry.Draw();
        }

        protected override void OnRebuild()
        {
            RebuildBoxGeometry(0.0f, Width, 0.0f, Height);
        }

        protected void ClearBoxGeometry()
        {
            m_geometry.Clear();
            m_geometry.Rebuild();
        }

        protected void RebuildBoxGeometry(float startX, float endX, float startY, float endY)
        {
            m_geometry.Clear();

            Vector2 origin = Position;
            float xEdgeWidth = (float)(m_texture.Width / 4) * 0.5f;
            float yEdgeWidth = (float)(m_texture.Height / 4) * 0.5f;
            m_geometry.Add2DNineSlice(origin + new Vector2(startX, startY), origin + new Vector2(endX, endY), xEdgeWidth, yEdgeWidth, xEdgeWidth, yEdgeWidth);

            m_geometry.Rebuild();
        }
    }
}
