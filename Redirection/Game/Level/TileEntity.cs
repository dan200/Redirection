using Dan200.Core.Render;
using OpenTK;

namespace Dan200.Game.Level
{
    public abstract class TileEntity : Entity
    {
        private TileCoordinates m_location;

        public Tile Tile
        {
            get;
            private set;
        }

        public override Matrix4 Transform
        {
            get
            {
                var direction = Level.Tiles[Location].GetDirection(Level, Location);
                return Tile.BuildTransform(Location, direction);
            }
        }

        public TileCoordinates Location
        {
            get
            {
                return m_location;
            }
            set
            {
                if (m_location != value)
                {
                    m_location = value;
                    OnLocationChanged();
                }
            }
        }

        public TileEntity(Tile tile, TileCoordinates location)
        {
            Tile = tile;
            m_location = location;
        }

        public override bool NeedsRenderPass(RenderPass pass)
        {
            return pass == Tile.RenderPass;
        }

        protected override void OnUpdate(float dt)
        {
        }

        protected virtual void OnLocationChanged()
        {
        }
    }
}

