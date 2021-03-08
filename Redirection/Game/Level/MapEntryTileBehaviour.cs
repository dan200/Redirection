using Dan200.Core.Assets;

namespace Dan200.Game.Level
{
    [TileBehaviour("map_entry")]
    public class MapEntryTileBehaviour : TileBehaviour
    {
        public MapEntryTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
        }
    }
}
