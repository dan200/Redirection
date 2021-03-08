using Dan200.Core.Animation;
using Dan200.Game.Level;

namespace Dan200.Game.Robot
{
    public class RobotGoalAction : RobotAction
    {
        public const float DURATION = 2.0f * Robot.STEP_TIME;

        public RobotGoalAction()
        {
        }

        public override RobotState Init(RobotState state)
        {
            var goal = state.Level.Tiles[state.Position].GetBehaviour(state.Level, state.Position) as GoalTileBehaviour;
            if (goal != null)
            {
                goal.OnOccupy(state.Level, state.Position, state.Robot);
            }
            return base.Init(state);
        }

        public override RobotState Update(RobotState state, float dt)
        {
            return state;
        }

        public override IAnimation GetAnimation(RobotState state)
        {
            return state.Robot.GetAnim("at_goal");
        }

        public override float GetAnimTime(RobotState state)
        {
            return state.Level.TimeMachine.Time - state.TimeStamp;
        }
    }
}

