namespace Dan200.Game.Level
{
    public class Tiles
    {
        public static Tile Air
        {
            get { return Tile.Get("tiles/air.tile"); }
        }

        public static Tile Extension
        {
            get { return Tile.Get("tiles/extension.tile"); }
        }

        public static void Init()
        {
            TileBehaviour.RegisterBehavioursFrom(typeof(Tiles).Assembly);
        }
    }
}

