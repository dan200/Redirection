using Dan200.Core.Animation;
using Dan200.Game.Level;
using OpenTK;

namespace Dan200.Game.Robot
{
    public class RobotTeleportOutAction : RobotAction
    {
        private const float DURATION = Robot.STEP_TIME;

        public RobotTeleportOutAction()
        {
        }

        public override RobotState Init(RobotState state)
        {
            var belowPos = state.Position.Below();
            var teleporterPos = state.Level.Tiles[belowPos].GetBase(state.Level, belowPos);
            var teleporterTile = state.Level.Tiles[teleporterPos];
            var teleporterBehaviour = (TelepadTileBehaviour)teleporterTile.GetBehaviour(state.Level, teleporterPos);
            var destination = teleporterBehaviour.GetDestination(state.Level, teleporterPos);
            state.Robot.PlaySound("teleport_out");
            return base.Init(state).With(teleportDestination: destination);
        }

        public override RobotState Update(RobotState state, float dt)
        {
            float newTimer = state.Level.TimeMachine.Time - state.TimeStamp;
            if (newTimer >= DURATION)
            {
                return RobotActions.TeleportIn.Init(state);
            }
            else
            {
                return state;
            }
        }

        public override Vector3 GetPosition(RobotState state)
        {
            var position = state.Position;
            return new Vector3(
                (float)position.X + 0.5f,
                (float)position.Y * 0.5f,
                (float)position.Z + 0.5f
            );
        }

        public override IAnimation GetAnimation(RobotState state)
        {
            return state.Robot.GetAnim("teleport_out");
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

