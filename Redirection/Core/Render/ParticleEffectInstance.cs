using OpenTK;

namespace Dan200.Core.Render
{
    public class ParticleEffectInstance : EffectInstance
    {
        private Matrix4 m_modelMatrix;
        private Matrix4 m_worldMatrix;
        private Matrix4 m_viewMatrix;
        private Matrix4 m_projectionMatrix;
        private Matrix4 m_modelViewMatrix;

        private ParticleStyle m_style;
        private Texture m_noiseTexture;
        private float m_time;
        private float m_stopTime;

        public Matrix4 ModelMatrix
        {
            get
            {
                return m_modelMatrix;
            }
            set
            {
                m_modelMatrix = value;
                UpdateMatrices();
            }
        }

        public Matrix4 WorldMatrix
        {
            get
            {
                return m_worldMatrix;
            }
            set
            {
                m_worldMatrix = value;
                UpdateMatrices();
            }
        }

        public Matrix4 ViewMatrix
        {
            get
            {
                return m_viewMatrix;
            }
            set
            {
                m_viewMatrix = value;
                UpdateMatrices();
            }
        }

        public Matrix4 ProjectionMatrix
        {
            get
            {
                return m_projectionMatrix;
            }
            set
            {
                m_projectionMatrix = value;
                UpdateMatrices();
            }
        }

        public ParticleStyle Style
        {
            get
            {
                return m_style;
            }
            set
            {
                m_style = value;
            }
        }

        public float Time
        {
            get
            {
                return m_time;
            }
            set
            {
                m_time = value;
            }
        }

        public float StopTime
        {
            get
            {
                return m_stopTime;
            }
            set
            {
                m_stopTime = value;
            }
        }

        public ParticleEffectInstance() : base("shaders/particles.effect")
        {
            m_modelMatrix = Matrix4.Identity;
            m_worldMatrix = Matrix4.Identity;
            m_viewMatrix = Matrix4.Identity;
            m_projectionMatrix = Matrix4.Identity;
            UpdateMatrices();

            m_noiseTexture = Texture.Get("shaders/noise.png", false);
            m_noiseTexture.Wrap = true;
            m_style = null;
            m_time = 0.0f;
            m_stopTime = 0.0f;
        }

        public override void Bind()
        {
            base.Bind();
            Set("modelViewMatrix", ref m_modelViewMatrix);
            Set("projectionMatrix", ref m_projectionMatrix);
            Set("noiseTexture", m_noiseTexture, 0);
            Set("time", m_time);
            Set("stopTime", m_stopTime);
            if (m_style != null)
            {
                Set("texture", Texture.Get(m_style.Texture, false), 1);
                Set("lifetime", m_style.Lifetime);
                Set("emitterRate", m_style.EmitterRate);
                Set("emitterPos", m_style.Position);
                Set("emitterPosRange", m_style.PositionRange);
                Set("emitterVel", m_style.Velocity);
                Set("emitterVelRange", m_style.VelocityRange);
                Set("gravity", m_style.Gravity);
                Set("initialRadius", m_style.Radius);
                Set("finalRadius", m_style.FinalRadius);
                Set("initialColour", m_style.Colour);
                Set("finalColour", m_style.FinalColour);
            }
        }

        private void UpdateMatrices()
        {
            m_modelViewMatrix = m_modelMatrix * m_worldMatrix * m_viewMatrix;
        }
    }
}
