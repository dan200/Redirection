using Dan200.Core.Animation;

namespace Dan200.Game.Robot
{
    public class RobotDrownedAction : RobotAction
    {
        public RobotDrownedAction()
        {
        }

        public override RobotState Init(RobotState state)
        {
            return base.Init(state);
        }

        public override RobotState Update(RobotState state, float dt)
        {
            return state;
        }

        public override IAnimation GetAnimation(RobotState state)
        {
            return state.Robot.GetAnim("drowned");
        }

        public override float GetAnimTime(RobotState state)
        {
            return 0.0f;
        }
    }
}

