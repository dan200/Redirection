
using Dan200.Core.Animation;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.Level;
using OpenTK;

namespace Dan200.Game.Game
{
    public class AnimatedCameraController : ICameraController
    {
        private TimeMachine m_timeMachine;
        private IAnimation m_animation;
        private float m_animStartTime;

        public AnimatedCameraController(TimeMachine timeMachine)
        {
            m_timeMachine = timeMachine;
            m_animation = null;
            m_animStartTime = 0.0f;
        }

        public void Play(IAnimation animation)
        {
            m_animation = animation;
            m_animStartTime = m_timeMachine.Time;
        }

        public void Populate(Camera camera)
        {
            Matrix4 transform;
            float fov;
            if (m_animation != null)
            {
                bool visible;
                Vector2 uvOffset;
                Vector2 uvScale;
                Vector4 colour;
                m_animation.Animate("camera", m_timeMachine.Time - m_animStartTime, out visible, out transform, out uvOffset, out uvScale, out colour, out fov);
                MathUtils.FastInvert(ref transform);
            }
            else
            {
                transform = Matrix4.Identity;
                fov = Game.DEFAULT_FOV;
            }
            camera.Transform = transform;
            camera.FOV = fov;
        }
    }
}

