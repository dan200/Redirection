using Dan200.Core.Render;
using Dan200.Core.Utils;
using OpenTK;
using System;

namespace Dan200.Game.Level
{
    public class Elevator : TileEntity
    {
        private ModelInstance m_modelInstance;
        private FlatDirection m_direction;

        private enum ElevationState
        {
            Rising,
            RisingBlocked,
            Risen,
            Falling,
            FallingBlocked,
            Fallen
        }

        private class ElevatorState : EntityState
        {
            public readonly ElevationState State;
            public readonly float Height;
            public readonly float Time;

            public ElevatorState(ElevationState state, float height, float time)
            {
                State = state;
                Height = height;
                Time = time;
            }
        }

        private new ElevatorState CurrentState
        {
            get
            {
                return (ElevatorState)base.CurrentState;
            }
        }

        public override Matrix4 Transform
        {
            get
            {
                float height = CurrentState.Height;
                var behaviour = (ElevatorTileBehaviour)Tile.Behaviour;
                if (CurrentState.State == ElevationState.Falling)
                {
                    float duration = CurrentTime - CurrentState.Time;
                    height -= behaviour.Speed * duration;
                }
                else if (CurrentState.State == ElevationState.Rising)
                {
                    float duration = CurrentTime - CurrentState.Time;
                    height += behaviour.Speed * duration;
                }

                var baseTransform = base.Transform;
                baseTransform.Row3.Y += height;
                return baseTransform;
            }
        }

        public Elevator(Tile tile, TileCoordinates coordinates, FlatDirection direction) : base(tile, coordinates)
        {
            m_direction = direction;
        }

