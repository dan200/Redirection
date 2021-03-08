using Dan200.Core.Animation;
using Dan200.Core.Render;

namespace Dan200.Game.Level
{
    public class AnimatedTile : TileEntity
    {
        private ModelInstance m_model;
        private ModelInstance m_poweredModel;
        private ParticleEmitter m_emitter;
        private bool m_powered;

        public AnimatedTile(Tile tile, TileCoordinates location) : base(tile, location)
        {
        }

        protected override void OnInit()
        {
            var behaviour = (AnimatedTileBehaviour)Tile.Behaviour;
            var direction = behaviour.GetModelDirection(Level, Location);
            var transform = Tile.BuildTransform(Location, direction);
            m_powered = !Level.InEditor && Tile.IsPowered(Level, Location);

            var model = behaviour.GetModel(Level, Location);
            if (model != null)
            {
                m_model = new ModelInstance(model, transform);
                if (behaviour.Animation != null)
                {
                    m_model.Animation = LuaAnimation.Get(behaviour.Animation);
                    m_model.AnimTime = Level.TimeMachine.RealTime;
                }
            }
            var poweredModel = behaviour.GetPoweredModel(Level, Location);
            if (poweredModel != null && poweredModel != model)
            {
                m_poweredModel = new ModelInstance(poweredModel, transform);
                if (behaviour.PoweredAnimation != null)
                {
                    m_poweredModel.Animation = LuaAnimation.Get(behaviour.PoweredAnimation);
                    m_poweredModel.AnimTime = Level.TimeMachine.RealTime;
                }
            }
            if (behaviour.PFX != null)
            {
                m_emitter = Level.Particles.Create(ParticleStyle.Get(m_powered ? behaviour.PoweredPFX : behaviour.PFX), false, true);
                m_emitter.Transform = transform;
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
            if (m_poweredModel != null)
            {
                m_poweredModel.Transform = transform;
            }
            if (m_emitter != null)
            {
                m_emitter.Transform = transform;
            }
        }

        protected override void OnShutdown()
        {
            if (m_emitter != null)
            {
                m_emitter.Stop(Level.TimeMachine.RealTime);
                m_emitter = null;
            }
        }

        protected override void OnUpdate(float dt)
        {
        }

        public override void Update()
        {
            base.Update();
            if (Level != null)
            {
                var powered = !Level.InEditor && Tile.IsPowered(Level, Location);
                if (powered != m_powered)
                {
                    var behaviour = (AnimatedTileBehaviour)Tile.Behaviour;
                    m_powered = powered;
                    if (m_emitter != null && behaviour.PoweredPFX != behaviour.PFX)
                    {
                        m_emitter.Stop(Level.TimeMachine.RealTime);
                        m_emitter = Level.Particles.Create(ParticleStyle.Get(m_powered ? behaviour.PoweredPFX : behaviour.PFX), false, true);

                        var direction = Tile.GetDirection(Level, Location);
                        m_emitter.Transform = Tile.BuildTransform(Location, direction);
                    }
                }
                if (m_powered && m_poweredModel != null)
                {
                    m_poweredModel.AnimTime = Level.TimeMachine.RealTime;
                    m_poweredModel.Animate();
                }
                else if (m_model != null)
                {
                    m_model.AnimTime = Level.TimeMachine.RealTime;
                    m_model.Animate();
                }
            }
        }

        protected override void OnDraw(ModelEffectInstance modelEffect, RenderPass pass)
        {
            if (!Tile.IsHidden(Level, Location))
            {
                if (m_powered && m_poweredModel != null)
                {
                    m_poweredModel.Draw(modelEffect);
                }
                else if (m_model != null)
                {
                    m_model.Draw(modelEffect);
                }
            }
        }

        protected override void OnDrawShadows(ShadowEffectInstance shadowEffect)
        {
            if (!Tile.IsHidden(Level, Location) && Tile.CastShadows)
            {
                if (m_powered && m_poweredModel != null)
                {
                    m_poweredModel.DrawShadows(shadowEffect);
                }
                else if (m_model != null)
                {
                    m_model.DrawShadows(shadowEffect);
                }
            }
        }
    }
}

