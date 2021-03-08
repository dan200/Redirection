using Dan200.Core.Assets;

namespace Dan200.Game.Level
{
    public enum FallingTrigger
    {
        Automatic,
        SteppedOn,
        Powered
    }

    [TileBehaviour(name: "falling")]
    public class FallingTileBehaviour : TileBehaviour
    {
        private FallingTrigger m_trigger;
        private string m_sound;

        public FallingTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
            m_trigger = kvp.GetEnum("trigger", FallingTrigger.Automatic);
            m_sound = kvp.GetString("sound", null);
        }

        public override void OnLevelStart(ILevel level, TileCoordinates coordinates)
        {
            if (!level.InEditor)
            {
                if (m_trigger == FallingTrigger.Automatic ||
                    (m_trigger == FallingTrigger.Powered && Tile.IsPowered(level, coordinates)))
                {
                    TryFalling(level, coordinates);
                }
            }
        }

        public override void OnSteppedOff(ILevel level, TileCoordinates coordinates, Robot.Robot robot, FlatDirection direction)
        {
            if (m_trigger == FallingTrigger.SteppedOn)
            {
                TryFalling(level, coordinates);
            }
        }

        public override void OnNeighbourChanged(ILevel level, TileCoordinates coordinates)
        {
            if (!level.InEditor)
            {
                if (m_trigger == FallingTrigger.Automatic ||
                    (m_trigger == FallingTrigger.Powered && Tile.IsPowered(level, coordinates)))
                {
                    TryFalling(level, coordinates);
                }
            }
        }

        public override bool AcceptsPower(ILevel level, TileCoordinates coordinates, Direction direction)
        {
            return true;
        }

        private bool IsSupported(ILevel level, TileCoordinates coordinates)
        {
            var belowCoords = coordinates.Below();
            return level.Tiles[belowCoords].IsSolidOnSide(level, belowCoords, Direction.Up);
        }

        private void TryFalling(ILevel level, TileCoordinates coordinates)
        {
            if (!IsSupported(level, coordinates))
            {
                Fall(level, coordinates);
            }
        }

        private void Fall(ILevel level, TileCoordinates coordinates)
        {
            var direction = Tile.GetDirection(level, coordinates);
            level.Tiles[coordinates].Clear(level, coordinates);
            level.Entities.Add(new FallingTile(Tile, coordinates, direction));
            if (m_sound != null)
            {
                level.Audio.PlaySound(m_sound);
            }
        }
    }
}

