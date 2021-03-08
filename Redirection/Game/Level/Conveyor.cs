using Dan200.Core.Animation;
using Dan200.Core.Render;
using Dan200.Game.Game;

namespace Dan200.Game.Level
{
    public class Conveyor : TileEntity
    {
        private class ConveyorState : EntityState
        {
            public readonly ConveyorMode Mode;
            public readonly float Distance;
            public readonly float TimeStamp;

            public ConveyorState(ConveyorMode mode, float distance, float timeStamp)
            {
                Mode = mode;
                Distance = distance;
                TimeStamp = timeStamp;
            }
        }

        private ModelInstance m_model;

        private new ConveyorState CurrentState
        {
            get
            {
                return (ConveyorState)base.CurrentState;
            }
        }

        public ConveyorMode CurrentMode
        {
            get
            {
                return CurrentState.Mode;
            }
        }

        public Conveyor(Tile tile, TileCoordinates position) : base(tile, position)
        {
        }

        protected override void OnInit()
        {
            var direction = Tile.GetDirection(Level, Location);
            var model = Tile.Behaviour.GetModel(Level, Location);
            if (model != null)
            {
                var behaviour = (ConveyorTileBehaviour)Tile.Behaviour;
                m_model = new ModelInstance(model, Tile.BuildTransform(Location, direction));
                if (behaviour.Animation != null)
                {
                    m_model.Animation = LuaAnimation.Get(behaviour.Animation);
                    m_model.AnimTime = 0.0f;
                }
            }
            PushState(new ConveyorState(ConveyorMode.Stopped, 0.0f, CurrentTime));
        }

        protected override void OnLocationChanged()
        {
            if (m_model != null)
            {
                var direction = Tile.Behaviour.GetModelDirection(Level, Location);
                m_model.Transform = Tile.BuildTransform(Location, direction);
            }
        }

        protected override void OnShutdown()
        {
        }

        private float GetCurrentDistance()
        {
            var currentState = CurrentState;
            var timer = Level.TimeMachine.Time - currentState.TimeStamp;
            switch (currentState.Mode)
            {
                case ConveyorMode.Forwards:
                    {
                        return currentState.Distance + timer / Robot.Robot.STEP_TIME;
                    }
                case ConveyorMode.Reverse:
                    {
                        return currentState.Distance - timer / Robot.Robot.STEP_TIME;
                    }
                case ConveyorMode.Stopped:
                default:
                    {
                        return currentState.Distance;
                    }
            }
        }

        protected override void OnUpdate(float dt)
        {
            var mode = ConveyorMode.Stopped;
            var time = Level.TimeMachine.Time;
            if (time > InGameState.INTRO_DURATION)
            {
                var behaviour = (ConveyorTileBehaviour)Tile.Behaviour;
                var powered = Tile.IsPowered(Level, Location);
                mode = powered ? behaviour.PoweredMode : behaviour.UnpoweredMode;
            }
            if (mode != CurrentState.Mode)
            {
                PushState(new ConveyorState(mode, GetCurrentDistance(), time));
            }
        }

        public override void Update()
        {
            base.Update();
            if (m_model != null)
            {
                m_model.AnimTime = GetCurrentDistance();
                m_model.Animate();
            }
        }

        protected override void OnDraw(ModelEffectInstance modelEffect, RenderPass pass)
        {
            if (m_model != null && !Tile.IsHidden(Level, Location))
            {
                m_model.Draw(modelEffect);
            }
        }

        protected override void OnDrawShadows(ShadowEffectInstance shadowEffect)
        {
            if (m_model != null && !Tile.IsHidden(Level, Location) && Tile.CastShadows)
            {
                m_model.DrawShadows(shadowEffect);
            }
        }
    }
}

