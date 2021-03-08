using Dan200.Core.Assets;

namespace Dan200.Core.Audio.Null
{
    public class NullSound : Sound
    {
        private string m_path;

        public override string Path
        {
            get
            {
                return m_path;
            }
        }

        public override float Duration
        {
            get
            {
                return 0.0f;
            }
        }

        public NullSound(string path, IFileStore store)
        {
            m_path = path;
        }

        public override void Dispose()
        {
        }

        public override void Reload(IFileStore store)
        {
        }
    }
}

