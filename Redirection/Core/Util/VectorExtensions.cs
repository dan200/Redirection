using OpenTK;

namespace Dan200.Core.Util
{
    public static class VectorExtensions
    {
        public static Vector2 Normalised(this Vector2 v)
        {
            v.Normalize();
            return v;
        }

        public static Vector3 Normalised(this Vector3 v)
        {
            v.Normalize();
            return v;
        }

        public static Vector2 SafeNormalised(this Vector2 v, Vector2 _default)
        {
            if (v.Length >= 0.000001f)
            {
                v.Normalize();
                return v;
            }
            return _default;
        }

        public static Vector3 SafeNormalised(this Vector3 v, Vector3 _default)
        {
            if (v.Length >= 0.000001f)
            {
                v.Normalize();
                return v;
            }
            return _default;
        }

        public static Vector3 Mul(this Vector3 a, Vector3 b)
        {
            return new Vector3(
                a.X * b.X,
                a.Y * b.Y,
                a.Z * b.Z
            );
        }

        public static Vector4 Mul(this Vector4 a, Vector4 b)
        {
            return new Vector4(
                a.X * b.X,
                a.Y * b.Y,
                a.Z * b.Z,
                a.W * b.W
            );
        }
    }
}

