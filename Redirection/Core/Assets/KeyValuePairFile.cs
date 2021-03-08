using System.IO;

namespace Dan200.Core.Assets
{
    public class KeyValuePairFile : KeyValuePairs
    {
        private IWritableFileStore m_fileStore;
        private string m_savePath;

        public KeyValuePairFile(string path) : this(null, path)
        {
        }

        public KeyValuePairFile(IWritableFileStore store, string path)
        {
            m_fileStore = store;
            m_savePath = path;
            if (m_fileStore != null)
            {
                // Backed by store
                if (m_fileStore.FileExists(path))
                {
                    using (var stream = m_fileStore.OpenTextFile(path))
                    {
                        Load(stream);
                        Modified = false;
                    }
                }
                else
                {
                    Modified = true;
                }
            }
            else
            {
                // Backed by file system
                if (File.Exists(path))
                {
                    using (var stream = new StreamReader(path))
                    {
                        Load(stream);
                        Modified = false;
                    }
                }
                else
                {
                    Modified = true;
                }
            }
        }

        public void SaveIfModified()
        {
            if (Modified)
            {
                Save();
            }
        }

        public void Save()
        {
            if (m_fileStore != null)
            {
                // Backed by store
                m_fileStore.CreateDirectory(AssetPath.GetDirectoryName(m_savePath));
                using (var stream = new StringWriter())
                {
                    Save(stream);
                    m_fileStore.SaveTextFile(m_savePath, stream.ToString());
                }
            }
            else
            {
                // Backed by filesystem
                Directory.CreateDirectory(Path.GetDirectoryName(m_savePath));
                using (var stream = new StreamWriter(m_savePath))
                {
                    Save(stream);
                }
            }
            Modified = false;
        }
    }
}

