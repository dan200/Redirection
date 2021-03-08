using Dan200.Core.Util;
using Dan200.Game.Level;
using OpenTK;
using System;

namespace Dan200.Core.Utils
{
    public struct Ray
    {
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;
        public readonly float Length;
        private readonly Vector3 DirFrac;

        public Vector3 End
        {
            get
            {
                return Origin + Direction * Length;
            }
        }

        public Ray(Vector3 origin, Vector3 direction, float length)
        {
            Origin = origin;
            Direction = direction;
            Length = length;
            DirFrac = new Vector3(
                1.0f / Direction.X,
                1.0f / Direction.Y,
                1.0f / Direction.Z
            );
        }

        public Ray ToLocal(Matrix4 transform)
        {
            MathUtils.FastInvert(ref transform);
            var posLocal = Vector3.TransformPosition(Origin, transform);
            var dirLocal = Vector3.TransformVector(Direction, transform);
            return new Ray(posLocal, dirLocal, Length);
        }

        public bool TestVersusBox(Vector3 min, Vector3 max, out Direction o_side, out float o_distance)
        {
            // r.dir is unit direction vector of ray
            var dirFrac = DirFrac;

            // lb is the corner of AABB with minimal coordinates - left bottom, rt is maximal corner
            // r.org is origin of ray
            float t1 = (min.X - Origin.X) * dirFrac.X;
            float t2 = (max.X - Origin.X) * dirFrac.X;
            float t3 = (min.Y - Origin.Y) * dirFrac.Y;
            float t4 = (max.Y - Origin.Y) * dirFrac.Y;
            float t5 = (min.Z - Origin.Z) * dirFrac.Z;
            float t6 = (max.Z - Origin.Z) * dirFrac.Z;

            float tmin = Math.Max(Math.Max(Math.Min(t1, t2), Math.Min(t3, t4)), Math.Min(t5, t6));
            float tmax = Math.Min(Math.Min(Math.Max(t1, t2), Math.Max(t3, t4)), Math.Max(t5, t6));

            // if tmax < 0, ray (line) is intersecting AABB, but whole AABB is behing us
            if (tmax >= 0.0f && tmin <= tmax)
            {
                float distance = Math.Max(tmin, 0.0f);
                if (distance <= Length)
                {
                    o_distance = distance;
                    if (tmin == t1)
                    {
                        o_side = Game.Level.Direction.East;
                    }
                    else if (tmin == t2)
                    {
                        o_side = Game.Level.Direction.West;
                    }
                    else if (tmin == t3)
                    {
                        o_side = Game.Level.Direction.Down;
                    }
                    else if (tmin == t4)
                    {
                        o_side = Game.Level.Direction.Up;
                    }
                    else if (tmin == t5)
                    {
                        o_side = Game.Level.Direction.South;
                    }
                    else// if( tmin == t6 )
                    {
                        o_side = Game.Level.Direction.North;
                    }
                    return true;
                }
            }
            o_side = default(Direction);
            o_distance = default(float);
            return false;
        }
    }
}

