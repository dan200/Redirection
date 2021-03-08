using OpenTK;

namespace Dan200.Core.Audio
{
    public class AudioListener
    {
        public Matrix4 Transform;

        public AudioListener()
        {
            Transform = Matrix4.Identity;
        }
    }
}

