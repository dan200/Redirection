namespace Dan200.Game.Level
{
    public interface ITileMap
    {
        int MinX { get; }
        int MinY { get; }
        int MinZ { get; }
        int MaxX { get; }
        int MaxY { get; }
        int MaxZ { get; }
        int Width { get; }
        int Height { get; }
        int Depth { get; }

        Tile this[TileCoordinates coordinates] { get; }
        Tile GetTile(TileCoordinates coordinates);
        TileState GetState(TileCoordinates coordinates);

        bool SetTile(TileCoordinates coordinates, Tile tile, FlatDirection direction = FlatDirection.North, bool notifyNeighbours = true);
        bool SetSubState(TileCoordinates coordinates, int subState, bool notifyNeighbours = true);

        void RequestRebuild();
        void RequestRebuild(TileCoordinates coordinates);
        void ExpandToFit(TileCoordinates coordinates);
    }
}

