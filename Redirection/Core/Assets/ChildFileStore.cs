using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Assets
{
    public class ChildFileStore : IFileStore
    {
        private IFileStore m_parent;
        private string m_rootPath;

        public ChildFileStore(IFileStore parent, string rootPath)
        {
            m_parent = parent;
            m_rootPath = rootPath;
        }

        public void ReloadIndex()
        {
            m_parent.ReloadIndex();
        }

        public bool FileExists(string path)
        {
            return m_parent.FileExists(Resolve(path));
        }

        public bool DirectoryExists(string path)
        {
            return m_parent.DirectoryExists(Resolve(path));
        }

        public long GetFileSize(string path)
        {
            return m_parent.GetFileSize(Resolve(path));
        }

        public DateTime GetFileModificationTime(string path)
        {
            return m_parent.GetFileModificationTime(Resolve(path));
        }

        public Stream OpenFile(string path)
        {
            return m_parent.OpenFile(Resolve(path));
        }

        public TextReader OpenTextFile(string path)
        {
            return m_parent.OpenTextFile(Resolve(path));
        }

        public IEnumerable<string> ListFiles(string path)
        {
            return m_parent.ListFiles(Resolve(path));
        }

        public IEnumerable<string> ListDirectories(string path)
        {
            return m_parent.ListDirectories(Resolve(path));
        }

        private string Resolve(string path)
        {
            return AssetPath.Combine(m_rootPath, path);
        }
    }
}

