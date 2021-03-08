using Dan200.Core.Assets;

namespace Dan200.Game.Level
{
    [TileBehaviour(name: "extension")]
    public class ExtensionTileBehaviour : TileBehaviour
    {
        public ExtensionTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
        }
    }
}

