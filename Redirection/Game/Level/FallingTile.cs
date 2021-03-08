using Dan200.Core.Render;
using Dan200.Core.Utils;
using OpenTK;
using System;

namespace Dan200.Game.Level
{
    public class FallingTile : TileEntity
    {
        public const float SPEED = 1.0f / Robot.Robot.STEP_TIME;

        private ModelInstance m_modelInstance;
        private FlatDirection m_direction;

        private enum FallState
        {
            Falling,
            Blocked,
            Landed
        }

        private class FallingTileState : EntityState
        {
            public readonly FallState State;
            public readonly float Distance;
            public readonly float Time;

            public FallingTileState(FallState state, float distance, float time)
            {
                State = state;
                Distance = distance;
                Time = time;
            }
        }

        private new FallingTileState CurrentState
        {
            get
            {
                return (FallingTileState)base.CurrentState;
            }
        }

        public override Matrix4 Transform
        {
            get
            {
                float distance = CurrentState.Distance;
                if (CurrentState.State == FallState.Falling)
                {
                    float duration = Level.TimeMachine.Time - CurrentState.Time;
                    distance += SPEED * duration;
                }

                var baseTransform = base.Transform;
                baseTransform.Row3.Y -= distance;
                return baseTransform;
            }
        }

        public FallingTile(Tile tile, TileCoordinates coordinates, FlatDirection direction) : base(tile, coordinates)
        {
            m_direction = direction;
        }

        protected override void OnInit()
        {
            var behaviour = (FallingTileBehaviour)Tile.Behaviour;
            PushState(new FallingTileState(FallState.Falling, 0.0f, Level.TimeMachine.Time));
            var model = behaviour.GetModel(Level, Location);
            if (model != null)
            {
                m_modelInstance = new ModelInstance(model, CalculateTransform());
            }
            Reoccupy();
        }

        protected override void OnLocationChanged()
        {
            if (m_modelInstance != null)
            {
                m_modelInstance.Transform = CalculateTransform();
            }
        }

        protected override void OnShutdown()
        {
            Unoccupy();
        }

        protected override void OnUpdate(float dt)
        {
            if (CurrentState.State != FallState.Landed)
            {
                // Work out where we are
                float newDistance = CurrentState.Distance;
                if (CurrentState.State == FallState.Falling)
                {
                    float duration = Level.TimeMachine.Time - CurrentState.Time;
                    newDistance += SPEED * duration;
                }

                // Work out if we need to land
                var newCoords = new TileCoordinates(Location.X, Location.Y - (int)(newDistance / 0.5f), Location.Z);
                if (!Level.Tiles[newCoords.Below()].CanEnterOnTop(Level, newCoords.Below()))
                {
                    // Stop
                    newDistance = (Location.Y - newCoords.Y) * 0.5f;

                    if (Level.Tiles[newCoords.Below()].IsSolidOnSide(Level, newCoords.Below(), Direction.Up))
                    {
                        // Land permanently
                        Level.Tiles[newCoords].Clear(Level, newCoords);
                        Level.Tiles.SetTile(newCoords, Tile, m_direction);
                        PushState(new FallingTileState(FallState.Landed, newDistance, Level.TimeMachine.Time));
                    }
                    else if (CurrentState.State != FallState.Blocked)
                    {
                        // Land temporarily
                        PushState(new FallingTileState(FallState.Blocked, newDistance, Level.TimeMachine.Time));
                    }
                }
                else if (CurrentState.State != FallState.Falling)
                {
                    // Resume falling
                    PushState(new FallingTileState(FallState.Falling, CurrentState.Distance, Level.TimeMachine.Time));
                }
            }
        }

        public override void Update()
        {
            Unoccupy();
            base.Update();
            if (CurrentState != null)
            {
                if (m_modelInstance != null)
                {
                    m_modelInstance.Transform = CalculateTransform();
                }
                Reoccupy();
            }
        }

