using Dan200.Core.GUI;
using Dan200.Core.Render;
using OpenTK;
using System;

namespace Dan200.Game.GUI
{
    public class RobotIndicator : EntityIndicator<Robot.Robot>
    {
        private const float SIZE = 32.0f;
        private const float MARGIN = 24.0f;

        private Geometry m_geometry;
        private Texture m_texture;
        private bool m_visible;

        public RobotIndicator(Robot.Robot entity, Camera camera) : base(entity, camera)
        {
            m_geometry = new Geometry(Primitive.Triangles, 4, 6, true);
            m_texture = Texture.Get("gui/arrows.png", true);
            m_visible = false;
        }

        public override void Dispose()
        {
            base.Dispose();
            m_geometry.Dispose();
            m_geometry = null;
        }

        private Vector2 GetCenterPos()
        {
            return CalculatePosition(new Vector3(0.0f, 0.375f, 0.0f));
        }

        protected override void OnRebuild()
        {
            base.OnRebuild();

            // Calculate position
            var screenCentre = new Vector3(Screen.Width * 0.5f, Screen.Height * 0.5f, 0.0f);
            var pos2D = GetCenterPos() - screenCentre.Xy;
            var position = new Vector3(pos2D.X, pos2D.Y, 0.0f);
            var offScreen =
                (Math.Abs(position.X) > Screen.Width * 0.5f + SIZE * 0.5f) ||
                (Math.Abs(position.Y) > Screen.Height * 0.5f + SIZE * 0.5f);

            // Determine visibility
            m_visible = offScreen && !Entity.Immobile && !Entity.IsStopped;
            if (m_visible)
            {
                // Calculate orientation
                var centre = position;
                if (Math.Abs(centre.X) > (Screen.Width * 0.5f) - MARGIN)
                {
                    centre.X *= 1.0f - ((Math.Abs(centre.X) - ((Screen.Width * 0.5f) - MARGIN)) / Math.Abs(centre.X));
                }
                if (Math.Abs(centre.Y) > (Screen.Height * 0.5f) - MARGIN)
                {
                    centre.Y *= 1.0f - ((Math.Abs(centre.Y) - ((Screen.Height * 0.5f) - MARGIN)) / Math.Abs(centre.Y));
                }
                var dir = position - centre;
                if (dir.Length > 0.0f)
                {
                    dir.Normalize();
                }
                else
                {
                    dir = Vector3.UnitY;
                }
                var right = new Vector3(dir.Y, -dir.X, 0.0f);

                // Build geometry
                m_geometry.Clear();
                m_geometry.AddQuad(
                    screenCentre + centre + SIZE * 0.5f * (-right - dir),
                    screenCentre + centre + SIZE * 0.5f * (right - dir),
                    screenCentre + centre + SIZE * 0.5f * (-right + dir),
                    screenCentre + centre + SIZE * 0.5f * (right + dir),
                    new Quad(0.5f, 0.5f, 0.5f, 0.5f),
                    Vector4.One
                );
                m_geometry.Rebuild();
            }
        }

        protected override void OnDraw()
        {
            base.OnDraw();
            if (m_visible)
            {
                Screen.Effect.Colour = new Vector4(Entity.GUIColour, 1.0f);
                Screen.Effect.Texture = m_texture;
                Screen.Effect.Bind();
                m_geometry.Draw();
            }
        }
    }
}
