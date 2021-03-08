using System;
using Dan200.Game.Level;

namespace Dan200.Game.Robot
{
	public class RobotHalfWaitAction : RobotAction
	{
        private const float DURATION = 0.5f * Robot.STEP_TIME;

        public RobotHalfWaitAction()
		{
		}

		public override RobotState Init( RobotState state )
		{
            if( state.Action != this )
            {
                return base.Init( state );
            }
            else
            {
                return state;
            }
		}

		public override RobotState Update( RobotState state, float dt )
		{
            var timer = state.Level.TimeMachine.Time - state.TimeStamp;
            if( timer >= DURATION )
            {
                return PickNextAction( state );
            }
            else
            {
                return state;
            }
		}
	}
}

