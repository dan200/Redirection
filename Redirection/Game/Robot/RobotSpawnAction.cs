using Dan200.Core.Animation;

namespace Dan200.Game.Robot
{
    public class RobotSpawnAction : RobotAction
    {
        public const float DURATION = 1.5f * Robot.STEP_TIME;

        public RobotSpawnAction()
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
                return RobotActions.Wait.Init(state);
            }
            else
            {
                return state;
            }
        }

        public override IAnimation GetAnimation(RobotState state)
        {
            var timer = state.Level.TimeMachine.Time - state.TimeStamp;
            if (timer < Robot.STEP_TIME)
            {
                return state.Robot.GetAnim("teleport_in");
            }
            else
            {
                return state.Robot.GetAnim("idle");
            }
        }

        public override float GetAnimTime(RobotState state)
        {
            var timer = state.Level.TimeMachine.Time - state.TimeStamp;
            if (timer < Robot.STEP_TIME)
            {
                return timer / Robot.STEP_TIME;
            }
            else
            {
                float offset = (float)(state.RandomSeed % 255) / 64.0f;
                return state.Level.TimeMachine.RealTime + offset;
            }
        }
    }
}
