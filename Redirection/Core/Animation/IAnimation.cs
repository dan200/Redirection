using OpenTK;

namespace Dan200.Core.Animation
{
    public interface IAnimation
    {
        void Animate(string partName, float time, out bool o_visible, out Matrix4 o_transform, out Vector2 o_uvOffset, out Vector2 o_uvScale, out Vector4 o_colour, out float o_cameraFov);
    }
}

