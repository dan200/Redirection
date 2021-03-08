using Dan200.Core.Render;

namespace Dan200.Game.Game
{
    public class CutTransition : Transition
    {
        private float m_delay;

        public override float Duration
        {
            get
            {
                return m_delay;
            }
        }

        public CutTransition(Game game, float delay) : base(game)
        {
            m_delay = delay;
        }

        protected override void OnInit()
        {
            if (m_delay <= 0.0f)
            {
                Before.Hide();
                After.Reveal();
                Complete = true;
            }
        }

        protected override void OnUpdate(float dt)
        {
            if (m_delay > 0.0f)
            {
                m_delay -= dt;
            }
            else
            {
                Before.Hide();
                After.Reveal();
                Complete = true;
            }
        }

        protected override void OnPopulateCamera(Camera camera)
        {
            Before.PopulateCamera(camera);
        }

        protected override void OnShutdown()
        {
        }
    }
}
