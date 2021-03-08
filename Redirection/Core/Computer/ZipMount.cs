using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dan200.Core.Computer
{
    public class ZipMount : IMount
    {
        private string m_name;
        private string m_zipFilePath;
        private FilePath m_subPath;

        private DateTime m_modifiedTime;
        private HashSet<FilePath> m_files;
        private HashSet<FilePath> m_directories;
        private Dictionary<FilePath, long> m_sizes;
        private long m_totalSize;

        public string Name
        {
            get
            {
                return m_name;
            }
        }

        public long Capacity
        {
            get
            {
                return m_totalSize;
            }
        }

        public long UsedSpace
        {
            get
            {
                return m_totalSize;
            }
        }

        public string ZipFilePath
        {
            get
            {
                return m_zipFilePath;
            }
        }

        public FilePath SubPath
        {
            get
            {
                return m_subPath;
            }
        }

        public ZipMount(string name, string zipFilePath, FilePath subPath)
        {
            m_name = name;
            m_zipFilePath = zipFilePath;
            m_subPath = subPath;
            m_files = new HashSet<FilePath>();
            m_directories = new HashSet<FilePath>();
            m_sizes = new Dictionary<FilePath, long>();
            BuildIndex();
        }

        public bool Exists(FilePath path)
        {
            var fullPath = Resolve(path);
            return m_files.Contains(fullPath) || m_directories.Contains(fullPath);
        }

        public bool IsDir(FilePath path)
        {
            var fullPath = Resolve(path);
            return m_directories.Contains(fullPath);
        }

        public string[] List(FilePath path)
        {
            var fullPath = Resolve(path);
            var results = new List<string>();
            foreach (var dir in m_directories)
            {
                if (!dir.IsEmpty && dir.GetDir() == fullPath)
                {
                    results.Add(dir.GetName());
                }
            }
            foreach (var file in m_files)
            {
                if (file.GetDir() == fullPath)
                {
                    results.Add(file.GetName());
                }
            }
            return results.ToArray();
        }

        public long GetSize(FilePath path)
        {
            var fullPath = Resolve(path);
            return m_sizes[fullPath];
        }

        public DateTime GetModifiedTime(FilePath path)
        {
            return m_modifiedTime;
        }

        public TextReader OpenForRead(FilePath path)
        {
            var bytes = GetAllBytes(path);
            var text = Encoding.UTF8.GetString(bytes);
            return new StringReader(text);
        }

        public Stream OpenForBinaryRead(FilePath path)
        {
            var bytes = GetAllBytes(path);
            return new MemoryStream(bytes, false);
        }

        private byte[] GetAllBytes(FilePath path)
        {
            var realPath = Resolve(path);
            using (var zipFile = new ZipFile(m_zipFilePath))
            {
                foreach (var entry in zipFile.Entries)
                {
                    if (!entry.IsDirectory && new FilePath(entry.FileName) == realPath)
                    {
                        var memoryStream = new MemoryStream();
                        using (var stream = entry.OpenReader())
                        {
                            stream.CopyTo(memoryStream);
                        }
                        return memoryStream.ToArray();
                    }
                }
            }
            return null;
        }

        private FilePath Resolve(FilePath path)
        {
            return FilePath.Combine(m_subPath, path);
        }

        private void BuildIndex()
        {
            m_modifiedTime = new FileInfo(m_zipFilePath).LastWriteTime;
            using (var zipFile = new ZipFile(m_zipFilePath))
            {
                m_directories.Add(FilePath.Empty);
                foreach (var entry in zipFile.Entries)
                {
                    var path = new FilePath(entry.FileName);
                    if (entry.IsDirectory)
                    {
                        m_directories.Add(path);
                    }
                    else
                    {
                        m_files.Add(path);
                        m_sizes.Add(path, entry.UncompressedSize);
                    }
                }
            }
            m_totalSize = FileSystem.Measure(this);
        }
    }
}

