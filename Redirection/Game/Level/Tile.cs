using Dan200.Core.Assets;
using Dan200.Core.Render;
using OpenTK;
using System;

namespace Dan200.Game.Level
{
    public class Tile : IBasicAsset
    {
        public static Tile Get(string path)
        {
            return Assets.Get<Tile>(path);
        }

        private string m_path;
        private TileBehaviour m_behaviour;
        private string m_modelPath;
        private string m_altModelPath;
        private string m_editorModelPath;
        private int m_height;
        private bool m_placeable;
        private bool m_replaceable;
        private bool[] m_solidity;
        private bool[] m_opacity;
        private int m_forwardIncline;
        private int m_rightIncline;
        private bool m_allowPlacement;
        private RenderPass m_renderPass;
        private bool m_castShadows;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public string ModelPath
        {
            get
            {
                return m_modelPath;
            }
        }

        public Model Model
        {
            get
            {
                if (m_modelPath != null)
                {
                    return Model.Get(m_modelPath);
                }
                return null;
            }
        }


        public string AltModelPath
        {
            get
            {
                return m_altModelPath;
            }
        }

        public Model AltModel
        {
            get
            {
                if (m_altModelPath != null)
                {
                    return Model.Get(m_altModelPath);
                }
                return null;
            }
        }

        public string EditorModelPath
        {
            get
            {
                return m_editorModelPath;
            }
        }

        public Model EditorModel
        {
            get
            {
                if (m_editorModelPath != null)
                {
                    return Model.Get(m_editorModelPath);
                }
                return null;
            }
        }

        public int Height
        {
            get
            {
                return m_height;
            }
        }

        public bool Placeable
        {
            get
            {
                return m_placeable;
            }
        }

        public TileBehaviour Behaviour
        {
            get
            {
                return m_behaviour;
            }
        }

        public bool AllowPlacement
        {
            get
            {
                return m_allowPlacement;
            }
        }

        public RenderPass RenderPass
        {
            get
            {
                return m_renderPass;
            }
        }

        public bool CastShadows
        {
            get
            {
                return m_castShadows;
            }
        }

        public Tile(string path, IFileStore store)
        {
            m_path = path;
            Load(store);
        }

        public void Reload(IFileStore store)
        {
            Load(store);
        }

        public void Dispose()
        {
        }

        public void Load(IFileStore store)
        {
            var kvp = new KeyValuePairs();
            using (var stream = store.OpenTextFile(m_path))
            {
                kvp.Load(stream);
            }

            m_modelPath = kvp.GetString("model", null);
            m_altModelPath = kvp.GetString("alt_model", m_modelPath);
            m_editorModelPath = kvp.GetString("editor_model", null);

            m_height = kvp.GetInteger("height", 1);
            m_placeable = kvp.GetBool("placeable", false);
            m_replaceable = kvp.GetBool("replaceable", false);

            m_solidity = new bool[6];
            bool solid = kvp.GetBool("solid", false);
            m_solidity[0] = kvp.GetBool("solid_front", solid);
            m_solidity[1] = kvp.GetBool("solid_right", solid);
            m_solidity[2] = kvp.GetBool("solid_back", solid);
            m_solidity[3] = kvp.GetBool("solid_left", solid);
            m_solidity[4] = kvp.GetBool("solid_top", solid);
            m_solidity[5] = kvp.GetBool("solid_bottom", solid);

            m_opacity = new bool[6];
            bool opaque = kvp.GetBool("opaque", false);
            m_opacity[0] = kvp.GetBool("opaque_front", opaque);
            m_opacity[1] = kvp.GetBool("opaque_right", opaque);
            m_opacity[2] = kvp.GetBool("opaque_back", opaque);
            m_opacity[3] = kvp.GetBool("opaque_left", opaque);
            m_opacity[4] = kvp.GetBool("opaque_top", opaque);
            m_opacity[5] = kvp.GetBool("opaque_bottom", opaque);

            m_forwardIncline = kvp.GetInteger("incline_forward", 0);
            m_rightIncline = kvp.GetInteger("incline_right", 0);
            m_allowPlacement = kvp.GetBool("allow_placement", true);

            var behaviour = kvp.GetString("behaviour", "generic");
            m_behaviour = TileBehaviour.CreateFromName(behaviour, this, kvp);

            m_renderPass = kvp.GetEnum("render_pass", RenderPass.Opaque);
            m_castShadows = kvp.GetBool("cast_shadows", true);
        }

