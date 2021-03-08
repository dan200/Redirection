using Dan200.Core.Animation;
using Dan200.Game.Level;
using OpenTK;

namespace Dan200.Game.Robot
{
    public class RobotWalkAction : RobotAction
    {
        private const float DURATION = Robot.STEP_TIME;

        private TurnDirection? m_lookDirection;
        private TurnDirection? m_previousLookDirection;

        public TurnDirection? LookDirection
        {
            get
            {
                return m_lookDirection;
            }
        }

        public RobotWalkAction(TurnDirection? previousLookDirection, TurnDirection? lookDirection)
        {
            m_previousLookDirection = previousLookDirection;
            m_lookDirection = lookDirection;
        }

        public override RobotState Init(RobotState state)
        {
            int incline = RobotAction.GetWalkIncline(state.Robot, state.Position, state.Direction);
            var destination = state.Position.Move(state.Direction);
            destination = destination.Move(0, incline, 0);
            state.Level.Tiles[destination.Below()].OnSteppedOn(state.Level, destination.Below(), state.Robot, state.Direction);
            state.Robot.PlaySound("idle_loop", true);
            return base.Init(state).With(walkIncline: incline);
        }

        public override RobotState Update(RobotState state, float dt)
        {
            float newTimer = state.Level.TimeMachine.Time - state.TimeStamp;
            if (newTimer >= DURATION)
            {
                var destination = GetDestination(state);
                var below = state.Position.Below();
                state.Level.Tiles[below].OnSteppedOff(state.Level, below, state.Robot, state.Direction);
                var newState = PickNextAction(state.With(position: destination), allowTeleport: true, allowTurntable: true);
                if (!(newState.Action is RobotWalkAction || newState.Action is RobotTurnAction || newState.Action is RobotUTurnAction))
                {
                    state.Robot.StopSound("idle_loop");
                }
                return newState;
            }
            else
            {
                return state;
            }
        }

        public override Vector3 GetPosition(RobotState state)
        {
            var timeInState = state.Level.TimeMachine.Time - state.TimeStamp;
            var progress = timeInState / DURATION;
            var pos = base.GetPosition(state);
            pos.Y += (float)state.WalkIncline * progress * 0.5f;
            return pos;
        }

        public override IAnimation GetAnimation(RobotState state)
        {
            if (m_lookDirection.HasValue)
            {
                if (m_lookDirection == TurnDirection.Left)
                {
                    // ->Left
                    if (m_previousLookDirection.HasValue)
                    {
                        return m_previousLookDirection.Value == TurnDirection.Left ?
                            state.Robot.GetAnim("walk_look_left") :
                            state.Robot.GetAnim("walk_look_left_from_right");
                    }
                    return state.Robot.GetAnim("walk_look_left_from_forward");
                }
                else
                {
                    // ->Right
                    if (m_previousLookDirection.HasValue)
                    {
                        return m_previousLookDirection.Value == TurnDirection.Left ?
                            state.Robot.GetAnim("walk_look_right_from_left") :
                            state.Robot.GetAnim("walk_look_right");
                    }
                    return state.Robot.GetAnim("walk_look_right_from_forward");
                }
            }
            else
            {
                // ->Forward
                if (m_previousLookDirection.HasValue)
                {
                    return m_previousLookDirection.Value == TurnDirection.Left ?
                        state.Robot.GetAnim("walk_look_forward_from_left") :
                        state.Robot.GetAnim("walk_look_forward_from_right");
                }
                return state.Robot.GetAnim("walk_look_forward");
            }
        }

        public override float GetAnimTime(RobotState state)
        {
            var timer = state.Level.TimeMachine.Time - state.TimeStamp;
            var progress = timer / DURATION;
            return progress;
        }

        public override TileCoordinates GetDestination(RobotState state)
        {
            return state.Position.Move(state.Direction.GetX(), state.WalkIncline, state.Direction.GetZ());
        }
    }
}

