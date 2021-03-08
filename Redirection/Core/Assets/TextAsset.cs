using Dan200.Core.Util;
using System.Collections.Generic;

namespace Dan200.Core.Assets
{
    public class TextAsset : IBasicAsset
    {
        public static TextAsset Get(string path)
        {
            return Assets.Get<TextAsset>(path);
        }

        private string m_path;
        private List<string> m_lines;
        private IReadOnlyList<string> m_readOnlyLines;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public IReadOnlyList<string> Lines
        {
            get
            {
                return m_readOnlyLines;
            }
        }

        public TextAsset(string path, IFileStore store)
        {
            m_path = path;
            m_lines = new List<string>();
            m_readOnlyLines = m_lines.ToReadOnly();
            Load(store);
        }

        public void Reload(IFileStore store)
        {
            m_lines.Clear();
            Load(store);
        }

        public void Dispose()
        {
            m_lines.Clear();
            m_lines = null;
        }

        private void Load(IFileStore store)
        {
            using (var reader = store.OpenTextFile(m_path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    m_lines.Add(line);
                }
                m_lines.TrimExcess();
            }
        }
    }
}

