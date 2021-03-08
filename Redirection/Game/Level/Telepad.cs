using Dan200.Core.Animation;
using Dan200.Core.Render;

namespace Dan200.Game.Level
{
    public class Telepad : TileEntity
    {
        private ModelInstance m_model;
        private ParticleEmitter m_emitter;

        public Telepad(Tile tile, TileCoordinates position) : base(tile, position)
        {
        }

        protected override void OnInit()
        {
            var direction = Tile.GetDirection(Level, Location);
            var behaviour = (TelepadTileBehaviour)Tile.Behaviour;
            var model = behaviour.GetModel(Level, Location);
            var transform = Tile.BuildTransform(Location, direction);
            if (model != null)
            {
                m_model = new ModelInstance(model, transform);
            }
            Level.Telepads.AddTelepad(behaviour.Colour, Location);
            if (behaviour.Animation != null)
            {
                if (m_model != null)
                {
                    m_model.Animation = LuaAnimation.Get(behaviour.Animation);
                    m_model.AnimTime = Level.TimeMachine.RealTime;
                }
            }
            if (behaviour.PFX != null)
            {
                if (m_model != null)
                {
                    m_emitter = Level.Particles.Create(ParticleStyle.Get(behaviour.PFX), false, true);
                    m_emitter.Transform = transform;
                }
            }
        }

        protected override void OnLocationChanged()
        {
            var direction = Tile.GetDirection(Level, Location);
            var transform = Tile.BuildTransform(Location, direction);
            if (m_model != null)
            {
                m_model.Transform = transform;
            }
            if (m_emitter != null)
            {
                m_emitter.Transform = m_model.Transform;
            }
        }

        protected override void OnShutdown()
        {
            var behaviour = (TelepadTileBehaviour)Tile.Behaviour;
            Level.Telepads.RemoveTelepad(behaviour.Colour, Location);
            if (m_emitter != null)
            {
                m_emitter.Stop(Level.TimeMachine.RealTime);
                m_emitter = null;
            }
        }

        public override void Update()
        {
            base.Update();
            m_model.AnimTime = Level.TimeMachine.RealTime;
            m_model.Animate();
        }

        protected override void OnDraw(ModelEffectInstance modelEffect, RenderPass pass)
        {
            if (!Tile.IsHidden(Level, Location))
            {
                if (m_model != null)
                {
                    m_model.Draw(modelEffect);
                }
            }
        }

        protected override void OnDrawShadows(ShadowEffectInstance shadowEffect)
        {
            if (!Tile.IsHidden(Level, Location) && Tile.CastShadows)
            {
                if (m_model != null)
                {
                    m_model.DrawShadows(shadowEffect);
                }
            }
        }
    }
}

