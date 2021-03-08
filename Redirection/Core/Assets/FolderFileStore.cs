using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Assets
{
    public class FolderFileStore : IWritableFileStore
    {
        private string m_rootPath;

        public string Path
        {
            get
            {
                return m_rootPath;
            }
        }

        public FolderFileStore(string rootPath)
        {
            m_rootPath = rootPath;
        }

        public void ReloadIndex()
        {
        }

        public bool FileExists(string path)
        {
            string fullPath = Resolve(path);
            return File.Exists(fullPath);
        }

        public long GetFileSize(string path)
        {
            string fullPath = Resolve(path);
            return new FileInfo(fullPath).Length;
        }

        public DateTime GetFileModificationTime(string path)
        {
            string fullPath = Resolve(path);
            return new FileInfo(fullPath).LastAccessTime;
        }

        public bool DirectoryExists(string path)
        {
            string fullPath = Resolve(path);
            return Directory.Exists(fullPath);
        }

        public Stream OpenFile(string path)
        {
            string fullPath = Resolve(path);
            return File.OpenRead(fullPath);
        }

        public TextReader OpenTextFile(string path)
        {
            string fullPath = Resolve(path);
            return File.OpenText(fullPath);
        }

        public IEnumerable<string> ListFiles(string path)
        {
            string fullPath = Resolve(path);
            if (Directory.Exists(fullPath))
            {
                foreach (string file in Directory.EnumerateFiles(fullPath))
                {
                    yield return System.IO.Path.GetFileName(file);
                }
            }
        }

        public IEnumerable<string> ListDirectories(string path)
        {
            string fullPath = Resolve(path);
            if (Directory.Exists(fullPath))
            {
                foreach (string file in Directory.EnumerateDirectories(fullPath))
                {
                    yield return System.IO.Path.GetFileName(file);
                }
            }
        }

        public void SaveFile(string path, byte[] bytes)
        {
            string fullPath = Resolve(path);
            File.WriteAllBytes(fullPath, bytes);
        }

        public void SaveTextFile(string path, string text)
        {
            string fullPath = Resolve(path);
            File.WriteAllText(fullPath, text);
        }

        public void DeleteFile(string path)
        {
            string fullPath = Resolve(path);
            File.Delete(fullPath);
        }

        public void CreateDirectory(string path)
        {
            string fullPath = Resolve(path);
            Directory.CreateDirectory(fullPath);
        }

        public void DeleteDirectory(string path)
        {
            string fullPath = Resolve(path);
            Directory.Delete(fullPath, true);
        }

        private string Resolve(string path)
        {
            return System.IO.Path.Combine(
                m_rootPath,
                path.Replace('/', System.IO.Path.DirectorySeparatorChar)
            );
        }
    }
}

