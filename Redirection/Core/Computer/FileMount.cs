using Dan200.Core.Computer.APIs;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Computer
{
    public class FileMount : IWritableMount
    {
        private string m_name;
        private string m_path;
        private bool m_readOnly;
        private long m_capacity;
        private long m_usedSpace;

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
                return m_capacity;
            }
        }

        public long UsedSpace
        {
            get
            {
                return m_usedSpace;
            }
        }

        public long FreeSpace
        {
            get
            {
                return Math.Max(m_capacity - m_usedSpace, 0);
            }
        }

        public bool ReadOnly
        {
            get
            {
                return m_readOnly;
            }
        }

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        private class CountingStream : Stream
        {
            private FileMount m_parent;
            private Stream m_innerStream;
            private long m_bytesRemainingInBlock;

            public override bool CanRead
            {
                get
                {
                    return false;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return false;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return true;
                }
            }

            public override long Length
            {
                get
                {
                    throw new NotSupportedException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new NotSupportedException();
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            public CountingStream(FileMount parent, Stream innerStream, long bytesRemainingInBlock)
            {
                m_parent = parent;
                m_innerStream = innerStream;
                m_bytesRemainingInBlock = bytesRemainingInBlock;
            }

            protected override void Dispose(bool disposing)
            {
                m_innerStream.Dispose();
            }

            public override void Flush()
            {
                m_innerStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override int ReadByte()
            {
                throw new NotSupportedException();
            }

            public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                TryExpand(count);
                m_innerStream.Write(buffer, offset, count);
            }

            public override void WriteByte(byte value)
            {
                TryExpand(1);
                m_innerStream.WriteByte(value);
            }

            private void TryExpand(long size)
            {
                m_bytesRemainingInBlock -= size;
                if (m_bytesRemainingInBlock < 0)
                {
                    long newBlocks = ((-m_bytesRemainingInBlock) / FileSystem.BLOCK_SIZE) + 1;
                    if (m_parent.FreeSpace < newBlocks * FileSystem.BLOCK_SIZE)
                    {
                        throw new IOException("out of space");
                    }
                    m_parent.m_usedSpace += newBlocks * FileSystem.BLOCK_SIZE;
                    m_bytesRemainingInBlock = FileSystem.BLOCK_SIZE - ((-m_bytesRemainingInBlock) % FileSystem.BLOCK_SIZE);
                }
            }
        }

        public FileMount(string name, string path, long capacity, bool readOnly)
        {
            m_name = name;
            m_path = path;
            m_capacity = capacity;
            m_readOnly = readOnly;
            m_usedSpace = FileSystem.Measure(this);
        }

        public bool Exists(FilePath path)
        {
            if (Created())
            {
                var realPath = Resolve(path);
                return File.Exists(realPath) || Directory.Exists(realPath);
            }
            else
            {
                return path.IsEmpty;
            }
        }

        public bool IsDir(FilePath path)
        {
            if (Created())
            {
                var realPath = Resolve(path);
                return Directory.Exists(realPath);
            }
            else
            {
                return path.IsEmpty;
            }
        }

        public string[] List(FilePath path)
        {
            if (Created())
            {
                var realPath = Resolve(path);
                var entries = Directory.GetFileSystemEntries(realPath);
                var results = new List<string>(entries.Length);
                for (int i = 0; i < entries.Length; ++i)
                {
                    var entry = entries[i];
                    results.Add(System.IO.Path.GetFileName(entry));
                }
                return results.ToArray();
            }
            else
            {
                return new string[0];
            }
        }

        public long GetSize(FilePath path)
        {
            if (Created())
            {
                var realPath = Resolve(path);
                return new FileInfo(realPath).Length;
            }
            else
            {
                return 0;
            }
        }

        public DateTime GetModifiedTime(FilePath path)
        {
            if (Created())
            {
                var realPath = Resolve(path);
                if (Directory.Exists(realPath))
                {
                    return new DirectoryInfo(realPath).LastWriteTime;
                }
                else
                {
                    return new FileInfo(realPath).LastWriteTime;
                }
            }
            else
            {
                return OSAPI.ZeroTime;
            }
        }

        public TextReader OpenForRead(FilePath path)
        {
            if (Created())
            {
                var realPath = Resolve(path);
                return File.OpenText(realPath);
            }
            else
            {
                return null;
            }
        }

        public Stream OpenForBinaryRead(FilePath path)
        {
            if (Created())
            {
                var realPath = Resolve(path);
                return File.OpenRead(realPath);
            }
            else
            {
                return null;
            }
        }

        public void MakeDir(FilePath path)
        {
            Create();
            var realPath = Resolve(path);
            var parentPath = System.IO.Path.GetDirectoryName(realPath);
            var newDirs = 1;
            while (true)
            {
                if (Directory.Exists(parentPath))
                {
                    break;
                }
                else if (File.Exists(parentPath))
                {
                    throw new IOException("File already exists");
                }
                parentPath = System.IO.Path.GetDirectoryName(parentPath);
                newDirs++;
            }
            Directory.CreateDirectory(realPath);
            m_usedSpace += newDirs * FileSystem.BLOCK_SIZE;
        }

        public void Delete(FilePath path)
        {
            if (Created())
            {
                var realPath = Resolve(path);
                m_usedSpace -= FileSystem.Measure(this, path);
                if (File.Exists(realPath))
                {
                    File.Delete(realPath);
                }
                else
                {
                    Directory.Delete(realPath, true);
                }
            }
        }

        public TextWriter OpenForWrite(FilePath path, bool append)
        {
            return new StreamWriter(OpenForBinaryWrite(path, append));
        }

        public Stream OpenForBinaryWrite(FilePath path, bool append)
        {
            Create();
            var realPath = Resolve(path);
            if (append)
            {
                var stream = File.Open(realPath, FileMode.Append);
                return new CountingStream(
                    this, stream,
                    FileSystem.BLOCK_SIZE - (stream.Position % FileSystem.BLOCK_SIZE)
                );
            }
            else
            {
                if (File.Exists(realPath))
                {
                    m_usedSpace -= FileSystem.Measure(this, path);
                }
                if (FreeSpace < FileSystem.BLOCK_SIZE)
                {
                    throw new IOException("out of space");
                }
                m_usedSpace += FileSystem.BLOCK_SIZE;
                var stream = File.Open(realPath, FileMode.Create);
                return new CountingStream(
                    this, stream,
                    FileSystem.BLOCK_SIZE
                );
            }
        }

        public void Move(FilePath path, FilePath dest)
        {
            var realPath = Resolve(path);
            var realDest = Resolve(dest);
            Create();
            MoveImpl(realPath, realDest);
        }

        private void MoveImpl(string realPath, string realDest)
        {
            if (File.Exists(realPath))
            {
                File.Move(realPath, realDest);
            }
            else
            {
                Directory.Move(realPath, realDest);
            }
        }

        public void Copy(FilePath path, FilePath dest)
        {
            var realPath = Resolve(path);
            var realDest = Resolve(dest);
            Create();
            CopyImpl(realPath, realDest);
        }

        private void CopyImpl(string realPath, string realDest)
        {
            if (File.Exists(realPath))
            {
                File.Copy(realPath, realDest);
            }
            else if (Directory.Exists(realPath))
            {
                Directory.CreateDirectory(realDest);
                foreach (var fullSubPath in Directory.EnumerateFileSystemEntries(realPath))
                {
                    var fullSubDest = System.IO.Path.Combine(realDest, System.IO.Path.GetFileName(fullSubPath));
                    CopyImpl(fullSubPath, fullSubDest);
                }
            }
        }

        private string Resolve(FilePath path)
        {
            return m_path + System.IO.Path.DirectorySeparatorChar + path.ToString().Replace('/', System.IO.Path.DirectorySeparatorChar);
        }

        private bool Created()
        {
            return Directory.Exists(m_path);
        }

        private void Create()
        {
            if (!Directory.Exists(m_path))
            {
                Directory.CreateDirectory(m_path);
            }
        }
    }
}

