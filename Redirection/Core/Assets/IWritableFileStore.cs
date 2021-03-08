namespace Dan200.Core.Assets
{
    public interface IWritableFileStore : IFileStore
    {
        void SaveFile(string path, byte[] bytes);
        void SaveTextFile(string path, string text);
        void DeleteFile(string path);
        void CreateDirectory(string path);
        void DeleteDirectory(string path);
    }
}