        protected void OnInit(ILevel level, TileCoordinates coordinates)
        {
            if (!IsExtension())
            {
                m_behaviour.OnInit(level, coordinates);
                var entity = m_behaviour.CreateEntity(level, coordinates);
                if (entity != null)
                {
                    SetEntity(level, coordinates, entity);
                }
            }
        }

        protected void OnShutdown(ILevel level, TileCoordinates coordinates)
        {
            if (!IsExtension())
            {
                SetEntity(level, coordinates, null);
                m_behaviour.OnShutdown(level, coordinates);
            }
        }

        public bool IsOccupied(ILevel level, TileCoordinates coordinates)
        {
            return GetOccupant(level, coordinates) != null;
        }

        public Entity GetOccupant(ILevel level, TileCoordinates coordinates)
        {
            var state = level.Tiles.GetState(coordinates);
            return state.Occupant;
        }

        public void SetOccupant(ILevel level, TileCoordinates coordinates, Entity entity, bool notifyNeighbours = false)
        {
            if (entity != null && coordinates.Y >= level.Tiles.MaxY)
            {
                level.Tiles.ExpandToFit(coordinates);
            }

            var state = level.Tiles.GetState(coordinates);
            if (state.Occupant != entity)
            {
                state.Occupant = entity;
                if (notifyNeighbours)
                {
                    NotifyNeighboursChanged(level, coordinates);
                }
            }
        }

