namespace Dan200.Game.Level
{
    public interface ITileLookup
    {
        Tile GetTileFromID(int id);
        int GetIDForTile(Tile tile);
    }
}

