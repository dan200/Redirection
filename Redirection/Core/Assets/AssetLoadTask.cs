using System;
using System.Collections.Generic;

namespace Dan200.Core.Assets
{
    public class AssetLoadTask
    {
        private List<string> m_assets;
        private int m_loaded;

        public int Total
        {
            get
            {
                return m_assets.Count;
            }
        }

        public int Loaded
        {
            get
            {
                return m_loaded;
            }
        }

        public int Remaining
        {
            get
            {
                return m_assets.Count - m_loaded;
            }
        }

        public AssetLoadTask()
        {
            m_assets = new List<string>();
            m_loaded = 0;
        }

        public void AddPath(string path)
        {
            m_assets.Add(path);
        }

        public void LoadAll()
        {
            while (LoadOne())
            {
            }
        }

        public void LoadSome(TimeSpan maxDuration)
        {
            var start = DateTime.UtcNow;
            while (LoadOne() && (DateTime.UtcNow - start) < maxDuration)
            {
            }
        }

        public bool LoadOne()
        {
            if (m_loaded < m_assets.Count)
            {
                var path = m_assets[m_loaded];
                Assets.Reload(path);
                m_loaded++;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

