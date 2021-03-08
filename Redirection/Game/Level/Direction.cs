using OpenTK;

namespace Dan200.Game.Level
{
    public enum Direction
    {
        North = 0,
        East,
        South,
        West,
        Up,
        Down
    }

    public static class DirectionExtensions
    {
        private static int[] DIR_TO_X = { 0, -1, 0, 1, 0, 0 };
        private static int[] DIR_TO_Y = { 0, 0, 0, 0, 1, -1 };
        private static int[] DIR_TO_Z = { 1, 0, -1, 0, 0, 0 };
        private static int[] ROTATE_LEFT = { 3, 0, 1, 2, 4, 5 };
        private static int[] ROTATE_RIGHT = { 1, 2, 3, 0, 4, 5 };
        private static int[] ROTATE_180 = { 2, 3, 0, 1, 4, 5 };
        private static int[] OPPOSITE = { 2, 3, 0, 1, 5, 4 };

        public static bool IsFlat(this Direction dir)
        {
            FlatDirection unused;
            return IsFlat(dir, out unused);
        }

        public static bool IsFlat(this Direction dir, out FlatDirection o_flatDir)
        {
            o_flatDir = (FlatDirection)dir;
            return dir < Direction.Up;
        }

        public static int GetX(this Direction dir)
        {
            return DIR_TO_X[(int)dir];
        }

        public static int GetY(this Direction dir)
        {
            return DIR_TO_Y[(int)dir];
        }

        public static int GetZ(this Direction dir)
        {
            return DIR_TO_Z[(int)dir];
        }

        public static Vector3 ToVector(this Direction dir)
        {
            return new Vector3(
                DIR_TO_X[(int)dir],
                DIR_TO_Y[(int)dir],
                DIR_TO_Z[(int)dir]
            );
        }

        public static Direction RotateLeft(this Direction dir)
        {
            return (Direction)ROTATE_LEFT[(int)dir];
        }

        public static Direction RotateRight(this Direction dir)
        {
            return (Direction)ROTATE_RIGHT[(int)dir];
        }

        public static Direction Rotate180(this Direction dir)
        {
            return (Direction)ROTATE_180[(int)dir];
        }

        public static Direction Opposite(this Direction dir)
        {
            return (Direction)OPPOSITE[(int)dir];
        }

        public static Side ToSide(this Direction dir, FlatDirection forwardDir)
        {
            switch (forwardDir)
            {
                case FlatDirection.North:
                default:
                    {
                        return (Side)dir;
                    }
                case FlatDirection.East:
                    {
                        return (Side)dir.RotateLeft();
                    }
                case FlatDirection.South:
                    {
                        return (Side)dir.Rotate180();
                    }
                case FlatDirection.West:
                    {
                        return (Side)dir.RotateRight();
                    }
            }
        }
    }
}

