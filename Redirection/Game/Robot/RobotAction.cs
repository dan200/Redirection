using Dan200.Core.Animation;
using Dan200.Game.Level;
using OpenTK;
using System;

namespace Dan200.Game.Robot
{
    public abstract class RobotAction
    {
        protected RobotAction()
        {
        }

        public virtual RobotState Init(RobotState state)
        {
            return state.With(action: this, timeStamp: state.Robot.Level.TimeMachine.Time);
        }

        public virtual RobotState Update(RobotState state, float dt)
        {
            return state;
        }

        public float GetYaw(RobotState state)
        {
            return state.Direction.ToYaw();
        }

        public virtual Vector3 GetPosition(RobotState state)
        {
            var position = state.Position;
            return new Vector3(
                (float)position.X + 0.5f,
                (float)position.Y * 0.5f,
                (float)position.Z + 0.5f
            );
        }

        public virtual IAnimation GetAnimation(RobotState state)
        {
            return state.Robot.GetAnim("idle");
        }

        public virtual float GetAnimTime(RobotState state)
        {
            float offset = (float)(state.RandomSeed % 255) / 64.0f;
            return state.Level.TimeMachine.Time + offset;
        }

        public void GetLightInfo(RobotState state, out Vector3 o_position, out Vector3 o_colour)
        {
            var pos = GetPosition(state);
            var anim = GetAnimation(state);
            if (anim != null)
            {
                bool visible;
                Matrix4 transform;
                Vector2 uvOffset;
                Vector2 uvScale;
                Vector4 colour;
                float cameraFOV;
                anim.Animate("Light", GetAnimTime(state), out visible, out transform, out uvOffset, out uvScale, out colour, out cameraFOV);
                o_position = pos + transform.Row3.Xyz;
                o_colour = visible ? (colour.Xyz * colour.W) : Vector3.Zero;
            }
            else
            {
                o_position = pos;
                o_colour = Vector3.One;
            }
        }

        public virtual float GetLightBrightness(RobotState state)
        {
            return 1.0f;
        }

        public virtual TileCoordinates GetDestination(RobotState state)
        {
            return state.Position;
        }

        protected static bool CanEnter(Robot robot, TileCoordinates position, FlatDirection direction, bool ignoreMovingObjects)
        {
            return
                robot.Level.Tiles[position].CanEnterOnSide(robot, position, direction.Opposite(), ignoreMovingObjects) &&
                robot.Level.Tiles[position.Above()].CanEnterOnSide(robot, position.Above(), direction.Opposite(), ignoreMovingObjects);
        }

        protected static TurnDirection GetTurnDirection(Robot robot, TileCoordinates position, FlatDirection direction, TurnDirection preference, out bool o_uTurn, bool ignoreMovingObjects)
        {
            var left = direction.RotateLeft();
            var right = direction.RotateRight();

            bool canGoLeft = CanEnter(robot, position.Move(left), left, ignoreMovingObjects);
            bool canGoRight = CanEnter(robot, position.Move(right), right, ignoreMovingObjects);
            if (canGoRight && !canGoLeft)
            {
                o_uTurn = false;
                return TurnDirection.Right;
            }
            else if (canGoLeft && !canGoRight)
            {
                o_uTurn = false;
                return TurnDirection.Left;
            }
            else
            {
                o_uTurn = (!canGoLeft && !canGoRight);
                return preference;
            }
        }

        public static int GetWalkIncline(Robot robot, TileCoordinates position, FlatDirection direction)
        {
            var level = robot.Level;
            var here = position;
            var below = here.Below();
            int incline = Math.Min(level.Tiles[below].GetIncline(level, below, direction), 0);
            if (incline < 0 && !CanEnter(robot, here.Move(direction).Move(Direction.Up, incline), direction, false))
            {
                incline = 0;
            }
            var destination = here.Move(direction).Move(Direction.Up, incline);
            incline += level.Tiles[destination].GetIncline(level, destination, direction);
            return incline;
        }

