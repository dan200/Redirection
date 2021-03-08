using Dan200.Core.Assets;
using Dan200.Core.Render;
using System;
using System.IO;

namespace Dan200.Core.Modding
{
    public enum ModSource
    {
        Local,
        Editor,
        Workshop
    }

    public class Mod
    {
        public readonly string Path;
        public readonly ModSource Source;

        public string Title;
        public Version Version;
        public Version MinimumGameVersion;
        public string Author;
        public string AuthorTwitter;
        public bool AutoLoad;
        public bool Loaded;
        public ulong? SteamWorkshopID;
        public ulong? SteamUserID;

        private IFileStore m_contents;
        private FileAssetSource m_assets;

        public IAssetSource Assets
        {
            get
            {
                return m_assets;
            }
        }

        public Mod(string path, ModSource source)
        {
            Path = path;
            Source = source;
            if (File.Exists(Path) && Path.EndsWith(".zip"))
            {
                m_contents = new ZipArchiveFileStore(Path, "");
                m_assets = new FileAssetSource("Untitled Mod", new ZipArchiveFileStore(Path, "assets"));
            }
            else if (Directory.Exists(Path))
            {
                m_contents = new FolderFileStore(Path);
                m_assets = new FileAssetSource("Untitled Mod", new FolderFileStore(System.IO.Path.Combine(Path, "assets")));
            }
            else
            {
                m_contents = null;
                m_assets = new FileAssetSource("Untitled Mod", new EmptyFileStore());
            }
            m_assets.Mod = this;
            ReloadInfo();
            Loaded = false;
        }

        public Texture LoadIcon(bool filter)
        {
            // Load Icon
            if (m_contents != null && m_contents.FileExists("icon.png"))
            {
                var texture = new Texture("icon.png", m_contents);
                texture.Filter = filter;
                return texture;
            }
            return null;
        }

        public void ReloadInfo()
        {
            // Set default info
            Title = "Untitled Mod";
            Version = new Version(1, 0, 0);
            MinimumGameVersion = new Version(0, 0, 0);
            Author = null;
            AuthorTwitter = null;
            AutoLoad = false;
            SteamWorkshopID = null;
            SteamUserID = null;

            // Reload the index
            if (m_contents != null)
            {
                m_contents.ReloadIndex();
            }

            // Parse info.txt
            var infoPath = "info.txt";
            if (m_contents != null && m_contents.FileExists(infoPath))
            {
                var kvp = new KeyValuePairs();
                using (var stream = m_contents.OpenTextFile(infoPath))
                {
                    kvp.Load(stream);
                }

                Title = kvp.GetString("title", Title);
                Version = kvp.GetVersion("version", Version);
                MinimumGameVersion = kvp.GetVersion("game_version", MinimumGameVersion);
                Author = kvp.GetString("author", null);
                AuthorTwitter = kvp.GetString("author_twitter", null);
                AutoLoad = kvp.GetBool("autoload", false);
                if (kvp.ContainsKey("steam_workshop_id"))
                {
                    SteamWorkshopID = kvp.GetULong("steam_workshop_id");
                }
                else
                {
                    SteamWorkshopID = null;
                }
                if (kvp.ContainsKey("steam_user_id"))
                {
                    SteamUserID = kvp.GetULong("steam_user_id");
                }
                else
                {
                    SteamUserID = null;
                }
            }

            // Reload the index
            m_assets.Name = Title;
            m_assets.FileStore.ReloadIndex();
        }

        public void SaveInfo()
        {
            var infoPath = System.IO.Path.Combine(Path, "info.txt");
            var kvpFile = new KeyValuePairFile(infoPath);
            kvpFile.Set("title", Title);
            kvpFile.Set("version", Version);
            kvpFile.Set("game_version", MinimumGameVersion);
            kvpFile.Set("author", Author);
            kvpFile.Set("author_twitter", AuthorTwitter);
            kvpFile.Set("autoload", AutoLoad);
            if (SteamWorkshopID.HasValue)
            {
                kvpFile.Set("steam_workshop_id", SteamWorkshopID.Value.ToString());
            }
            else
            {
                kvpFile.Remove("steam_workshop_id");
            }
            if (SteamUserID.HasValue)
            {
                kvpFile.Set("steam_user_id", SteamUserID.Value.ToString());
            }
            else
            {
                kvpFile.Remove("steam_user_id");
            }
            kvpFile.SaveIfModified();
        }
    }
}

