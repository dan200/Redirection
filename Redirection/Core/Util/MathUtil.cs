using OpenTK;
using System;

namespace Dan200.Core.Util
{
    public static class MathUtils
    {
        public const float PI_OVER_ONE_EIGHTY = (float)Math.PI / 180.0f;
        public const float ONE_EIGHTY_OVER_PI = 180.0f / (float)Math.PI;

        public static void GenerateNormalAndTangent(Vector3 v1, Vector3 v2, Vector3 v3, Vector2 t1, Vector2 t2, Vector2 t3, out Vector3 o_normal, out Vector3 o_tangent)
        {
            GenerateNormalAndTangent(v2 - v1, v3 - v1, t2 - t1, t3 - t1, out o_normal, out o_tangent);
        }

        private static void GenerateNormalAndTangent(Vector3 v1, Vector3 v2, Vector2 st1, Vector2 st2, out Vector3 o_normal, out Vector3 o_tangent)
        {
            o_normal = Vector3.Cross(v1, v2);
            o_normal.Normalize();

            float coefInv = st1.X * st2.Y - st2.X * st1.Y;
            if (Math.Abs(coefInv) < 0.000001f)
            {
                o_tangent = v1;
                o_tangent.Normalize();
            }
            else
            {
                float coef = 1.0f / coefInv;
                o_tangent.X = coef * ((v1.X * st2.Y) + (v2.X * -st1.Y));
                o_tangent.Y = coef * ((v1.Y * st2.Y) + (v2.Y * -st1.Y));
                o_tangent.Z = coef * ((v1.Z * st2.Y) + (v2.Z * -st1.Y));
                o_tangent.Normalize();
            }
        }

        public static int Clamp(int f, int min, int max)
        {
            return Math.Min(Math.Max(f, min), max);
        }

        public static long Clamp(long f, long min, long max)
        {
            return Math.Min(Math.Max(f, min), max);
        }

        public static float Clamp(float f, float min, float max)
        {
            return Math.Min(Math.Max(f, min), max);
        }

        public static float Ease(float f)
        {
            f = Clamp(f, 0.0f, 1.0f);
            return (3.0f - 2.0f * f) * f * f;
        }

        public static float Bounce(float f)
        {
            return (float)Math.Sin(f * Math.PI) + f;
        }

        public static void CreateTransRotScaleMatrix(Vector3 trans, Vector3 rot, Vector3 scale, out Matrix4 o_result)
        {
            // Create a simple rotation/scale matrix
            o_result.Row0.X = scale.X;
            o_result.Row0.Y = 0.0f;
            o_result.Row0.Z = 0.0f;
            o_result.Row0.W = 0.0f;
            o_result.Row1.X = 0.0f;
            o_result.Row1.Y = scale.Y;
            o_result.Row1.Z = 0.0f;
            o_result.Row1.W = 0.0f;
            o_result.Row2.X = 0.0f;
            o_result.Row2.Y = 0.0f;
            o_result.Row2.Z = scale.Z;
            o_result.Row2.W = 0.0f;
            o_result.Row3.X = trans.X;
            o_result.Row3.Y = trans.Y;
            o_result.Row3.Z = trans.Z;
            o_result.Row3.W = 1.0f;

            // Apply rotations 
            if (rot.Y != 0.0f)
            {
                o_result = Matrix4.CreateRotationY(rot.Y) * o_result;
            }
            if (rot.X != 0.0f)
            {
                o_result = Matrix4.CreateRotationX(rot.X) * o_result;
            }
            if (rot.Z != 0.0f)
            {
                o_result = Matrix4.CreateRotationZ(rot.Z) * o_result;
            }
        }

        public static int SimpleStableHash(string s)
        {
            unchecked
            {
                int hash = 0;
                foreach (char c in s)
                {
                    hash = (hash * 31 + c) % 65535;
                }
                return hash;
            }
        }

        private static Random s_idGen = new Random();

        public static int GenerateLevelID(string path)
        {
            return s_idGen.Next() ^ MathUtils.SimpleStableHash(path);
        }

        [ThreadStatic]
        static int[] s_collIdx = new int[4];

        [ThreadStatic]
        static int[] s_rowIdx = new int[4];

        [ThreadStatic]
        static int[] s_pivotIdx = new int[4];

        [ThreadStatic]
        static float[,] s_inverse = new float[4, 4];

