using Dan200.Core.Assets;
using System.IO;

namespace Dan200.Game.Arcade
{
    public class ArcadeDisk : IBasicAsset
    {
        public static ArcadeDisk Get(string path)
        {
            return Assets.Get<ArcadeDisk>(path);
        }

        private string m_path;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public int ID
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public string ContentPath
        {
            get;
            set;
        }

        public string Button0Prompt
        {
            get;
            set;
        }

        public string Button1Prompt
        {
            get;
            set;
        }

        public string UnlockCampaign
        {
            get;
            set;
        }

        public int UnlockLevelCount
        {
            get;
            set;
        }

        private string DefaultContentPath
        {
            get
            {
                return AssetPath.Combine(AssetPath.GetDirectoryName(m_path), AssetPath.GetFileNameWithoutExtension(m_path) + "_content");
            }
        }

        public ArcadeDisk(string path, IFileStore store)
        {
            m_path = path;
            Reload(store);
        }

        public void Dispose()
        {
        }

        public void Reload(IFileStore store)
        {
            var kvp = new KeyValuePairs();
            using (var reader = store.OpenTextFile(m_path))
            {
                kvp.Load(reader);
            }

            ID = kvp.GetInteger("id", 0);
            Title = kvp.GetString("title", null);
            ContentPath = kvp.GetString("content_path", DefaultContentPath);
            Button0Prompt = kvp.GetString("button0_prompt", "A");
            Button1Prompt = kvp.GetString("button1_prompt", "B");
            UnlockCampaign = kvp.GetString("unlock_campaign", null);
            UnlockLevelCount = kvp.GetInteger("unlock_level_count", 0);
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            using (var stream = new StreamWriter(path))
            {
                Save(stream);
            }
        }

        public void Save(TextWriter writer)
        {
            var kvp = new KeyValuePairs();
            if (ID != 0)
            {
                kvp.Set("id", ID);
            }
            kvp.Set("title", Title);
            if (ContentPath != DefaultContentPath)
            {
                kvp.Set("content_path", ContentPath);
            }
            kvp.Set("button0_prompt", Button0Prompt);
            kvp.Set("button1_prompt", Button1Prompt);
            if (UnlockCampaign != null)
            {
                kvp.Set("unlock_campaign", UnlockCampaign);
            }
            if (UnlockLevelCount != 0)
            {
                kvp.Set("unlock_level_count", UnlockLevelCount);
            }
            kvp.Save(writer);
        }
    }
}
