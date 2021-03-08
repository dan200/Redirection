using OpenTK;

namespace Dan200.Core.Render
{
    public struct Quad
    {
        public static Quad UnitSquare
        {
            get
            {
                return new Quad(0.0f, 0.0f, 1.0f, 1.0f);
            }
        }

        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public Vector2 TopLeft
        {
            get
            {
                return new Vector2(X, Y);
            }
        }

        public Vector2 TopRight
        {
            get
            {
                return new Vector2(X + Width, Y);
            }
        }

        public Vector2 BottomLeft
        {
            get
            {
                return new Vector2(X, Y + Height);
            }
        }

        public Vector2 BottomRight
        {
            get
            {
                return new Vector2(X + Width, Y + Height);
            }
        }

        public Quad(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Vector2 Interpolate(float xFraction, float yFraction)
        {
            return new Vector2(X + xFraction * Width, Y + yFraction * Height);
        }

        public Quad Sub(float xFraction, float yFraction, float widthFraction, float heightFraction)
        {
            return new Quad(
                X + xFraction * Width, Y + yFraction * Height,
                widthFraction * Width, heightFraction * Height
            );
        }
    }
}

