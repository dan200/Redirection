using Dan200.Core.Animation;
using Dan200.Game.Level;

namespace Dan200.Game.Robot
{
    public class RobotFallAction : RobotAction
    {
        private const float DURATION = 0.5f * Robot.STEP_TIME;

        public RobotFallAction()
        {
        }

        public override RobotState Init(RobotState state)
        {
            if (state.Position.Y == state.Level.Tiles.MinY - 1)
            {
                state.Robot.EmitOnFall();
            }
            return base.Init(state);
        }

        public override RobotState Update(RobotState state, float dt)
        {
            var timer = state.Level.TimeMachine.Time - state.TimeStamp;
            if (timer >= DURATION && state.Position.Y > state.Level.Tiles.MinY - 2)
            {
                var below = state.Position.Below();
                var belowBelow = below.Below();
                if (state.Level.Tiles[belowBelow].IsLiquid(state.Level, belowBelow))
                {
                    return RobotActions.Drown.Init(state.With(position: below));
                }
                else if (state.Level.Tiles[belowBelow].CanEnterOnTop(state.Level, belowBelow))
                {
                    return RobotActions.Fall.Init(state.With(position: below));
                }
                else
                {
                    state.Level.Tiles[belowBelow].OnSteppedOn(state.Level, belowBelow, state.Robot, state.Direction);
                    return PickNextAction(state.With(position: below), allowTeleport: true, allowTurntable: true);
                }
            }
            else
            {
                return state;
            }
        }

        public override TileCoordinates GetDestination(RobotState data)
        {
            return data.Position.Below();
        }

        public override IAnimation GetAnimation(RobotState state)
        {
            if (state.Position.Y <= state.Level.Tiles.MinY)
            {
                return state.Robot.GetAnim("fall_dead");
            }
            else
            {
                return state.Robot.GetAnim("fall");
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

