
using OpenTK;

namespace Dan200.Core.Render
{
    public class PointLight
    {
        public bool Active;
        public Vector3 Position;
        public Vector3 Colour;
        public float Radius;

        public PointLight(Vector3 position, Vector3 colour, float radius)
        {
            Active = true;
            Position = position;
            Colour = colour;
            Radius = radius;
        }
    }
}

