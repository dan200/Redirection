
using Dan200.Core.Assets;
using Dan200.Core.Render;
using OpenTK;

namespace Dan200.Game.Level
{
    [TileBehaviour(name: "xray")]
    public class XRayTileBehaviour : TileBehaviour
    {
        public XRayTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
        }

        private int Render(ILevel level, TileCoordinates coordinates, Geometry output, TextureAtlas textures, bool allFaces)
        {
            // Draw adjoining faces only
            int facesDrawn = 0;
            var diffuseArea = textures.GetTextureArea("models/tiles/xray.png").Value;
            var specularArea = textures.GetTextureArea("black.png").Value;
            var emissiveArea = textures.GetTextureArea("black.png").Value;
            var normalArea = textures.GetTextureArea("flat.png").Value;
            var center = new Vector3(coordinates.X + 0.5f, coordinates.Y * 0.5f + 0.5f, coordinates.Z + 0.5f);
            for (int i = 0; i < 6; ++i)
            {
                var dir = (Direction)i;
                var neighbour = (dir == Direction.Up) ? coordinates.Move(dir, Tile.Height) : coordinates.Move(dir);
                var neighbourTile = level.Tiles[neighbour];
                if (allFaces ||
                    (!(neighbourTile.GetBehaviour(level, neighbour) is XRayTileBehaviour) &&
                        neighbourTile.IsOpaqueOnSide(level, neighbour, dir.Opposite())))
                {
                    Vector3 fwd = 0.45f * dir.ToVector();
                    if (allFaces)
                    {
                        fwd = -fwd;
                    }

                    Vector3 up, right;
                    if (dir.IsFlat())
                    {
                        up = 0.45f * Vector3.UnitY;
                        right = 0.45f * dir.RotateRight().ToVector();
                    }
                    else if (dir == Direction.Up)
                    {
                        up = 0.45f * Vector3.UnitZ;
                        right = 0.45f * Vector3.UnitX;
                    }
                    else// if( dir == Direction.Down )
                    {
                        up = -0.45f * Vector3.UnitZ;
                        right = 0.45f * Vector3.UnitX;
                    }
                    output.AddQuad(
                        center + fwd + up - right,
                        center + fwd + up + right,
                        center + fwd - up - right,
                        center + fwd - up + right,
                        diffuseArea,
                        specularArea,
                        normalArea,
                        emissiveArea,
                        Vector4.One
                    );
                    ++facesDrawn;
                }
            }
            return facesDrawn;
        }

        public override void OnRender(ILevel level, TileCoordinates coordinates, Geometry output, TextureAtlas textures)
        {
            if (level.InEditor)
            {
                // Draw adjoining faces only
                int facesDrawn = Render(level, coordinates, output, textures, false);

                // If tile is in isolation, draw all tiles
                if (facesDrawn == 0)
                {
                    Render(level, coordinates, output, textures, true);
                }
            }
        }
    }
}
