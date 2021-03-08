using Dan200.Core.Util;
using OpenTK;

namespace Dan200.Core.Render
{
    public class WorldEffectInstance : EffectInstance
    {
        private Matrix4 m_worldMatrix;
        private Matrix4 m_modelMatrix;
        private Matrix4 m_viewMatrix;
        private Matrix4 m_projectionMatrix;

        private Matrix4 m_worldModelMatrix;
        private Matrix4 m_viewProjectionMatrix;
        private Matrix4 m_normalMatrix;
        private Vector3 m_cameraPosition;

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

        public Vector3 CameraPosition
        {
            get
            {
                return m_cameraPosition;
            }
        }

        public WorldEffectInstance(string effectPath) : base(effectPath)
        {
            // Values
            m_modelMatrix = Matrix4.Identity;
            m_viewMatrix = Matrix4.Identity;
            m_projectionMatrix = Matrix4.Identity;
            UpdateMatrices();
        }

        public override void Bind()
        {
            base.Bind();

            Set("modelMatrix", ref m_worldModelMatrix);
            Set("viewProjectionMatrix", ref m_viewProjectionMatrix);
            Set("normalMatrix", ref m_normalMatrix);
            Set("cameraPosition", m_cameraPosition);
        }

        private void UpdateMatrices()
        {
            m_worldModelMatrix = m_modelMatrix * m_worldMatrix;
            m_viewProjectionMatrix = m_viewMatrix * m_projectionMatrix;
            m_normalMatrix = m_modelMatrix;
            m_normalMatrix.Transpose();

            var cameraTransInv = MathUtils.FastInverted(m_viewMatrix);
            m_cameraPosition = Vector3.TransformPosition(Vector3.Zero, cameraTransInv);
        }
    }
}
