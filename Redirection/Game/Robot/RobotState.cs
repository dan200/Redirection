using Dan200.Game.Level;

namespace Dan200.Game.Robot
{
    public class RobotState : EntityState
    {
        public readonly Robot Robot;
        public readonly ILevel Level;
        public readonly int RandomSeed;

        public readonly TileCoordinates Position;
        public readonly FlatDirection Direction;

        public readonly TurnDirection TurnPreference;

        public readonly RobotAction Action;
        public readonly float TimeStamp;

        public readonly int WalkIncline;
        public readonly TileCoordinates TeleportDestination;

        public RobotState(
            Robot robot,
            int seed,
            TileCoordinates position,
            FlatDirection direction,
            TurnDirection turnPreference,
            RobotAction action
        ) : this(robot, seed, position, direction, turnPreference, action, 0.0f, 0, position)
        {
        }

        private RobotState(
            Robot robot,
            int seed,
            TileCoordinates position,
            FlatDirection direction,
            TurnDirection turnPreference,
            RobotAction action,
            float timeStamp,
            int walkIncline,
            TileCoordinates teleportDestination
        )
        {
            Robot = robot;
            Level = robot.Level;
            RandomSeed = seed;
            Position = position;
            Direction = direction;
            TurnPreference = turnPreference;
            Action = action;
            TimeStamp = timeStamp;
            WalkIncline = walkIncline;
            TeleportDestination = teleportDestination;
        }

        public RobotState With(
            TileCoordinates? position = null,
            FlatDirection? direction = null,
            TurnDirection? turnPreference = null,
            RobotAction action = null,
            float? timeStamp = null,
            int? walkIncline = null,
            TileCoordinates? teleportDestination = null
        )
        {
            return new RobotState(
                Robot,
                RandomSeed,
                position.HasValue ? position.Value : Position,
                direction.HasValue ? direction.Value : Direction,
                turnPreference.HasValue ? turnPreference.Value : TurnPreference,
                action != null ? action : Action,
                timeStamp.HasValue ? timeStamp.Value : TimeStamp,
                walkIncline.HasValue ? walkIncline.Value : WalkIncline,
                teleportDestination.HasValue ? teleportDestination.Value : TeleportDestination
            );
        }
    }
}

