using Dan200.Core.Render;
using Dan200.Game.GUI;

namespace Dan200.Game.Game
{
    public enum BlackTransitionType
    {
        ToBlack,
        FromBlack
    }

    public class BlackTransition : Transition
    {
        public const float DURATION = 0.5f;

        private BlackTransitionType m_type;
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

        public BlackTransition(Game game, BlackTransitionType type, float delayBeforeStart) : base(game)
        {
            m_type = type;
            m_wipe = new BoxWipe();
            m_wipe.Coverage = (type == BlackTransitionType.FromBlack) ? 1.0f : 0.0f;
            m_delayBeforeStart = delayBeforeStart;
        }

        protected override void OnInit()
        {
            m_progress = 0.0f;
            Game.Screen.Elements.Add(m_wipe);
            if (m_type == BlackTransitionType.FromBlack)
            {
                Before.Hide();
                After.Reveal();
            }
        }

        protected override void OnUpdate(float dt)
        {
            if (m_progress < 1.0f)
            {
                if (m_delayBeforeStart > 0.0f)
                {
                    m_delayBeforeStart -= dt;
                }
                else
                {
                    m_progress += (dt / DURATION);
                    if (m_progress >= 1.0f)
                    {
                        m_progress = 1.0f;
                        Complete = true;
                    }
                }
            }

            if (m_type == BlackTransitionType.ToBlack)
            {
                m_wipe.Coverage = m_progress;
            }
            else
            {
                m_wipe.Coverage = 1.0f - m_progress;
            }
        }

        protected override void OnShutdown()
        {
            Game.Screen.Elements.Remove(m_wipe);
            m_wipe.Dispose();
            m_wipe = null;
            if (m_type == BlackTransitionType.ToBlack)
            {
                Before.Hide();
                After.Reveal();
            }
        }

        protected override void OnPopulateCamera(Camera camera)
        {
            if (Before is LevelState)
            {
                if (m_type == BlackTransitionType.ToBlack)
                {
                    ((LevelState)Before).PopulateCamera(camera);
                }
            }
            if (After is LevelState)
            {
                if (m_type == BlackTransitionType.FromBlack)
                {
                    ((LevelState)After).PopulateCamera(camera);
                }
            }
        }
    }
}
