
using Dan200.Core.Render;
using OpenTK;

namespace Dan200.Game.Level
{
    public class TilePreview : Entity
    {
        private Level m_subLevel;
        private bool m_visible;

        public Tile Tile
        {
            get
            {
                return m_subLevel.Tiles[m_subLevel.Tiles.Origin];
            }
        }

        public FlatDirection Direction
        {
            get
            {
                return Tile.GetDirection(m_subLevel, m_subLevel.Tiles.Origin);
            }
        }

        public TileCoordinates Location
        {
            get
            {
                return m_subLevel.Tiles.Origin;
            }
            set
            {
                if (m_subLevel.Tiles.Origin != value)
                {
                    Unhide();
                    m_subLevel.Tiles.Origin = value;
                    Hide();
                }
            }
        }

        public override Matrix4 Transform
        {
            get
            {
                return Tile.BuildTransform(Location, Direction);
            }
        }

        public bool Visible
        {
            get
            {
                return m_visible;
            }
            set
            {
                if (m_visible != value)
                {
                    Unhide();
                    m_visible = value;
                    Hide();
                }
            }
        }

        public TilePreview()
        {
            m_subLevel = new Level(0, 0, 0, 1, 1, 1);
            m_subLevel.Info.InEditor = true;
            m_visible = false;
        }

        public void SetTile(Tile tile, FlatDirection direction)
        {
            if (tile != Tile || direction != Direction)
            {
                Unhide();
                m_subLevel.Tiles.SetTile(
                    m_subLevel.Tiles.Origin,
                    tile,
                    direction,
                    false
                );
                Hide();
            }
        }

        protected override void OnInit()
        {
            Hide();
        }

        protected override void OnShutdown()
        {
            Unhide();
            m_subLevel.Dispose();
            m_subLevel = null;
        }

        protected override void OnUpdate(float dt)
        {
            m_subLevel.Update(dt);
        }

        public override bool NeedsRenderPass(RenderPass pass)
        {
            return true;
        }

        protected override void OnDraw(ModelEffectInstance modelEffect, RenderPass pass)
        {
            if (m_visible)
            {
                m_subLevel.DepthSortEntities(modelEffect.CameraPosition);
                m_subLevel.Draw(modelEffect, pass);
            }
        }

        protected override void OnDrawShadows(ShadowEffectInstance shadowEffect)
        {
            if (m_visible)
            {
                m_subLevel.DrawShadows(shadowEffect);
            }
        }

        private void Unhide()
        {
            if (Level != null)
            {
                var location = Location;
                for (int i = 0; i < Tile.Height; ++i)
                {
                    var above = location.Move(Dan200.Game.Level.Direction.Up, i);
                    Level.Tiles[above].SetHidden(Level, above, false);
                }
            }
        }

        public void Hide()
        {
            if (Level != null)
            {
                var location = Location;
                for (int i = 0; i < Tile.Height; ++i)
                {
                    var above = location.Move(Dan200.Game.Level.Direction.Up, i);
                    Level.Tiles[above].SetHidden(Level, above, m_visible);
                }
            }
        }
    }
}
