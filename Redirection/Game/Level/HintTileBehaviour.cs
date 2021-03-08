using Dan200.Core.Assets;

namespace Dan200.Game.Level
{
    [TileBehaviour(name: "hint")]
    public class HintTileBehaviour : TileBehaviour
    {
        public HintTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
        }

        public override void OnInit(ILevel level, TileCoordinates coordinates)
        {
            level.Hints.AddHint(coordinates);
        }

        public override void OnShutdown(ILevel level, TileCoordinates coordinates)
        {
            level.Hints.RemoveHint(coordinates);
        }
    }
}
