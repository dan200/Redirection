using Dan200.Core.Modding;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dan200.Core.Assets
{
    public class FileAssetSource : IAssetSource
    {
        private string m_name;
        private IFileStore m_fileStore;
        private Mod m_mod;

        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                m_name = value;
            }
        }

        public IEnumerable<string> AllLoadablePaths
        {
            get
            {
                return EnumeratePaths("");
            }
        }

        public Mod Mod
        {
            get
            {
                return m_mod;
            }
            set
            {
                m_mod = value;
            }
        }

        public IFileStore FileStore
        {
            get
            {
                return m_fileStore;
            }
        }

        public FileAssetSource(string name, IFileStore fileStore)
        {
            m_name = name;
            m_fileStore = fileStore;
            m_mod = null;
        }

        private IEnumerable<string> EnumeratePaths(string root = "")
        {
            if (m_fileStore.FileExists(root))
            {
                if (Assets.GetType(AssetPath.GetExtension(root)) != null)
                {
                    yield return root;
                }
            }
            else if (m_fileStore.DirectoryExists(root))
            {
                foreach (var dirName in m_fileStore.ListDirectories(root))
                {
                    var dirPath = AssetPath.Combine(root, dirName);
                    foreach (var filePath in EnumeratePaths(dirPath))
                    {
                        yield return filePath;
                    }
                }
                foreach (var fileName in m_fileStore.ListFiles(root))
                {
                    var filePath = AssetPath.Combine(root, fileName);
                    if (Assets.GetType(AssetPath.GetExtension(filePath)) != null)
                    {
                        yield return filePath;
                    }
                }
            }
        }

        public bool CanLoad(string path)
        {
            return
                Assets.GetType(AssetPath.GetExtension(path)) != null &&
                m_fileStore.FileExists(path);
        }

        public IBasicAsset LoadBasic(string path)
        {
            try
            {
                //App.Log( "Loading {0}", path );
                var assetType = Assets.GetType(AssetPath.GetExtension(path));
                var constructor = assetType.GetConstructor(new Type[] {
                    typeof( string ),
                    typeof( IFileStore )
                });
                try
                {
                    return (IBasicAsset)constructor.Invoke(new object[] {
                        path, m_fileStore
                    });
                }
                catch (TargetInvocationException e)
                {
                    throw e.InnerException;
                }
            }
            catch (Exception e)
            {
                throw new AssetLoadException(path, e);
            }
        }

        public void ReloadBasic(IBasicAsset asset)
        {
            try
            {
                //App.Log( "Reloading {0}", asset.Path );
                asset.Reload(m_fileStore);
            }
            catch (Exception e)
            {
                throw new AssetLoadException(asset.Path, e);
            }
        }

        public void AddCompoundLayer(ICompoundAsset asset)
        {
            try
            {
                //App.Log( "Adding layer to {0}", asset.Path );
                asset.AddLayer(m_fileStore);
            }
            catch (Exception e)
            {
                throw new AssetLoadException(asset.Path, e);
            }
        }
    }
}
