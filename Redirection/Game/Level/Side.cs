namespace Dan200.Game.Level
{
    public enum Side
    {
        Front = 0,
        Right,
        Back,
        Left,
        Top,
        Bottom
    }

    public static class SideExtensions
    {
        private static int[] ROTATE_LEFT = { 3, 0, 1, 2, 4, 5 };
        private static int[] ROTATE_RIGHT = { 1, 2, 3, 0, 4, 5 };
        private static int[] ROTATE_180 = { 2, 3, 0, 1, 4, 5 };

        public static Direction ToDirection(this Side side, FlatDirection forwardDir)
        {
            switch (forwardDir)
            {
                case FlatDirection.North:
                default:
                    {
                        return (Direction)side;
                    }
                case FlatDirection.East:
                    {
                        return (Direction)ROTATE_RIGHT[(int)side];
                    }
                case FlatDirection.South:
                    {
                        return (Direction)ROTATE_180[(int)side];
                    }
                case FlatDirection.West:
                    {
                        return (Direction)ROTATE_LEFT[(int)side];
                    }
            }
        }
    }
}

