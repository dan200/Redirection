namespace Dan200.Core.Assets
{
    public interface ICompoundAsset : IAsset
    {
        // Must also have a constructor (string path)
        void Reset();
        void AddLayer(IFileStore store);
    }
}

