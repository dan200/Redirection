using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Assets
{
    public class ZipArchiveFileStore : IFileStore
    {
        private string m_zipFilePath;
        private string m_rootPath;

        private DateTime m_modifiedTime;
        private ISet<string> m_files;
        private ISet<string> m_directories;
        private Dictionary<string, long> m_sizes;

        public ZipArchiveFileStore(string zipFilePath, string rootPath)
        {
            m_zipFilePath = zipFilePath;
            m_rootPath = rootPath;

            m_files = new HashSet<string>();
            m_directories = new HashSet<string>();
            m_sizes = new Dictionary<string, long>();
            LoadIndex();
        }

        public void ReloadIndex()
        {
            m_files.Clear();
            m_directories.Clear();
            if (File.Exists(m_zipFilePath))
            {
                LoadIndex();
            }
        }

        private void LoadIndex()
        {
            m_modifiedTime = new FileInfo(m_zipFilePath).LastWriteTime;
            using (var stream = File.OpenRead(m_zipFilePath))
            {
                using (var file = ZipFile.Read(stream))
                {
                    m_directories.Add("");
                    foreach (var entry in file.Entries)
                    {
                        string sanePath = Sanitize(entry.FileName);
                        if (entry.IsDirectory)
                        {
                            m_directories.Add(sanePath);
                        }
                        else
                        {
                            m_files.Add(sanePath);
                            m_sizes.Add(sanePath, entry.UncompressedSize);
                        }
                    }
                }
            }
        }

        public bool FileExists(string path)
        {
            string fullPath = Resolve(path);
            return m_files.Contains(fullPath);
        }

        public bool DirectoryExists(string path)
        {
            string fullPath = Resolve(path);
            return m_directories.Contains(fullPath);
        }

        public long GetFileSize(string path)
        {
            var fullPath = Resolve(path);
            return m_sizes[fullPath];
        }

        public DateTime GetFileModificationTime(string path)
        {
            return m_modifiedTime;
        }

        public Stream OpenFile(string path)
        {
            string fullPath = Resolve(path);
            using (var stream = File.OpenRead(m_zipFilePath))
            {
                using (var file = ZipFile.Read(stream))
                {
                    foreach (var entry in file.Entries)
                    {
                        if (!entry.IsDirectory && Sanitize(entry.FileName) == fullPath)
                        {
                            return new MemoryStream(
                                entry.OpenReader().ReadToEnd()
                            );
                        }
                    }
                }
            }
            return null;
        }

        public TextReader OpenTextFile(string path)
        {
            var stream = OpenFile(path);
            if (stream != null)
            {
                return new StreamReader(stream);
            }
            return null;
        }

        public IEnumerable<string> ListFiles(string path)
        {
            string fullPath = Resolve(path);
            foreach (string file in m_files)
            {
                if (AssetPath.GetDirectoryName(file) == fullPath)
                {
                    yield return AssetPath.GetFileName(file);
                }
            }
        }

        public IEnumerable<string> ListDirectories(string path)
        {
            string fullPath = Resolve(path);
            foreach (string dir in m_directories)
            {
                if (AssetPath.GetDirectoryName(dir) == fullPath)
                {
                    yield return AssetPath.GetFileName(dir);
                }
            }
        }

        private string Resolve(string path)
        {
            return AssetPath.Combine(m_rootPath, path);
        }

        private string Sanitize(string path)
        {
            string saneName = path.Replace('\\', '/');
            if (saneName.EndsWith("/"))
            {
                saneName = saneName.Substring(0, saneName.Length - 1);
            }
            return saneName;
        }
    }
}

