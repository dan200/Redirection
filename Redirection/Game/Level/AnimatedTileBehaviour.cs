using Dan200.Core.Assets;
using Dan200.Core.Render;

namespace Dan200.Game.Level
{
    [TileBehaviour(name: "animated")]
    public class AnimatedTileBehaviour : TileBehaviour
    {
        public readonly string Animation;
        public readonly string PoweredAnimation;
        public readonly string PFX;
        public readonly string PoweredPFX;
        public readonly string PoweredModel;
        public readonly string AltPoweredModel;

        public AnimatedTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
            Animation = kvp.GetString("animation", null);
            PoweredAnimation = kvp.GetString("powered_animation", Animation);
            PFX = kvp.GetString("pfx", null);
            PoweredPFX = kvp.GetString("powered_pfx", PFX);
            PoweredModel = kvp.GetString("powered_model", tile.ModelPath);
            AltPoweredModel = kvp.GetString("alt_powered_model", PoweredModel);
        }

        public override Entity CreateEntity(ILevel level, TileCoordinates coordinates)
        {
            return new AnimatedTile(level.Tiles[coordinates], coordinates);
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

        public Model GetPoweredModel(ILevel level, TileCoordinates coordinates)
        {
            if (level.InEditor && Tile.EditorModel != null)
            {
                return Tile.EditorModel;
            }
            else
            {
                if (((coordinates.X + coordinates.Z) & 0x1) == 1)
                {
                    return Model.Get(AltPoweredModel);
                }
                else
                {
                    return Model.Get(PoweredModel);
                }
            }
        }
    }
}
