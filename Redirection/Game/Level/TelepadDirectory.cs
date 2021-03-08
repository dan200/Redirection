using System.Collections.Generic;

namespace Dan200.Game.Level
{
    public class TelepadDirectory
    {
        private IDictionary<string, IList<TileCoordinates>> m_telepads;

        public TelepadDirectory()
        {
            m_telepads = new Dictionary<string, IList<TileCoordinates>>();
        }

        public void AddTelepad(string colour, TileCoordinates coordinates)
        {
            if (!m_telepads.ContainsKey(colour))
            {
                m_telepads.Add(colour, new List<TileCoordinates>());
            }
            m_telepads[colour].Add(coordinates);
        }

        public void RemoveTelepad(string colour, TileCoordinates coordinates)
        {
            if (m_telepads.ContainsKey(colour))
            {
                m_telepads[colour].Remove(coordinates);
            }
        }

        public TileCoordinates? GetMatchingTelepad(string colour, TileCoordinates coordinates)
        {
            TileCoordinates? result = null;
            if (m_telepads.ContainsKey(colour))
            {
                var list = m_telepads[colour];
                for (int i = 0; i < list.Count; ++i)
                {
                    var candidate = list[i];
                    if (candidate != coordinates)
                    {
                        if (!result.HasValue || candidate.Y > result.Value.Y)
                        {
                            result = candidate;
                        }
                    }
                }
            }
            return result;
        }
    }
}

