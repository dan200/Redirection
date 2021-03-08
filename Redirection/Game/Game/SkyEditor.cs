using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.Level;
using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Dan200.Game.Game
{
    public class SkyEditor : LevelState
    {
        private Geometry m_debugGeometry;
        private FlatEffectInstance m_debugEffect;

        public SkyEditor( Game game, string levelPath ) : base( game, levelPath, LevelOptions.Editor )
        {
        }

        protected override string GetMusicPath()
        {
            return null;
        }

        protected override void OnInit()
        {
            base.OnInit();
            m_debugGeometry = new Geometry( Primitive.Lines );
            m_debugEffect = new FlatEffectInstance();
        }

        protected override void OnShutdown()
        {
            m_debugGeometry.Dispose();
            base.OnShutdown();
        }

        private Vector2 Rot2D( float x, float y, float angle )
        {
            var ca = (float)Math.Cos(angle);
            var sa = (float)Math.Sin(angle);
            return new Vector2(
                x * ca - y * sa,
                x * sa + y * ca
            );
        }

        private Vector3 RotYaw( Vector3 vector, float angle )
        {
            var xz = Rot2D(vector.X, vector.Z, angle);
            return new Vector3(xz.X, vector.Y, xz.Y);
        }

        private Vector3 RotPitch(Vector3 vector, float angle)
        {
            var xz = new Vector2(vector.X, vector.Z);
            var r = xz.Length;
            var ry = Rot2D(r, vector.Y, angle);
            if (ry.X > 0)
            {
                return new Vector3(
                    (xz.X / r) * ry.X,
                    ry.Y,
                    (xz.Y / r) * ry.X
                );
            }
            else
            {
                return vector;
            }
        }

        private Vector3 AddR(Vector3 colour, float diff)
        {
            colour.X = MathUtils.Clamp(colour.X + diff, 0.0f, 1.0f);
            return colour;
        }

        private Vector3 AddG(Vector3 colour, float diff)
        {
            colour.Y = MathUtils.Clamp(colour.Y + diff, 0.0f, 1.0f);
            return colour;
        }

        private Vector3 AddB(Vector3 colour, float diff)
        {
            colour.Z = MathUtils.Clamp(colour.Z + diff, 0.0f, 1.0f);
            return colour;
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

            if( Game.ActiveGamepad != null )
            {
                var leftX = Game.ActiveGamepad.Joysticks[GamepadJoystick.Left].X;
                var leftY = Game.ActiveGamepad.Joysticks[GamepadJoystick.Left].Y;
                var rightX = Game.ActiveGamepad.Joysticks[GamepadJoystick.Right].X;
                var rightY = Game.ActiveGamepad.Joysticks[GamepadJoystick.Right].Y;

                var yawSpeed = MathHelper.DegreesToRadians(45.0f);
                var pitchSpeed = MathHelper.DegreesToRadians(45.0f);
                Game.Sky.Sky.LightDirection = RotYaw(Game.Sky.Sky.LightDirection, leftX * yawSpeed * dt);
                Game.Sky.Sky.LightDirection = RotPitch(Game.Sky.Sky.LightDirection, leftY * pitchSpeed * dt);
                Game.Sky.Sky.Light2Direction = RotYaw(Game.Sky.Sky.Light2Direction, rightX * yawSpeed * dt);
                Game.Sky.Sky.Light2Direction = RotPitch(Game.Sky.Sky.Light2Direction, rightY * pitchSpeed * dt);

                var rgb = Game.ActiveGamepad.Axes[GamepadAxis.RightTrigger].Value - Game.ActiveGamepad.Axes[GamepadAxis.LeftTrigger].Value;
                var rgbSpeed = 0.25f;
                if (Game.ActiveGamepad.Buttons[GamepadButton.B].Held || Game.ActiveGamepad.Buttons[GamepadButton.Y].Held)
                {
                    Game.Sky.Sky.LightColour = AddR(Game.Sky.Sky.LightColour, rgb * rgbSpeed * dt);
                }
                if (Game.ActiveGamepad.Buttons[GamepadButton.A].Held || Game.ActiveGamepad.Buttons[GamepadButton.Y].Held)
                {
                    Game.Sky.Sky.LightColour = AddG(Game.Sky.Sky.LightColour, rgb * rgbSpeed * dt);
                }
                if (Game.ActiveGamepad.Buttons[GamepadButton.X].Held || Game.ActiveGamepad.Buttons[GamepadButton.Y].Held)
                {
                    Game.Sky.Sky.LightColour = AddB(Game.Sky.Sky.LightColour, rgb * rgbSpeed * dt);
                }

                Game.Sky.ReloadAssets();
            }

            if( Game.Keyboard.Keys[Key.S].Pressed )
            {
                var skyPath = Path.Combine(App.AssetPath, "base/" + Game.Sky.Sky.Path);
                using (var writer = new StreamWriter(skyPath))
                {
                    Game.Sky.Sky.Save(writer);
                    App.Log("Sky saved to " + skyPath);
                }
            }
        }

        protected override void OnDraw()
        {
            base.OnDraw();

            // Rebuild
            m_debugGeometry.Clear();
            m_debugGeometry.AddLine(Vector3.Zero, -Game.Sky.Sky.LightDirection.Normalised() * 10.0f, new Vector4(Game.Sky.Sky.LightColour, 1.0f));
            m_debugGeometry.AddLine(Vector3.Zero, -Game.Sky.Sky.Light2Direction.Normalised() * 10.0f, new Vector4(Game.Sky.Sky.Light2Colour, 1.0f));
            m_debugGeometry.Rebuild();

            // Draw
            m_debugEffect.WorldMatrix = Matrix4.Identity;
            m_debugEffect.ModelMatrix = Matrix4.Identity;
            m_debugEffect.ViewMatrix = Game.Camera.Transform;
            m_debugEffect.ProjectionMatrix = Game.Camera.CreateProjectionMatrix();
            m_debugEffect.Bind();
            m_debugGeometry.Draw();
        }

        public void SaveSky()
        {
            var sky = Level.Info.SkyPath;
        }
    }
}
