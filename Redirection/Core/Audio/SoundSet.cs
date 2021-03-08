using Dan200.Core.Assets;

namespace Dan200.Core.Audio
{
    public class SoundSet : IBasicAsset
    {
        public static SoundSet Get(string path)
        {
            return Assets.Assets.Get<SoundSet>(path);
        }

        private string m_path;
        private KeyValuePairs m_kvp;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public SoundSet(string path, IFileStore store)
        {
            m_path = path;
            m_kvp = new KeyValuePairs();
            Load(store);
        }

        public void Reload(IFileStore store)
        {
            m_kvp.Clear();
            Load(store);
        }

        public void Dispose()
        {
        }

        public string GetSound(string key)
        {
            if (m_kvp.ContainsKey(key))
            {
                return m_kvp.GetString(key);
            }
            if (m_kvp.ContainsKey("default"))
            {
                return m_kvp.GetString("default");
            }
            return null;
        }

        private void Load(IFileStore store)
        {
            using (var reader = store.OpenTextFile(m_path))
            {
                m_kvp.Load(reader);
            }
        }
    }
}

