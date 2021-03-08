using Dan200.Core.GUI;
using Dan200.Core.Util;
using Dan200.Game.Level;
using OpenTK;
using System;

namespace Dan200.Game.GUI
{
    public class TileSelect : Element
    {
        private Game.Game m_game;
        private Level.Level m_level;

        public Tile Tile
        {
            get
            {
                return m_level.Tiles[TileCoordinates.Zero];
            }
            set
            {
                m_level.Tiles.SetTile(TileCoordinates.Zero, value, FlatDirection.South, false);
            }
        }

        public TileSelect(Game.Game game, Tile tile)
        {
            m_game = game;
            m_level = new Level.Level(0, 0, 0, 1, 1, 1);
            m_level.Info.InEditor = true;
            m_level.Tiles.SetTile(
                TileCoordinates.Zero,
                tile,
                FlatDirection.South,
                false
            );
        }

        public override void Dispose()
        {
            m_level.Dispose();
        }

        protected override void OnInit()
        {
        }

        protected override void OnUpdate(float dt)
        {
            m_level.Update(dt);
        }

        protected override void OnDraw()
        {
        }

        public void ReloadAssets()
        {
            m_level.Tiles.RequestRebuild();
        }

        private void SetLevelTransform(Level.Level level, Vector2 screenPosition)
        {
            float x = (screenPosition.X / (0.5f * Screen.Width)) - 1.0f;
            float y = (screenPosition.Y / (0.5f * Screen.Height)) - 1.0f;
            Vector3 dirCS = new Vector3(
                (float)(Math.Tan(0.5f * m_game.Camera.FOV)) * (x * m_game.Camera.AspectRatio),
                -(float)(Math.Tan(0.5f * m_game.Camera.FOV)) * y,
                -1.0f
            );
            dirCS.Normalize();

            Matrix4 cameraTransInv = m_game.Camera.Transform;
            MathUtils.FastInvert(ref cameraTransInv);

            var posWS = Vector3.TransformPosition(Vector3.Zero, cameraTransInv);
            var dirWS = Vector3.TransformVector(dirCS, cameraTransInv);

            level.Transform = Matrix4.CreateTranslation(
                posWS + 20.0f * dirWS - new Vector3(0.5f, 0.25f * level.Tiles.Height, 0.5f)
            );
        }

        protected override void OnDraw3D()
        {
            //if( Screen.ModalDialog == this.Parent )
            {
                // Draw the tile
                SetLevelTransform(m_level, Position);
                if (m_game.Sky != null)
                {
                    m_level.Lights.AmbientLight.Colour = m_game.Sky.AmbientColour;
                    m_level.Lights.SkyLight.Active = (m_game.Sky.LightColour.LengthSquared > 0.0f);
                    m_level.Lights.SkyLight.Colour = m_game.Sky.LightColour;
                    m_level.Lights.SkyLight.Direction = m_game.Sky.LightDirection;
                    m_level.Lights.SkyLight2.Active = (m_game.Sky.Light2Colour.LengthSquared > 0.0f);
                    m_level.Lights.SkyLight2.Colour = m_game.Sky.Light2Colour;
                    m_level.Lights.SkyLight2.Direction = m_game.Sky.Light2Direction;
                }
                m_level.Draw(
                    m_game.Camera,
                    drawShadows: false
                );
            }
        }

        protected override void OnRebuild()
        {
        }
    }
}

