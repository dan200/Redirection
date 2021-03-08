using Dan200.Core.Render;


namespace Dan200.Game.Game
{
    public class ShutdownState : State
    {
        public ShutdownState(Game game) : base(game)
        {
        }

        protected override void OnPreInit(State previous, Transition transition)
        {
            Game.Audio.PlayMusic(null, (transition != null) ? transition.Duration : 0.0f);
        }

        protected override void OnInit()
        {
            Game.Over = true;
        }

        protected override void OnReveal()
        {
        }

        protected override void OnHide()
        {
        }

        protected override void OnPreUpdate(float dt)
        {
        }

        protected override void OnUpdate(float dt)
        {
        }

        protected override void OnPopulateCamera(Camera camera)
        {
        }

        protected override void OnShutdown()
        {
        }

        protected override void OnPostUpdate(float dt)
        {
        }

        protected override void OnPostShutdown()
        {
        }

        protected override void OnPreDraw()
        {
        }

        protected override void OnDraw()
        {
        }

        protected override void OnPostDraw()
        {
        }
    }
}
