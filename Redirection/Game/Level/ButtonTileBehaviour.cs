
using Dan200.Core.Assets;
using Dan200.Core.Render;

namespace Dan200.Game.Level
{
    public enum ButtonType
    {
        Momentary,
        Latch,
        Toggle,
        Directional,
    }

    [TileBehaviour(name: "button")]
    public class ButtonTileBehaviour : TileBehaviour
    {
        private string m_poweredModelPath;
        private string m_altPoweredModelPath;
        private string m_soundPath;
        private bool[] m_connectivity;
        private ButtonType m_type;
        private bool m_inverted;
        private string m_colour;

        public ButtonTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
            m_poweredModelPath = kvp.GetString("powered_model", tile.ModelPath);
            m_altPoweredModelPath = kvp.GetString("alt_powered_model", m_poweredModelPath);

            m_soundPath = kvp.GetString("sound", null);

            bool connected = kvp.GetBool("connected", false);
            m_connectivity = new bool[6];
            m_connectivity[0] = kvp.GetBool("connected_front", connected);
            m_connectivity[1] = kvp.GetBool("connected_right", connected);
            m_connectivity[2] = kvp.GetBool("connected_back", connected);
            m_connectivity[3] = kvp.GetBool("connected_left", connected);
            m_connectivity[4] = kvp.GetBool("connected_top", connected);
            m_connectivity[5] = kvp.GetBool("connected_bottom", connected);

            m_type = kvp.GetEnum("type", ButtonType.Momentary);
            m_inverted = kvp.GetBool("inverted", false);
            m_colour = kvp.GetString("colour");
        }

        private void OnDirectionalStepOnOff(ILevel level, TileCoordinates coordinates, FlatDirection direction)
        {
            var tileDirection = level.Tiles[coordinates].GetDirection(level, coordinates);
            if (direction == (m_inverted ? tileDirection.Opposite() : tileDirection))
            {
                if (IsPowered(level, coordinates))
                {
                    SetPowered(level, coordinates, false, true);
                    if (m_soundPath != null)
                    {
                        level.Audio.PlaySound(m_soundPath);
                    }
                }
            }
            else if (direction == (m_inverted ? tileDirection : tileDirection.Opposite()))
            {
                if (!IsPowered(level, coordinates))
                {
                    SetPowered(level, coordinates, true, true);
                    if (m_soundPath != null)
                    {
                        level.Audio.PlaySound(m_soundPath);
                    }
                }
            }
        }

        public override void OnSteppedOn(ILevel level, TileCoordinates coordinates, Robot.Robot robot, FlatDirection direction)
        {
            if (m_colour == null || robot.Colour == m_colour)
            {
                switch (m_type)
                {
                    case ButtonType.Momentary:
                    case ButtonType.Latch:
                        {
                            if (!IsPowered(level, coordinates))
                            {
                                SetPowered(level, coordinates, true, true);
                                if (m_soundPath != null)
                                {
                                    level.Audio.PlaySound(m_soundPath);
                                }
                            }
                            break;
                        }
                    case ButtonType.Directional:
                        {
                            OnDirectionalStepOnOff(level, coordinates, direction);
                            break;
                        }
                    case ButtonType.Toggle:
                        {
                            SetPowered(level, coordinates, !IsPowered(level, coordinates), true);
                            if (m_soundPath != null)
                            {
                                level.Audio.PlaySound(m_soundPath);
                            }
                            break;
                        }
                }
            }
        }

        public override void OnSteppedOff(ILevel level, TileCoordinates coordinates, Robot.Robot robot, FlatDirection direction)
        {
            if (m_colour == null || robot.Colour == m_colour)
            {
                switch (m_type)
                {
                    case ButtonType.Momentary:
                        {
                            if (IsPowered(level, coordinates))
                            {
                                SetPowered(level, coordinates, false, true);
                                if (m_soundPath != null)
                                {
                                    level.Audio.PlaySound(m_soundPath);
                                }
                            }
                            break;
                        }
                    case ButtonType.Directional:
                        {
                            OnDirectionalStepOnOff(level, coordinates, direction);
                            break;
                        }
                }
            }
        }

        private bool IsConnected(ILevel level, TileCoordinates coordinates, Direction direction)
        {
            var dir = Tile.GetDirection(level, coordinates);
            return m_connectivity[(int)direction.ToSide(dir)];
        }

        public override bool GetPowerOutput(ILevel level, TileCoordinates coordinates, Direction direction)
        {
            if (IsConnected(level, coordinates, direction))
            {
                return GetPowerOutput(level, coordinates);
            }
            return false;
        }

        public bool GetPowerOutput(ILevel level, TileCoordinates coordinates)
        {
            return (Tile.GetSubState(level, coordinates) > 0) ^ m_inverted;
        }

        public override Model GetModel(ILevel level, TileCoordinates coordinates)
        {
            if (GetPowerOutput(level, coordinates))
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

        private bool IsPowered(ILevel level, TileCoordinates coordinates)
        {
            return (Tile.GetSubState(level, coordinates) > 0);
        }

        private bool SetPowered(ILevel level, TileCoordinates coordinates, bool powered, bool notifyNeighbours)
        {
            bool currentlyPowered = Tile.GetSubState(level, coordinates) > 0;
            if (currentlyPowered != powered)
            {
                Tile.SetSubState(level, coordinates, powered ? 1 : 0, notifyNeighbours);
                return true;
            }
            return false;
        }
    }
}
