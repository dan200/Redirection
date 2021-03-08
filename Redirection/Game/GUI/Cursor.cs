using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Render;
using Dan200.Game.Input;
using OpenTK;
using System;


namespace Dan200.Game.GUI
{
    public class Cursor : Element
    {
        public const float SIZE = 32.0f;

        private Texture m_texture;
        private Geometry m_geometry;
        private float m_timer;
        private int m_frame;

        public Cursor()
        {
            m_texture = Texture.Get("gui/crosshair.png", false);
            m_geometry = new Geometry(Primitive.Triangles, 4, 6);
            m_timer = 0.0f;
            m_frame = 0;
        }

        public override void Dispose()
        {
            base.Dispose();
            Screen.Cursor = null;

            m_geometry.Dispose();
            m_geometry = null;
        }

        protected override void OnInit()
        {
            Screen.Cursor = this;
            if (Screen.InputMethod == InputMethod.Mouse || Screen.InputMethod == InputMethod.Keyboard)
            {
                LocalPosition = Screen.MousePosition;
            }
            else
            {
                LocalPosition = new Vector2(Screen.Width * 0.5f, Screen.Height * 0.5f);
            }
        }

        protected override void OnUpdate(float dt)
        {
            m_timer += dt;
            int frame = ((int)(m_timer / 0.5f)) % 2;
            if (frame != m_frame)
            {
                m_frame = frame;
                RequestRebuild();
            }
            if (Screen.Mouse.DX != 0 || Screen.Mouse.DY != 0)
            {
                Screen.InputMethod = InputMethod.Mouse;
                LocalPosition = Screen.MousePosition;
                RequestRebuild();
            }
            if (Screen.Gamepad != null)
            {
                var joystick = Screen.Gamepad.Joysticks[GamepadJoystick.Left];
                if (joystick.X != 0.0f || joystick.Y != 0.0f)
                {
                    Screen.InputMethod = InputMethod.Gamepad;
                    var position = LocalPosition;
                    position += 1.2f * Screen.Height * new Vector2(
                        joystick.X,
                        joystick.Y
                    ) * dt;
                    position.X = Math.Min(Math.Max(position.X, 0.0f), Screen.Width);
                    position.Y = Math.Min(Math.Max(position.Y, 0.0f), Screen.Height);
                    LocalPosition = position;
                }
            }
            if (Screen.SteamController != null)
            {
                var joystick = Screen.SteamController.Joysticks[SteamControllerJoystick.InGameCursor.GetID()];
                if (joystick.X != 0.0f || joystick.Y != 0.0f)
                {
                    Screen.InputMethod = InputMethod.SteamController;
                    var position = LocalPosition;
                    position += new Vector2(
                        joystick.X,
                        joystick.Y
                    );
                    position.X = Math.Min(Math.Max(position.X, 0.0f), Screen.Width);
                    position.Y = Math.Min(Math.Max(position.Y, 0.0f), Screen.Height);
                    LocalPosition = position;
                }
            }
        }

        protected override void OnDraw()
        {
            if (Screen.InputMethod == InputMethod.Gamepad || Screen.InputMethod == InputMethod.SteamController)
            {
                Screen.Effect.Colour = Vector4.One;
                Screen.Effect.Texture = m_texture;
                Screen.Effect.Bind();
                m_geometry.Draw();
            }
        }

        private void RoundToPixel(ref Vector2 io_center, ref Vector2 io_size)
        {
            int pixelScale = Math.Max(Screen.PixelHeight / (int)Screen.Height, 1);
            int px = (int)((io_center.X * (float)Screen.PixelWidth) / Screen.Width);
            int py = (int)((io_center.Y * (float)Screen.PixelHeight) / Screen.Height);
            int sx = (int)io_size.X * pixelScale;
            int sy = (int)io_size.Y * pixelScale;
            io_center = new Vector2(
                ((float)px * Screen.Width) / (float)Screen.PixelWidth,
                ((float)py * Screen.Height) / (float)Screen.PixelHeight
            );
            io_size = new Vector2(
                ((float)sx * Screen.Width) / (float)Screen.PixelWidth,
                ((float)sy * Screen.Height) / (float)Screen.PixelHeight
            );
        }

        protected override void OnRebuild()
        {
            // Rebuild self
            var pos = Position;
            var size = new Vector2(SIZE, SIZE);
            RoundToPixel(ref pos, ref size);
            m_geometry.Clear();
            m_geometry.Add2DQuad(
                pos - 0.5f * size, pos + 0.5f * size,
                new Quad(0.5f * m_frame, 0.0f, 0.5f, 0.5f),
                Vector4.One
            );
            m_geometry.Rebuild();
        }
    }
}
