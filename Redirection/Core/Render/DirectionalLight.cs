
using OpenTK;

namespace Dan200.Core.Render
{
    public class DirectionalLight
    {
        public bool Active;
        public Vector3 Colour;
        private Vector3 m_direction;

        public Vector3 Direction
        {
            get
            {
                return m_direction;
            }
            set
            {
                m_direction = value;
                m_direction.Normalize();
            }
        }

        public DirectionalLight(Vector3 direction, Vector3 colour)
        {
            Active = true;
            Direction = direction;
            Colour = colour;
        }
    }
}
