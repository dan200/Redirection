using Dan200.Core.Assets;
using Dan200.Core.Util;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Game.Game
{
    public class Campaign : IBasicAsset
    {
        // Statics

        public static Campaign Get(string path)
        {
            return Assets.Get<Campaign>(path);
        }

        // Nonstatics

        private string m_path;

        private string m_title;
        private int m_id;
        private List<string> m_levels;
        private List<int> m_checkpoints;
        private int m_initialLevelsUnlocked;
		private bool m_hidden;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public string Title
        {
            get
            {
                return m_title;
            }
            set
            {
                m_title = value;
            }
        }

        public int ID
        {
            get
            {
                return m_id;
            }
            set
            {
                m_id = value;
            }
        }

        public List<string> Levels
        {
            get
            {
                return m_levels;
            }
        }

        public List<int> Checkpoints
        {
            get
            {
                return m_checkpoints;
            }
        }

        public int InitialLevelsUnlocked
        {
            get
            {
                return m_initialLevelsUnlocked;
            }
            set
            {
                m_initialLevelsUnlocked = value;
            }
        }

		public bool Hidden
		{
			get
			{
				return m_hidden;
			}
			set
			{
				m_hidden = value;
			}
		}

        public Campaign(string path)
        {
            m_path = path;
            m_title = "Untitled Campaign";
            m_levels = new List<string>();
            m_checkpoints = new List<int>();
            m_id = MathUtils.GenerateLevelID(path);
            m_initialLevelsUnlocked = 1;
			m_hidden = false;
        }

        public Campaign(string path, IFileStore store)
        {
            m_path = path;
            m_levels = new List<string>();
            m_checkpoints = new List<int>();
            Load(store);
        }

        public Campaign Copy()
        {
            var copy = new Campaign(m_path);
            copy.Title = Title;
            copy.Levels.AddRange(Levels);
            copy.Checkpoints.AddRange(Checkpoints);
            copy.ID = ID;
            copy.InitialLevelsUnlocked = InitialLevelsUnlocked;
			copy.Hidden = Hidden;
            return copy;
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
            kvp.Comment = "Campaign data";
            kvp.Set("title", Title);
            kvp.SetStringArray("levels", Levels.ToArray());
            kvp.SetIntegerArray("checkpoints", Checkpoints.ToArray());
            kvp.Set("id", m_id);
            kvp.Set("initial_levels_unlocked", m_initialLevelsUnlocked);
			if (m_hidden)
			{
				kvp.Set("hidden", m_hidden);
			}
			kvp.Save(writer);
        }

        public void Reload(IFileStore store)
        {
            Unload();
            Load(store);
        }

        public void Dispose()
        {
            Unload();
        }

        private void Load(IFileStore store)
        {
            var kvp = new KeyValuePairs();
            using (var reader = store.OpenTextFile(m_path))
            {
                kvp.Load(reader);
            }

            if (kvp.Count == 0)
            {
                LegacyLoad(store);
            }
            else
            {
                // Read in the title and levels
                m_title = kvp.GetString("title", "Untitled Campaign");
                m_levels.AddRange(kvp.GetStringArray("levels", new string[0]));
                m_checkpoints.AddRange(kvp.GetIntegerArray("checkpoints", new int[0]));
                m_id = kvp.GetInteger("id", MathUtils.SimpleStableHash(m_path));
                m_initialLevelsUnlocked = kvp.GetInteger("initial_levels_unlocked", 1);
				m_hidden = kvp.GetBool("hidden", false);
            }
        }

        private void LegacyLoad(IFileStore store)
        {
            using (var reader = store.OpenTextFile(m_path))
            {
                // Read in the title
                m_title = reader.ReadLine();

                // Read in the levels
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    int commentIndex = line.IndexOf("//");
                    if (commentIndex >= 0)
                    {
                        line = line.Substring(0, commentIndex);
                    }
                    var trimmedLine = line.Trim();
                    if (trimmedLine.Length > 0)
                    {
                        m_levels.Add(trimmedLine);
                    }
                }

                // Generate defaults
                m_id = MathUtils.SimpleStableHash(m_path);
                m_initialLevelsUnlocked = 1;
				m_hidden = false;
            }
        }

        private void Unload()
        {
            m_levels.Clear();
            m_checkpoints.Clear();
        }
    }
}
