using Dan200.Core.Render;
using OpenTK;
using System;
using System.Collections.Generic;

namespace Dan200.Game.Level
{
    public class ParticleManager : IDisposable
    {
        private ILevel m_level;
        private List<ParticleEmitter> m_emitters;

        public ParticleManager(ILevel level)
        {
            m_level = level;
            m_emitters = new List<ParticleEmitter>();
        }

        public ParticleEmitter Create(ParticleStyle style, bool startActive, bool realTime)
        {
            var startTime = realTime ? m_level.TimeMachine.RealTime : m_level.TimeMachine.Time;
            if (startActive)
            {
                startTime -= style.Lifetime;
            }
            var emitter = new ParticleEmitter(style, Matrix4.Identity, startTime);
            emitter.RealTime = realTime;
            m_emitters.Add(emitter);
            return emitter;
        }

        public void Update()
        {
            var time = m_level.TimeMachine.Time;
            var realTime = m_level.TimeMachine.RealTime;
            for (int i = m_emitters.Count - 1; i >= 0; --i)
            {
                var emitter = m_emitters[i];
                var emitterTime = emitter.RealTime ? realTime : time;
                if (emitterTime < emitter.SpawnTime || emitter.IsFinished(emitterTime))
                {
                    m_emitters.RemoveAt(i);
                    emitter.Dispose();
                }
            }
        }

        public void Draw(Camera camera)
        {
            var time = m_level.TimeMachine.Time;
            var realTime = m_level.TimeMachine.RealTime;
            for (int i = 0; i < m_emitters.Count; ++i)
            {
                var emitter = m_emitters[i];
                var emitterTime = emitter.RealTime ? realTime : time;
                emitter.Draw(camera, m_level.Transform, emitterTime);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < m_emitters.Count; ++i)
            {
                var emitter = m_emitters[i];
                emitter.Dispose();
            }
        }
    }
}

