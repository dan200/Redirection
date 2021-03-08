using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Assets
{
    public class EmptyFileStore : IFileStore
    {
        private static List<string> s_emptyList = new List<string>(0);

        public EmptyFileStore()
        {
        }

        public void ReloadIndex()
        {
        }

        public bool FileExists(string path)
        {
            return false;
        }

        public bool DirectoryExists(string path)
        {
            return false;
        }

        public long GetFileSize(string path)
        {
            return 0;
        }

        public DateTime GetFileModificationTime(string path)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        public byte[] LoadFile(string path)
        {
            return null;
        }

        public Stream OpenFile(string path)
        {
            return null;
        }

        public TextReader OpenTextFile(string path)
        {
            return null;
        }

        public IEnumerable<string> ListFiles(string path)
        {
            return s_emptyList;
        }

        public IEnumerable<string> ListDirectories(string path)
        {
            return s_emptyList;
        }
    }
}

