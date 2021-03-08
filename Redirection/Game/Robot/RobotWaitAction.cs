using Dan200.Core.Animation;

namespace Dan200.Game.Robot
{
    public class RobotWaitAction : RobotAction
    {
        private float m_duration;

        public RobotWaitAction(int steps)
        {
            m_duration = (float)steps * Robot.STEP_TIME;
        }

        public override RobotState Init(RobotState state)
        {
            if (state.Action != this)
            {
                return base.Init(state);
            }
            else
            {
                return state;
            }
        }

        public override RobotState Update(RobotState state, float dt)
        {
            var timer = state.Level.TimeMachine.Time - state.TimeStamp;
            if (timer >= m_duration)
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
            return state.Robot.GetAnim("idle");
        }

        public override float GetAnimTime(RobotState state)
        {
            float offset = (float)(state.RandomSeed % 255) / 64.0f;
            return state.Level.TimeMachine.RealTime + offset;
        }
    }
}

