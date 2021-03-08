using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Assets
{
    public interface IFileStore
    {
        void ReloadIndex();
        bool FileExists(string path);
        bool DirectoryExists(string path);
        long GetFileSize(string path);
        DateTime GetFileModificationTime(string path);
        Stream OpenFile(string path);
        TextReader OpenTextFile(string path);
        IEnumerable<string> ListFiles(string path);
        IEnumerable<string> ListDirectories(string path);
    }
}
