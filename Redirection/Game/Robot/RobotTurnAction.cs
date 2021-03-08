using Dan200.Core.Animation;
using Dan200.Game.Level;

namespace Dan200.Game.Robot
{
    public class RobotTurnAction : RobotAction
    {
        private const float DURATION = Robot.STEP_TIME;
        private TurnDirection m_turnDirection;
        private TurnDirection? m_initialLookDirection;

        public TurnDirection Direction
        {
            get
            {
                return m_turnDirection;
            }
        }

        public RobotTurnAction(TurnDirection turnDirection, TurnDirection? initialLookDirection = null)
        {
            m_turnDirection = turnDirection;
            m_initialLookDirection = initialLookDirection;
        }

        public override RobotState Init(RobotState state)
        {
            state.Robot.PlaySound("turn");
            state.Robot.PlaySound("idle_loop", true);
            return base.Init(state);
        }

        public override RobotState Update(RobotState state, float dt)
        {
            var newTimer = state.Level.TimeMachine.Time - state.TimeStamp;
            if (newTimer >= DURATION)
            {
                var newDirection = state.Direction.Rotate(m_turnDirection);
                var canProceed = CanEnter(state.Robot, state.Position.Move(newDirection), newDirection, false);
                var newState = PickNextAction(state.With(direction: newDirection), allowTurntable: !canProceed);
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

        public override IAnimation GetAnimation(RobotState state)
        {
            if (m_turnDirection == TurnDirection.Left)
            {
                if (m_initialLookDirection.HasValue)
                {
                    if (m_initialLookDirection.Value == TurnDirection.Left)
                    {
                        return state.Robot.GetAnim("turn_left_from_look_left");
                    }
                    else
                    {
                        return state.Robot.GetAnim("turn_left_from_look_right");
                    }
                }
                else
                {
                    return state.Robot.GetAnim("turn_left");
                }
            }
            else
            {
                if (m_initialLookDirection.HasValue)
                {
                    if (m_initialLookDirection.Value == TurnDirection.Left)
                    {
                        return state.Robot.GetAnim("turn_right_from_look_left");
                    }
                    else
                    {
                        return state.Robot.GetAnim("turn_right_from_look_right");
                    }
                }
                else
                {
                    return state.Robot.GetAnim("turn_right");
                }
            }
        }

        public override float GetAnimTime(RobotState state)
        {
            var timer = state.Level.TimeMachine.Time - state.TimeStamp;
            var progress = timer / DURATION;
            return progress;
        }
    }
}

