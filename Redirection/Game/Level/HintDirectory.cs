using Dan200.Core.Util;
using System.Collections.Generic;

namespace Dan200.Game.Level
{
    public class HintDirectory
    {
        private List<TileCoordinates> m_hints;

        public HintDirectory()
        {
            m_hints = new List<TileCoordinates>();
        }

        public void AddHint(TileCoordinates coordinates)
        {
            if (!m_hints.Contains(coordinates))
            {
                m_hints.Add(coordinates);
            }
        }

        public void RemoveHint(TileCoordinates coordinates)
        {
            m_hints.Remove(coordinates);
        }

        public Dan200.Core.Util.IReadOnlyCollection<TileCoordinates> GetHints()
        {
            return m_hints.ToReadOnly();
        }
    }
}

