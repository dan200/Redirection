namespace Dan200.Core.Assets
{
    public class BinaryAsset : IBasicAsset
    {
        public static BinaryAsset Get(string path)
        {
            return Assets.Get<BinaryAsset>(path);
        }

        private string m_path;
        private byte[] m_bytes;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public byte[] Bytes
        {
            get
            {
                return m_bytes;
            }
        }

        public BinaryAsset(string path, IFileStore store)
        {
            m_path = path;
            Load(store);
        }

        public void Reload(IFileStore store)
        {
            m_bytes = null;
            Load(store);
        }

        public void Dispose()
        {
            m_bytes = null;
        }

        private void Load(IFileStore store)
        {
            using (var stream = store.OpenFile(m_path))
            {
                m_bytes = stream.ReadToEnd();
            }
        }
    }
}

