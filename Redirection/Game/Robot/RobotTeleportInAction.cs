using Dan200.Core.Animation;
using Dan200.Game.Level;
using OpenTK;

namespace Dan200.Game.Robot
{
    public class RobotTeleportInAction : RobotAction
    {
        public const float DURATION = Robot.STEP_TIME;

        public RobotTeleportInAction()
        {
        }

        public override RobotState Init(RobotState state)
        {
            state.Robot.PlaySound("teleport_in");
            return base.Init(state);
        }

        public override RobotState Update(RobotState state, float dt)
        {
            float newTimer = state.Level.TimeMachine.Time - state.TimeStamp;
            if (newTimer >= DURATION)
            {
                return PickNextAction(state.With(position: GetDestination(state)));
            }
            else
            {
                return state;
            }
        }

        public override Vector3 GetPosition(RobotState state)
        {
            var position = GetDestination(state);
            return new Vector3(
                (float)position.X + 0.5f,
                (float)position.Y * 0.5f,
                (float)position.Z + 0.5f
            );
        }

        public override IAnimation GetAnimation(RobotState state)
        {
            return state.Robot.GetAnim("teleport_in");
        }

        public override float GetAnimTime(RobotState state)
        {
            var timer = state.Level.TimeMachine.Time - state.TimeStamp;
            var progress = timer / DURATION;
            return progress;
        }

        public override TileCoordinates GetDestination(RobotState state)
        {
            return state.TeleportDestination;
        }
    }
}

