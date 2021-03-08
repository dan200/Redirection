using Dan200.Core.Assets;
using Dan200.Core.Render;
using System;

namespace Dan200.Game.Level
{
    [TileBehaviour(name: "terrain")]
    public class TerrainTileBehaviour : TileBehaviour
    {
        private enum TerrainLayer
        {
            Top,
            Middle,
            Bottom
        }

        [Flags]
        private enum TerrainConnectivity
        {
            Invalid = -1,
            None = 0,

            NorthEast = 1,
            North = 2,
            NorthWest = 4,
            East = 8,
            West = 16,
            SouthEast = 32,
            South = 64,
            SouthWest = 128,
            Up = 256,
            Down = 512,
            NorthEastSouthWest = North | East | South | West,
        }

        private string m_islandModelPath;
        private string m_centerModelPath;
        private string m_cornerModelPath;
        private string m_edgeModelPath;
        private string m_insideCornerModelPath;
        private string m_inside2CornersModelPath;
        private string m_inside2CornersOppModelPath;
        private string m_inside3CornersModelPath;
        private string m_lineModelPath;
        private string m_peninsularModelPath;
        private string m_teeModelPath;
        private string m_crossModelPath;
        private string m_bendModelPath;
        private string m_edgeWithInsideCornerPath;
        private string m_edgeWithInsideCornerOppPath;

        private string m_islandModelPathInv;
        private string m_centerModelPathInv;
        private string m_cornerModelPathInv;
        private string m_edgeModelPathInv;
        private string m_insideCornerModelPathInv;
        private string m_inside2CornersModelPathInv;
        private string m_inside2CornersOppModelPathInv;
        private string m_inside3CornersModelPathInv;
        private string m_lineModelPathInv;
        private string m_peninsularModelPathInv;
        private string m_teeModelPathInv;
        private string m_crossModelPathInv;
        private string m_bendModelPathInv;
        private string m_edgeWithInsideCornerPathInv;
        private string m_edgeWithInsideCornerOppPathInv;

        private string m_wallPath;
        private string m_wallLinePath;
        private string m_wallIslandPath;
        private string m_wallPeninsularPath;
        private string m_wallCornerPath;
        private string m_wallInteriorPath;

        private static string GetPartPath(Tile tile, KeyValuePairs kvp, string partName)
        {
            var def = AssetPath.Combine(
                AssetPath.GetDirectoryName(tile.ModelPath),
                AssetPath.GetFileNameWithoutExtension(tile.ModelPath) + "_" + partName + ".obj"
            );
            return kvp.GetString(partName + "_model", def);
        }

        public TerrainTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
            m_centerModelPath = GetPartPath(tile, kvp, "center");
            m_islandModelPath = GetPartPath(tile, kvp, "island");
            m_cornerModelPath = GetPartPath(tile, kvp, "corner");
            m_edgeModelPath = GetPartPath(tile, kvp, "edge");
            m_insideCornerModelPath = GetPartPath(tile, kvp, "inside_corner");
            m_inside2CornersModelPath = GetPartPath(tile, kvp, "inside_2corners");
            m_inside2CornersOppModelPath = GetPartPath(tile, kvp, "inside_2corners_opp");
            m_inside3CornersModelPath = GetPartPath(tile, kvp, "inside_3corners");
            m_lineModelPath = GetPartPath(tile, kvp, "line");
            m_peninsularModelPath = GetPartPath(tile, kvp, "peninsular");
            m_teeModelPath = GetPartPath(tile, kvp, "tee");
            m_crossModelPath = GetPartPath(tile, kvp, "cross");
            m_bendModelPath = GetPartPath(tile, kvp, "bend");
            m_edgeWithInsideCornerPath = GetPartPath(tile, kvp, "edge_with_inside_corner");
            m_edgeWithInsideCornerOppPath = GetPartPath(tile, kvp, "edge_with_inside_corner_opp");

            m_centerModelPathInv = GetPartPath(tile, kvp, "inv_center");
            m_islandModelPathInv = GetPartPath(tile, kvp, "inv_island");
            m_cornerModelPathInv = GetPartPath(tile, kvp, "inv_corner");
            m_edgeModelPathInv = GetPartPath(tile, kvp, "inv_edge");
            m_insideCornerModelPathInv = GetPartPath(tile, kvp, "inv_inside_corner");
            m_inside2CornersModelPathInv = GetPartPath(tile, kvp, "inv_inside_2corners");
            m_inside2CornersOppModelPathInv = GetPartPath(tile, kvp, "inv_inside_2corners_opp");
            m_inside3CornersModelPathInv = GetPartPath(tile, kvp, "inv_inside_3corners");
            m_lineModelPathInv = GetPartPath(tile, kvp, "inv_line");
            m_peninsularModelPathInv = GetPartPath(tile, kvp, "inv_peninsular");
            m_teeModelPathInv = GetPartPath(tile, kvp, "inv_tee");
            m_crossModelPathInv = GetPartPath(tile, kvp, "inv_cross");
            m_bendModelPathInv = GetPartPath(tile, kvp, "inv_bend");
            m_edgeWithInsideCornerPathInv = GetPartPath(tile, kvp, "inv_edge_with_inside_corner");
            m_edgeWithInsideCornerOppPathInv = GetPartPath(tile, kvp, "inv_edge_with_inside_corner_opp");

