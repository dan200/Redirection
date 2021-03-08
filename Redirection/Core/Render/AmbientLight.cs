using OpenTK;

namespace Dan200.Core.Render
{
    public class AmbientLight
    {
        public bool Active;
        public Vector3 Colour;

        public AmbientLight(Vector3 colour)
        {
            Active = true;
            Colour = colour;
        }
    }
}

