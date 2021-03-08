namespace Dan200.Game.Level
{
    public class LevelOptions
    {
        public static LevelOptions InGame
        {
            get
            {
                return new LevelOptions();
            }
        }

        public static LevelOptions Editor
        {
            get
            {
                var options = new LevelOptions();
                options.InEditor = true;
                return options;
            }
        }

        public static LevelOptions Menu
        {
            get
            {
                var options = new LevelOptions();
                options.InMenu = true;
                return options;
            }
        }

        public static LevelOptions Cutscene
        {
            get
            {
                return new LevelOptions();
            }
        }

        public bool InEditor;
        public bool InMenu;

        private LevelOptions()
        {
            InEditor = false;
            InMenu = false;
        }
    }
}

