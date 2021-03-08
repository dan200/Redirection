using Dan200.Core.Animation;

namespace Dan200.Game.Robot
{
    public class RobotBeamDownAction : RobotAction
    {
        public const float DURATION = 1.0f * Robot.STEP_TIME;

        public RobotBeamDownAction()
        {
        }

        public override RobotState Init(RobotState state)
        {
            state.Robot.PlaySound("beam_down");
            return base.Init(state);
        }

        public override RobotState Update(RobotState state, float dt)
        {
            var timer = state.Level.TimeMachine.Time - state.TimeStamp;
            if (timer >= DURATION && !state.Level.InEditor)
            {
                return PickNextAction(state);
            }
            else
            {
                return state;
            }
        }

        public override IAnimation GetAnimation(RobotState state)
        {
            return state.Robot.GetAnim("beam_down");
        }

        public override float GetAnimTime(RobotState state)
        {
            float timer = state.Level.TimeMachine.Time - state.TimeStamp;
            return timer / DURATION;
        }
    }
}
