using Dan200.Core.Assets;
using Dan200.Core.Render;

namespace Dan200.Game.Level
{
    public enum ElevatorDirection
    {
        Up = 0,
        Down
    }

    public static class ElevatorDirectionExtensions
    {
        public static ElevatorDirection Opposite(this ElevatorDirection direction)
        {
            return (ElevatorDirection)(1 - (int)direction);
        }
    }

    public enum ElevatorTrigger
    {
        Powered,
        Unpowered
    }

    [TileBehaviour(name: "elevator")]
    public class ElevatorTileBehaviour : TileBehaviour
    {
        public readonly int Distance;
        public readonly float Speed;
        public readonly ElevatorDirection Direction;
        public readonly ElevatorTrigger Trigger;
        public readonly string RiseSoundPath;
        public readonly string FallSoundPath;

        public ElevatorTileBehaviour(Tile tile, KeyValuePairs kvp) : base(tile, kvp)
        {
            Distance = kvp.GetInteger("distance", 1);
            Speed = 1.0f / Robot.Robot.STEP_TIME;
            Direction = kvp.GetEnum("direction", ElevatorDirection.Up);
            Trigger = kvp.GetEnum("trigger", ElevatorTrigger.Powered);
            RiseSoundPath = kvp.GetString("rise_sound", null);
            FallSoundPath = kvp.GetString("fall_sound", null);
        }

        public override Entity CreateEntity(ILevel level, TileCoordinates coordinates)
        {
            return new Elevator(Tile, coordinates, Tile.GetDirection(level, coordinates));
        }

        public override bool AcceptsPower(ILevel level, TileCoordinates coordinates, Direction direction)
        {
            return true;
        }

        public override bool CanPlaceOnSide(ILevel level, TileCoordinates coordinates, Direction direction)
        {
            // The entity handles this
            return false;
        }

        public override void OnRender(ILevel level, TileCoordinates coordinates, Geometry output, TextureAtlas textures)
        {
            // The entity handles this
        }

        public override void OnRenderShadows(ILevel level, TileCoordinates coordinates, Geometry output)
        {
            // The entity handles this
        }
    }
}

