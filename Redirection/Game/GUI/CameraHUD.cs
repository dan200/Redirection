using Dan200.Core.Audio;
using Dan200.Core.GUI;
using Dan200.Core.Render;
using OpenTK;

namespace Dan200.Game.GUI
{
    public class CameraHUD : Element
    {
        private const float FLASH_FALLOFF = 0.96f;

        private Texture m_texture;
        private Geometry m_geometry;
        private Geometry m_flashGeometry;

        private float m_flashAlpha;

        public bool ShowViewfinder;

        public CameraHUD()
        {
            m_texture = Texture.Get("gui/camera.png", false);
            m_geometry = new Geometry(Primitive.Triangles, 4 * 4, 6 * 4);
            m_flashGeometry = new Geometry(Primitive.Triangles, 4, 6);
            ShowViewfinder = true;
        }

        public override void Dispose()
        {
            base.Dispose();
            m_geometry.Dispose();
            m_flashGeometry.Dispose();
        }

        public void Flash()
        {
            m_flashAlpha = 1.0f;
            Screen.Audio.PlaySound("sound/camera.wav");
        }

        protected override void OnInit()
        {
            m_flashAlpha = 0.0f;
        }

        protected override void OnUpdate(float dt)
        {
            m_flashAlpha *= FLASH_FALLOFF;
        }

        protected override void OnRebuild()
        {
            // The viewfinder
            float halfWidth = m_texture.Width / 2;
            float halfHeight = m_texture.Height / 2;
            float h = Screen.Height;
            float w = Screen.Width;
            float x = (Screen.Width - w) * 0.5f;
            float y = (Screen.Height - h) * 0.5f;

            m_geometry.Clear();
            m_geometry.Add2DQuad(
                new Vector2(x, y),
                new Vector2(x + halfWidth, y + halfHeight),
                new Quad(0.0f, 0.0f, 0.5f, 0.5f)
            );
            m_geometry.Add2DQuad(
                new Vector2(x + w - halfWidth, y),
                new Vector2(x + w, y + halfHeight),
                new Quad(0.5f, 0.0f, 0.5f, 0.5f)
            );
            m_geometry.Add2DQuad(
                new Vector2(x, y + h - halfHeight),
                new Vector2(x + halfWidth, y + h),
                new Quad(0.0f, 0.5f, 0.5f, 0.5f)
            );
            m_geometry.Add2DQuad(
                new Vector2(x + w - halfWidth, y + h - halfHeight),
                new Vector2(x + w, y + h),
                new Quad(0.5f, 0.5f, 0.5f, 0.5f)
            );
            m_geometry.Rebuild();

            // The flash
            m_flashGeometry.Clear();
            m_flashGeometry.Add2DQuad(
                new Vector2(0.0f, 0.0f),
                new Vector2(Screen.Width, Screen.Height)
            );
            m_flashGeometry.Rebuild();
        }

        protected override void OnDraw()
        {
            if (ShowViewfinder)
            {
                // The viewfinder
                Screen.Effect.Texture = m_texture;
                Screen.Effect.Colour = Vector4.One;
                Screen.Effect.Bind();
                m_geometry.Draw();
            }

            if (m_flashAlpha >= 0.001f)
            {
                // The flash
                Screen.Effect.Texture = Texture.White;
                Screen.Effect.Colour = new Vector4(1.0f, 1.0f, 1.0f, m_flashAlpha);
                Screen.Effect.Bind();
                m_flashGeometry.Draw();
            }
        }
    }
}