        protected override void OnInit()
        {
            var behaviour = (ElevatorTileBehaviour)Tile.Behaviour;
            var initialHeight = 0.0f;
            var initialState = behaviour.Direction == ElevatorDirection.Up ? ElevationState.Fallen : ElevationState.Risen;
            PushState(new ElevatorState(initialState, initialHeight, CurrentTime));
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

        public float GetCurrentHeight()
        {
            var behaviour = (ElevatorTileBehaviour)Tile.Behaviour;
            float height = CurrentState.Height;
            if (CurrentState.State == ElevationState.Falling)
            {
                float duration = CurrentTime - CurrentState.Time;
                height -= behaviour.Speed * duration;
            }
            else if (CurrentState.State == ElevationState.Rising)
            {
                float duration = CurrentTime - CurrentState.Time;
                height += behaviour.Speed * duration;
            }
            return height;
        }

        protected override void OnUpdate(float dt)
        {
            // Change direction if necessary
            float height = GetCurrentHeight();
            var behaviour = (ElevatorTileBehaviour)Tile.Behaviour;
            var triggered = false;
            if (!Level.InEditor)
            {
                var powered = Tile.IsPowered(Level, Location);
                triggered = (behaviour.Trigger == ElevatorTrigger.Powered ? powered : !powered);
            }
            var intendedDirection = triggered ? behaviour.Direction : behaviour.Direction.Opposite();
            if ((CurrentState.State == ElevationState.Fallen ||
                 CurrentState.State == ElevationState.Falling ||
                 CurrentState.State == ElevationState.FallingBlocked) &&
                intendedDirection == ElevatorDirection.Up)
            {
                PushState(new ElevatorState(ElevationState.Rising, height, CurrentTime));
                if (behaviour.RiseSoundPath != null)
                {
                    PlaySound(behaviour.RiseSoundPath);
                }
            }
            else
            if ((CurrentState.State == ElevationState.Risen ||
                 CurrentState.State == ElevationState.Rising ||
                 CurrentState.State == ElevationState.RisingBlocked) &&
                 intendedDirection == ElevatorDirection.Down)
            {
                PushState(new ElevatorState(ElevationState.Falling, height, CurrentTime));
                if (behaviour.FallSoundPath != null)
                {
                    PlaySound(behaviour.FallSoundPath);
                }
            }

            // Keep moving
            if (CurrentState.State == ElevationState.Falling)
            {
                float minHeight = (behaviour.Direction == ElevatorDirection.Up) ? 0.0f : behaviour.Distance * -0.5f;
                if (height <= minHeight)
                {
                    PushState(new ElevatorState(ElevationState.Fallen, minHeight, CurrentTime));
                }
                else
                {
                    var belowTileCoords = new TileCoordinates(Location.X, Location.Y + (int)Math.Floor(height / 0.5f), Location.Z);
                    if (!Level.Tiles[belowTileCoords].CanEnterOnTop(Level, belowTileCoords))
                    {
                        var blockageHeight = (belowTileCoords.Y + 1 - Location.Y) * 0.5f;
                        PushState(new ElevatorState(ElevationState.FallingBlocked, blockageHeight, CurrentTime));
                    }
                }
            }
            else if (CurrentState.State == ElevationState.FallingBlocked)
            {
                var belowTileCoords = new TileCoordinates(Location.X, Location.Y + (int)Math.Floor(height / 0.5f), Location.Z);
                if (Level.Tiles[belowTileCoords].CanEnterOnTop(Level, belowTileCoords))
                {
                    PushState(new ElevatorState(ElevationState.Falling, height, CurrentTime));
                }
            }
            else if (CurrentState.State == ElevationState.Rising)
            {
                float maxHeight = (behaviour.Direction == ElevatorDirection.Up) ? behaviour.Distance * 0.5f : 0.0f;
                if (height >= maxHeight)
                {
                    PushState(new ElevatorState(ElevationState.Risen, maxHeight, CurrentTime));
                }
                else
                {
                    var aboveTileCoords = new TileCoordinates(Location.X, Location.Y + (int)Math.Floor(height / 0.5f) + Tile.Height, Location.Z);
                    if (!Level.Tiles[aboveTileCoords].CanEnterOnBottom(Level, aboveTileCoords))
                    {
                        var blockageHeight = (aboveTileCoords.Y - Tile.Height - Location.Y) * 0.5f;
                        PushState(new ElevatorState(ElevationState.RisingBlocked, blockageHeight, CurrentTime));
                    }
                }
            }
            else if (CurrentState.State == ElevationState.RisingBlocked)
            {
                var aboveTileCoords = new TileCoordinates(Location.X, Location.Y + (int)Math.Floor(height / 0.5f) + Tile.Height, Location.Z);
                if (Level.Tiles[aboveTileCoords].CanEnterOnBottom(Level, aboveTileCoords))
                {
                    PushState(new ElevatorState(ElevationState.Rising, height, CurrentTime));
                }
            }
        }

        public override void Update()
        {
            Unoccupy();
            base.Update();
            if (m_modelInstance != null)
            {
                m_modelInstance.Transform = CalculateTransform();
            }
            Reoccupy();
        }

        protected override void OnDraw(ModelEffectInstance modelEffect, RenderPass pass)
        {
            if (!Level.Tiles[Location].IsHidden(Level, Location))
            {
                if (m_modelInstance != null)
                {
                    m_modelInstance.Draw(modelEffect);
                }
            }
        }

        protected override void OnDrawShadows(ShadowEffectInstance shadowEffect)
        {
            if (!Level.Tiles[Location].IsHidden(Level, Location) && Tile.CastShadows)
            {
                if (m_modelInstance != null)
                {
                    m_modelInstance.DrawShadows(shadowEffect);
                }
            }
        }

        public override bool Raycast(Ray ray, out Direction o_side, out float o_distance)
        {
            float height = GetCurrentHeight();
            float bottomY = 0.5f * Location.Y + height;
            float topY = bottomY + 0.5f * Tile.Height;
            return ray.TestVersusBox(
                new Vector3(Location.X, bottomY, Location.Z),
                new Vector3(Location.X + 1.0f, topY, Location.Z + 1.0f),
                out o_side,
                out o_distance
            );
        }

        public override bool CanPlaceOnTop(out TileCoordinates o_coordinates)
        {
            if (Tile.AllowPlacement && CurrentState.State != ElevationState.Falling && CurrentState.State != ElevationState.Rising)
            {
                float height = GetCurrentHeight();
                o_coordinates = new TileCoordinates(Location.X, Location.Y + (int)Math.Floor(height / 0.5f) + Tile.Height, Location.Z);
                return true;
            }
            o_coordinates = default(TileCoordinates);
            return false;
        }

        private Matrix4 CalculateTransform()
        {
            // Work out where we are
            float height = GetCurrentHeight();
            if (m_direction != FlatDirection.North)
            {
                return
                    Matrix4.CreateTranslation(-0.5f, 0.0f, -0.5f) *
                    Matrix4.CreateRotationY(m_direction.ToYaw()) *
                    Matrix4.CreateTranslation((float)Location.X + 0.5f, (float)Location.Y * 0.5f + height, (float)Location.Z + 0.5f);
            }
            else
            {
                return
                    Matrix4.CreateTranslation((float)Location.X, (float)Location.Y * 0.5f + height, (float)Location.Z);
            }
        }

        private void Unoccupy()
        {
            if (CurrentState != null)
            {
                float height = GetCurrentHeight();
                int bottomTile = Location.Y + (int)Math.Floor((height + 0.01f) / 0.5f);
                int topTile = Location.Y + (int)Math.Floor((height + Tile.Height * 0.5f - 0.01f) / 0.5f);
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
            float height = GetCurrentHeight();
            int bottomTile = Location.Y + (int)Math.Floor((height + 0.01f) / 0.5f);
            int topTile = Location.Y + (int)Math.Floor((height + Tile.Height * 0.5f - 0.01f) / 0.5f);
            for (int y = bottomTile; y <= topTile; ++y)
            {
                var coords = new TileCoordinates(Location.X, y, Location.Z);
                Level.Tiles[coords].SetOccupant(Level, coords, this);
            }
        }
    }
}

