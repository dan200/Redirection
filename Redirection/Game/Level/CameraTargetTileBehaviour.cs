using Dan200.Core.Assets;

namespace Dan200.Game.Level
{
    [TileBehaviour(name: "camera_target")]
    public class CameraTargetTileBehaviour : TileBehaviour
    {
        public CameraTargetTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
        }
    }
}