        private static RobotAction NextAction(RobotState state, bool allowTeleport = false, bool allowTurntable = false, bool ignoreMovingObjects = false)
        {
            var level = state.Level;
            var position = state.Position;
            var below = position.Below();
            var dir = state.Direction;

            var currentTile = level.Tiles[position];
            var belowTile = level.Tiles[below];

            // Try drowning
            if (belowTile.IsLiquid(level, below))
            {
                return RobotActions.Drown;
            }

            // Try falling
            if (belowTile.CanEnterOnTop(level, below))
            {
                return RobotActions.Fall;
            }

            // Try sitting at goal
            if (currentTile.IsGoal(level, position))
            {
                var colour = ((GoalTileBehaviour)currentTile.GetBehaviour(level, position)).Colour;
                if (colour == state.Robot.Colour)
                {
                    return RobotActions.Goal;
                }
            }

            // Try teleporting
            if (allowTeleport && belowTile.IsTelepad(level, below))
            {
                var basePos = belowTile.GetBase(level, below);
                var baseTile = level.Tiles[basePos];
                var destination = ((TelepadTileBehaviour)baseTile.GetBehaviour(level, basePos)).GetDestination(level, basePos);
                if (destination.HasValue)
                {
                    return RobotActions.TeleportOut;
                }
            }

            // Try conveying
            if (belowTile.IsConveyor(level, below))
            {
                var conveyor = ((Conveyor)belowTile.GetEntity(level, below));
                var conveyorDirection = belowTile.GetDirection(level, below);
                if (conveyor.CurrentMode == ConveyorMode.Forwards)
                {
                    var conveyDirection = conveyorDirection;
                    if (CanEnter(state.Robot, position.Move(conveyDirection), conveyDirection, false))
                    {
                        return RobotActions.GetConvey(conveyDirection);
                    }
                }
                else if (conveyor.CurrentMode == ConveyorMode.Reverse)
                {
                    var conveyDirection = conveyorDirection.Opposite();
                    if (CanEnter(state.Robot, position.Move(conveyDirection), conveyDirection, false))
                    {
                        return RobotActions.GetConvey(conveyDirection);
                    }
                }
            }

            // Try turntabling
            if (allowTurntable && currentTile.IsTurntable(level, position))
            {
                var turn = ((TurntableTileBehaviour)currentTile.GetBehaviour(level, position)).TurnDirection;
                switch (turn)
                {
                    case TurnDirection.Left:
                        {
                            return RobotActions.TurntableLeft;
                        }
                    case TurnDirection.Right:
                        {
                            return RobotActions.TurntableRight;
                        }
                }
            }

            // Try moving
            if (!state.Robot.Immobile)
            {
                if (CanEnter(state.Robot, position.Move(dir), dir, ignoreMovingObjects))
                {
                    // Walk forward
                    int incline = GetWalkIncline(state.Robot, state.Position, state.Direction);
                    var nextPos = position.Move(dir.GetX(), incline, dir.GetZ());
                    var nextAction = NextAction(state.With(action: RobotActions.WalkLookForward, position: nextPos), allowTeleport: true, allowTurntable: true, ignoreMovingObjects: true);
                    TurnDirection? previousLookDir = null;
                    if (state.Action is RobotWalkAction)
                    {
                        previousLookDir = ((RobotWalkAction)state.Action).LookDirection;
                    }
                    if (nextAction is RobotTurnAction)
                    {
                        var turnAction = (RobotTurnAction)nextAction;
                        return RobotActions.GetWalk(previousLookDir, turnAction.Direction);
                    }
                    else if (nextAction is RobotUTurnAction)
                    {
                        var turnAction = (RobotUTurnAction)nextAction;
                        return RobotActions.GetWalk(previousLookDir, turnAction.Direction);
                    }
                    else
                    {
                        return RobotActions.GetWalk(previousLookDir, null);
                    }
                }
                else
                {
                    // Turn
                    TurnDirection? lookDir = null;
                    if (state.Action is RobotWalkAction)
                    {
                        lookDir = ((RobotWalkAction)state.Action).LookDirection;
                    }

                    bool uTurn;
                    var turn = GetTurnDirection(state.Robot, position, dir, state.TurnPreference, out uTurn, false);
                    if (uTurn)
                    {
                        return RobotActions.GetUTurn(turn, lookDir);
                    }
                    else
                    {
                        return RobotActions.GetTurn(turn, lookDir);
                    }
                }
            }

            // Wait
            return RobotActions.Wait;
        }

        protected static RobotState PickNextAction(RobotState state, bool allowTeleport = false, bool allowTurntable = false)
        {
            var action = NextAction(state, allowTeleport, allowTurntable, ignoreMovingObjects: false);
            return action.Init(state);
        }
    }
}

