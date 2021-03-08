using Dan200.Core.Assets;

namespace Dan200.Core.Script
{
    public class LuaScript : IBasicAsset
    {
        public static LuaScript Get(string path)
        {
            return Assets.Assets.Get<LuaScript>(path);
        }

        private string m_path;
        private string m_code;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public string Code
        {
            get
            {
                return m_code;
            }
        }

        public LuaScript(string path, IFileStore store)
        {
            m_path = path;
            Load(store);
        }

        public void Dispose()
        {
            Unload();
        }

        public void Reload(IFileStore store)
        {
            Unload();
            Load(store);
        }

        private void Load(IFileStore store)
        {
            using (var reader = store.OpenTextFile(m_path))
            {
                m_code = reader.ReadToEnd();
            }
        }

        private void Unload()
        {
            m_code = null;
        }
    }
}