            m_wallPath = GetPartPath(tile, kvp, "wall");
            m_wallLinePath = GetPartPath(tile, kvp, "wall_line");
            m_wallIslandPath = GetPartPath(tile, kvp, "wall_island");
            m_wallPeninsularPath = GetPartPath(tile, kvp, "wall_peninsular");
            m_wallCornerPath = GetPartPath(tile, kvp, "wall_corner");
            m_wallInteriorPath = GetPartPath(tile, kvp, "wall_interior");
        }

        public override void OnRender(ILevel level, TileCoordinates coordinates, Geometry output, TextureAtlas textures)
        {
            var models = GetModels(level, coordinates);
            if (models.TopModel != TerrainConnectivity.Invalid)
            {
                RenderModel(level, coordinates, models.TopModel, TerrainLayer.Top, output, textures, models.Connectivity);
            }
            if (models.MiddleModel != TerrainConnectivity.Invalid)
            {
                RenderModel(level, coordinates, models.MiddleModel, TerrainLayer.Middle, output, textures, models.Connectivity);
            }
            if (models.BottomModel != TerrainConnectivity.Invalid)
            {
                RenderModel(level, coordinates, models.BottomModel, TerrainLayer.Bottom, output, textures, models.Connectivity);
            }
        }

        public override void OnRenderShadows(ILevel level, TileCoordinates coordinates, Geometry output)
        {
            var models = GetModels(level, coordinates);
            if (models.TopModel != TerrainConnectivity.Invalid)
            {
                RenderModelShadows(level, coordinates, models.TopModel, TerrainLayer.Top, output, models.Connectivity);
            }
            if (models.MiddleModel != TerrainConnectivity.Invalid)
            {
                RenderModelShadows(level, coordinates, models.MiddleModel, TerrainLayer.Middle, output, models.Connectivity);
            }
            if (models.BottomModel != TerrainConnectivity.Invalid)
            {
                RenderModelShadows(level, coordinates, models.BottomModel, TerrainLayer.Bottom, output, models.Connectivity);
            }
        }

        private static TerrainConnectivity[] s_directionToMask = {
            TerrainConnectivity.North,
            TerrainConnectivity.East,
            TerrainConnectivity.South,
            TerrainConnectivity.West,
            TerrainConnectivity.Up,
            TerrainConnectivity.Down
        };

        private bool IsTileHidden(ILevel level, TileCoordinates coordinates)
        {
            return level.Tiles[coordinates].IsHidden(level, coordinates);
        }

        private void RenderModel(ILevel level, TileCoordinates coordinates, TerrainConnectivity modelConnectivity, TerrainLayer layer, Geometry output, TextureAtlas textures, TerrainConnectivity actualConnectivity)
        {
            Model model;
            FlatDirection direction;
            GetModelAndDirection(modelConnectivity, layer, level.RandomSeed, coordinates, out model, out direction);
            if (model != null)
            {
                var hiddenFaces = modelConnectivity & actualConnectivity;
                var leftDir = direction.RotateLeft().ToDirection();
                var rightDir = direction.RotateRight().ToDirection();
                var frontDir = direction.ToDirection();
                var backDir = direction.Opposite().ToDirection();
                RenderModel(
                    level, coordinates,
                    output, textures,
                    model, direction,
                    ((hiddenFaces & s_directionToMask[(int)leftDir]) != TerrainConnectivity.None && !IsTileHidden(level, coordinates.Move(leftDir))) || IsFaceHidden(level, coordinates, leftDir),
                    ((hiddenFaces & s_directionToMask[(int)rightDir]) != TerrainConnectivity.None && !IsTileHidden(level, coordinates.Move(rightDir))) || IsFaceHidden(level, coordinates, rightDir),
                    ((hiddenFaces & s_directionToMask[(int)Direction.Up]) != TerrainConnectivity.None && !IsTileHidden(level, coordinates.Above())) || IsFaceHidden(level, coordinates, Direction.Up),
                    ((hiddenFaces & s_directionToMask[(int)Direction.Down]) != TerrainConnectivity.None && !IsTileHidden(level, coordinates.Below())) || IsFaceHidden(level, coordinates, Direction.Down),
                    ((hiddenFaces & s_directionToMask[(int)frontDir]) != TerrainConnectivity.None && !IsTileHidden(level, coordinates.Move(frontDir))) || IsFaceHidden(level, coordinates, frontDir),
                    ((hiddenFaces & s_directionToMask[(int)backDir]) != TerrainConnectivity.None && !IsTileHidden(level, coordinates.Move(backDir))) || IsFaceHidden(level, coordinates, backDir)
                );
            }
        }

