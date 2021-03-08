using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.Input;
using OpenTK;
using System;

namespace Dan200.Game.Game
{
    public class InGameCameraController : ICameraController
    {
        private Game m_game;

        public float Pitch;
        public float Yaw;
        public float Distance;
        public float TargetDistance;
        public Vector3 Focus;

        public bool AllowUserRotate;
        public bool AllowUserPan;
        public bool AllowUserZoom;
        public bool AllowUserInvert;

        private Quad m_bounds;

        public Quad Bounds
        {
            get
            {
                return m_bounds;
            }
            set
            {
                m_bounds = value;
                ApplyBounds();
            }
        }

        public InGameCameraController(Game game)
        {
            m_game = game;

            Pitch = 60.0f;
            Yaw = 270.0f;
            Distance = 18.0f;

            TargetDistance = Distance;
            Focus = Vector3.Zero;
            Bounds = new Quad(-5.0f, -5.0f, 10.0f, 10.0f);
        }

        public void Update(float dt)
        {
            var dialog = m_game.Screen.ModalDialog;
            var allowButtonInput = (dialog == null) || (dialog is DialogBox && !((DialogBox)dialog).BlockInput);

            if (AllowUserRotate)
            {
                // Rotate camera
                if (m_game.Mouse.Buttons[MouseButton.Right].Held)
                {
                    m_game.Screen.InputMethod = InputMethod.Mouse;
                    Yaw += 12.0f * ((float)m_game.Mouse.DX / (float)m_game.Window.Width) * m_game.Camera.FOV;
                    Pitch += 6.0f * ((float)m_game.Mouse.DY / (float)m_game.Window.Height) * m_game.Camera.FOV;
                }
                if (m_game.ActiveSteamController != null)
                {
                    var joystick = m_game.ActiveSteamController.Joysticks[SteamControllerJoystick.InGameCamera.GetID()];
                    var dx = joystick.X;
                    var dy = joystick.Y;
                    if (dx != 0.0f || dy != 0.0f)
                    {
                        m_game.Screen.InputMethod = InputMethod.SteamController;
                        Yaw = Yaw - 2.0f * dx * dt;
                        Pitch = Pitch + 2.0f * dy * dt;
                    }
                }
                if (m_game.ActiveGamepad != null)
                {
                    var joystick = m_game.ActiveGamepad.Joysticks[GamepadJoystick.Right];
                    var dx = joystick.X;
                    var dy = joystick.Y;
                    if (dx != 0.0f || dy != 0.0f)
                    {
                        m_game.Screen.InputMethod = InputMethod.Gamepad;
                        Yaw = Yaw - 2.0f * dx * dt;
                        Pitch = Pitch - 2.0f * dy * dt;
                    }
                }

                // Clamp to allowed limits
                if (AllowUserInvert)
                {
                    Pitch = Math.Min(Math.Max(
                        Pitch,
                        MathHelper.DegreesToRadians(-85.0f)),
                        MathHelper.DegreesToRadians(85.0f)
                    );
                }
                else
                {
                    Pitch = Math.Min(Math.Max(
                        Pitch,
                        MathHelper.DegreesToRadians(30.0f)),
                        MathHelper.DegreesToRadians(85.0f)
                    );
                }
            }

            if (AllowUserZoom)
            {
                // Zoom camera
                if (m_game.Mouse.Wheel != 0)
                {
                    m_game.Screen.InputMethod = InputMethod.Mouse;
                    TargetDistance -= m_game.Mouse.Wheel * 2.0f;
                }
                if (allowButtonInput && m_game.ActiveGamepad != null)
                {
                    if (m_game.ActiveGamepad.Buttons[GamepadButton.Down].Held)
                    {
                        m_game.Screen.InputMethod = InputMethod.Gamepad;
                        TargetDistance += 16.0f * dt;
                    }
                    if (m_game.ActiveGamepad.Buttons[GamepadButton.Up].Held)
                    {
                        m_game.Screen.InputMethod = InputMethod.Gamepad;
                        TargetDistance -= 16.0f * dt;
                    }
                }

                // Clamp to allowed limits
                TargetDistance = MathUtils.Clamp(TargetDistance, 12.0f, 30.0f);
            }
            if (Distance < TargetDistance)
            {
                Distance = Math.Min(Distance + 48.0f * dt, TargetDistance);
            }
            else if (Distance > TargetDistance)
            {
                Distance = Math.Max(Distance - 48.0f * dt, TargetDistance);
            }

            if (AllowUserPan)
            {
                // Scrolling
                var margin = 10.0f;
                var autoScrollSpeed = 10.0f;
                var forward = new Vector3(
                    -(float)Math.Cos(Yaw),
                    0.0f,
                    -(float)Math.Sin(Yaw)
                );
                var right = new Vector3(
                    (float)Math.Sin(Yaw),
                    0.0f,
                    -(float)Math.Cos(Yaw)
                );
                var scroll = Vector3.Zero;
                var distance = margin;
                if (m_game.Cursor.Position.X < margin)
                {
                    scroll -= right;
                    distance = Math.Min(m_game.Cursor.Position.X, margin);
                }
                else if (m_game.Cursor.Position.X >= m_game.Screen.Width - margin)
                {
                    scroll += right;
                    distance = Math.Min(m_game.Screen.Width - m_game.Cursor.Position.X, margin);
                }
                if (m_game.Cursor.Position.Y < margin)
                {
                    scroll += forward;
                    distance = Math.Min(m_game.Cursor.Position.Y, margin);
                }
                else if (m_game.Cursor.Position.Y >= m_game.Screen.Height - margin)
                {
                    scroll -= forward;
                    distance = Math.Min(m_game.Screen.Height - m_game.Cursor.Position.Y, margin);
                }
                if (scroll.LengthSquared > 0.0f)
                {
                    scroll.Normalize();
                    Focus += scroll * dt * (1.0f - (distance / margin)) * autoScrollSpeed;
                    ApplyBounds();
                }
            }
        }

        private void ApplyBounds()
        {
            Focus.X = Math.Max(Focus.X, Bounds.X);
            Focus.X = Math.Min(Focus.X, Bounds.X + Bounds.Width);
            Focus.Z = Math.Max(Focus.Z, Bounds.Y);
            Focus.Z = Math.Min(Focus.Z, Bounds.Y + Bounds.Height);
        }

        public void Populate(Camera camera)
        {
            // Setup camera transform
            Vector3 direction = new Vector3(
                -(float)Math.Cos(Pitch) * (float)Math.Cos(Yaw),
                -(float)Math.Sin(Pitch),
                -(float)Math.Cos(Pitch) * (float)Math.Sin(Yaw)
            );
            direction.Normalize();

            camera.Transform = Matrix4.LookAt(
                Focus - direction * Distance,
                Focus,
                Vector3.UnitY
            );
            camera.FOV = Game.DEFAULT_FOV;
        }
    }
}

