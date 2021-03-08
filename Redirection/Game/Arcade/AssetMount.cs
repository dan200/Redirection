using Dan200.Core.Assets;
using Dan200.Core.Computer;
using Dan200.Core.Computer.APIs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dan200.Game.Arcade
{
    public class AssetMount : IMount
    {
        private string m_name;
        private string m_root;
        private long m_size;
        private List<IFileStore> m_sources;

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
                return m_size;
            }
        }

        public long UsedSpace
        {
            get
            {
                return m_size;
            }
        }

        private static List<IFileStore> FilterSources(IEnumerable<IAssetSource> sources, string root)
        {
            var results = new List<IFileStore>();

            // Add all loaded asset sources
            foreach (var source in sources)
            {
                if (source.FileStore.DirectoryExists(root))
                {
                    results.Add(source.FileStore);
                }
            }

            return results;
        }

        public AssetMount(string name, IEnumerable<IAssetSource> sources, string root)
        {
            m_name = name;
            m_root = root;
            m_sources = FilterSources(sources, root);
            m_size = FileSystem.Measure(this);
        }

        public bool Exists(FilePath path)
        {
            var fullPath = Resolve(path);
            var sources = m_sources;
            for (int i = sources.Count - 1; i >= 0; --i)
            {
                var source = sources[i];
                if (source.DirectoryExists(fullPath) ||
                    source.FileExists(fullPath))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsDir(FilePath path)
        {
            var fullPath = Resolve(path);
            var sources = m_sources;
            for (int i = sources.Count - 1; i >= 0; --i)
            {
                var source = sources[i];
                if (source.DirectoryExists(fullPath))
                {
                    return true;
                }
                else if (source.FileExists(fullPath))
                {
                    return false;
                }
            }
            return false;
        }

        public string[] List(FilePath path)
        {
            var fullPath = Resolve(path);
            var sources = m_sources;
            var results = new HashSet<string>();
            for (int i = sources.Count - 1; i >= 0; --i)
            {
                var source = sources[i];
                if (source.DirectoryExists(fullPath))
                {
                    results.UnionWith(source.ListFiles(fullPath));
                    results.UnionWith(source.ListDirectories(fullPath));
                }
            }
            return results.ToArray();
        }

        public long GetSize(FilePath path)
        {
            var fullPath = Resolve(path);
            var sources = m_sources;
            for (int i = sources.Count - 1; i >= 0; --i)
            {
                var source = sources[i];
                if (source.FileExists(fullPath))
                {
                    return source.GetFileSize(fullPath);
                }
            }
            return 0;
        }

        public DateTime GetModifiedTime(FilePath path)
        {
            var fullPath = Resolve(path);
            var sources = m_sources;
            for (int i = sources.Count - 1; i >= 0; --i)
            {
                var source = sources[i];
                if (source.FileExists(fullPath))
                {
                    return source.GetFileModificationTime(fullPath);
                }
            }
            return OSAPI.ZeroTime;
        }

        public TextReader OpenForRead(FilePath path)
        {
            var fullPath = Resolve(path);
            var sources = m_sources;
            for (int i = sources.Count - 1; i >= 0; --i)
            {
                var source = sources[i];
                if (source.FileExists(fullPath))
                {
                    return source.OpenTextFile(fullPath);
                }
            }
            return null;
        }

        public Stream OpenForBinaryRead(FilePath path)
        {
            var fullPath = Resolve(path);
            var sources = m_sources;
            for (int i = sources.Count - 1; i >= 0; --i)
            {
                var source = sources[i];
                if (source.FileExists(fullPath))
                {
                    return source.OpenFile(fullPath);
                }
            }
            return null;
        }

        private string Resolve(FilePath path)
        {
            return AssetPath.Combine(m_root, path.UnRoot().ToString());
        }
    }
}