        private void RenderModelShadows(ILevel level, TileCoordinates coordinates, TerrainConnectivity modelConnectivity, TerrainLayer layer, Geometry output, TerrainConnectivity actualConnectivity)
        {
            Model model;
            FlatDirection direction;
            GetModelAndDirection(modelConnectivity, layer, level.RandomSeed, coordinates, out model, out direction);
            if (model != null)
            {
                var hiddenFaces = modelConnectivity & actualConnectivity;
                var leftDir = direction.RotateLeft().ToDirection();
                var rightDir = direction.RotateRight().ToDirection();
                var frontDir = direction.ToDirection();
                var backDir = direction.Opposite().ToDirection();
                RenderModelShadows(
                    level, coordinates,
                    output,
                    model, direction,
                    ((hiddenFaces & s_directionToMask[(int)leftDir]) > 0 && !IsTileHidden(level, coordinates.Move(leftDir))) || IsFaceHidden(level, coordinates, leftDir),
                    ((hiddenFaces & s_directionToMask[(int)rightDir]) > 0 && !IsTileHidden(level, coordinates.Move(rightDir))) || IsFaceHidden(level, coordinates, rightDir),
                    ((hiddenFaces & s_directionToMask[(int)Direction.Up]) > 0 && !IsTileHidden(level, coordinates.Above())) || IsFaceHidden(level, coordinates, Direction.Up),
                    ((hiddenFaces & s_directionToMask[(int)Direction.Down]) > 0 && !IsTileHidden(level, coordinates.Below())) || IsFaceHidden(level, coordinates, Direction.Down),
                    ((hiddenFaces & s_directionToMask[(int)frontDir]) > 0 && !IsTileHidden(level, coordinates.Move(frontDir))) || IsFaceHidden(level, coordinates, frontDir),
                    ((hiddenFaces & s_directionToMask[(int)backDir]) > 0 && !IsTileHidden(level, coordinates.Move(backDir))) || IsFaceHidden(level, coordinates, backDir)
                );
            }
        }

        private struct TerrainModels
        {
            public TerrainConnectivity Connectivity;
            public TerrainConnectivity TopModel;
            public TerrainConnectivity MiddleModel;
            public TerrainConnectivity BottomModel;
        }

        private TerrainModels GetModels(ILevel level, TileCoordinates coordinates)
        {
            TerrainModels result;
            var connectivity = GetConnectivity(level, coordinates);

            var above = coordinates.Above();
            var below = coordinates.Below();
            var up = (level.Tiles[above] == Tile);
            var down = (level.Tiles[below] == Tile) || ShouldConnectOnSide(level, below, Direction.Up);
            if (up)
            {
                var aboveConnectivity = GetConnectivity(level, above);
                if ((connectivity & TerrainConnectivity.NorthEastSouthWest) != (aboveConnectivity & TerrainConnectivity.NorthEastSouthWest))
                {
                    if (down)
                    {
                        result.TopModel = connectivity | TerrainConnectivity.Down;
                        result.MiddleModel = aboveConnectivity | TerrainConnectivity.Up | TerrainConnectivity.Down;
                        result.BottomModel = TerrainConnectivity.Invalid;
                    }
                    else
                    {
                        result.TopModel = TerrainConnectivity.Invalid;
                        result.MiddleModel = TerrainConnectivity.Invalid;
                        result.BottomModel = aboveConnectivity | TerrainConnectivity.Up;
                    }
                }
                else
                {
                    if (down)
                    {
                        result.TopModel = TerrainConnectivity.Invalid;
                        result.MiddleModel = connectivity | TerrainConnectivity.Up | TerrainConnectivity.Down;
                        result.BottomModel = TerrainConnectivity.Invalid;
                    }
                    else
                    {
                        result.TopModel = TerrainConnectivity.Invalid;
                        result.MiddleModel = TerrainConnectivity.Invalid;
                        result.BottomModel = connectivity | TerrainConnectivity.Up;
                    }
                }
            }
            else
            {
                result.TopModel = connectivity;
                result.MiddleModel = TerrainConnectivity.Invalid;
                result.BottomModel = TerrainConnectivity.Invalid;
            }
            result.Connectivity = connectivity | (up ? TerrainConnectivity.Up : 0) | (down ? TerrainConnectivity.Down : 0);
            return result;
        }

        private bool CheckFlags(TerrainConnectivity connections, TerrainConnectivity mustHave, TerrainConnectivity mustNotHave)
        {
            return ((connections & mustHave) | (connections & mustNotHave)) == mustHave;
        }

        private bool ShouldConnectOnSide(ILevel level, TileCoordinates pos, Direction dir)
        {
            var tile = level.Tiles[pos];
            return
                !(tile.Behaviour is FallingTileBehaviour) &&
                !(tile.Behaviour is ElevatorTileBehaviour) &&
                tile.IsOpaqueOnSide(level, pos, dir);
        }

        private bool ShouldConnectOnSides(ILevel level, TileCoordinates pos, Direction dir, Direction dir2)
        {
            var tile = level.Tiles[pos];
            return
                !(tile.Behaviour is FallingTileBehaviour) &&
                !(tile.Behaviour is ElevatorTileBehaviour) &&
                tile.IsOpaqueOnSide(level, pos, dir) &&
                tile.IsOpaqueOnSide(level, pos, dir2);
        }

        private TerrainConnectivity GetConnectivity(ILevel level, TileCoordinates pos)
        {
            var n = pos.North();
            var ne = n.East();
            var nw = n.West();
            var e = pos.East();
            var w = pos.West();
            var s = pos.South();
            var se = s.East();
            var sw = s.West();

            bool nesolid = level.Tiles[ne] == Tile || ShouldConnectOnSides(level, ne, Direction.West, Direction.South);
            bool nsolid = level.Tiles[n] == Tile || ShouldConnectOnSide(level, n, Direction.South);
            bool nwsolid = level.Tiles[nw] == Tile || ShouldConnectOnSides(level, nw, Direction.East, Direction.South);
            bool esolid = level.Tiles[e] == Tile || ShouldConnectOnSide(level, e, Direction.West);
            bool wsolid = level.Tiles[w] == Tile || ShouldConnectOnSide(level, w, Direction.East);
            bool sesolid = level.Tiles[se] == Tile || ShouldConnectOnSides(level, se, Direction.West, Direction.North);
            bool ssolid = level.Tiles[s] == Tile || ShouldConnectOnSide(level, s, Direction.North);
            bool swsolid = level.Tiles[sw] == Tile || ShouldConnectOnSides(level, sw, Direction.East, Direction.North);

            return
                (nesolid ? TerrainConnectivity.NorthEast : 0) | (nsolid ? TerrainConnectivity.North : 0) | (nwsolid ? TerrainConnectivity.NorthWest : 0) |
                (esolid ? TerrainConnectivity.East : 0) | (wsolid ? TerrainConnectivity.West : 0) |
                (sesolid ? TerrainConnectivity.SouthEast : 0) | (ssolid ? TerrainConnectivity.South : 0) | (swsolid ? TerrainConnectivity.SouthWest : 0);
        }

