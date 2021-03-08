using Dan200.Core.Animation;
using Dan200.Core.Render;
using OpenTK;
using System.Collections.Generic;

namespace Dan200.Game.Level
{
    public class CutsceneEntity : Entity
    {
        private ModelInstance m_model;
        private float m_animStartTime;
        private bool m_animateRoot;

        private RenderPass m_renderPass;
        private bool m_castShadows;

        private class QueuedSound
        {
            public string Path;
            public bool Looping;
            public float TimeLeft;

            public QueuedSound(string path, bool looping, float delay)
            {
                Path = path;
                Looping = looping;
                TimeLeft = delay;
            }
        }

        private List<QueuedSound> m_queuedSounds;

        public override Matrix4 Transform
        {
            get
            {
                return m_model.Transform;
            }
        }

        public bool Visible
        {
            get
            {
                return m_model.Visible;
            }
            set
            {
                m_model.Visible = value;
            }
        }

        protected ModelInstance Model
        {
            get
            {
                return m_model;
            }
        }

        public bool CastShadows
        {
            get
            {
                return m_castShadows;
            }
            set
            {
                m_castShadows = value;
            }
        }

        public CutsceneEntity(Model model, RenderPass renderPass)
        {
            m_model = new ModelInstance(model, Matrix4.Identity);
            m_animStartTime = 0.0f;
            m_animateRoot = false;

            m_renderPass = renderPass;
            m_castShadows = true;
        }

        public void StartAnimation(IAnimation anim, bool animateRoot)
        {
            m_model.Animation = anim;
            m_animStartTime = Level.TimeMachine.Time;
            m_animateRoot = animateRoot;
            m_model.AnimTime = 0.0f;
            m_model.Animate();
            AnimateRoot();
        }

        public void PlaySoundAfterDelay(string path, bool looping, float delay)
        {
            if (m_queuedSounds == null)
            {
                m_queuedSounds = new List<QueuedSound>(1);
            }
            m_queuedSounds.Add(new QueuedSound(path, looping, delay));
        }

        public new void PlaySound(string path, bool looping)
        {
            base.PlaySound(path, looping);
        }

        public new void StopSound(string path)
        {
            base.StopSound(path);
            if (m_queuedSounds != null)
            {
                for (int i = m_queuedSounds.Count - 1; i >= 0; --i)
                {
                    var sound = m_queuedSounds[i];
                    if (sound.Path == path)
                    {
                        m_queuedSounds.RemoveAt(i);
                    }
                }
            }
        }

        public void StartParticles(string path, bool startActive)
        {
            base.StartParticles(path, startActive, false);
        }

        public new void StopParticles(string path)
        {
            base.StopParticles(path);
        }

        protected override void OnInit()
        {
        }

        protected override void OnShutdown()
        {
        }

        protected override void OnUpdate(float dt)
        {
            m_model.AnimTime = Level.TimeMachine.Time - m_animStartTime;
            m_model.Animate();
            AnimateRoot();

            if (m_queuedSounds != null)
            {
                for (int i = m_queuedSounds.Count - 1; i >= 0; --i)
                {
                    var sound = m_queuedSounds[i];
                    sound.TimeLeft -= dt;
                    if (sound.TimeLeft <= 0)
                    {
                        PlaySound(sound.Path, sound.Looping);
                        m_queuedSounds.RemoveAt(i);
                    }
                }
            }
        }

        public override bool NeedsRenderPass(RenderPass pass)
        {
            return pass == m_renderPass;
        }

        protected override void OnDraw(ModelEffectInstance modelEffect, RenderPass pass)
        {
            m_model.Draw(modelEffect);
        }

        protected override void OnDrawShadows(ShadowEffectInstance shadowEffect)
        {
            if (m_castShadows)
            {
                m_model.DrawShadows(shadowEffect);
            }
        }

        private void AnimateRoot()
        {
            if (m_model.Animation != null && m_animateRoot)
            {
                bool visible;
                Matrix4 transform;
                Vector2 uvOffset;
                Vector2 uvScale;
                Vector4 colour;
                float cameraFOV;
                m_model.Animation.Animate("Root", m_model.AnimTime, out visible, out transform, out uvOffset, out uvScale, out colour, out cameraFOV);
                m_model.Visible = visible;
                m_model.Transform = transform;
            }
            else
            {
                m_model.Visible = true;
                m_model.Transform = Matrix4.Identity;
            }
        }
    }
}

