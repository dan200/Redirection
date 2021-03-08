namespace Dan200.Core.Assets
{
    public interface IBasicAsset : IAsset
    {
        // Must also have a constructor (string path, IFileStore data)
        void Reload(IFileStore store);
    }
}

