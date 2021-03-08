using System;

namespace Dan200.Game.Level
{
	public class LevelOptions
	{
		public static LevelOptions Default
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
                options.EditorMode = true;
                return options;
            }
        }

		public bool EditorMode;

        private LevelOptions()
		{
			EditorMode = false;
		}
	}
}

