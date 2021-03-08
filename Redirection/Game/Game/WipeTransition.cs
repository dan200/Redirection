using Dan200.Core.Render;
using Dan200.Game.GUI;

namespace Dan200.Game.Game
{
    public class WipeTransition : Transition
    {
        public const float DURATION = 1.0f;

        private BoxWipe m_wipe;
        private float m_progress;
        private float m_delayBeforeStart;

        public override float Duration
        {
            get
            {
                return m_delayBeforeStart + DURATION;
            }
        }

        public WipeTransition(Game game, float delayBeforeStart) : base(game)
        {
            m_wipe = new BoxWipe();
            m_delayBeforeStart = delayBeforeStart;
        }

        protected override void OnInit()
        {
            m_progress = 0.0f;
            m_wipe.Coverage = 0.0f;
            Game.Screen.Elements.Add(m_wipe);
        }

        protected override void OnUpdate(float dt)
        {
            var oldProgress = m_progress;
            if (m_delayBeforeStart > 0.0f)
            {
                m_delayBeforeStart -= dt;
            }
            else
            {
                m_progress += dt / DURATION;
            }

            if (oldProgress < 0.5f & m_progress >= 0.5f)
            {
                Before.Hide();
                After.Reveal();
            }
            if (m_progress >= 1.0f)
            {
                Complete = true;
                m_progress = 1.0f;
            }

            if (m_progress < 0.5f)
            {
                m_wipe.Coverage = m_progress / 0.5f;
            }
            else
            {
                m_wipe.Coverage = 1.0f - ((m_progress - 0.5f) / 0.5f);
            }
        }

        protected override void OnPopulateCamera(Camera camera)
        {
            if (m_progress < 0.5f)
            {
                Before.PopulateCamera(camera);
            }
            else
            {
                After.PopulateCamera(camera);
            }
        }

        protected override void OnShutdown()
        {
            Game.Screen.Elements.Remove(m_wipe);
            m_wipe.Dispose();
            m_wipe = null;
        }
    }
}
