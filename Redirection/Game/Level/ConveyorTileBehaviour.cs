using Dan200.Core.Assets;

namespace Dan200.Game.Level
{
    public enum ConveyorMode
    {
        Stopped,
        Forwards,
        Reverse
    }

    [TileBehaviour(name: "conveyor")]
    public class ConveyorTileBehaviour : TileBehaviour
    {
        public readonly ConveyorMode PoweredMode;
        public readonly ConveyorMode UnpoweredMode;
        public readonly string Animation;

        public ConveyorTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
            PoweredMode = kvp.GetEnum("powered_mode", ConveyorMode.Stopped);
            UnpoweredMode = kvp.GetEnum("unpowered_mode", ConveyorMode.Forwards);
            Animation = kvp.GetString("animation", null);
        }

        public override Entity CreateEntity(ILevel level, TileCoordinates coordinates)
        {
            return new Conveyor(level.Tiles[coordinates], coordinates);
        }

        public override bool ShouldRenderModelGroup(ILevel level, TileCoordinates coordinates, string groupName)
        {
            return false;
        }

        public override bool ShouldRenderGroupShadows(ILevel level, TileCoordinates coordinates, string groupName)
        {
            return false;
        }

        public override bool AcceptsPower(ILevel level, TileCoordinates coordinates, Direction direction)
        {
            return true;
        }
    }
}
