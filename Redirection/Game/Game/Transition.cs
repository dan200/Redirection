using Dan200.Core.Render;

namespace Dan200.Game.Game
{
    public abstract class Transition
    {
        private Game m_game;
        private bool m_complete;
        private State m_before;
        private State m_after;

        public abstract float Duration
        {
            get;
        }

        public bool Complete
        {
            get
            {
                return m_complete;
            }
            protected set
            {
                m_complete = value;
            }
        }

        protected Game Game
        {
            get
            {
                return m_game;
            }
        }

        protected State Before
        {
            get
            {
                return m_before;
            }
        }

        protected State After
        {
            get
            {
                return m_after;
            }
        }

        public Transition(Game game)
        {
            m_game = game;
            m_complete = false;
            m_before = null;
            m_after = null;
        }

        protected abstract void OnInit();
        protected abstract void OnUpdate(float dt);
        protected abstract void OnPopulateCamera(Camera camera);
        protected abstract void OnShutdown();

        public void Init(State before, State after)
        {
            m_before = before;
            m_after = after;
            OnInit();
        }

        public void Update(float dt)
        {
            OnUpdate(dt);
        }

        public void PopulateCamera(Camera camera)
        {
            OnPopulateCamera(camera);
        }

        public void Shutdown()
        {
            OnShutdown();
        }
    }
}

