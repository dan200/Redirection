
using Dan200.Core.Assets;
using Dan200.Core.Render;
using System.Collections.Generic;

namespace Dan200.Game.Level
{
    [TileBehaviour(name: "wire")]
    public class WireTileBehaviour : TileBehaviour
    {
        private static bool s_searching = false;
        private static HashSet<TileCoordinates> s_visited = new HashSet<TileCoordinates>();
        private static HashSet<TileCoordinates> s_changed = new HashSet<TileCoordinates>();

        private string m_poweredModelPath;
        private string m_altPoweredModelPath;
        private bool[] m_connectivity;

        public WireTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
            m_poweredModelPath = kvp.GetString("powered_model", tile.ModelPath);
            m_altPoweredModelPath = kvp.GetString("alt_powered_model", m_poweredModelPath);

            bool connected = kvp.GetBool("connected", false);
            m_connectivity = new bool[6];
            m_connectivity[0] = kvp.GetBool("connected_front", connected);
            m_connectivity[1] = kvp.GetBool("connected_right", connected);
            m_connectivity[2] = kvp.GetBool("connected_back", connected);
            m_connectivity[3] = kvp.GetBool("connected_left", connected);
            m_connectivity[4] = kvp.GetBool("connected_top", connected);
            m_connectivity[5] = kvp.GetBool("connected_bottom", connected);
        }

        public override void OnLevelStart(ILevel level, TileCoordinates coordinates)
        {
            if (!s_searching && !level.InEditor)
            {
                UpdateNetwork(level, coordinates);
            }
        }

        public override void OnNeighbourChanged(ILevel level, TileCoordinates coordinates)
        {
            if (!s_searching && !level.InEditor)
            {
                UpdateNetwork(level, coordinates);
            }
        }

        private bool IsConnected(ILevel level, TileCoordinates coordinates, Direction direction)
        {
            var dir = Tile.GetDirection(level, coordinates);
            return m_connectivity[(int)direction.ToSide(dir)];
        }

        public override bool AcceptsPower(ILevel level, TileCoordinates coordinates, Direction direction)
        {
            return IsConnected(level, coordinates, direction);
        }

        public override bool GetPowerOutput(ILevel level, TileCoordinates coordinates, Direction direction)
        {
            if (!s_searching && IsConnected(level, coordinates, direction))
            {
                return Tile.GetSubState(level, coordinates) > 0;
            }
            return false;
        }

        public override Model GetModel(ILevel level, TileCoordinates coordinates)
        {
            if (Tile.GetSubState(level, coordinates) > 0)
            {
                if (((coordinates.X + coordinates.Z) & 0x1) == 1)
                {
                    return m_altPoweredModelPath != null ? Model.Get(m_altPoweredModelPath) : null;
                }
                else
                {
                    return m_poweredModelPath != null ? Model.Get(m_poweredModelPath) : null;
                }
            }
            else
            {
                return base.GetModel(level, coordinates);
            }
        }

        private static bool ExploreNetwork(ILevel level, TileCoordinates coordinates, HashSet<TileCoordinates> o_coordinates)
        {
            if (!o_coordinates.Contains(coordinates))
            {
                o_coordinates.Add(coordinates);

                var tile = level.Tiles[coordinates];
                var behaviour = (WireTileBehaviour)tile.GetBehaviour(level, coordinates);
                bool powered = tile.IsPowered(level, coordinates);
                for (int i = 0; i < 6; ++i)
                {
                    var dir = (Direction)i;
                    if (behaviour.IsConnected(level, coordinates, dir))
                    {
                        var neighbourCoords = coordinates.Move(dir, (dir == Direction.Up) ? tile.Height : 1);
                        var neighbourTile = level.Tiles[neighbourCoords];
                        if (neighbourTile.IsWire(level, neighbourCoords))
                        {
                            var neighbourBehaviour = (WireTileBehaviour)neighbourTile.GetBehaviour(level, neighbourCoords);
                            var neighbourBaseCoords = neighbourTile.GetBase(level, neighbourCoords);
                            if (neighbourBehaviour.IsConnected(level, neighbourBaseCoords, dir.Opposite()))
                            {
                                powered = powered || ExploreNetwork(level, neighbourBaseCoords, o_coordinates);
                            }
                        }
                    }
                }
                return powered;
            }
            return false;
        }

        private void UpdateNetwork(ILevel level, TileCoordinates coordinates)
        {
            try
            {
                // Find all the tiles in the network, and whether any of them are powered
                s_searching = true;
                bool powered = ExploreNetwork(level, coordinates, s_visited);

                // Set all those tiles to the poweredness of the network
                foreach (TileCoordinates networkCoords in s_visited)
                {
                    var networkTile = level.Tiles[networkCoords];
                    var networkBaseCoords = networkTile.GetBase(level, networkCoords);
                    var networkBehaviour = (WireTileBehaviour)networkTile.GetBehaviour(level, coordinates);
                    if (networkBehaviour.SetPowered(level, networkBaseCoords, powered))
                    {
                        s_changed.Add(networkBaseCoords);
                    }
                }
            }
            finally
            {
                s_searching = false;
                s_visited.Clear();
            }

            // Notify the neighbours of all the changed tiles
            if (s_changed.Count > 0)
            {
                var changed = new HashSet<TileCoordinates>(s_changed);
                s_changed.Clear();
                foreach (TileCoordinates networkCoords in changed)
                {
                    var networkTile = level.Tiles[networkCoords];
                    networkTile.NotifyNeighboursChanged(level, networkCoords);
                }
            }
        }

        private bool SetPowered(ILevel level, TileCoordinates coordinates, bool powered)
        {
            bool currentlyPowered = Tile.GetSubState(level, coordinates) > 0;
            if (currentlyPowered != powered)
            {
                Tile.SetSubState(level, coordinates, powered ? 1 : 0, false);
                return true;
            }
            return false;
        }
    }
}
