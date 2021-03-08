using System;
using System.IO;

namespace Dan200.Core.Computer
{
    public class EmptyMount : IMount
    {
        private string m_name;

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
                return 0;
            }
        }

        public long UsedSpace
        {
            get
            {
                return 0;
            }
        }

        public EmptyMount(string name)
        {
            m_name = name;
        }

        public bool Exists(FilePath path)
        {
            return path.IsEmpty;
        }

        public bool IsDir(FilePath path)
        {
            return path.IsEmpty;
        }

        private static string[] s_emptyList = new string[0];

        public string[] List(FilePath path)
        {
            return s_emptyList;
        }

        public long GetSize(FilePath path)
        {
            return 0;
        }

        public DateTime GetModifiedTime(FilePath path)
        {
            return new DateTime(0, DateTimeKind.Utc);
        }

        public TextReader OpenForRead(FilePath path)
        {
            return null;
        }

        public Stream OpenForBinaryRead(FilePath path)
        {
            return null;
        }
    }
}

