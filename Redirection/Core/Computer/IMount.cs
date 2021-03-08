using System;
using System.IO;

namespace Dan200.Core.Computer
{
    public interface IMount
    {
        string Name { get; }
        long Capacity { get; }
        long UsedSpace { get; }
        bool Exists(FilePath path);
        bool IsDir(FilePath path);
        string[] List(FilePath path);
        long GetSize(FilePath path);
        DateTime GetModifiedTime(FilePath path);
        TextReader OpenForRead(FilePath path);
        Stream OpenForBinaryRead(FilePath path);
    }
}
