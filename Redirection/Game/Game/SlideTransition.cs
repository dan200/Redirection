
using Dan200.Core.Render;
using Dan200.Core.Util;
using OpenTK;

namespace Dan200.Game.Game
{
    public enum SlideDirection
    {
        Left,
        Right
    }

    public class SlideTransition : Transition
    {
        public const float DURATION = 0.75f;
        public const float DISTANCE = 20.0f;

        private SlideDirection m_direction;
        private float m_progress;

        public override float Duration
        {
            get
            {
                return DURATION;
            }
        }

        public SlideTransition(Game game, SlideDirection direction) : base(game)
        {
            m_direction = direction;
        }

        protected override void OnInit()
        {
            m_progress = 0.0f;
            if (Before is LevelState)
            {
                LevelState beforeLevel = (LevelState)Before;

                Matrix4 cameraTransInv = Game.Camera.Transform;
                cameraTransInv.Invert();
                Vector3 right = Vector3.TransformVector(Vector3.UnitX, cameraTransInv);

                beforeLevel.Level.Transform = Matrix4.CreateTranslation(
                    right * DISTANCE * ((m_direction == SlideDirection.Right) ? -1.0f : 1.0f)
                ) * beforeLevel.Level.Transform;
            }
            After.Reveal();
        }

        protected override void OnUpdate(float dt)
        {
            m_progress += (dt / DURATION);
            if (m_progress >= 1.0f)
            {
                Complete = true;
            }
        }

        protected override void OnShutdown()
        {
            Before.Hide();
        }

        protected override void OnPopulateCamera(Camera camera)
        {
            if (After is LevelState)
            {
                ((LevelState)After).PopulateCamera(camera);
            }

            camera.Transform =
                camera.Transform *
                Matrix4.CreateTranslation(
                    (1.0f - MathUtils.Ease(m_progress)) * DISTANCE * ((m_direction == SlideDirection.Right) ? 1.0f : -1.0f),
                    0.0f,
                    0.0f
                );
        }
    }
}