        protected override void OnDraw(ModelEffectInstance modelEffect, RenderPass pass)
        {
            if (CurrentState.State != FallState.Landed)
            {
                if (m_modelInstance != null)
                {
                    m_modelInstance.Draw(modelEffect);
                }
            }
        }

        protected override void OnDrawShadows(ShadowEffectInstance shadowEffect)
        {
            if (CurrentState.State != FallState.Landed && Tile.CastShadows)
            {
                if (m_modelInstance != null)
                {
                    m_modelInstance.DrawShadows(shadowEffect);
                }
            }
        }

        public override bool Raycast(Ray ray, out Direction o_side, out float o_distance)
        {
            if (CurrentState.State != FallState.Landed)
            {
                float bottomY = 0.5f * Location.Y - GetDistance();
                float topY = bottomY + 0.5f * Tile.Height;
                return ray.TestVersusBox(
                    new Vector3(Location.X, bottomY, Location.Z),
                    new Vector3(Location.X + 1.0f, topY, Location.Z + 1.0f),
                    out o_side,
                    out o_distance
                );
            }
            o_side = default(Direction);
            o_distance = default(float);
            return false;
        }

        public override bool CanPlaceOnTop(out TileCoordinates o_coordinates)
        {
            if (Tile.AllowPlacement && CurrentState.State == FallState.Blocked)
            {
                float distance = GetDistance();
                o_coordinates = new TileCoordinates(Location.X, Location.Y - (int)(distance / 0.5f) + Tile.Height, Location.Z);
                return true;
            }
            o_coordinates = default(TileCoordinates);
            return false;
        }

        private Matrix4 CalculateTransform()
        {
            // Work out where we are
            float fallDistance = GetDistance();
            if (m_direction != FlatDirection.North)
            {
                return
                    Matrix4.CreateTranslation(-0.5f, 0.0f, -0.5f) *
                    Matrix4.CreateRotationY(m_direction.ToYaw()) *
                    Matrix4.CreateTranslation((float)Location.X + 0.5f, (float)Location.Y * 0.5f - fallDistance, (float)Location.Z + 0.5f);
            }
            else
            {
                return
                    Matrix4.CreateTranslation((float)Location.X, (float)Location.Y * 0.5f - fallDistance, (float)Location.Z);
            }
        }

        private float GetDistance()
        {
            float distance = CurrentState.Distance;
            if (CurrentState.State == FallState.Falling)
            {
                float duration = CurrentTime - CurrentState.Time;
                distance += SPEED * duration;
            }
            return distance;
        }

        private void Unoccupy()
        {
            if (CurrentState != null)
            {
                float distance = GetDistance();
                int bottomTile = Location.Y + (int)Math.Floor((-distance + 0.01f) / 0.5f);
                int topTile = Location.Y + (int)Math.Floor((-distance + Tile.Height * 0.5f - 0.01f) / 0.5f);
                for (int y = bottomTile; y <= topTile; ++y)
                {
                    var coords = new TileCoordinates(Location.X, y, Location.Z);
                    if (Level.Tiles[coords].GetOccupant(Level, coords) == this)
                    {
                        Level.Tiles[coords].SetOccupant(Level, coords, null);
                    }
                }
            }
        }

        private void Reoccupy()
        {
            if (CurrentState != null && CurrentState.State != FallState.Landed)
            {
                float distance = GetDistance();
                int bottomTile = Location.Y + (int)Math.Floor((-distance + 0.01f) / 0.5f);
                int topTile = Location.Y + (int)Math.Floor((-distance + Tile.Height * 0.5f - 0.01f) / 0.5f);
                for (int y = bottomTile; y <= topTile; ++y)
                {
                    var coords = new TileCoordinates(Location.X, y, Location.Z);
                    Level.Tiles[coords].SetOccupant(Level, coords, this);
                }
            }
        }
    }
}

