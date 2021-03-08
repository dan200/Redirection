using OpenTK;
using System;

namespace Dan200.Core.Render
{
    public class Camera
    {
        public Matrix4 Transform;
        public float FOV;
        public float AspectRatio;
        public float NearPlane = 0.1f;
        public float FarPlane = 100.0f;

        public Camera(Matrix4 transform, float fov, float aspectRatio)
        {
            Transform = transform;
            FOV = fov;
            AspectRatio = aspectRatio;
        }

        public Matrix4 CreateProjectionMatrix()
        {
            float halfClipVertical = (float)Math.Tan(0.5f * FOV) * NearPlane;
            float halfClipHorizontal = halfClipVertical * AspectRatio;
            return Matrix4.CreatePerspectiveOffCenter(
                -halfClipHorizontal, halfClipHorizontal,
                -halfClipVertical, halfClipVertical,
                NearPlane, FarPlane
            );
        }
    }
}
