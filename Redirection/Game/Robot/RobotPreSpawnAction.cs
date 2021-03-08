using Dan200.Core.Animation;

namespace Dan200.Game.Robot
{
    public class RobotPreSpawnAction : RobotAction
    {
        public const float DURATION = 0.5f * Robot.STEP_TIME;

        public RobotPreSpawnAction()
        {
        }

        public override RobotState Update(RobotState state, float dt)
        {
            var timer = state.Level.TimeMachine.Time - state.TimeStamp;
            if (timer >= DURATION)
            {
                return RobotActions.Spawn.Init(state);
            }
            else
            {
                return state;
            }
        }

        public override IAnimation GetAnimation(RobotState state)
        {
            return LuaAnimation.Get("animation/invisible.anim.lua");
        }

        public override float GetAnimTime(RobotState state)
        {
            return 0.0f;
        }
    }
}
