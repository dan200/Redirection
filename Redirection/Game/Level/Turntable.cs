using Dan200.Core.Animation;
using Dan200.Core.Render;
using Dan200.Game.Robot;

namespace Dan200.Game.Level
{
    public class Turntable : TileEntity
    {
        private enum TurntableMode
        {
            Wait,
            Turn
        }

        private class TurntableState : EntityState
        {
            public readonly TurntableMode Mode;
            public readonly FlatDirection Direction;
            public readonly float TimeStamp;

            public TurntableState(TurntableMode mode, FlatDirection direction, float timeStamp)
            {
                Mode = mode;
                Direction = direction;
                TimeStamp = timeStamp;
            }
        }

        private ModelInstance m_model;

        private new TurntableState CurrentState
        {
            get
            {
                return (TurntableState)base.CurrentState;
            }
        }

        public Turntable(Tile tile, TileCoordinates location) : base(tile, location)
        {
        }

        protected override void OnInit()
        {
            var direction = Tile.GetDirection(Level, Location);
            var behaviour = (TurntableTileBehaviour)Tile.GetBehaviour(Level, Location);
            PushState(new TurntableState(TurntableMode.Wait, direction, CurrentTime));

            var model = behaviour.GetModel(Level, Location);
            if (model != null)
            {
                m_model = new ModelInstance(model, Tile.BuildTransform(Location, direction));
                if (behaviour.Animation != null)
                {
                    m_model.Animation = LuaAnimation.Get(behaviour.Animation);
                    m_model.AnimTime = 0.0f;
                }
            }
            UpdateModel();
        }

        protected override void OnLocationChanged()
        {
            var direction = Tile.GetDirection(Level, Location);
            m_model.Transform = Tile.BuildTransform(Location, direction);
        }

        public void Turn()
        {
            if (CurrentState.Mode == TurntableMode.Wait)
            {
                var behaviour = (TurntableTileBehaviour)Tile.Behaviour;
                PushState(new TurntableState(TurntableMode.Turn, CurrentState.Direction, CurrentTime));
                if (behaviour.Sound != null)
                {
                    PlaySound(behaviour.Sound);
                }
            }
        }

        protected override void OnShutdown()
        {
        }

        public override void Update()
        {
            base.Update();
            UpdateModel();
        }

        private void UpdateModel()
        {
            if (m_model != null)
            {
                m_model.Transform = Tile.BuildTransform(Location, CurrentState.Direction);

                var timer = Level.TimeMachine.Time - CurrentState.TimeStamp;
                float progress = (CurrentState.Mode == TurntableMode.Turn) ?
                    timer / Robot.Robot.STEP_TIME :
                    0.0f;
                m_model.AnimTime = progress;
                m_model.Animate();
            }
        }

        protected override void OnUpdate(float dt)
        {
            if (CurrentState.Mode == TurntableMode.Turn)
            {
                var timer = Level.TimeMachine.Time - CurrentState.TimeStamp;
                float progress = timer / Robot.Robot.STEP_TIME;
                if (progress >= 1.0f)
                {
                    var dir = CurrentState.Direction;
                    if (Tile.IsTurntable(Level, Location))
                    {
                        var turnDirection = ((TurntableTileBehaviour)Tile.GetBehaviour(Level, Location)).TurnDirection;
                        if (turnDirection == TurnDirection.Left)
                        {
                            dir = dir.RotateLeft();
                        }
                        else
                        {
                            dir = dir.RotateRight();
                        }
                    }
                    PushState(new TurntableState(TurntableMode.Wait, dir, CurrentTime));
                }
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

