using Dan200.Core.Input;
using Dan200.Core.Render;
using OpenTK;
using System;

namespace Dan200.Game.Game
{
    public class DebugCameraController : ICameraController
    {
        private Game m_game;

        public float Pitch;
        public float Yaw;
        public Vector3 Position;

        public DebugCameraController(Game game)
        {
            m_game = game;

            Pitch = MathHelper.DegreesToRadians(0.0f);
            Yaw = MathHelper.DegreesToRadians(0.0f);
            Position = Vector3.Zero;
        }

        public void Update(float dt)
        {
            // Gather input
            float yaw = 0.0f;
            float pitch = 0.0f;
            float up = 0.0f;
            float forward = 0.0f;
            float strafe = 0.0f;
            if (m_game.Keyboard.Keys[Key.Left].Held)
            {
                yaw -= 1.0f;
            }
            if (m_game.Keyboard.Keys[Key.Right].Held)
            {
                yaw += 1.0f;
            }
            if (m_game.ActiveGamepad != null)
            {
                yaw += m_game.ActiveGamepad.Joysticks[GamepadJoystick.Right].X;
            }
            if (m_game.Keyboard.Keys[Key.Down].Held)
            {
                pitch += 1.0f;
            }
            if (m_game.Keyboard.Keys[Key.Up].Held)
            {
                pitch -= 1.0f;
            }
            if (m_game.ActiveGamepad != null)
            {
                pitch += m_game.ActiveGamepad.Joysticks[GamepadJoystick.Right].Y;
            }
            if (m_game.Keyboard.Keys[Key.Q].Held)
            {
                up += 1.0f;
            }
            if (m_game.Keyboard.Keys[Key.E].Held)
            {
                up -= 1.0f;
            }
            if (m_game.ActiveGamepad != null)
            {
                up -= m_game.ActiveGamepad.Axes[GamepadAxis.LeftTrigger].Value;
                up += m_game.ActiveGamepad.Axes[GamepadAxis.RightTrigger].Value;
            }
            if (m_game.Keyboard.Keys[Key.W].Held)
            {
                forward += 1.0f;
            }
            if (m_game.Keyboard.Keys[Key.S].Held)
            {
                forward -= 1.0f;
            }
            if (m_game.ActiveGamepad != null)
            {
                forward -= m_game.ActiveGamepad.Joysticks[GamepadJoystick.Left].Y;
            }
            if (m_game.Keyboard.Keys[Key.D].Held)
            {
                strafe += 1.0f;
            }
            if (m_game.Keyboard.Keys[Key.A].Held)
            {
                strafe -= 1.0f;
            }
            if (m_game.ActiveGamepad != null)
            {
                strafe += m_game.ActiveGamepad.Joysticks[GamepadJoystick.Left].X;
            }

            // Apply input
            Yaw = Yaw + 2.0f * yaw * dt;
            Pitch = Math.Min(Math.Max(
                Pitch + 2.0f * pitch * dt,
                MathHelper.DegreesToRadians(-89.0f)),
                MathHelper.DegreesToRadians(89.0f)
            );
            Position.Y += 3.0f * up * dt;

            Vector3 direction = new Vector3(
                -(float)Math.Cos(Pitch) * (float)Math.Cos(Yaw),
                -(float)Math.Sin(Pitch),
                -(float)Math.Cos(Pitch) * (float)Math.Sin(Yaw)
            );
            direction.Normalize();
            Position += direction * 3.0f * forward * dt;

            Vector3 right = Vector3.Normalize(Vector3.Cross(direction, Vector3.UnitY));
            Position += right * 3.0f * strafe * dt;
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
                Position,
                Position + direction,
                Vector3.UnitY
            );
            camera.FOV = Game.DEFAULT_FOV;
        }
    }
}

