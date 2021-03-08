using Dan200.Core.Assets;
using System.Collections;
using System.Collections.Generic;

namespace Dan200.Game.Level
{
    public class TileList : Dan200.Core.Util.IReadOnlyList<Tile>
    {
        private string[] m_paths;
        private List<Tile> m_tiles;

        public int Count
        {
            get
            {
                return m_tiles.Count;
            }
        }

        public Tile this[int index]
        {
            get
            {
                return m_tiles[index];
            }
        }

        public TileList(params string[] paths)
        {
            m_paths = paths;
            m_tiles = new List<Tile>();
            Load();
        }

        public void Reload()
        {
            m_tiles.Clear();
            Load();
        }

        private void Load()
        {
            foreach (string path in m_paths)
            {
                foreach (Tile tile in Assets.Find<Tile>(path))
                {
                    if (tile.Placeable /*&& 
                        AssetPath.GetDirectoryName(tile.Path) != "tiles/classic"*/ )
                    {
                        m_tiles.Add(tile);
                    }
                }
            }
            m_tiles.Sort((x, y) => x.Path.CompareTo(y.Path));
        }

        public Tile GetFirstTile()
        {
            return m_tiles[0];
        }

        public int GetTileIndex(Tile tile)
        {
            for (int i = 0; i < m_tiles.Count; ++i)
            {
                if (m_tiles[i] == tile)
                {
                    return i;
                }
            }
            return -1;
        }

        public Tile GetNextTile(Tile tile)
        {
            for (int i = 0; i < m_tiles.Count; ++i)
            {
                if (m_tiles[i] == tile && i < m_tiles.Count - 1)
                {
                    return m_tiles[i + 1];
                }
            }
            return m_tiles[0];
        }

        public Tile GetPreviousTile(Tile tile)
        {
            for (int i = 0; i < m_tiles.Count; ++i)
            {
                if (m_tiles[i] == tile && i > 0)
                {
                    return m_tiles[i - 1];
                }
            }
            return m_tiles[m_tiles.Count - 1];
        }

        public IEnumerator<Tile> GetEnumerator()
        {
            return m_tiles.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)m_tiles).GetEnumerator();
        }
    }
}

