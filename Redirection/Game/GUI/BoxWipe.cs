using Dan200.Core.GUI;
using Dan200.Core.Render;
using Dan200.Core.Util;
using OpenTK;

namespace Dan200.Game.GUI
{
    public class BoxWipe : Element
    {
        private Geometry m_geometry;
        private float m_coverage;

        public float Coverage
        {
            get
            {
                return m_coverage;
            }
            set
            {
                if (m_coverage != value)
                {
                    m_coverage = value;
                    RequestRebuild();
                }
            }
        }

        public BoxWipe()
        {
            m_geometry = new Geometry(Primitive.Triangles, 4 * 4, 6 * 4);
        }

        public override void Dispose()
        {
            base.Dispose();
            m_geometry.Dispose();
        }

        protected override void OnInit()
        {
        }

        protected override void OnUpdate(float dt)
        {
        }

        protected override void OnRebuild()
        {
            float coverage = MathUtils.Ease(m_coverage);
            m_geometry.Clear();
            // top
            m_geometry.Add2DQuad(
                Vector2.Zero,
                new Vector2(Screen.Width, coverage * 0.5f * Screen.Height)
            );
            // left
            m_geometry.Add2DQuad(
                Vector2.Zero,
                new Vector2(coverage * 0.5f * Screen.Width, Screen.Height)
            );
            // right
            m_geometry.Add2DQuad(
                new Vector2(Screen.Width - coverage * 0.5f * Screen.Width, 0.0f),
                new Vector2(Screen.Width, Screen.Height)
            );
            // bottom
            m_geometry.Add2DQuad(
                new Vector2(0.0f, Screen.Height - coverage * 0.5f * Screen.Height),
                new Vector2(Screen.Width, Screen.Height)
            );
            m_geometry.Rebuild();
        }

        protected override void OnDraw()
        {
            Screen.Effect.Texture = Texture.Black;
            Screen.Effect.Colour = Vector4.One;
            Screen.Effect.Bind();
            m_geometry.Draw();
        }
    }
}

