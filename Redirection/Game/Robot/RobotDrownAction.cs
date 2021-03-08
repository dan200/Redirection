using Dan200.Core.Animation;
using Dan200.Game.Level;

namespace Dan200.Game.Robot
{
    public class RobotDrownAction : RobotAction
    {
        private const float DURATION = 4.0f * Robot.STEP_TIME;

        public RobotDrownAction()
        {
        }

        public override RobotState Init(RobotState state)
        {
            state.Robot.EmitOnDrown();
            return base.Init(state);
        }

        public override RobotState Update(RobotState state, float dt)
        {
            var timer = state.Level.TimeMachine.Time - state.TimeStamp;
            if (timer >= DURATION)
            {
                var newPosition = GetDestination(state);
                return RobotActions.Drowned.Init(state.With(position: newPosition));
            }
            else
            {
                return state;
            }
        }

        public override TileCoordinates GetDestination(RobotState state)
        {
            return base.GetDestination(state).Move(Direction.Down, 2);
        }

        public override IAnimation GetAnimation(RobotState state)
        {
            return state.Robot.GetAnim("drown");
        }

        public override float GetAnimTime(RobotState state)
        {
            return (state.Level.TimeMachine.Time - state.TimeStamp) / DURATION;
        }
    }
}

