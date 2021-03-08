using Dan200.Core.Animation;
using Dan200.Game.Level;
using OpenTK;

namespace Dan200.Game.Robot
{
    public class RobotConveyAction : RobotAction
    {
        private const float DURATION = Robot.STEP_TIME;

        private FlatDirection m_direction;

        public RobotConveyAction(FlatDirection direction)
        {
            m_direction = direction;
        }

        public override RobotState Init(RobotState state)
        {
            int incline = RobotAction.GetWalkIncline(state.Robot, state.Position, m_direction);
            var destination = state.Position.Move(m_direction);
            destination = destination.Move(0, incline, 0);
            state.Level.Tiles[destination.Below()].OnSteppedOn(state.Level, destination.Below(), state.Robot, m_direction);
            return base.Init(state).With(walkIncline: incline);
        }

        public override RobotState Update(RobotState state, float dt)
        {
            float timeInState = state.Level.TimeMachine.Time - state.TimeStamp;
            if (timeInState >= DURATION)
            {
                var destination = GetDestination(state);
                var below = state.Position.Below();
                state.Level.Tiles[below].OnSteppedOff(state.Level, below, state.Robot, m_direction);
                return PickNextAction(state.With(position: destination), allowTeleport: true, allowTurntable: true);
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

        public override TileCoordinates GetDestination(RobotState state)
        {
            return state.Position.Move(m_direction.GetX(), state.WalkIncline, m_direction.GetZ());
        }

        public override IAnimation GetAnimation(RobotState state)
        {
            if (m_direction == state.Direction)
            {
                return state.Robot.GetAnim("convey_forward");
            }
            else if (m_direction == state.Direction.Opposite())
            {
                return state.Robot.GetAnim("convey_back");
            }
            else if (m_direction == state.Direction.RotateLeft())
            {
                return state.Robot.GetAnim("convey_left");
            }
            else //if (m_direction == state.Direction.RotateRight())
            {
                return state.Robot.GetAnim("convey_right");
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

