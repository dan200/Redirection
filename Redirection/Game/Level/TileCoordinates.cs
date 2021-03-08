using Dan200.Core.Lua;

namespace Dan200.Game.Level
{
    public struct TileCoordinates
    {
        public static TileCoordinates Zero
        {
            get
            {
                return new TileCoordinates(0, 0, 0);
            }
        }

        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public TileCoordinates(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public TileCoordinates Move(Direction dir)
        {
            return new TileCoordinates(X + dir.GetX(), Y + dir.GetY(), Z + dir.GetZ());
        }

        public TileCoordinates Move(Direction dir, int distance)
        {
            return new TileCoordinates(X + dir.GetX() * distance, Y + dir.GetY() * distance, Z + dir.GetZ() * distance);
        }

        public TileCoordinates Move(FlatDirection dir)
        {
            return new TileCoordinates(X + dir.GetX(), Y, Z + dir.GetZ());
        }

        public TileCoordinates Move(FlatDirection dir, int distance)
        {
            return new TileCoordinates(X + dir.GetX() * distance, Y, Z + dir.GetZ() * distance);
        }

        public TileCoordinates Move(int x, int z)
        {
            return new TileCoordinates(X + x, Y, Z + z);
        }

        public TileCoordinates Move(int x, int y, int z)
        {
            return new TileCoordinates(X + x, Y + y, Z + z);
        }

        public TileCoordinates Above()
        {
            return new TileCoordinates(X, Y + 1, Z);
        }

        public TileCoordinates Below()
        {
            return new TileCoordinates(X, Y - 1, Z);
        }

        public TileCoordinates North()
        {
            return Move(Direction.North);
        }

        public TileCoordinates South()
        {
            return Move(Direction.South);
        }

        public TileCoordinates East()
        {
            return Move(Direction.East);
        }

        public TileCoordinates West()
        {
            return Move(Direction.West);
        }

        public LuaTable ToLuaValue()
        {
            var table = new LuaTable();
            table["x"] = X;
            table["y"] = Y;
            table["z"] = Z;
            return table;
        }

        public static TileCoordinates operator +(TileCoordinates a, TileCoordinates b)
        {
            return new TileCoordinates(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static TileCoordinates operator -(TileCoordinates a, TileCoordinates b)
        {
            return new TileCoordinates(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static bool operator ==(TileCoordinates a, TileCoordinates b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(TileCoordinates a, TileCoordinates b)
        {
            return a.X != b.X || a.Y != b.Y || a.Z != b.Z;
        }

        public override bool Equals(object other)
        {
            if (other is TileCoordinates)
            {
                var o = (TileCoordinates)other;
                return o.X == X && o.Y == Y && o.Z == Z;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return ((X * 251) + Y) * 251 + Z;
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", X, Y, Z);
        }
    }
}
