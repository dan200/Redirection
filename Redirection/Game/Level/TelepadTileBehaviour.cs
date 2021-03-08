using Dan200.Core.Assets;

namespace Dan200.Game.Level
{
    [TileBehaviour(name: "telepad")]
    public class TelepadTileBehaviour : TileBehaviour
    {
        public readonly string Colour;
        public readonly string Animation;
        public readonly string PFX;

        public TelepadTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
            Colour = kvp.GetString("colour", "green");
            Animation = kvp.GetString("animation", null);
            PFX = kvp.GetString("pfx", null);
        }

        public override Entity CreateEntity(ILevel level, TileCoordinates coordinates)
        {
            return new Telepad(Tile, coordinates);
        }

        public override bool ShouldRenderModelGroup(ILevel level, TileCoordinates coordinates, string groupName)
        {
            return false;
        }

        public override bool ShouldRenderGroupShadows(ILevel level, TileCoordinates coordinates, string groupName)
        {
            return false;
        }

        public TileCoordinates? GetDestination(ILevel level, TileCoordinates coordinates)
        {
            TileCoordinates? coords = level.Telepads.GetMatchingTelepad(Colour, coordinates);
            if (coords.HasValue)
            {
                var matchingTile = level.Tiles[coords.Value];
                var baseCords = matchingTile.GetBase(level, coords.Value);
                var baseTile = level.Tiles[baseCords];
                var aboveCoords = baseCords.Move(Direction.Up, baseTile.Height);
                if (!level.Tiles[aboveCoords].IsOccupied(level, aboveCoords))
                {
                    return aboveCoords;
                }
            }
            return null;
        }
    }
}
