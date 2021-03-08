
using OpenTK;

namespace Dan200.Core.Render
{
    public class ModelEffectInstance : WorldEffectInstance
    {
        private Vector2 m_uvOffset;
        private Vector2 m_uvScale;

        private Vector4 m_diffuseColour;
        private ITexture m_diffuseTexture;

        private Vector3 m_specularColour;
        private ITexture m_specularTexture;

        private Vector3 m_emissiveColour;
        private ITexture m_emissiveTexture;

        private ITexture m_normalTexture;

        public Vector2 UVOffset
        {
            get
            {
                return m_uvOffset;
            }
            set
            {
                m_uvOffset = value;
            }
        }

        public Vector2 UVScale
        {
            get
            {
                return m_uvScale;
            }
            set
            {
                m_uvScale = value;
            }
        }

        public Vector4 DiffuseColour
        {
            get
            {
                return m_diffuseColour;
            }
            set
            {
                m_diffuseColour = value;
            }
        }

        public ITexture DiffuseTexture
        {
            get
            {
                return m_diffuseTexture;
            }
            set
            {
                m_diffuseTexture = value;
            }
        }

        public Vector3 SpecularColour
        {
            get
            {
                return m_specularColour;
            }
            set
            {
                m_specularColour = value;
            }
        }

        public ITexture SpecularTexture
        {
            get
            {
                return m_specularTexture;
            }
            set
            {
                m_specularTexture = value;
            }
        }

        public Vector3 EmissiveColour
        {
            get
            {
                return m_emissiveColour;
            }
            set
            {
                m_emissiveColour = value;
            }
        }

        public ITexture EmissiveTexture
        {
            get
            {
                return m_emissiveTexture;
            }
            set
            {
                m_emissiveTexture = value;
            }
        }

        public ITexture NormalTexture
        {
            get
            {
                return m_normalTexture;
            }
            set
            {
                m_normalTexture = value;
            }
        }

        protected ModelEffectInstance(string path) : base(path)
        {
            m_uvOffset = Vector2.Zero;
            m_uvScale = Vector2.One;
            m_diffuseColour = Vector4.One;
            m_diffuseTexture = Texture.White;
            m_specularColour = Vector3.One;
            m_specularTexture = Texture.Black;
            m_emissiveColour = Vector3.One;
            m_emissiveTexture = Texture.Black;
            m_normalTexture = Texture.Flat;
        }

        public override void Bind()
        {
            base.Bind();

            Set("uvOffset", m_uvOffset);
            Set("uvScale", m_uvScale);

            Set("diffuseColour", m_diffuseColour);
            Set("diffuseTexture", m_diffuseTexture, 0);

            Set("specularColour", m_specularColour);
            Set("specularTexture", m_specularTexture, 1);

            Set("emissiveColour", m_emissiveColour);
            Set("emissiveTexture", m_emissiveTexture, 2);

            Set("normalTexture", m_normalTexture, 3);
        }
    }
}
