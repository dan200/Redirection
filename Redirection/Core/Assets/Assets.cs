using Dan200.Core.Main;
using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dan200.Core.Assets
{
    public static class Assets
    {
        private class RegisteredType
        {
            public Type Type;
            public string Extension;
            public bool Compound;

            public RegisteredType()
            {
                Type = null;
                Extension = null;
                Compound = false;
            }
        }

        private class LoadedBasicAsset
        {
            public readonly string Path;
            public readonly IBasicAsset Asset;
            public IAssetSource Source;

            public LoadedBasicAsset(string path, IBasicAsset asset)
            {
                Path = path;
                Asset = asset;
                Source = null;
            }
        }

        private class LoadedCompoundAsset
        {
            public readonly string Path;
            public readonly ICompoundAsset Asset;
            public List<IAssetSource> Sources;

            public LoadedCompoundAsset(string path, ICompoundAsset asset)
            {
                Path = path;
                Asset = asset;
                Sources = new List<IAssetSource>();
            }
        }

        private static Dictionary<string, RegisteredType> s_extensionToRegisteredType = new Dictionary<string, RegisteredType>();
        private static Dictionary<Type, RegisteredType> s_typeToRegisteredType = new Dictionary<Type, RegisteredType>();

        private static List<IAssetSource> s_assetSources = new List<IAssetSource>();
        private static Dictionary<string, LoadedBasicAsset> s_loadedBasicAssets = new Dictionary<string, LoadedBasicAsset>();
        private static Dictionary<string, LoadedCompoundAsset> s_loadedCompoundAssets = new Dictionary<string, LoadedCompoundAsset>();

        public static int Count
        {
            get
            {
                return s_loadedBasicAssets.Count + s_loadedCompoundAssets.Count;
            }
        }

        public static IReadOnlyList<IAssetSource> Sources
        {
            get
            {
                return s_assetSources.ToReadOnly();
            }
        }

        // SETUP

        public static void RegisterType<TAssetType>(string extension) where TAssetType : class, IAsset
        {
            // Check valid constructor exists
            bool compound;
            var assetType = typeof(TAssetType);
            if (typeof(IBasicAsset).IsAssignableFrom(assetType))
            {
                compound = false;
                var constructor = assetType.GetConstructor(new Type[] {
                    typeof( string ),
                    typeof( IFileStore )
                });
                if (constructor == null)
                {
                    throw new ArgumentException("Type " + assetType.Name + " does not have required constructor");
                }
            }
            else if (typeof(ICompoundAsset).IsAssignableFrom(assetType))
            {
                compound = true;
                var constructor = assetType.GetConstructor(new Type[] {
                    typeof( string )
                });
                if (constructor == null)
                {
                    throw new ArgumentException("Type " + assetType.Name + " does not have required constructor");
                }
            }
            else
            {
                throw new ArgumentException("Type " + assetType.Name + " is not " + typeof(IBasicAsset).Name + " or " + typeof(ICompoundAsset).Name);
            }

            // Check not already registered
            if (s_extensionToRegisteredType.ContainsKey(extension))
            {
                throw new ArgumentException("Extension " + extension + " is already registered to type " + s_extensionToRegisteredType[extension].Type.Name);
            }
            if (s_typeToRegisteredType.ContainsKey(assetType))
            {
                throw new ArgumentException("Type " + assetType.Name + " is already registered to extension " + s_typeToRegisteredType[assetType].Extension);
            }

            // Register
            var registeredType = new RegisteredType();
            registeredType.Type = assetType;
            registeredType.Extension = extension;
            registeredType.Compound = compound;

            s_extensionToRegisteredType.Add(extension, registeredType);
            s_typeToRegisteredType.Add(assetType, registeredType);
            while (assetType.BaseType != typeof(object))
            {
                s_typeToRegisteredType.Add(assetType.BaseType, registeredType);
                assetType = assetType.BaseType;
            }
        }

        public static Type GetType(string extension)
        {
            RegisteredType type;
            if (s_extensionToRegisteredType.TryGetValue(extension, out type))
            {
                return type.Type;
            }
            return null;
        }

        public static string GetExtension<TAsset>() where TAsset : IAsset
        {
            var assetType = typeof(TAsset);
            RegisteredType type;
            if (s_typeToRegisteredType.TryGetValue(assetType, out type))
            {
                return type.Extension;
            }
            return null;
        }

        public static void AddSource(IAssetSource source)
        {
            if (!s_assetSources.Contains(source))
            {
                App.Log("Adding {0} assets", source.Name);
                s_assetSources.Add(source);
            }
        }

        public static void RemoveSource(IAssetSource source)
        {
            if (s_assetSources.Contains(source))
            {
                App.Log("Removing {0} assets", source.Name);
                s_assetSources.Remove(source);
            }
        }

        // LOAD

        public static void Load(string path)
        {
            LoadAsset(path, false);
        }

        public static void LoadAll()
        {
            var task = BuildLoadAllTask(false, null);
            task.LoadAll();
            App.Log("Loaded {0} assets", task.Total);
        }

        public static AssetLoadTask StartLoadAll()
        {
            return BuildLoadAllTask(false, null);
        }

        public static void Reload(string path)
        {
            LoadAsset(path, true);
        }

        public static void ReloadAll()
        {
            var task = BuildLoadAllTask(true, null);
            task.LoadAll();
            App.Log("Loaded {0} assets", task.Total);
        }

        public static AssetLoadTask StartReloadAll()
        {
            return BuildLoadAllTask(true, null);
        }

        public static void ReloadSource(IAssetSource source)
        {
            var task = BuildLoadAllTask(false, source);
            task.LoadAll();
        }

        public static AssetLoadTask StartReloadSource(IAssetSource source)
        {
            return BuildLoadAllTask(false, source);
        }

        public static void Unload(string path)
        {
            UnloadAsset(path);
        }

        public static void UnloadAll()
        {
            foreach (var path in s_loadedBasicAssets.Keys.ToList())
            {
                UnloadAsset(path);
            }
            foreach (var path in s_loadedCompoundAssets.Keys.ToList())
            {
                UnloadAsset(path);
            }
        }

        public static void UnloadUnsourced()
        {
            foreach (var path in s_loadedBasicAssets.Keys.ToList())
            {
                var source = LookupBasic(path).Source;
                if (!s_assetSources.Contains(source) ||
                    !source.CanLoad(path))
                {
                    UnloadAsset(path);
                }
            }
            foreach (var path in s_loadedCompoundAssets.Keys.ToList())
            {
                var sources = LookupCompound(path).Sources;
                bool sourced = false;
                for (int i = 0; i < sources.Count; ++i)
                {
                    var source = sources[i];
                    if (s_assetSources.Contains(source) &&
                        source.CanLoad(path))
                    {
                        sourced = true;
                    }
                }
                if (!sourced)
                {
                    UnloadAsset(path);
                }
            }
        }

        private static AssetLoadTask BuildLoadAllTask(bool reloadIfSourceUnchanged, IAssetSource forceReloadSource)
        {
            var result = new AssetLoadTask();
            var pathsSeen = new HashSet<string>();
            for (int i = s_assetSources.Count - 1; i >= 0; --i)
            {
                var source = s_assetSources[i];
                foreach (var path in source.AllLoadablePaths)
                {
                    if (!pathsSeen.Contains(path))
                    {
                        var existing = LookupBasic(path);
                        if (existing == null ||
                            existing.Source != source ||
                            reloadIfSourceUnchanged ||
                            source == forceReloadSource)
                        {
                            result.AddPath(path);
                        }
                        pathsSeen.Add(path);
                    }
                }
            }
            return result;
        }

        private static void LoadAsset(string path, bool reloadIfSourceUnchanged)
        {
            var extension = AssetPath.GetExtension(path);
            var type = LookupType(extension);
            if (type != null)
            {
                if (type.Compound)
                {
                    var sources = new List<IAssetSource>();
                    for (int i = 0; i < s_assetSources.Count; ++i)
                    {
                        var source = s_assetSources[i];
                        if (source.CanLoad(path))
                        {
                            sources.Add(source);
                        }
                    }
                    if (sources.Count > 0)
                    {
                        LoadCompoundAsset(path, sources, reloadIfSourceUnchanged);
                    }
                }
                else
                {
                    for (int i = s_assetSources.Count - 1; i >= 0; --i)
                    {
                        var source = s_assetSources[i];
                        if (source.CanLoad(path))
                        {
                            LoadBasicAsset(path, source, reloadIfSourceUnchanged);
                            return;
                        }
                    }
                }
            }
        }

        private static void LoadBasicAsset(string path, IAssetSource source, bool reloadIfSourceUnchanged)
        {
            var existingBasic = LookupBasic(path);
            if (existingBasic != null)
            {
                // Reload an existing asset
                if (reloadIfSourceUnchanged || existingBasic.Source != source)
                {
                    source.ReloadBasic(existingBasic.Asset);
                    existingBasic.Source = source;
                }
            }
            else
            {
                // Create a new asset
                var asset = source.LoadBasic(path);
                var loadedAsset = new LoadedBasicAsset(path, asset);
                loadedAsset.Source = source;
                s_loadedBasicAssets.Add(path, loadedAsset);
            }
        }

        private static bool CompareSources(List<IAssetSource> a, List<IAssetSource> b)
        {
            if (a.Count == b.Count)
            {
                for (int i = 0; i < a.Count; ++i)
                {
                    if (a[i] != b[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private static ICompoundAsset ConstructCompoundAsset(string path)
        {
            var assetType = Assets.GetType(AssetPath.GetExtension(path));
            var constructor = assetType.GetConstructor(new Type[] {
                typeof( string )
            });
            try
            {
                return (ICompoundAsset)constructor.Invoke(new object[] {
                    path
                });
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        private static void LoadCompoundAsset(string path, List<IAssetSource> sources, bool reloadIfSourcesUnchanged)
        {
            var existingCompound = LookupCompound(path);
            if (existingCompound != null)
            {
                // Reload an existing asset
                if (reloadIfSourcesUnchanged || !CompareSources(sources, existingCompound.Sources))
                {
                    existingCompound.Asset.Reset();
                    for (int i = 0; i < sources.Count; ++i)
                    {
                        var source = sources[i];
                        source.AddCompoundLayer(existingCompound.Asset);
                    }
                    existingCompound.Sources = sources;
                }
            }
            else
            {
                // Create a new asset
                var asset = ConstructCompoundAsset(path);
                for (int i = 0; i < sources.Count; ++i)
                {
                    var source = sources[i];
                    source.AddCompoundLayer(asset);
                }
                var loadedAsset = new LoadedCompoundAsset(path, asset);
                loadedAsset.Sources = sources;
                s_loadedCompoundAssets.Add(path, loadedAsset);
            }
        }

        private static void UnloadAsset(string path)
        {
            var extension = AssetPath.GetExtension(path);
            var type = LookupType(extension);
            if (type != null)
            {
                if (type.Compound)
                {
                    var loadedCompound = LookupCompound(path);
                    if (loadedCompound != null)
                    {
                        // Unload an asset
                        s_loadedCompoundAssets.Remove(path);
                        loadedCompound.Asset.Dispose();
                    }
                }
                else
                {
                    var loadedBasic = LookupBasic(path);
                    if (loadedBasic != null)
                    {
                        // Unload an asset
                        s_loadedBasicAssets.Remove(path);
                        loadedBasic.Asset.Dispose();
                    }
                }
            }
        }

        // QUERY

        public static bool Exists<TAsset>(string path) where TAsset : class, IAsset
        {
            var type = LookupType(typeof(TAsset));
            if (type != null)
            {
                if (type.Compound)
                {
                    var loadedCompound = LookupCompound(path);
                    if (loadedCompound != null && loadedCompound.Asset is TAsset)
                    {
                        return true;
                    }
                }
                else
                {
                    var loadedBasic = LookupBasic(path);
                    if (loadedBasic != null && loadedBasic.Asset is TAsset)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static TAsset Get<TAsset>(string path) where TAsset : class, IAsset
        {
            var type = LookupType(typeof(TAsset));
            if (type != null)
            {
                if (type.Compound)
                {
                    // Try to get the asset
                    var loadedCompound = LookupCompound(path);
                    if (loadedCompound != null && loadedCompound.Asset is TAsset)
                    {
                        return (TAsset)loadedCompound.Asset;
                    }

                    // Try to get a fallback asset
                    var fallbackPath = "defaults/default." + type.Extension;
                    var fallbackCompound = LookupCompound(fallbackPath);
                    if (fallbackCompound != null && fallbackCompound.Asset is TAsset)
                    {
                        App.Log("Error: Could not find asset {0}. Using {1} instead.", path, fallbackPath);
                        return (TAsset)fallbackCompound.Asset;
                    }
                }
                else
                {
                    // Try to get the asset
                    var loadedBasic = LookupBasic(path);
                    if (loadedBasic != null && loadedBasic.Asset is TAsset)
                    {
                        return (TAsset)loadedBasic.Asset;
                    }

                    // Try to get a fallback asset
                    var fallbackPath = "defaults/default." + type.Extension;
                    var fallbackBasic = LookupBasic(fallbackPath);
                    if (fallbackBasic != null && fallbackBasic.Asset is TAsset)
                    {
                        App.Log("Error: Could not find asset {0}. Using {1} instead.", path, fallbackPath);
                        return (TAsset)fallbackBasic.Asset;
                    }
                }
            }

            // Error
            App.Log("Error: Could not find asset {0}. No fallback available.", path);
            throw new AssetLoadException(path, "No such asset");
        }

        private static IReadOnlyList<IAssetSource> s_emptyAssetSourceList = new List<IAssetSource>().ToReadOnly();

        public static IReadOnlyList<IAssetSource> GetSources(string path)
        {
            var extension = AssetPath.GetExtension(path);
            var type = LookupType(extension);
            if (type != null)
            {
                if (type.Compound)
                {
                    var loadedCompoundAsset = LookupCompound(path);
                    if (loadedCompoundAsset != null)
                    {
                        return loadedCompoundAsset.Sources.ToReadOnly();
                    }
                }
                else
                {
                    var loadedAsset = LookupBasic(path);
                    if (loadedAsset != null)
                    {
                        var list = new List<IAssetSource>();
                        list.Add(loadedAsset.Source);
                        return list.ToReadOnly();
                    }
                }
            }
            return s_emptyAssetSourceList;
        }

        public static IEnumerable<TAsset> List<TAsset>(string path, IAssetSource filter = null) where TAsset : class, IAsset
        {
            var type = LookupType(typeof(TAsset));
            if (type != null)
            {
                if (type.Compound)
                {
                    // Find the asset in the compound list
                    foreach (var asset in s_loadedCompoundAssets.Values)
                    {
                        if (asset.Asset is TAsset &&
                            AssetPath.GetDirectoryName(asset.Path) == path &&
                            (filter == null || asset.Sources.Contains(filter)))
                        {
                            yield return (TAsset)asset.Asset;
                        }
                    }
                }
                else
                {
                    // Find the asset in the basic list
                    foreach (var asset in s_loadedBasicAssets.Values)
                    {
                        if (asset.Asset is TAsset &&
                            AssetPath.GetDirectoryName(asset.Path) == path &&
                            (filter == null || asset.Source == filter))
                        {
                            yield return (TAsset)asset.Asset;
                        }
                    }
                }
            }
        }

        public static IEnumerable<TAsset> Find<TAsset>(IAssetSource filter = null) where TAsset : class, IBasicAsset
        {
            return Find<TAsset>("", filter);
        }

        public static IEnumerable<TAsset> Find<TAsset>(string path, IAssetSource filter = null) where TAsset : class, IAsset
        {
            var pathWithSlash = (path != "") ? path + "/" : "";
            var type = LookupType(typeof(TAsset));
            if (type != null)
            {
                if (type.Compound)
                {
                    // Find the asset in the compound list
                    foreach (var asset in s_loadedCompoundAssets.Values)
                    {
                        if (asset.Asset is TAsset &&
                            asset.Path.StartsWith(pathWithSlash, StringComparison.InvariantCulture) &&
                            (filter == null || asset.Sources.Contains(filter)))
                        {
                            yield return (TAsset)asset.Asset;
                        }
                    }
                }
                else
                {
                    // Find the asset in the basic list
                    foreach (var asset in s_loadedBasicAssets.Values)
                    {
                        if (asset.Asset is TAsset &&
                            asset.Path.StartsWith(pathWithSlash, StringComparison.InvariantCulture) &&
                            (filter == null || asset.Source == filter))
                        {
                            yield return (TAsset)asset.Asset;
                        }
                    }
                }
            }
        }

        private static RegisteredType LookupType(string extension)
        {
            RegisteredType type;
            if (s_extensionToRegisteredType.TryGetValue(extension, out type))
            {
                return type;
            }
            return null;
        }

        private static RegisteredType LookupType(Type assetType)
        {
            RegisteredType type;
            if (s_typeToRegisteredType.TryGetValue(assetType, out type))
            {
                return type;
            }
            return null;
        }

        private static LoadedBasicAsset LookupBasic(string path)
        {
            LoadedBasicAsset asset;
            if (s_loadedBasicAssets.TryGetValue(path, out asset))
            {
                return asset;
            }
            return null;
        }

        private static LoadedCompoundAsset LookupCompound(string path)
        {
            LoadedCompoundAsset asset;
            if (s_loadedCompoundAssets.TryGetValue(path, out asset))
            {
                return asset;
            }
            return null;
        }
    }
}
