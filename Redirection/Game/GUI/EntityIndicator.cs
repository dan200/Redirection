using Dan200.Core.GUI;
using Dan200.Core.Render;
using Dan200.Game.Level;
using OpenTK;
using System;

namespace Dan200.Game.GUI
{
    public class EntityIndicator<TEntity> : Element where TEntity : Entity
    {
        private TEntity m_entity;
        private Camera m_camera;

        public TEntity Entity
        {
            get
            {
                return m_entity;
            }
        }

        public EntityIndicator(TEntity entity, Camera camera)
        {
            m_entity = entity;
            m_camera = camera;
        }

        protected override void OnInit()
        {
            RequestRebuild();
        }

        protected override void OnUpdate(float dt)
        {
            RequestRebuild();
        }

        protected override void OnDraw()
        {
        }

        protected override void OnRebuild()
        {
        }

        protected Vector2 CalculatePosition(Vector3 localSpaceOffset)
        {
            // Get position of entity
            Vector3 posLS = Vector3.TransformPosition(localSpaceOffset, m_entity.Transform);
            Vector3 posWS = Vector3.TransformPosition(posLS, m_entity.Level.Transform);
            Vector3 posCS = Vector3.TransformPosition(posWS, m_camera.Transform);

            // Transform to screen space
            float screenAspect = Screen.Width / Screen.Height;
            Vector2 posSS = new Vector2(
                (0.5f * Screen.Width) + ((float)Math.Atan2(posCS.X, -posCS.Z) / (m_camera.FOV * 0.5f * screenAspect)) * (0.5f * Screen.Width),
                (0.5f * Screen.Height) + ((float)Math.Atan2(-posCS.Y, -posCS.Z) / (m_camera.FOV * 0.5f)) * (0.5f * Screen.Height)
            );
            return new Vector2(posSS.X, posSS.Y);
        }
    }
}
