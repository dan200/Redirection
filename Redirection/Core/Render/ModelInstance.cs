
using Dan200.Core.Animation;
using OpenTK;

namespace Dan200.Core.Render
{
    public class ModelInstance
    {
        private Model m_model;
        private Matrix4 m_baseTransform;
        private Matrix4[] m_groupTransforms;
        private Matrix4[] m_fullTransforms;
        private bool m_baseVisible;
        private bool[] m_visibility;
        private Vector2[] m_uvOffset;
        private Vector2[] m_uvScale;
        private Vector4[] m_colour;
        private Vector4[] m_fullColour;
        private float m_alpha;

        private IAnimation m_animation;
        private float m_animTime;

        public Model Model
        {
            get
            {
                return m_model;
            }
        }

        public Matrix4 Transform
        {
            get
            {
                return m_baseTransform;
            }
            set
            {
                m_baseTransform = value;
                for (int i = 0; i < m_groupTransforms.Length; ++i)
                {
                    m_fullTransforms[i] = m_groupTransforms[i] * m_baseTransform;
                }
            }
        }

        public bool Visible
        {
            get
            {
                return m_baseVisible;
            }
            set
            {
                m_baseVisible = value;
            }
        }

        public float Alpha
        {
            get
            {
                return m_alpha;
            }
            set
            {
                if (m_alpha != value)
                {
                    m_alpha = value;
                    for (int i = 0; i < m_colour.Length; ++i)
                    {
                        m_fullColour[i] = m_colour[i];
                        m_fullColour[i].W *= m_alpha;
                    }
                }
            }
        }

        public IAnimation Animation
        {
            get
            {
                return m_animation;
            }
            set
            {
                m_animation = value;
            }
        }

        public float AnimTime
        {
            get
            {
                return m_animTime;
            }
            set
            {
                m_animTime = value;
            }
        }

        public ModelInstance(Model model, Matrix4 transform, float alpha = 1.0f)
        {
            m_model = model;
            m_baseTransform = transform;
            m_alpha = alpha;
            m_groupTransforms = new Matrix4[model.GroupCount];
            m_fullTransforms = new Matrix4[model.GroupCount];
            m_baseVisible = true;
            m_visibility = new bool[model.GroupCount];
            m_uvOffset = new Vector2[model.GroupCount];
            m_uvScale = new Vector2[model.GroupCount];
            m_colour = new Vector4[model.GroupCount];
            m_fullColour = new Vector4[model.GroupCount];
            for (int i = 0; i < m_groupTransforms.Length; ++i)
            {
                m_groupTransforms[i] = Matrix4.Identity;
                m_fullTransforms[i] = transform;
                m_visibility[i] = true;
                m_uvOffset[i] = Vector2.Zero;
                m_uvScale[i] = Vector2.One;
                m_colour[i] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                m_fullColour[i] = m_colour[i];
                m_fullColour[i].W *= m_alpha;
            }
            m_animation = null;
            m_animTime = 0.0f;
        }

        public bool SetGroupVisible(string groupName, bool visible)
        {
            int index = m_model.GetGroupIndex(groupName);
            if (index >= 0)
            {
                bool wasVisible = m_visibility[index];
                m_visibility[index] = visible;
                return wasVisible;
            }
            return false;
        }

        public void SetGroupTransform(string groupName, ref Matrix4 transform)
        {
            int index = m_model.GetGroupIndex(groupName);
            if (index >= 0)
            {
                m_groupTransforms[index] = transform;
                m_fullTransforms[index] = transform * m_baseTransform;
            }
        }

        public void SetGroupUVOffset(string groupName, Vector2 uvOffset)
        {
            int index = m_model.GetGroupIndex(groupName);
            if (index >= 0)
            {
                m_uvOffset[index] = uvOffset;
            }
        }

        public void SetGroupColour(string groupName, Vector4 colour)
        {
            int index = m_model.GetGroupIndex(groupName);
            if (index >= 0)
            {
                m_colour[index] = colour;
                m_fullColour[index] = colour;
                m_fullColour[index].W *= m_alpha;
            }
        }

        public void Animate()
        {
            if (m_animation != null)
            {
                float cameraFOV;
                for (int i = 0; i < m_model.GroupCount; ++i)
                {
                    var partName = m_model.GetGroupName(i);
                    m_animation.Animate(partName, m_animTime, out m_visibility[i], out m_groupTransforms[i], out m_uvOffset[i], out m_uvScale[i], out m_colour[i], out cameraFOV);
                    m_fullTransforms[i] = m_groupTransforms[i] * m_baseTransform;
                    m_fullColour[i] = m_colour[i];
                    m_fullColour[i].W *= m_alpha;
                }
            }
        }

        public void Draw(ModelEffectInstance effect)
        {
            if (m_baseVisible)
            {
                m_model.Draw(effect, m_fullTransforms, m_visibility, m_uvOffset, m_uvScale, m_fullColour);
            }
        }

        public void DrawShadows(ShadowEffectInstance effect)
        {
            if (m_baseVisible)
            {
                m_model.DrawShadows(effect, m_fullTransforms, m_visibility);
            }
        }

        public void DrawSingleGroupWithCustomEmissiveTexture(ModelEffectInstance effect, string groupName, ITexture texture)
        {
            var groupIndex = m_model.GetGroupIndex(groupName);
            if (m_baseVisible && groupIndex >= 0 && groupIndex < m_model.GroupCount)
            {
                m_model.DrawGroup(effect, groupIndex, m_fullTransforms[groupIndex], m_uvOffset[groupIndex], m_uvScale[groupIndex], m_fullColour[groupIndex], texture);
            }
        }
    }
}

