using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Assets
{
    public class LocatedFileStore : IFileStore
    {
        private IFileStore m_child;
        private string m_childLocation;
        private string m_childLocationWithSlash;

        public LocatedFileStore(IFileStore child, string childLocation)
        {
            m_child = child;
            m_childLocation = childLocation;
            m_childLocationWithSlash = childLocation + "/";
        }

        public void ReloadIndex()
        {
            m_child.ReloadIndex();
        }

        public bool FileExists(string path)
        {
            return path.StartsWith(m_childLocationWithSlash) && m_child.FileExists(Resolve(path));
        }

        public bool DirectoryExists(string path)
        {
            return path.Equals("") || path.Equals(m_childLocation) || (path.StartsWith(m_childLocationWithSlash) && m_child.FileExists(Resolve(path)));
        }

        public long GetFileSize(string path)
        {
            return m_child.GetFileSize(Resolve(path));
        }

        public DateTime GetFileModificationTime(string path)
        {
            return m_child.GetFileModificationTime(Resolve(path));
        }

        public Stream OpenFile(string path)
        {
            return m_child.OpenFile(Resolve(path));
        }

        public TextReader OpenTextFile(string path)
        {
            return m_child.OpenTextFile(Resolve(path));
        }

        public IEnumerable<string> ListFiles(string path)
        {
            if (path == "")
            {
                return new string[] { };
            }
            else
            {
                return m_child.ListFiles(Resolve(path));
            }
        }

        public IEnumerable<string> ListDirectories(string path)
        {
            if (path == "")
            {
                return new string[] { m_childLocation };
            }
            else
            {
                return m_child.ListDirectories(Resolve(path));
            }
        }

        private string Resolve(string path)
        {
            if (path == m_childLocation)
            {
                return "";
            }
            if (path.StartsWith(m_childLocationWithSlash))
            {
                return path.Substring(m_childLocationWithSlash.Length);
            }
            throw new IOException("Path does not exist");
        }
    }
}