        private bool IsOccupiedByNonVacatingRobot(ILevel level, TileCoordinates coordinates, FlatDirection side, Robot.Robot exception, bool ignoreMovingRobots)
        {
            if (IsOccupied(level, coordinates))
            {
                var occupant = GetOccupant(level, coordinates);
                if (occupant is Robot.Robot)
                {
                    var robot = (Robot.Robot)occupant;
                    if (robot == exception)
                    {
                        // Ignore ourselves
                        return false;
                    }
                    if (ignoreMovingRobots && robot.IsMoving)
                    {
                        // Ignore moving robots
                        return false;
                    }
                    if (robot.Location != coordinates && robot.IsTurning)
                    {
                        // Ignore robot reserving the space ahead of them when turning
                        return false;
                    }
                    if ((robot.Location == coordinates || robot.Location == coordinates.Below()) &&
                        robot.Direction != side &&
                        robot.IsVacating)
                    {
                        // Ignore robot about to leave
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public bool CanEnterOnSide(Robot.Robot robot, TileCoordinates coordinates, FlatDirection side, bool ignoreMovingObjects)
        {
            if (GetIncline(robot.Level, coordinates, side.Opposite()) <= 0 && IsSolidOnSide(robot.Level, coordinates, side.ToDirection()))
            {
                return false;
            }
            if (IsOccupiedByNonVacatingRobot(robot.Level, coordinates, side, robot, ignoreMovingObjects))
            {
                return false;
            }
            var belowCoords = coordinates.Below();
            var belowTile = robot.Level.Tiles[belowCoords];
            if (belowTile.IsConveyor(robot.Level, belowCoords))
            {
                var conveyor = (Conveyor)belowTile.GetEntity(robot.Level, belowCoords);
                var conveyorDirection = belowTile.GetDirection(robot.Level, belowCoords);
                if (conveyor.CurrentMode == ConveyorMode.Forwards)
                {
                    return side != conveyorDirection;
                }
                else if (conveyor.CurrentMode == ConveyorMode.Reverse)
                {
                    return side != conveyorDirection.Opposite();
                }
            }
            return true;
        }

        public bool CanPlaceOnSide(ILevel level, TileCoordinates coordinates, Direction direction)
        {
            var baseCoords = GetBase(level, coordinates);
            var baseTile = level.Tiles[baseCoords];
            if (direction == Direction.Up && coordinates.Y != (baseCoords.Y + baseTile.Height - 1))
            {
                return false; // No placing inside blocks
            }
            else
            {
                return baseTile.Behaviour.CanPlaceOnSide(level, baseCoords, direction);
            }
        }

        public bool CanEnterOnTop(ILevel level, TileCoordinates coordinates)
        {
            if (IsSolidOnSide(level, coordinates, Direction.Up))
            {
                return false;
            }
            if (IsOccupied(level, coordinates))
            {
                return false;
            }
            return true;
        }

        public bool CanEnterOnBottom(ILevel level, TileCoordinates coordinates)
        {
            if (IsSolidOnSide(level, coordinates, Direction.Down))
            {
                return false;
            }
            if (IsOccupied(level, coordinates))
            {
                return false;
            }
            return true;
        }

        public void Clear(ILevel level, TileCoordinates coordinates, bool notifyNeighbours = true)
        {
            if (IsExtension())
            {
                var below = coordinates.Below();
                level.Tiles[below].Clear(level, below, notifyNeighbours);
            }
            else
            {
                for (int i = 0; i < m_height; ++i)
                {
                    level.Tiles.SetTile(coordinates.Move(Direction.Up, i), Tiles.Air, FlatDirection.North, notifyNeighbours);
                }
            }
        }

        public void Init(ILevel level, TileCoordinates coordinates, FlatDirection direction)
        {
            level.Tiles.GetState(coordinates).Direction = direction;
            for (int i = 1; i < m_height; ++i)
            {
                level.Tiles.SetTile(coordinates.Move(Direction.Up, i), Tiles.Extension, direction, false);
            }
            OnInit(level, coordinates);
        }

        public void Shutdown(ILevel level, TileCoordinates coordinates)
        {
            OnShutdown(level, coordinates);
            for (int i = 1; i < m_height; ++i)
            {
                level.Tiles.SetTile(coordinates.Move(Direction.Up, i), Tiles.Air, FlatDirection.North, false);
            }
        }

        protected void OnRender(ILevel level, TileCoordinates coordinates, Geometry output, TextureAtlas textures)
        {
            if (!IsExtension())
            {
                m_behaviour.OnRender(level, coordinates, output, textures);
            }
        }

        protected void OnRenderLiquid(ILevel level, TileCoordinates coordinates, Geometry output)
        {
            if (!IsExtension())
            {
                m_behaviour.OnRenderLiquid(level, coordinates, output);
            }
        }

        protected void OnRenderShadows(ILevel level, TileCoordinates coordinates, Geometry output)
        {
            if (!IsExtension())
            {
                m_behaviour.OnRenderShadows(level, coordinates, output);
            }
        }

        public bool IsSolidOnSide(ILevel level, TileCoordinates coordinates, Direction side)
        {
            if (IsExtension())
            {
                var baseCoords = GetBase(level, coordinates);
                return level.Tiles[baseCoords].IsSolidOnSide(level, baseCoords, side);
            }
            else
            {
                var dir = GetDirection(level, coordinates);
                return m_solidity[(int)side.ToSide(dir)];
            }
        }

        public bool IsOpaqueOnSide(ILevel level, TileCoordinates coordinates, Direction side)
        {
            var baseCoords = GetBase(level, coordinates);
            var baseTile = level.Tiles[baseCoords];
            var result = baseTile.Behaviour.IsOpaqueOnSide(level, baseCoords, side);
            if (result.HasValue)
            {
                return result.Value;
            }
            else
            {
                var dir = baseTile.GetDirection(level, baseCoords);
                return baseTile.m_opacity[(int)side.ToSide(dir)];
            }
        }

        public bool IsReplaceable(ILevel level, TileCoordinates coordinates)
        {
            if (IsExtension())
            {
                var below = coordinates.Below();
                return level.Tiles[below].IsReplaceable(level, below);
            }
            else
            {
                return m_replaceable;
            }
        }

        public void Render(ILevel level, TileCoordinates coordinates, Geometry output, TextureAtlas textures)
        {
            if (!IsHidden(level, coordinates))
            {
                OnRender(level, coordinates, output, textures);
            }
        }

        public void RenderLiquid(ILevel level, TileCoordinates coordinates, Geometry output)
        {
            if (!IsHidden(level, coordinates))
            {
                OnRenderLiquid(level, coordinates, output);
            }
        }

        public void RenderShadows(ILevel level, TileCoordinates coordinates, Geometry output)
        {
            if (!IsHidden(level, coordinates))
            {
                OnRenderShadows(level, coordinates, output);
            }
        }

        public void SetHidden(ILevel level, TileCoordinates coordinates, bool hidden)
        {
            if (IsExtension())
            {
                var below = coordinates.Below();
                level.Tiles[below].SetHidden(level, below, hidden);
            }
            else
            {
                var state = level.Tiles.GetState(coordinates);
                if (state.Hidden != hidden)
                {
                    state.Hidden = hidden;
                    level.Tiles.RequestRebuild(coordinates);
                }
            }
        }

        public bool IsHidden(ILevel level, TileCoordinates coordinates)
        {
            var baseCoords = GetBase(level, coordinates);
            return level.Tiles.GetState(baseCoords).Hidden;
        }

        public FlatDirection GetDirection(ILevel level, TileCoordinates coordinates)
        {
            if (IsExtension())
            {
                var below = coordinates.Below();
                return level.Tiles[below].GetDirection(level, below);
            }
            else
            {
                return level.Tiles.GetState(coordinates).Direction;
            }
        }

        public void OnLevelStart(ILevel level, TileCoordinates coordinates)
        {
            if (IsExtension())
            {
                var below = coordinates.Below();
                level.Tiles[below].OnLevelStart(level, below);
            }
            else
            {
                m_behaviour.OnLevelStart(level, coordinates);
            }
        }

        public void NotifyNeighboursChanged(ILevel level, TileCoordinates coordinates)
        {
            if (IsExtension())
            {
                var below = coordinates.Below();
                level.Tiles[below].NotifyNeighboursChanged(level, below);
            }
            else
            {
                for (int y = 0; y < Height; ++y)
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        var dir = (Direction)i;
                        var neighbourCoords = new TileCoordinates(coordinates.X + dir.GetX(), coordinates.Y + y, coordinates.Z + dir.GetZ());
                        level.Tiles[neighbourCoords].OnNeighbourChanged(level, neighbourCoords);
                    }
                }

                var belowCoords = coordinates.Below();
                level.Tiles[belowCoords].OnNeighbourChanged(level, belowCoords);

                var aboveCoords = coordinates.Move(Direction.Up, Height);
                level.Tiles[aboveCoords].OnNeighbourChanged(level, aboveCoords);
            }
        }

        public void OnNeighbourChanged(ILevel level, TileCoordinates coordinates)
        {
            if (IsExtension())
            {
                var below = coordinates.Below();
                level.Tiles[below].OnNeighbourChanged(level, below);
            }
            else
            {
                m_behaviour.OnNeighbourChanged(level, coordinates);
            }
        }

        public void OnSteppedOn(ILevel level, TileCoordinates coordinates, Robot.Robot robot, FlatDirection direction)
        {
            if (IsExtension())
            {
                var below = coordinates.Below();
                level.Tiles[below].OnSteppedOn(level, below, robot, direction);
            }
            else
            {
                m_behaviour.OnSteppedOn(level, coordinates, robot, direction);
            }
        }

        public void OnSteppedOff(ILevel level, TileCoordinates coordinates, Robot.Robot robot, FlatDirection direction)
        {
            if (IsExtension())
            {
                var below = coordinates.Below();
                level.Tiles[below].OnSteppedOff(level, below, robot, direction);
            }
            else
            {
                m_behaviour.OnSteppedOff(level, coordinates, robot, direction);
            }
        }

        public bool IsPowered(ILevel level, TileCoordinates coordinates)
        {
            if (IsExtension())
            {
                var below = coordinates.Below();
                return level.Tiles[below].IsPowered(level, below);
            }
            else
            {
                for (int i = 0; i < 4; ++i)
                {
                    var dir = (Direction)i;
                    if (m_behaviour.AcceptsPower(level, coordinates, dir))
                    {
                        for (int y = 0; y < Height; ++y)
                        {
                            var neighbourCoords = new TileCoordinates(coordinates.X + dir.GetX(), coordinates.Y + y, coordinates.Z + dir.GetZ());
                            if (level.Tiles[neighbourCoords].GetPowerOutput(level, neighbourCoords, dir.Opposite()))
                            {
                                return true;
                            }
                        }
                    }
                }
                if (m_behaviour.AcceptsPower(level, coordinates, Direction.Down))
                {
                    var belowCoords = coordinates.Below();
                    if (level.Tiles[belowCoords].GetPowerOutput(level, belowCoords, Direction.Up))
                    {
                        return true;
                    }
                }
                if (m_behaviour.AcceptsPower(level, coordinates, Direction.Up))
                {
                    var aboveCoords = coordinates.Move(Direction.Up, Height);
                    if (level.Tiles[aboveCoords].GetPowerOutput(level, aboveCoords, Direction.Down))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool GetPowerOutput(ILevel level, TileCoordinates coordinates, Direction direction)
        {
            if (IsExtension())
            {
                var below = coordinates.Below();
                return level.Tiles[below].GetPowerOutput(level, below, direction);
            }
            else
            {
                return m_behaviour.GetPowerOutput(level, coordinates, direction);
            }
        }

        public TileCoordinates GetBase(ILevel level, TileCoordinates coordinates)
        {
            if (IsExtension())
            {
                var below = coordinates.Below();
                while (level.Tiles[below].IsExtension())
                {
                    below = below.Below();
                }
                return below;
            }
            else
            {
                return coordinates;
            }
        }

        public int GetIncline(ILevel level, TileCoordinates coordinates, FlatDirection side)
        {
            var baseCoords = GetBase(level, coordinates);
            int baseIncline;
            var baseTile = level.Tiles[baseCoords];
            var dir = baseTile.GetDirection(level, baseCoords);
            Side localSide = side.ToSide(dir);
            switch (localSide)
            {
                case Side.Front:
                default:
                    {
                        baseIncline = baseTile.m_forwardIncline;
                        break;
                    }
                case Side.Right:
                    {
                        baseIncline = baseTile.m_rightIncline;
                        break;
                    }
                case Side.Left:
                    {
                        baseIncline = -baseTile.m_rightIncline;
                        break;
                    }
                case Side.Back:
                    {
                        baseIncline = -baseTile.m_forwardIncline;
                        break;
                    }
            }

            int height = baseTile.Height;
            int location = coordinates.Y - baseCoords.Y;
            if (location >= height - Math.Abs(baseIncline))
            {
                return baseIncline;
            }
            return 0;
        }

        public void SetSubState(ILevel level, TileCoordinates coordinates, int subState, bool notifyNeighbours)
        {
            var baseCoords = GetBase(level, coordinates);
            level.Tiles.SetSubState(baseCoords, subState, notifyNeighbours);
        }

        public int GetSubState(ILevel level, TileCoordinates coordinates)
        {
            var baseCoords = GetBase(level, coordinates);
            return level.Tiles.GetState(baseCoords).SubState;
        }

        private void SetEntity(ILevel level, TileCoordinates coordinates, Entity entity)
        {
            var state = level.Tiles.GetState(coordinates);
            if (state.Entity != null)
            {
                level.Entities.Remove(state.Entity);
            }
            state.Entity = entity;
            if (state.Entity != null)
            {
                level.Entities.Add(state.Entity);
            }
        }

        public Entity GetEntity(ILevel level, TileCoordinates coordinates)
        {
            return level.Tiles.GetState(coordinates).Entity;
        }

        public bool IsExtension()
        {
            return m_behaviour is ExtensionTileBehaviour;
        }

        public bool IsTelepad(ILevel level, TileCoordinates coordinates)
        {
            return GetBehaviour(level, coordinates) is TelepadTileBehaviour;
        }

        public bool IsConveyor(ILevel level, TileCoordinates coordinates)
        {
            return GetBehaviour(level, coordinates) is ConveyorTileBehaviour;
        }

        public bool IsTurntable(ILevel level, TileCoordinates coordinates)
        {
            return GetBehaviour(level, coordinates) is TurntableTileBehaviour;
        }

        public bool IsGoal(ILevel level, TileCoordinates coordinates)
        {
            return GetBehaviour(level, coordinates) is GoalTileBehaviour;
        }

        public bool IsWire(ILevel level, TileCoordinates coordinates)
        {
            return GetBehaviour(level, coordinates) is WireTileBehaviour;
        }

        public bool IsCameraTarget(ILevel level, TileCoordinates coordinates)
        {
            return GetBehaviour(level, coordinates) is CameraTargetTileBehaviour;
        }

        public bool IsLiquid(ILevel level, TileCoordinates coordinates)
        {
            return GetBehaviour(level, coordinates) is LiquidTileBehaviour;
        }

        public bool IsXRay(ILevel level, TileCoordinates coordinates)
        {
            return GetBehaviour(level, coordinates) is XRayTileBehaviour;
        }

        public TileBehaviour GetBehaviour(ILevel level, TileCoordinates coordinates)
        {
            var baseCoords = GetBase(level, coordinates);
            var baseTile = level.Tiles[baseCoords];
            return baseTile.m_behaviour;
        }

        public static Matrix4 BuildTransform(TileCoordinates coordinates, FlatDirection direction)
        {
            if (direction != FlatDirection.North)
            {
                return
                    Matrix4.CreateTranslation(-0.5f, 0.0f, -0.5f) *
                    Matrix4.CreateRotationY(direction.ToYaw()) *
                    Matrix4.CreateTranslation((float)coordinates.X + 0.5f, (float)coordinates.Y * 0.5f, (float)coordinates.Z + 0.5f);
            }
            else
            {
                return
                    Matrix4.CreateTranslation((float)coordinates.X, (float)coordinates.Y * 0.5f, (float)coordinates.Z);
            }
        }
    }
}

