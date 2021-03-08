namespace Dan200.Game.Level
{
    public class LevelInfo
    {
        private class State
        {
            public readonly int PlacementsLeft;

            public State(int placementsLeft)
            {
                PlacementsLeft = placementsLeft;
            }

            public State WithPlacementsLeft(int placementsLeft)
            {
                return new State(placementsLeft);
            }
        }
        private StateHistory<State> m_history;

        public string Path;
        public string Title;
        public string MusicPath;
        public string SkyPath;
        public string ScriptPath;
        public string ItemPath;
        public string IntroPath;
        public string OutroPath;
        public float CameraPitch;
        public float CameraYaw;
        public float CameraDistance;
        public int RandomSeed;
        public bool InEditor;
        public bool InMenu;
        public int ID;
        public int TotalPlacements;
        public bool EverCompleted;

        public int PlacementsLeft
        {
            get
            {
                return m_history.CurrentState.PlacementsLeft;
            }
            set
            {
                m_history.PushState(m_history.CurrentState.WithPlacementsLeft(value));
            }
        }

        public LevelInfo()
        {
            Title = "Untitled";
            MusicPath = null;
            SkyPath = null;
            ScriptPath = null;
            ItemPath = null;
            RandomSeed = 0;
            InEditor = false;
            InMenu = false;

            TotalPlacements = 0;
            m_history = new StateHistory<State>(0.0f);
            m_history.PushState(new State(0));
        }


        public void Update(float timeStamp)
        {
            m_history.Update(timeStamp);
        }
    }
}

