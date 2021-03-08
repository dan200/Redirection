using System.IO;

namespace Dan200.Core.Computer
{
    public interface IWritableMount : IMount
    {
        bool ReadOnly { get; }
        void MakeDir(FilePath path);
        void Delete(FilePath path);
        TextWriter OpenForWrite(FilePath path, bool append);
        Stream OpenForBinaryWrite(FilePath path, bool append);
        void Copy(FilePath path, FilePath dest);
        void Move(FilePath path, FilePath dest);
    }
}

