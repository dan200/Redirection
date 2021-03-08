using Dan200.Core.Render;

namespace Dan200.Game.Game
{
    public abstract class State
    {
        private readonly Game m_game;

        public Game Game
        {
            get
            {
                return m_game;
            }
        }

        public bool EnableGamepad
        {
            get;
            protected set;
        }

        public State(Game game)
        {
            m_game = game;
            EnableGamepad = true;
        }

        protected abstract void OnPreInit(State previous, Transition transition);
        protected abstract void OnPreUpdate(float dt);
        protected abstract void OnReveal();
        protected abstract void OnInit();
        protected abstract void OnUpdate(float dt);
        protected abstract void OnPopulateCamera(Camera camera);
        protected abstract void OnShutdown();
        protected abstract void OnPostUpdate(float dt);
        protected abstract void OnHide();
        protected abstract void OnPostShutdown();

        protected abstract void OnPreDraw();
        protected abstract void OnDraw();
        protected abstract void OnPostDraw();

        public void PreInit(State previous, Transition transition)
        {
            OnPreInit(previous, transition);
        }

        public void Reveal()
        {
            OnReveal();
        }

        public void PreUpdate(float dt)
        {
            OnPreUpdate(dt);
        }

        public void Init()
        {
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

        public void Hide()
        {
            OnHide();
        }

        public void PostUpdate(float dt)
        {
            OnPostUpdate(dt);
        }

        public void PostShutdown()
        {
            OnPostShutdown();
        }

        public void PreDraw()
        {
            OnPreDraw();
        }

        public void Draw()
        {
            OnDraw();
        }

        public void PostDraw()
        {
            OnPostDraw();
        }
    }
}
