using Dan200.Core.Assets;

namespace Dan200.Core.Audio.Null
{
    public class NullMusic : Music
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

        public NullMusic(string path, IFileStore store)
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

