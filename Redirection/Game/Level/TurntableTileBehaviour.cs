using Dan200.Core.Assets;
using Dan200.Game.Robot;

namespace Dan200.Game.Level
{
    [TileBehaviour(name: "turntable")]
    public class TurntableTileBehaviour : TileBehaviour
    {
        public readonly TurnDirection TurnDirection;
        public readonly string Animation;
        public readonly string Sound;

        public TurntableTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
            TurnDirection = kvp.GetEnum("turn_direction", TurnDirection.Left);
            Animation = kvp.GetString("animation", null);
            Sound = kvp.GetString("sound", null);
        }

        public override Entity CreateEntity(ILevel level, TileCoordinates coordinates)
        {
            return new Turntable(Tile, coordinates);
        }

        public override bool ShouldRenderModelGroup(ILevel level, TileCoordinates coordinates, string groupName)
        {
            return false;
        }

        public override bool ShouldRenderGroupShadows(ILevel level, TileCoordinates coordinates, string groupName)
        {
            return false;
        }
    }
}
