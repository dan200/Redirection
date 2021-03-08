using Dan200.Core.Audio;
using Dan200.Core.Render;
using Dan200.Core.Utils;
using OpenTK;
using System.Collections.Generic;
using System.Linq;

namespace Dan200.Game.Level
{
    public abstract class Entity
    {
        private ILevel m_level;
        private StateHistory<EntityState> m_history;

        private struct PlayingSound
        {
            public float TimeStamp;
            public IStoppable Playback;
        }
        private Dictionary<string, PlayingSound> m_sounds;
        private Dictionary<string, ParticleEmitter> m_particles;

        public ILevel Level
        {
            get
            {
                return m_level;
            }
        }

        public float SpawnTime
        {
            get
            {
                return m_history.InitialTimeStamp;
            }
        }

        public float CurrentTime
        {
            get
            {
                return m_history.LatestTimeStamp;
            }
        }

        protected EntityState CurrentState
        {
            get
            {
                return m_history.CurrentState;
            }
        }

        public Vector3 Position
        {
            get
            {
                return Transform.Row3.Xyz;
            }
        }

        public abstract Matrix4 Transform
        {
            get;
        }

        public bool Dead
        {
            get;
            private set;
        }

        public Entity()
        {
            Dead = false;
        }

        protected abstract void OnInit();
        protected abstract void OnShutdown();
        protected abstract void OnUpdate(float dt);
        protected abstract void OnDraw(ModelEffectInstance modelEffect, RenderPass pass);
        protected abstract void OnDrawShadows(ShadowEffectInstance shadowEffect);

        public void Init(ILevel level)
        {
            m_level = level;
            m_history = new StateHistory<EntityState>(Level.TimeMachine.Time);
            m_sounds = new Dictionary<string, PlayingSound>();
            m_particles = new Dictionary<string, ParticleEmitter>();
            OnInit();
        }

        public void Shutdown()
        {
            OnShutdown();
            foreach (var sound in m_sounds.Values)
            {
                sound.Playback.Stop();
            }
            m_sounds.Clear();
            foreach (var particle in m_particles.Values)
            {
                if (!particle.IsDisposed)
                {
                    particle.Stop(m_level.TimeMachine.Time);
                }
            }
            m_particles.Clear();
            Dead = true;
        }

        protected void PushState(EntityState state)
        {
            m_history.PushState(state);
        }

        public virtual void Update()
        {
            float now = Level.TimeMachine.Time;
            if (now < m_history.InitialTimeStamp)
            {
                // Wind time back to before we were born
                m_history.Reset();
                Level.Entities.Remove(this);
                return;
            }
            else if (now < m_history.LatestTimeStamp)
            {
                // Wind time back through our history
                m_history.Update(now);
            }
            else if (now >= m_history.LatestTimeStamp)
            {
                // Move time forwards
                float dt = now - m_history.LatestTimeStamp;
                m_history.Update(now);
                OnUpdate(dt);
            }

            // Update sounds and particles
            foreach (var soundPath in m_sounds.Keys.ToArray())
            {
                var sound = m_sounds[soundPath];
                if (sound.TimeStamp > CurrentTime)
                {
                    sound.Playback.Stop();
                    m_sounds.Remove(soundPath);
                }
                else if (sound.Playback.Stopped)
                {
                    m_sounds.Remove(soundPath);
                }
            }

            var transform = Transform;
            foreach (var pfxPath in m_particles.Keys.ToArray())
            {
                var pfx = m_particles[pfxPath];
                if (pfx.IsDisposed || pfx.IsStopped(CurrentTime))
                {
                    m_particles.Remove(pfxPath);
                }
                else
                {
                    pfx.Transform = transform;
                }
            }
        }

        public virtual bool NeedsRenderPass(RenderPass pass)
        {
            return pass == RenderPass.Opaque;
        }

        public void Draw(ModelEffectInstance modelEffect, RenderPass pass)
        {
            OnDraw(modelEffect, pass);
        }

        public void DrawShadows(ShadowEffectInstance shadowEffect)
        {
            OnDrawShadows(shadowEffect);
        }

        public virtual bool Raycast(Ray ray, out Direction o_side, out float o_distance)
        {
            o_side = default(Direction);
            o_distance = default(float);
            return false;
        }

        public virtual bool CanPlaceOnTop(out TileCoordinates o_coordinates)
        {
            o_coordinates = default(TileCoordinates);
            return false;
        }

        protected void PlaySound(string path, bool looping = false)
        {
            if (!m_sounds.ContainsKey(path))
            {
                var audio = Level.Audio;
                if (audio != null)
                {
                    var playback = audio.PlaySound(path, looping);
                    if (playback != null)
                    {
                        var sound = new PlayingSound();
                        sound.Playback = playback;
                        sound.TimeStamp = CurrentTime;
                        m_sounds.Add(path, sound);
                    }
                }
            }
        }

        protected void StopSound(string path)
        {
            if (m_sounds.ContainsKey(path))
            {
                var sound = m_sounds[path];
                sound.Playback.Stop();
                m_sounds.Remove(path);
            }
        }

        protected void StartParticles(string path, bool startActive, bool realTime)
        {
            if (!m_particles.ContainsKey(path))
            {
                var style = ParticleStyle.Get(path);
                var emitter = Level.Particles.Create(style, startActive, realTime);
                emitter.Transform = Transform;
                m_particles.Add(path, emitter);
            }
        }

        protected void StopParticles(string path)
        {
            if (m_particles.ContainsKey(path))
            {
                var emitter = m_particles[path];
                var emitterTime = emitter.RealTime ? Level.TimeMachine.RealTime : Level.TimeMachine.Time;
                emitter.Stop(emitterTime);
                m_particles.Remove(path);
            }
        }
    }
}