        public static void FastInvert(ref Matrix4 mat)
        {
            // convert the matrix to an array for easy looping
            s_collIdx[0] = 0;
            s_collIdx[1] = 0;
            s_collIdx[2] = 0;
            s_collIdx[3] = 0;
            s_rowIdx[0] = 0;
            s_rowIdx[1] = 0;
            s_rowIdx[2] = 0;
            s_rowIdx[3] = 0;
            s_pivotIdx[0] = -1;
            s_pivotIdx[1] = -1;
            s_pivotIdx[2] = -1;
            s_pivotIdx[3] = -1;
            s_inverse[0, 0] = mat.Row0.X;
            s_inverse[0, 1] = mat.Row0.Y;
            s_inverse[0, 2] = mat.Row0.Z;
            s_inverse[0, 3] = mat.Row0.W;
            s_inverse[1, 0] = mat.Row1.X;
            s_inverse[1, 1] = mat.Row1.Y;
            s_inverse[1, 2] = mat.Row1.Z;
            s_inverse[1, 3] = mat.Row1.W;
            s_inverse[2, 0] = mat.Row2.X;
            s_inverse[2, 1] = mat.Row2.Y;
            s_inverse[2, 2] = mat.Row2.Z;
            s_inverse[2, 3] = mat.Row2.W;
            s_inverse[3, 0] = mat.Row3.X;
            s_inverse[3, 1] = mat.Row3.Y;
            s_inverse[3, 2] = mat.Row3.Z;
            s_inverse[3, 3] = mat.Row3.W;

            int icol = 0;
            int irow = 0;
            for (int i = 0; i < 4; i++)
            {
                // Find the largest pivot value
                float maxPivot = 0.0f;
                for (int j = 0; j < 4; j++)
                {
                    if (s_pivotIdx[j] != 0)
                    {
                        for (int k = 0; k < 4; ++k)
                        {
                            if (s_pivotIdx[k] == -1)
                            {
                                float absVal = System.Math.Abs(s_inverse[j, k]);
                                if (absVal > maxPivot)
                                {
                                    maxPivot = absVal;
                                    irow = j;
                                    icol = k;
                                }
                            }
                            else if (s_pivotIdx[k] > 0)
                            {
                                return;
                            }
                        }
                    }
                }

                ++(s_pivotIdx[icol]);

                // Swap rows over so pivot is on diagonal
                if (irow != icol)
                {
                    for (int k = 0; k < 4; ++k)
                    {
                        float f = s_inverse[irow, k];
                        s_inverse[irow, k] = s_inverse[icol, k];
                        s_inverse[icol, k] = f;
                    }
                }

                s_rowIdx[i] = irow;
                s_collIdx[i] = icol;

                float pivot = s_inverse[icol, icol];
                // check for singular matrix
                if (pivot == 0.0f)
                {
                    throw new InvalidOperationException("Matrix is singular and cannot be inverted.");
                    //return mat;
                }

                // Scale row so it has a unit diagonal
                float oneOverPivot = 1.0f / pivot;
                s_inverse[icol, icol] = 1.0f;
                for (int k = 0; k < 4; ++k)
                    s_inverse[icol, k] *= oneOverPivot;

                // Do elimination of non-diagonal elements
                for (int j = 0; j < 4; ++j)
                {
                    // check this isn't on the diagonal
                    if (icol != j)
                    {
                        float f = s_inverse[j, icol];
                        s_inverse[j, icol] = 0.0f;
                        for (int k = 0; k < 4; ++k)
                            s_inverse[j, k] -= s_inverse[icol, k] * f;
                    }
                }
            }

            for (int j = 3; j >= 0; --j)
            {
                int ir = s_rowIdx[j];
                int ic = s_collIdx[j];
                for (int k = 0; k < 4; ++k)
                {
                    float f = s_inverse[k, ir];
                    s_inverse[k, ir] = s_inverse[k, ic];
                    s_inverse[k, ic] = f;
                }
            }

            mat.Row0 = new Vector4(s_inverse[0, 0], s_inverse[0, 1], s_inverse[0, 2], s_inverse[0, 3]);
            mat.Row1 = new Vector4(s_inverse[1, 0], s_inverse[1, 1], s_inverse[1, 2], s_inverse[1, 3]);
            mat.Row2 = new Vector4(s_inverse[2, 0], s_inverse[2, 1], s_inverse[2, 2], s_inverse[2, 3]);
            mat.Row3 = new Vector4(s_inverse[3, 0], s_inverse[3, 1], s_inverse[3, 2], s_inverse[3, 3]);
        }

        public static Matrix4 FastInverted(Matrix4 mat)
        {
            FastInvert(ref mat);
            return mat;
        }
    }
}

