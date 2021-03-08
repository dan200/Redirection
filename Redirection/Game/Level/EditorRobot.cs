
using Dan200.Core.Render;
using OpenTK;

namespace Dan200.Game.Level
{
    public class EditorRobot : TileEntity
    {
        private ModelInstance m_modelInstance;

        public EditorRobot(Tile spawnTile, TileCoordinates location) : base(spawnTile, location)
        {
            m_modelInstance = null;
        }

        protected override void OnInit()
        {
            var behaviour = ((SpawnTileBehaviour)Tile.Behaviour);
            m_modelInstance = new ModelInstance(
                behaviour.RobotModel,
                BuildTransform()
            );

            var animSet = behaviour.RobotAnimSet;
            if (animSet != null)
            {
                m_modelInstance.Animation = animSet.GetAnim("idle");
                m_modelInstance.AnimTime = 0.0f;
                m_modelInstance.Animate();
            }
        }

        protected override void OnLocationChanged()
        {
            m_modelInstance.Transform = BuildTransform();
        }

        protected override void OnShutdown()
        {
        }

        public override void Update()
        {
            base.Update();
            if (m_modelInstance.Animation != null)
            {
                m_modelInstance.AnimTime = CurrentTime - SpawnTime;
                m_modelInstance.Animate();
            }
        }

        protected override void OnDraw(ModelEffectInstance modelEffect, RenderPass pass)
        {
            if (Level.InEditor && Tile.IsHidden(Level, Location))
            {
                return;
            }
            m_modelInstance.Draw(modelEffect);
        }

        protected override void OnDrawShadows(ShadowEffectInstance shadowEffect)
        {
            if (Level.InEditor && Tile.IsHidden(Level, Location))
            {
                return;
            }
            if (Tile.CastShadows)
            {
                m_modelInstance.DrawShadows(shadowEffect);
            }
        }

        private Matrix4 BuildTransform()
        {
            var direction = Tile.GetDirection(Level, Location);
            return
                Matrix4.CreateRotationY(direction.ToYaw()) *
                Matrix4.CreateTranslation(
                    Location.X + 0.5f,
                    Location.Y * 0.5f,
                    Location.Z + 0.5f
                );
        }
    }
}
