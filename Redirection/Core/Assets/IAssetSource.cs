using Dan200.Core.Modding;
using System.Collections.Generic;
using System.Linq;

namespace Dan200.Core.Assets
{
    public interface IAssetSource
    {
        string Name { get; }
        Mod Mod { get; }
        IFileStore FileStore { get; }

        IEnumerable<string> AllLoadablePaths { get; }
        bool CanLoad(string path);

        IBasicAsset LoadBasic(string path);
        void ReloadBasic(IBasicAsset asset);

        void AddCompoundLayer(ICompoundAsset asset);
    }

    public static class AssetSourceExtensions
    {
        public static TAsset Load<TAsset>(this IAssetSource source, string path) where TAsset : IBasicAsset
        {
            var asset = source.LoadBasic(path);
            if (asset is TAsset)
            {
                return (TAsset)asset;
            }
            else
            {
                asset.Dispose();
                throw new AssetLoadException(path, "Asset is of incorrect type");
            }
        }

        public static TAsset[] LoadAll<TAsset>(this IAssetSource source, string dir) where TAsset : IBasicAsset
        {
            var paths = source.ListAll<TAsset>(dir).ToArray();
            var results = new TAsset[paths.Length];
            for (int i = 0; i < paths.Length; ++i)
            {
                var path = paths[i];
                results[i] = source.Load<TAsset>(path);
            }
            return results;
        }

        public static IEnumerable<string> ListAll<TAsset>(this IAssetSource source, string dir) where TAsset : IBasicAsset
        {
            var extension = Assets.GetExtension<TAsset>();
            if (extension != null)
            {
                foreach (var path in source.AllLoadablePaths)
                {
                    if (AssetPath.GetDirectoryName(path) == dir &&
                        AssetPath.GetExtension(path) == extension)
                    {
                        yield return path;
                    }
                }
            }
        }
    }
}
