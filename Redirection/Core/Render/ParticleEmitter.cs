using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;

namespace Dan200.Core.Render
{
    public class ParticleEmitter : IDisposable
    {
        private static Random s_random = new Random();

        private ParticleStyle m_style;
        private Matrix4 m_transform;
        private Geometry m_geometry;
        private ParticleEffectInstance m_effect;
        private float m_startTime;
        private float? m_stopTime;
        private bool m_disposed;

        public ParticleStyle Style
        {
            get
            {
                return m_style;
            }
        }

        public Matrix4 Transform
        {
            get
            {
                return m_transform;
            }
            set
            {
                m_transform = value;
            }
        }

        public float SpawnTime
        {
            get
            {
                return m_startTime;
            }
        }

        public bool IsDisposed
        {
            get
            {
                return m_disposed;
            }
        }

        public bool RealTime;

        public ParticleEmitter(ParticleStyle effect, Matrix4 transform, float spawnTime)
        {
            m_style = effect;
            m_transform = transform;
            m_startTime = spawnTime;
            m_stopTime = null;
            m_geometry = new Geometry(Primitive.Triangles);
            m_effect = new ParticleEffectInstance();
            m_disposed = false;
            RealTime = false;
            Rebuild();
        }

        public void Dispose()
        {
            m_geometry.Dispose();
            m_disposed = true;
        }

        public void Stop(float time)
        {
            if (!m_stopTime.HasValue)
            {
                m_stopTime = time;
            }
        }

        public bool IsStopped(float time)
        {
            return m_stopTime.HasValue && time > m_stopTime.Value;
        }

        public bool IsFinished(float time)
        {
            return m_stopTime.HasValue && time > (m_stopTime.Value + m_style.Lifetime);
        }

        public void Draw(Camera camera, Matrix4 worldTransform, float time)
        {
            int maxParticleCount = (int)Math.Ceiling(m_style.EmitterRate * m_style.Lifetime);
            int particlesSinceStart = (time > m_startTime) ?
                (int)Math.Ceiling((time - m_startTime) * m_style.EmitterRate) :
                0;
            int aliveParticles = Math.Min(particlesSinceStart, maxParticleCount);
            if (aliveParticles > 0)
            {
                m_effect.ModelMatrix = m_transform;
                m_effect.WorldMatrix = worldTransform;
                m_effect.ViewMatrix = camera.Transform;
                m_effect.ProjectionMatrix = camera.CreateProjectionMatrix();
                m_effect.Style = m_style;
                m_effect.Time = time - m_startTime;
                m_effect.StopTime = m_stopTime.HasValue ? (m_stopTime.Value - m_startTime) : (m_effect.Time + m_style.Lifetime);
                m_effect.Bind();

                GL.DepthMask(false);
                if (aliveParticles * 6 > m_geometry.IndexCount)
                {
                    Rebuild();
                }
                m_geometry.DrawRange(0, aliveParticles * 6);
                GL.DepthMask(true);
            }
        }

        private void Rebuild()
        {
            m_geometry.Clear();
            int maxParticleCount = (int)Math.Ceiling(m_style.EmitterRate * m_style.Lifetime);
            for (int i = 0; i < maxParticleCount; ++i)
            {
                m_geometry.AddQuad(
                    new Vector3(-1.0f, 1.0f, 0.0f),
                    new Vector3(1.0f, 1.0f, 0.0f),
                    new Vector3(-1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    Quad.UnitSquare,
                    new Vector4(
                        (float)s_random.NextDouble(), // Noise texture X
                        (float)s_random.NextDouble(), // Noise texture Y
                        (float)i, // Particle index
                        1.0f
                    )
                );
            }
            m_geometry.Rebuild();
        }
    }
}
