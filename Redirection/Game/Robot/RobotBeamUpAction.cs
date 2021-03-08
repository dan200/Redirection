using Dan200.Core.Animation;

namespace Dan200.Game.Robot
{
    public class RobotBeamUpAction : RobotAction
    {
        public const float DURATION = 1.0f * Robot.STEP_TIME;

        public RobotBeamUpAction()
        {
        }

        public override RobotState Init(RobotState state)
        {
            state.Robot.PlaySound("beam_up");
            return base.Init(state);
        }

        public override RobotState Update(RobotState state, float dt)
        {
            return state;
        }

        public override IAnimation GetAnimation(RobotState state)
        {
            return state.Robot.GetAnim("beam_up");
        }

        public override float GetAnimTime(RobotState state)
        {
            float timer = state.Level.TimeMachine.Time - state.TimeStamp;
            return timer / DURATION;
        }
    }
}
