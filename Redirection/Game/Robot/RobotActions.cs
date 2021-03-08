using Dan200.Game.Level;

namespace Dan200.Game.Robot
{
    public static class RobotActions
    {
        public static RobotPreSpawnAction PreSpawn = new RobotPreSpawnAction();
        public static RobotSpawnAction Spawn = new RobotSpawnAction();

        public static RobotBeamDownAction BeamDown = new RobotBeamDownAction();
        public static RobotBeamUpAction BeamUp = new RobotBeamUpAction();

        public static RobotWaitAction Wait = new RobotWaitAction(1);
        public static RobotWaitAction LongWait = new RobotWaitAction(2);

        public static RobotWalkAction WalkLookForward = new RobotWalkAction(null, null);
        public static RobotWalkAction WalkLookForwardFromLeft = new RobotWalkAction(TurnDirection.Left, null);
        public static RobotWalkAction WalkLookForwardFromRight = new RobotWalkAction(TurnDirection.Right, null);

        public static RobotWalkAction WalkLookLeft = new RobotWalkAction(TurnDirection.Left, TurnDirection.Left);
        public static RobotWalkAction WalkLookLeftFromForward = new RobotWalkAction(null, TurnDirection.Left);
        public static RobotWalkAction WalkLookLeftFromRight = new RobotWalkAction(TurnDirection.Right, TurnDirection.Left);

        public static RobotWalkAction WalkLookRight = new RobotWalkAction(TurnDirection.Right, TurnDirection.Right);
        public static RobotWalkAction WalkLookRightFromLeft = new RobotWalkAction(TurnDirection.Left, TurnDirection.Right);
        public static RobotWalkAction WalkLookRightFromForward = new RobotWalkAction(null, TurnDirection.Right);

        public static RobotTurnAction TurnLeft = new RobotTurnAction(TurnDirection.Left);
        public static RobotTurnAction TurnLeftFromLookLeft = new RobotTurnAction(TurnDirection.Left, TurnDirection.Left);
        public static RobotTurnAction TurnLeftFromLookRight = new RobotTurnAction(TurnDirection.Left, TurnDirection.Right);
        public static RobotTurnAction TurnRight = new RobotTurnAction(TurnDirection.Right);
        public static RobotTurnAction TurnRightFromLookLeft = new RobotTurnAction(TurnDirection.Right, TurnDirection.Left);
        public static RobotTurnAction TurnRightFromLookRight = new RobotTurnAction(TurnDirection.Right, TurnDirection.Right);

        public static RobotUTurnAction UTurnLeft = new RobotUTurnAction(TurnDirection.Left);
        public static RobotUTurnAction UTurnLeftFromLookLeft = new RobotUTurnAction(TurnDirection.Left, TurnDirection.Left);
        public static RobotUTurnAction UTurnLeftFromLookRight = new RobotUTurnAction(TurnDirection.Left, TurnDirection.Right);
        public static RobotUTurnAction UTurnRight = new RobotUTurnAction(TurnDirection.Right);
        public static RobotUTurnAction UTurnRightFromLookLeft = new RobotUTurnAction(TurnDirection.Right, TurnDirection.Left);
        public static RobotUTurnAction UTurnRightFromLookRight = new RobotUTurnAction(TurnDirection.Right, TurnDirection.Right);

        public static RobotConveyAction ConveyNorth = new RobotConveyAction(FlatDirection.North);
        public static RobotConveyAction ConveyEast = new RobotConveyAction(FlatDirection.East);
        public static RobotConveyAction ConveySouth = new RobotConveyAction(FlatDirection.South);
        public static RobotConveyAction ConveyWest = new RobotConveyAction(FlatDirection.West);

        public static RobotTurntableAction TurntableLeft = new RobotTurntableAction(TurnDirection.Left);
        public static RobotTurntableAction TurntableRight = new RobotTurntableAction(TurnDirection.Right);

        public static RobotFallAction Fall = new RobotFallAction();

        public static RobotTeleportOutAction TeleportOut = new RobotTeleportOutAction();
        public static RobotTeleportInAction TeleportIn = new RobotTeleportInAction();

        public static RobotGoalAction Goal = new RobotGoalAction();

        public static RobotDrownAction Drown = new RobotDrownAction();
        public static RobotDrownedAction Drowned = new RobotDrownedAction();

        public static RobotWalkAction GetWalk(TurnDirection? previousLookDir, TurnDirection? lookDir)
        {
            if (lookDir.HasValue)
            {
                if (lookDir.Value == TurnDirection.Left)
                {
                    // ->Left
                    if (previousLookDir.HasValue)
                    {
                        if (previousLookDir.Value == TurnDirection.Left)
                        {
                            return RobotActions.WalkLookLeft;
                        }
                        else
                        {
                            return RobotActions.WalkLookLeftFromRight;
                        }
                    }
                    return RobotActions.WalkLookLeftFromForward;
                }
                else
                {
                    // ->Right
                    if (previousLookDir.HasValue)
                    {
                        if (previousLookDir.Value == TurnDirection.Left)
                        {
                            return RobotActions.WalkLookRightFromLeft;
                        }
                        else
                        {
                            return RobotActions.WalkLookRight;
                        }
                    }
                    return RobotActions.WalkLookRightFromForward;
                }
            }
            // ->Forward
            if (previousLookDir.HasValue)
            {
                if (previousLookDir.Value == TurnDirection.Left)
                {
                    return RobotActions.WalkLookForwardFromLeft;
                }
                else
                {
                    return RobotActions.WalkLookForwardFromRight;
                }
            }
            return RobotActions.WalkLookForward;
        }

        public static RobotTurnAction GetTurn(TurnDirection dir, TurnDirection? initialLookDir)
        {
            if (initialLookDir.HasValue)
            {
                if (initialLookDir.Value == TurnDirection.Left)
                {
                    return dir == TurnDirection.Left ? TurnLeftFromLookLeft : TurnRightFromLookLeft;
                }
                else
                {
                    return dir == TurnDirection.Left ? TurnLeftFromLookRight : TurnRightFromLookRight;
                }
            }
            else
            {
                return dir == TurnDirection.Left ? TurnLeft : TurnRight;
            }
        }

        public static RobotUTurnAction GetUTurn(TurnDirection dir, TurnDirection? initialLookDir)
        {
            if (initialLookDir.HasValue)
            {
                if (initialLookDir.Value == TurnDirection.Left)
                {
                    return dir == TurnDirection.Left ? UTurnLeftFromLookLeft : UTurnRightFromLookLeft;
                }
                else
                {
                    return dir == TurnDirection.Left ? UTurnLeftFromLookRight : UTurnRightFromLookRight;
                }
            }
            else
            {
                return dir == TurnDirection.Left ? UTurnLeft : UTurnRight;
            }
        }

        public static RobotConveyAction GetConvey(FlatDirection dir)
        {
            switch (dir)
            {
                case FlatDirection.North:
                default:
                    {
                        return ConveyNorth;
                    }
                case FlatDirection.East:
                    {
                        return ConveyEast;
                    }
                case FlatDirection.South:
                    {
                        return ConveySouth;
                    }
                case FlatDirection.West:
                    {
                        return ConveyWest;
                    }
            }
        }
    }
}

