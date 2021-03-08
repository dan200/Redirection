using System;
using System.Collections.Generic;

namespace Dan200.Game.Level
{
    public class TileLookup : ITileLookup
    {
        private int m_lowestUnusedID;
        private Dictionary<int, Tile> m_idToTile;
        private Dictionary<Tile, int> m_tileToID;

        public TileLookup()
        {
            m_idToTile = new Dictionary<int, Tile>();
            m_tileToID = new Dictionary<Tile, int>();
            m_lowestUnusedID = 0;
        }

        public Tile GetTileFromID(int id)
        {
            if (m_idToTile.ContainsKey(id))
            {
                return m_idToTile[id];
            }
            return Tiles.Air;
        }

        public int GetIDForTile(Tile tile)
        {
            if (m_tileToID.ContainsKey(tile))
            {
                return m_tileToID[tile];
            }
            return -1;
        }

        public void AddTile(Tile tile, int id)
        {
            if (m_idToTile.ContainsKey(id))
            {
                throw new Exception("ID Conflict");
            }
            m_idToTile[id] = tile;
            m_tileToID[tile] = id;
            m_lowestUnusedID = Math.Max(m_lowestUnusedID, id + 1);
        }

        public int AddTile(Tile tile)
        {
            int id = m_lowestUnusedID;
            AddTile(tile, id);
            return id;
        }
    }
}