        private void GetModelAndDirection(TerrainConnectivity connections, TerrainLayer layer, int seed, TileCoordinates pos, out Model o_model, out FlatDirection o_direction)
        {
            if (layer == TerrainLayer.Bottom)
            {
                // BOTTOM
                if (CheckFlags(connections, TerrainConnectivity.NorthEast | TerrainConnectivity.North | TerrainConnectivity.NorthWest | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.SouthEast | TerrainConnectivity.South | TerrainConnectivity.SouthWest, 0))
                {
                    // Center
                    o_model = Model.Get(m_centerModelPathInv);
                    o_direction = RandomDir(seed, pos);
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.South, TerrainConnectivity.NorthEast | TerrainConnectivity.NorthWest | TerrainConnectivity.SouthEast | TerrainConnectivity.SouthWest))
                {
                    // Cross
                    o_model = Model.Get(m_crossModelPathInv);
                    o_direction = RandomDir(seed, pos);
                }
                else if (CheckFlags(connections, 0, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.West))
                {
                    // Island
                    o_model = Model.Get(m_islandModelPathInv);
                    o_direction = RandomDir(seed, pos);
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.SouthEast, TerrainConnectivity.North | TerrainConnectivity.West))
                {
                    // NW corner
                    o_model = Model.Get(m_cornerModelPathInv);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.West | TerrainConnectivity.SouthWest, TerrainConnectivity.North | TerrainConnectivity.East))
                {
                    // NE corner
                    o_model = Model.Get(m_cornerModelPathInv);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.NorthEast, TerrainConnectivity.South | TerrainConnectivity.West))
                {
                    // SW corner
                    o_model = Model.Get(m_cornerModelPathInv);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.West | TerrainConnectivity.NorthWest, TerrainConnectivity.South | TerrainConnectivity.East))
                {
                    // SE corner
                    o_model = Model.Get(m_cornerModelPathInv);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.SouthEast | TerrainConnectivity.West | TerrainConnectivity.SouthWest, TerrainConnectivity.North))
                {
                    // N edge
                    o_model = Model.Get(m_edgeModelPathInv);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.NorthEast | TerrainConnectivity.West | TerrainConnectivity.NorthWest, TerrainConnectivity.South))
                {
                    // S edge
                    o_model = Model.Get(m_edgeModelPathInv);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.West | TerrainConnectivity.NorthWest | TerrainConnectivity.SouthWest, TerrainConnectivity.East))
                {
                    // E edge
                    o_model = Model.Get(m_edgeModelPathInv);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.NorthEast | TerrainConnectivity.SouthEast, TerrainConnectivity.West))
                {
                    // W edge
                    o_model = Model.Get(m_edgeModelPathInv);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.NorthEast | TerrainConnectivity.East | TerrainConnectivity.SouthEast | TerrainConnectivity.South | TerrainConnectivity.SouthWest | TerrainConnectivity.West, TerrainConnectivity.NorthWest))
                {
                    // NW inside corner
                    o_model = Model.Get(m_insideCornerModelPathInv);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.East | TerrainConnectivity.SouthEast | TerrainConnectivity.South | TerrainConnectivity.SouthWest | TerrainConnectivity.West | TerrainConnectivity.NorthWest | TerrainConnectivity.North, TerrainConnectivity.NorthEast))
                {
                    // NE inside corner
                    o_model = Model.Get(m_insideCornerModelPathInv);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.SouthWest | TerrainConnectivity.West | TerrainConnectivity.NorthWest | TerrainConnectivity.North | TerrainConnectivity.NorthEast | TerrainConnectivity.East, TerrainConnectivity.SouthEast))
                {
                    // SE inside corner
                    o_model = Model.Get(m_insideCornerModelPathInv);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.West | TerrainConnectivity.NorthWest | TerrainConnectivity.North | TerrainConnectivity.NorthEast | TerrainConnectivity.East | TerrainConnectivity.SouthEast | TerrainConnectivity.South, TerrainConnectivity.SouthWest))
                {
                    // SW inside corner
                    o_model = Model.Get(m_insideCornerModelPathInv);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.East | TerrainConnectivity.West, TerrainConnectivity.North | TerrainConnectivity.South))
                {
                    // EW line
                    o_model = Model.Get(m_lineModelPathInv);
                    o_direction = RandomFlip(seed, pos, FlatDirection.East);
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.South, TerrainConnectivity.East | TerrainConnectivity.West))
                {
                    // NS line
                    o_model = Model.Get(m_lineModelPathInv);
                    o_direction = RandomFlip(seed, pos, FlatDirection.North);
                }
                else if (CheckFlags(connections, TerrainConnectivity.North, TerrainConnectivity.East | TerrainConnectivity.South | TerrainConnectivity.West))
                {
                    // N peninsular
                    o_model = Model.Get(m_peninsularModelPathInv);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West))
                {
                    // S peninsular
                    o_model = Model.Get(m_peninsularModelPathInv);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.East, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.West))
                {
                    // E peninsular
                    o_model = Model.Get(m_peninsularModelPathInv);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.West, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.East))
                {
                    // W peninsular
                    o_model = Model.Get(m_peninsularModelPathInv);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.SouthEast | TerrainConnectivity.South | TerrainConnectivity.SouthWest, TerrainConnectivity.NorthEast | TerrainConnectivity.NorthWest))
                {
                    // N inside 2corners
                    o_model = Model.Get(m_inside2CornersModelPathInv);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.NorthEast | TerrainConnectivity.North | TerrainConnectivity.NorthWest | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.South, TerrainConnectivity.SouthEast | TerrainConnectivity.SouthWest))
                {
                    // S inside 2corners
                    o_model = Model.Get(m_inside2CornersModelPathInv);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.NorthWest | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.South | TerrainConnectivity.SouthWest, TerrainConnectivity.NorthEast | TerrainConnectivity.SouthEast))
                {
                    // E inside 2corners
                    o_model = Model.Get(m_inside2CornersModelPathInv);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.NorthEast | TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.SouthEast | TerrainConnectivity.South, TerrainConnectivity.NorthWest | TerrainConnectivity.SouthWest))
                {
                    // N inside 2corners
                    o_model = Model.Get(m_inside2CornersModelPathInv);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.South | TerrainConnectivity.West | TerrainConnectivity.NorthWest | TerrainConnectivity.SouthEast, TerrainConnectivity.NorthEast | TerrainConnectivity.SouthWest))
                {
                    // NS inside 2corners opp
                    o_model = Model.Get(m_inside2CornersOppModelPathInv);
                    o_direction = RandomFlip(seed, pos, FlatDirection.North);
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.South | TerrainConnectivity.West | TerrainConnectivity.NorthEast | TerrainConnectivity.SouthWest, TerrainConnectivity.NorthWest | TerrainConnectivity.SouthEast))
                {
                    // EW inside 2corners opp
                    o_model = Model.Get(m_inside2CornersOppModelPathInv);
                    o_direction = RandomFlip(seed, pos, FlatDirection.East);
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.South | TerrainConnectivity.NorthWest, TerrainConnectivity.NorthEast | TerrainConnectivity.SouthEast | TerrainConnectivity.SouthWest))
                {
                    // N inside3corners
                    o_model = Model.Get(m_inside3CornersModelPathInv);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.South | TerrainConnectivity.SouthEast, TerrainConnectivity.NorthEast | TerrainConnectivity.NorthWest | TerrainConnectivity.SouthWest))
                {
                    // S inside3corners
                    o_model = Model.Get(m_inside3CornersModelPathInv);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.South | TerrainConnectivity.NorthEast, TerrainConnectivity.NorthWest | TerrainConnectivity.SouthEast | TerrainConnectivity.SouthWest))
                {
                    // E inside3corners
                    o_model = Model.Get(m_inside3CornersModelPathInv);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.South | TerrainConnectivity.SouthWest, TerrainConnectivity.NorthEast | TerrainConnectivity.NorthWest | TerrainConnectivity.SouthEast))
                {
                    // W inside3corners
                    o_model = Model.Get(m_inside3CornersModelPathInv);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West, TerrainConnectivity.NorthEast | TerrainConnectivity.NorthWest | TerrainConnectivity.South))
                {
                    // N tee
                    o_model = Model.Get(m_teeModelPathInv);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.South, TerrainConnectivity.North | TerrainConnectivity.SouthEast | TerrainConnectivity.SouthWest))
                {
                    // S tee
                    o_model = Model.Get(m_teeModelPathInv);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.South, TerrainConnectivity.NorthEast | TerrainConnectivity.West | TerrainConnectivity.SouthEast))
                {
                    // E tee
                    o_model = Model.Get(m_teeModelPathInv);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.West | TerrainConnectivity.South, TerrainConnectivity.NorthWest | TerrainConnectivity.East | TerrainConnectivity.SouthWest))
                {
                    // W tee
                    o_model = Model.Get(m_teeModelPathInv);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.East, TerrainConnectivity.North | TerrainConnectivity.West | TerrainConnectivity.SouthEast))
                {
                    // NW bend
                    o_model = Model.Get(m_bendModelPathInv);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.West, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.SouthWest))
                {
                    // NE bend
                    o_model = Model.Get(m_bendModelPathInv);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East, TerrainConnectivity.South | TerrainConnectivity.West | TerrainConnectivity.NorthEast))
                {
                    // SW bend
                    o_model = Model.Get(m_bendModelPathInv);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.West, TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.NorthWest))
                {
                    // SE bend
                    o_model = Model.Get(m_bendModelPathInv);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.SouthEast, TerrainConnectivity.North | TerrainConnectivity.SouthWest))
                {
                    // N edge with inside corner
                    o_model = Model.Get(m_edgeWithInsideCornerPathInv);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.NorthWest, TerrainConnectivity.South | TerrainConnectivity.NorthEast))
                {
                    // S edge with inside corner
                    o_model = Model.Get(m_edgeWithInsideCornerPathInv);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.West | TerrainConnectivity.SouthWest, TerrainConnectivity.East | TerrainConnectivity.NorthWest))
                {
                    // E edge with inside corner
                    o_model = Model.Get(m_edgeWithInsideCornerPathInv);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.NorthEast, TerrainConnectivity.West | TerrainConnectivity.SouthEast))
                {
                    // W edge with inside corner
                    o_model = Model.Get(m_edgeWithInsideCornerPathInv);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.SouthWest, TerrainConnectivity.North | TerrainConnectivity.SouthEast))
                {
                    // N edge with inside corner opp
                    o_model = Model.Get(m_edgeWithInsideCornerOppPathInv);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.NorthEast, TerrainConnectivity.South | TerrainConnectivity.NorthWest))
                {
                    // S edge with inside corner opp
                    o_model = Model.Get(m_edgeWithInsideCornerOppPathInv);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.West | TerrainConnectivity.NorthWest, TerrainConnectivity.East | TerrainConnectivity.SouthWest))
                {
                    // E edge with inside corner opp
                    o_model = Model.Get(m_edgeWithInsideCornerOppPathInv);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.SouthEast, TerrainConnectivity.West | TerrainConnectivity.NorthEast))
                {
                    // W edge with inside corner opp
                    o_model = Model.Get(m_edgeWithInsideCornerOppPathInv);
                    o_direction = FlatDirection.North;
                }
                else
                {
                    // UNSUPPORTED
                    throw new Exception("Unsupported combination");
                }
            }
            else if (layer == TerrainLayer.Middle)
            {
                // MIDDLE
                if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.South | TerrainConnectivity.West, 0))
                {
                    // Interior
                    o_model = Model.Get(m_wallInteriorPath);
                    o_direction = RandomDir(seed, pos);
                }
                else if (CheckFlags(connections, 0, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.South | TerrainConnectivity.West))
                {
                    // Island
                    o_model = Model.Get(m_wallIslandPath);
                    o_direction = RandomDir(seed, pos);
                }
                else if (CheckFlags(connections, TerrainConnectivity.East | TerrainConnectivity.South | TerrainConnectivity.West, TerrainConnectivity.North))
                {
                    // N Wall
                    o_model = Model.Get(m_wallPath);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.West, TerrainConnectivity.East))
                {
                    // E Wall
                    o_model = Model.Get(m_wallPath);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West, TerrainConnectivity.South))
                {
                    // S Wall
                    o_model = Model.Get(m_wallPath);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.South, TerrainConnectivity.West))
                {
                    // W Wall
                    o_model = Model.Get(m_wallPath);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.South, TerrainConnectivity.East | TerrainConnectivity.West))
                {
                    // NS Line
                    o_model = Model.Get(m_wallLinePath);
                    o_direction = RandomFlip(seed, pos, FlatDirection.North);
                }
                else if (CheckFlags(connections, TerrainConnectivity.East | TerrainConnectivity.West, TerrainConnectivity.North | TerrainConnectivity.South))
                {
                    // EW Line
                    o_model = Model.Get(m_wallLinePath);
                    o_direction = RandomFlip(seed, pos, FlatDirection.East);
                }
                else if (CheckFlags(connections, TerrainConnectivity.South, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West))
                {
                    // N Peninsular
                    o_model = Model.Get(m_wallPeninsularPath);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.West, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.East))
                {
                    // E Peninsular
                    o_model = Model.Get(m_wallPeninsularPath);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North, TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.West))
                {
                    // S Peninsular
                    o_model = Model.Get(m_wallPeninsularPath);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.East, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.West))
                {
                    // W Peninsular
                    o_model = Model.Get(m_wallPeninsularPath);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.West, TerrainConnectivity.North | TerrainConnectivity.East))
                {
                    // NE Corner
                    o_model = Model.Get(m_wallCornerPath);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.East, TerrainConnectivity.North | TerrainConnectivity.West))
                {
                    // NW Corner
                    o_model = Model.Get(m_wallCornerPath);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.West, TerrainConnectivity.South | TerrainConnectivity.East))
                {
                    // SE Corner
                    o_model = Model.Get(m_wallCornerPath);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East, TerrainConnectivity.South | TerrainConnectivity.West))
                {
                    // SW Corner
                    o_model = Model.Get(m_wallCornerPath);
                    o_direction = FlatDirection.South;
                }
                else
                {
                    // Unsupported
                    throw new Exception("Unsupported combination");
                }
            }
            else if (layer == TerrainLayer.Top)
            {
                // TOP
                if (CheckFlags(connections, TerrainConnectivity.NorthEast | TerrainConnectivity.North | TerrainConnectivity.NorthWest | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.SouthEast | TerrainConnectivity.South | TerrainConnectivity.SouthWest, 0))
                {
                    // Center
                    o_model = Model.Get(m_centerModelPath);
                    o_direction = RandomDir(seed, pos);
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.South, TerrainConnectivity.NorthEast | TerrainConnectivity.NorthWest | TerrainConnectivity.SouthEast | TerrainConnectivity.SouthWest))
                {
                    // Cross
                    o_model = Model.Get(m_crossModelPath);
                    o_direction = RandomDir(seed, pos);
                }
                else if (CheckFlags(connections, 0, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.West))
                {
                    // Island
                    o_model = Model.Get(m_islandModelPath);
                    o_direction = RandomDir(seed, pos);
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.SouthEast, TerrainConnectivity.North | TerrainConnectivity.West))
                {
                    // NW corner
                    o_model = Model.Get(m_cornerModelPath);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.West | TerrainConnectivity.SouthWest, TerrainConnectivity.North | TerrainConnectivity.East))
                {
                    // NE corner
                    o_model = Model.Get(m_cornerModelPath);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.NorthEast, TerrainConnectivity.South | TerrainConnectivity.West))
                {
                    // SW corner
                    o_model = Model.Get(m_cornerModelPath);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.West | TerrainConnectivity.NorthWest, TerrainConnectivity.South | TerrainConnectivity.East))
                {
                    // SE corner
                    o_model = Model.Get(m_cornerModelPath);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.SouthEast | TerrainConnectivity.West | TerrainConnectivity.SouthWest, TerrainConnectivity.North))
                {
                    // N edge
                    o_model = Model.Get(m_edgeModelPath);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.NorthEast | TerrainConnectivity.West | TerrainConnectivity.NorthWest, TerrainConnectivity.South))
                {
                    // S edge
                    o_model = Model.Get(m_edgeModelPath);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.West | TerrainConnectivity.NorthWest | TerrainConnectivity.SouthWest, TerrainConnectivity.East))
                {
                    // E edge
                    o_model = Model.Get(m_edgeModelPath);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.NorthEast | TerrainConnectivity.SouthEast, TerrainConnectivity.West))
                {
                    // W edge
                    o_model = Model.Get(m_edgeModelPath);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.NorthEast | TerrainConnectivity.East | TerrainConnectivity.SouthEast | TerrainConnectivity.South | TerrainConnectivity.SouthWest | TerrainConnectivity.West, TerrainConnectivity.NorthWest))
                {
                    // NW inside corner
                    o_model = Model.Get(m_insideCornerModelPath);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.East | TerrainConnectivity.SouthEast | TerrainConnectivity.South | TerrainConnectivity.SouthWest | TerrainConnectivity.West | TerrainConnectivity.NorthWest | TerrainConnectivity.North, TerrainConnectivity.NorthEast))
                {
                    // NE inside corner
                    o_model = Model.Get(m_insideCornerModelPath);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.SouthWest | TerrainConnectivity.West | TerrainConnectivity.NorthWest | TerrainConnectivity.North | TerrainConnectivity.NorthEast | TerrainConnectivity.East, TerrainConnectivity.SouthEast))
                {
                    // SE inside corner
                    o_model = Model.Get(m_insideCornerModelPath);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.West | TerrainConnectivity.NorthWest | TerrainConnectivity.North | TerrainConnectivity.NorthEast | TerrainConnectivity.East | TerrainConnectivity.SouthEast | TerrainConnectivity.South, TerrainConnectivity.SouthWest))
                {
                    // SW inside corner
                    o_model = Model.Get(m_insideCornerModelPath);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.East | TerrainConnectivity.West, TerrainConnectivity.North | TerrainConnectivity.South))
                {
                    // EW line
                    o_model = Model.Get(m_lineModelPath);
                    o_direction = RandomFlip(seed, pos, FlatDirection.East);
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.South, TerrainConnectivity.East | TerrainConnectivity.West))
                {
                    // NS line
                    o_model = Model.Get(m_lineModelPath);
                    o_direction = RandomFlip(seed, pos, FlatDirection.North);
                }
                else if (CheckFlags(connections, TerrainConnectivity.North, TerrainConnectivity.East | TerrainConnectivity.South | TerrainConnectivity.West))
                {
                    // N peninsular
                    o_model = Model.Get(m_peninsularModelPath);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West))
                {
                    // S peninsular
                    o_model = Model.Get(m_peninsularModelPath);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.East, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.West))
                {
                    // E peninsular
                    o_model = Model.Get(m_peninsularModelPath);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.West, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.East))
                {
                    // W peninsular
                    o_model = Model.Get(m_peninsularModelPath);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.SouthEast | TerrainConnectivity.South | TerrainConnectivity.SouthWest, TerrainConnectivity.NorthEast | TerrainConnectivity.NorthWest))
                {
                    // N inside 2corners
                    o_model = Model.Get(m_inside2CornersModelPath);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.NorthEast | TerrainConnectivity.North | TerrainConnectivity.NorthWest | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.South, TerrainConnectivity.SouthEast | TerrainConnectivity.SouthWest))
                {
                    // S inside 2corners
                    o_model = Model.Get(m_inside2CornersModelPath);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.NorthWest | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.South | TerrainConnectivity.SouthWest, TerrainConnectivity.NorthEast | TerrainConnectivity.SouthEast))
                {
                    // E inside 2corners
                    o_model = Model.Get(m_inside2CornersModelPath);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.NorthEast | TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.SouthEast | TerrainConnectivity.South, TerrainConnectivity.NorthWest | TerrainConnectivity.SouthWest))
                {
                    // N inside 2corners
                    o_model = Model.Get(m_inside2CornersModelPath);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.South | TerrainConnectivity.West | TerrainConnectivity.NorthWest | TerrainConnectivity.SouthEast, TerrainConnectivity.NorthEast | TerrainConnectivity.SouthWest))
                {
                    // NS inside 2corners opp
                    o_model = Model.Get(m_inside2CornersOppModelPath);
                    o_direction = RandomFlip(seed, pos, FlatDirection.North);
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.South | TerrainConnectivity.West | TerrainConnectivity.NorthEast | TerrainConnectivity.SouthWest, TerrainConnectivity.NorthWest | TerrainConnectivity.SouthEast))
                {
                    // EW inside 2corners opp
                    o_model = Model.Get(m_inside2CornersOppModelPath);
                    o_direction = RandomFlip(seed, pos, FlatDirection.East);
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.South | TerrainConnectivity.NorthWest, TerrainConnectivity.NorthEast | TerrainConnectivity.SouthEast | TerrainConnectivity.SouthWest))
                {
                    // N inside3corners
                    o_model = Model.Get(m_inside3CornersModelPath);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.South | TerrainConnectivity.SouthEast, TerrainConnectivity.NorthEast | TerrainConnectivity.NorthWest | TerrainConnectivity.SouthWest))
                {
                    // S inside3corners
                    o_model = Model.Get(m_inside3CornersModelPath);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.South | TerrainConnectivity.NorthEast, TerrainConnectivity.NorthWest | TerrainConnectivity.SouthEast | TerrainConnectivity.SouthWest))
                {
                    // E inside3corners
                    o_model = Model.Get(m_inside3CornersModelPath);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.South | TerrainConnectivity.SouthWest, TerrainConnectivity.NorthEast | TerrainConnectivity.NorthWest | TerrainConnectivity.SouthEast))
                {
                    // W inside3corners
                    o_model = Model.Get(m_inside3CornersModelPath);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West, TerrainConnectivity.NorthEast | TerrainConnectivity.NorthWest | TerrainConnectivity.South))
                {
                    // N tee
                    o_model = Model.Get(m_teeModelPath);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.South, TerrainConnectivity.North | TerrainConnectivity.SouthEast | TerrainConnectivity.SouthWest))
                {
                    // S tee
                    o_model = Model.Get(m_teeModelPath);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.South, TerrainConnectivity.NorthEast | TerrainConnectivity.West | TerrainConnectivity.SouthEast))
                {
                    // E tee
                    o_model = Model.Get(m_teeModelPath);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.West | TerrainConnectivity.South, TerrainConnectivity.NorthWest | TerrainConnectivity.East | TerrainConnectivity.SouthWest))
                {
                    // W tee
                    o_model = Model.Get(m_teeModelPath);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.East, TerrainConnectivity.North | TerrainConnectivity.West | TerrainConnectivity.SouthEast))
                {
                    // NW bend
                    o_model = Model.Get(m_bendModelPath);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.West, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.SouthWest))
                {
                    // NE bend
                    o_model = Model.Get(m_bendModelPath);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East, TerrainConnectivity.South | TerrainConnectivity.West | TerrainConnectivity.NorthEast))
                {
                    // SW bend
                    o_model = Model.Get(m_bendModelPath);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.West, TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.NorthWest))
                {
                    // SE bend
                    o_model = Model.Get(m_bendModelPath);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.SouthEast, TerrainConnectivity.North | TerrainConnectivity.SouthWest))
                {
                    // N edge with inside corner
                    o_model = Model.Get(m_edgeWithInsideCornerPath);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.NorthWest, TerrainConnectivity.South | TerrainConnectivity.NorthEast))
                {
                    // S edge with inside corner
                    o_model = Model.Get(m_edgeWithInsideCornerPath);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.West | TerrainConnectivity.SouthWest, TerrainConnectivity.East | TerrainConnectivity.NorthWest))
                {
                    // E edge with inside corner
                    o_model = Model.Get(m_edgeWithInsideCornerPath);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.NorthEast, TerrainConnectivity.West | TerrainConnectivity.SouthEast))
                {
                    // W edge with inside corner
                    o_model = Model.Get(m_edgeWithInsideCornerPath);
                    o_direction = FlatDirection.North;
                }
                else if (CheckFlags(connections, TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.SouthWest, TerrainConnectivity.North | TerrainConnectivity.SouthEast))
                {
                    // N edge with inside corner opp
                    o_model = Model.Get(m_edgeWithInsideCornerOppPath);
                    o_direction = FlatDirection.East;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.East | TerrainConnectivity.West | TerrainConnectivity.NorthEast, TerrainConnectivity.South | TerrainConnectivity.NorthWest))
                {
                    // S edge with inside corner opp
                    o_model = Model.Get(m_edgeWithInsideCornerOppPath);
                    o_direction = FlatDirection.West;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.West | TerrainConnectivity.NorthWest, TerrainConnectivity.East | TerrainConnectivity.SouthWest))
                {
                    // E edge with inside corner opp
                    o_model = Model.Get(m_edgeWithInsideCornerOppPath);
                    o_direction = FlatDirection.South;
                }
                else if (CheckFlags(connections, TerrainConnectivity.North | TerrainConnectivity.South | TerrainConnectivity.East | TerrainConnectivity.SouthEast, TerrainConnectivity.West | TerrainConnectivity.NorthEast))
                {
                    // W edge with inside corner opp
                    o_model = Model.Get(m_edgeWithInsideCornerOppPath);
                    o_direction = FlatDirection.North;
                }
                else
                {
                    // UNSUPPORTED
                    throw new Exception("Unsupported combination");
                }
            }
            else
            {
                // Non-terrain
                o_model = null;
                o_direction = FlatDirection.South;
            }
        }

        private static FlatDirection RandomDir(int seed, TileCoordinates pos)
        {
            return (FlatDirection)((pos.X ^ pos.Y ^ pos.Z ^ seed) & 0x3);
        }

        private static FlatDirection RandomFlip(int seed, TileCoordinates pos, FlatDirection dir)
        {
            return ((pos.X ^ pos.Y ^ pos.Z ^ seed) & 0x1) > 0 ? dir : dir.Opposite();
        }
    }
}
