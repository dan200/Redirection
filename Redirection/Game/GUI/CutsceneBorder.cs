using Dan200.Core.GUI;
using Dan200.Core.Render;
using Dan200.Core.Util;
using OpenTK;
using System;

namespace Dan200.Game.GUI
{
    public class CutsceneBorder : Element
    {
        private const float BAR_HEIGHT = 52.0f;
        private const float SHOW_DURATION = 0.5f;

        private Geometry m_geometry;
        private float m_coverage;
        private bool m_show;

        public float BarHeight
        {
            get
            {
                return BAR_HEIGHT;
            }
        }

        public bool Show
        {
            get
            {
                return m_show;
            }
            set
            {
                m_show = value;
            }
        }

        public CutsceneBorder(bool show)
        {
            m_geometry = new Geometry(Primitive.Triangles, 4 * 2, 6 * 2);
            m_show = show;
            m_coverage = show ? 1.0f : 0.0f;
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
            if (m_show)
            {
                if (m_coverage < 1.0f)
                {
                    m_coverage = Math.Min(m_coverage + dt * (1.0f / SHOW_DURATION), 1.0f);
                    RequestRebuild();
                }
            }
            else
            {
                if (m_coverage > 0.0f)
                {
                    m_coverage = Math.Max(m_coverage - dt * (1.0f / SHOW_DURATION), 0.0f);
                    RequestRebuild();
                }
            }
        }

        protected override void OnRebuild()
        {
            m_geometry.Clear();
            if (m_coverage > 0.0f)
            {
                float maxSize = BarHeight;
                float coverage = MathUtils.Ease(m_coverage);
                m_geometry.Add2DQuad(
                    Vector2.Zero,
                    new Vector2(Screen.Width, coverage * maxSize)
                );
                m_geometry.Add2DQuad(
                    new Vector2(0.0f, Screen.Height - coverage * maxSize),
                    new Vector2(Screen.Width, Screen.Height)
                );
            }
            m_geometry.Rebuild();
        }

        protected override void OnDraw()
        {
            if (m_coverage > 0.0f)
            {
                Screen.Effect.Texture = Texture.Black;
                Screen.Effect.Colour = Vector4.One;
                Screen.Effect.Bind();
                m_geometry.Draw();
            }
        }
    }
}

