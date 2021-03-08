using Dan200.Game.Robot;
using System;

namespace Dan200.Game.Level
{
    public enum FlatDirection
    {
        North = 0,
        East,
        South,
        West
    }

    public static class FlatDirectionExtensions
    {
        private static int[] DIR_TO_X = { 0, -1, 0, 1 };
        private static int[] DIR_TO_Z = { 1, 0, -1, 0 };
        private static int[] ROTATE_LEFT = { 3, 0, 1, 2 };
        private static int[] ROTATE_RIGHT = { 1, 2, 3, 0 };
        private static int[] OPPOSITE = { 2, 3, 0, 1 };

        public static Direction ToDirection(this FlatDirection flatDir)
        {
            return (Direction)flatDir;
        }

        public static int GetX(this FlatDirection dir)
        {
            return DIR_TO_X[(int)dir];
        }

        public static int GetZ(this FlatDirection dir)
        {
            return DIR_TO_Z[(int)dir];
        }

        public static FlatDirection RotateLeft(this FlatDirection dir)
        {
            return (FlatDirection)ROTATE_LEFT[(int)dir];
        }

        public static FlatDirection RotateRight(this FlatDirection dir)
        {
            return (FlatDirection)ROTATE_RIGHT[(int)dir];
        }

        public static FlatDirection Rotate180(this FlatDirection dir)
        {
            return (FlatDirection)OPPOSITE[(int)dir];
        }

        public static FlatDirection Opposite(this FlatDirection dir)
        {
            return (FlatDirection)OPPOSITE[(int)dir];
        }

        public static FlatDirection Rotate(this FlatDirection dir, TurnDirection turnDir)
        {
            switch (turnDir)
            {
                case TurnDirection.Left:
                    {
                        return dir.RotateLeft();
                    }
                case TurnDirection.Right:
                default:
                    {
                        return dir.RotateRight();
                    }
            }
        }

        public static float ToYaw(this FlatDirection dir)
        {
            float halfPi = (float)Math.PI * 0.5f;
            return -(float)dir * halfPi;
        }

        public static Side ToSide(this FlatDirection dir, FlatDirection forwardDir)
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

