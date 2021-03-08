
using Dan200.Core.Animation;
using Dan200.Core.Render;
using OpenTK;

namespace Dan200.Game.Level
{
    public class SpawnMarkerPreview : TileEntity
    {
        private ModelInstance m_model;
        private bool m_visible;

        public bool Visible
        {
            get
            {
                return m_visible;
            }
            set
            {
                m_visible = value;
            }
        }

        public SpawnMarkerPreview(Tile tile, TileCoordinates location) : base(tile, location)
        {
            m_model = null;
            m_visible = true;
        }

        protected override void OnInit()
        {
            m_model = new ModelInstance(
                Model.Get("models/entities/spawn_marker.obj"),
                BuildTransform()
            );
            m_model.Animation = LuaAnimation.Get(
                "animation/entities/spawn_marker/preview.anim.lua"
            );
            m_model.AnimTime = 0.0f;
        }

        protected override void OnLocationChanged()
        {
            m_model.Transform = BuildTransform();
        }

        protected override void OnShutdown()
        {
        }

        public override void Update()
        {
            base.Update();
            m_model.AnimTime = Level.TimeMachine.RealTime;
            m_model.Animate();
        }

        public override bool NeedsRenderPass(RenderPass pass)
        {
            return pass == RenderPass.Opaque;
        }

        protected override void OnDraw(ModelEffectInstance modelEffect, RenderPass pass)
        {
            if (Level.InEditor && Tile.IsHidden(Level, Location))
            {
                return;
            }

            if (Visible)
            {
                m_model.Draw(modelEffect);
            }
        }

        protected override void OnDrawShadows(ShadowEffectInstance shadowEffect)
        {
            if (Level.InEditor && Tile.IsHidden(Level, Location))
            {
                return;
            }

            if (Visible)
            {
                m_model.DrawShadows(shadowEffect);
            }
        }

        private Matrix4 BuildTransform()
        {
            return Matrix4.CreateTranslation(
                Location.X + 0.5f,
                Location.Y * 0.5f,
                Location.Z + 0.5f
            );
        }
    }
}
