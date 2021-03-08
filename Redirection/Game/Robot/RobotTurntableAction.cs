using Dan200.Core.Animation;
using Dan200.Game.Level;

namespace Dan200.Game.Robot
{
    public class RobotTurntableAction : RobotAction
    {
        private const float DURATION = Robot.STEP_TIME;
        private TurnDirection m_turnDirection;

        public RobotTurntableAction(TurnDirection turnDirection)
        {
            m_turnDirection = turnDirection;
        }

        public override RobotState Init(RobotState state)
        {
            var tile = state.Level.Tiles[state.Position];
            ((Turntable)tile.GetEntity(state.Level, state.Position)).Turn();
            return base.Init(state);
        }

        public override RobotState Update(RobotState state, float dt)
        {
            var newTimer = state.Level.TimeMachine.Time - state.TimeStamp;
            if (newTimer >= DURATION)
            {
                var newDirection = state.Direction.Rotate(m_turnDirection);
                var canProceed = CanEnter(state.Robot, state.Position.Move(newDirection), newDirection, false);
                return PickNextAction(state.With(direction: newDirection), allowTurntable: !canProceed);
            }
            else
            {
                return state;
            }
        }

        public override TileCoordinates GetDestination(RobotState state)
        {
            return state.Position;
        }

        public override IAnimation GetAnimation(RobotState state)
        {
            if (m_turnDirection == TurnDirection.Left)
            {
                return state.Robot.GetAnim("turntable_left");
            }
            else
            {
                return state.Robot.GetAnim("turntable_right");
            }
        }

        public override float GetAnimTime(RobotState state)
        {
            var timer = state.Level.TimeMachine.Time - state.TimeStamp;
            var progress = timer / DURATION;
            return progress;
        }
    }
}

