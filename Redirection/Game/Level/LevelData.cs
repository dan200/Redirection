using Dan200.Core.Assets;
using Dan200.Core.Util;

namespace Dan200.Game.Level
{
    public class LevelData : IBasicAsset
    {
        public static LevelData Get(string path)
        {
            return Assets.Get<LevelData>(path);
        }

        private string m_path;
        private int m_id;

        public string Path
        {
            get
            {
                return m_path;
            }
        }

        public int ID
        {
            get
            {
                return m_id;
            }
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Depth { get; private set; }
        public int XOrigin { get; private set; }
        public int YOrigin { get; private set; }
        public int ZOrigin { get; private set; }

        public string[] TileLookup { get; private set; }
        public int[,,] TileIDs { get; private set; }
        public FlatDirection[,,] TileDirections { get; private set; }

        public string Title { get; private set; }
        public string Music { get; private set; }
        public string Sky { get; private set; }
        public string Script { get; private set; }
        public string Item { get; private set; }
        public int ItemCount { get; private set; }
        public bool EverCompleted { get; private set; }

        public float CameraPitch { get; private set; }
        public float CameraYaw { get; private set; }
        public float CameraDistance { get; private set; }

        public int RandomSeed { get; private set; }

        public string Intro { get; private set; }
        public string Outro { get; private set; }

        public int RobotCount { get; private set; }

        public LevelData(string path, IFileStore store)
        {
            m_path = path;
            Load(store);
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
            // Load the file
            var kvp = new KeyValuePairs();
            using (var stream = store.OpenTextFile(m_path))
            {
                kvp.Load(stream);
            }

            // Load the ID
            m_id = kvp.GetInteger("id", MathUtils.SimpleStableHash(m_path));

            // Load the dimensions
            Width = kvp.GetInteger("tiles.width", 0);
            Height = kvp.GetInteger("tiles.height", 0);
            Depth = kvp.GetInteger("tiles.depth", 0);
            XOrigin = kvp.GetInteger("tiles.x_origin", 0);
            YOrigin = kvp.GetInteger("tiles.y_origin", 0);
            ZOrigin = kvp.GetInteger("tiles.z_origin", 0);

            // Load the tile lookup
            TileLookup = kvp.GetStringArray("tiles.lookup", new string[0]);

            // Load the tiles
            TileIDs = new int[Width, Height, Depth];
            TileDirections = new FlatDirection[Width, Height, Depth];
            var tileData = kvp.GetString("tiles.data", "");
            for (int x = 0; x < Width; ++x)
            {
                for (int y = 0; y < Height; ++y)
                {
                    for (int z = 0; z < Depth; ++z)
                    {
                        int index = (x * Height * Depth) + (y * Depth) + z;
                        var id = 0;
                        var direction = FlatDirection.North;
                        if (((index * 3) + 3) <= tileData.Length)
                        {
                            id = Base64.ParseInt(tileData.Substring(index * 3, 2));
                            int directionNum = Base64.ParseInt(tileData.Substring((index * 3) + 2, 1));
                            if (directionNum >= 0 && directionNum < 4)
                            {
                                direction = (FlatDirection)directionNum;
                            }
                        }
                        TileIDs[x, y, z] = id;
                        TileDirections[x, y, z] = direction;
                    }
                }
            }

            // Load the level info
            Title = kvp.GetString("title", "Untitled");
            Music = kvp.GetString("music", "music/lightless_dawn.ogg");
            Sky = kvp.GetString("sky", "skies/starfield.sky");
            Script = kvp.GetString("script", null);

            if (kvp.ContainsKey("items.grey_cube.count"))
            {
                Item = "tiles/new/cone_spawn.tile";
                ItemCount = kvp.GetInteger("items.grey_cube.count", 0);
            }
            else
            {
                Item = kvp.GetString("item", "tiles/new/cone_spawn.tile");
                ItemCount = kvp.GetInteger("item_count", 0);
            }
            EverCompleted = kvp.GetBool("ever_completed", false);

            CameraPitch = kvp.GetFloat("camera.pitch", 60.0f);
            CameraYaw = kvp.GetFloat("camera.yaw", 270.0f - 22.5f);
            CameraDistance = kvp.GetFloat("camera.distance", 18.0f);

            Intro = kvp.GetString("intro", null);
            Outro = kvp.GetString("outro", null);

            RandomSeed = kvp.GetInteger("random.seed", Path.GetHashCode());
            RobotCount = kvp.GetInteger("robot_count", 0);
        }

        private void Unload()
        {
        }
    }
}

