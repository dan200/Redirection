using Dan200.Core.Main;
using Dan200.Core.Render;
using Dan200.Core.Utils;
using Dan200.Game.Robot;
using OpenTK;
using System;

namespace Dan200.Game.Level
{
    public class SpawnMarker : TileEntity
    {
        public const float FADE_TIME = 0.5f * Robot.Robot.STEP_TIME;
        public const float SHELL_PUSH = 0.05f;

        public const int STEP_MULTIPLIER = 2;
        public const int MAX_DELAY = 99;

        private int m_spawnDelay;
        private ModelInstance m_ghostModel;
        private FlatDirection m_direction;
        private bool m_highlight;

        private enum ESpawnMarkerState
        {
            Initial,
            Countdown,
            Blocked,
            Spawned
        }

        private class SpawnMarkerState : EntityState
        {
            public readonly ESpawnMarkerState State;
            public readonly float TimeStamp;

            public readonly float GhostAlpha;
            public readonly float TargetGhostAlpha;
            public readonly float GhostAlphaTimeStamp;

            public SpawnMarkerState(ESpawnMarkerState state, float timeStamp, float ghostAlpha, float targetGhostAlpha, float ghostAlphaTimestamp)
            {
                State = state;
                TimeStamp = timeStamp;

                GhostAlpha = ghostAlpha;
                TargetGhostAlpha = targetGhostAlpha;
                GhostAlphaTimeStamp = ghostAlphaTimestamp;
            }
        }

        private new SpawnMarkerState CurrentState
        {
            get
            {
                return (SpawnMarkerState)base.CurrentState;
            }
        }

        public int SpawnDelay
        {
            get
            {
                return m_spawnDelay;
            }
            set
            {
                m_spawnDelay = value;
            }
        }

        public bool Highlight
        {
            get
            {
                return m_highlight;
            }
            set
            {
                m_highlight = value;
            }
        }

        public bool Spawned
        {
            get
            {
                return CurrentState != null && CurrentState.State == ESpawnMarkerState.Spawned;
            }
        }

        public float Progress
        {
            get
            {
                switch (CurrentState.State)
                {
                    case ESpawnMarkerState.Initial:
                    default:
                        {
                            return 0.0f;
                        }
                    case ESpawnMarkerState.Countdown:
                        {
                            return Math.Min(
                                (CurrentTime - CurrentState.TimeStamp) / (m_spawnDelay * STEP_MULTIPLIER * Robot.Robot.STEP_TIME),
                                1.0f
                            );
                        }
                    case ESpawnMarkerState.Blocked:
                    case ESpawnMarkerState.Spawned:
                        {
                            return 1.0f;
                        }
                }
            }
        }

        public float TimeLeft
        {
            get
            {
                switch (CurrentState.State)
                {
                    case ESpawnMarkerState.Initial:
                    default:
                        {
                            return m_spawnDelay * STEP_MULTIPLIER * Robot.Robot.STEP_TIME;
                        }
                    case ESpawnMarkerState.Countdown:
                        {
                            return Math.Max(
                                (m_spawnDelay * STEP_MULTIPLIER * Robot.Robot.STEP_TIME) - (CurrentTime - CurrentState.TimeStamp),
                                0.0f
                            );
                        }
                    case ESpawnMarkerState.Blocked:
                    case ESpawnMarkerState.Spawned:
                        {
                            return 0.0f;
                        }
                }
            }
        }

        public EventHandler OnSpawn;

        public SpawnMarker(Tile spawnTile, TileCoordinates location, FlatDirection direction, int spawnDelay) : base(spawnTile, location)
        {
            m_spawnDelay = spawnDelay;
            m_direction = direction;
            m_highlight = false;
        }

        protected override void OnInit()
        {
            if (Tile.Behaviour is SpawnTileBehaviour)
            {
                var spawnTile = (SpawnTileBehaviour)Tile.Behaviour;
                m_ghostModel = new ModelInstance(
                    spawnTile.RobotModel,
                    BuildTransform()
                );
                var animset = spawnTile.RobotAnimSet;
                if (animset != null)
                {
                    m_ghostModel.Animation = animset.GetAnim("ghost");
                    m_ghostModel.AnimTime = 0.0f;
                    m_ghostModel.Animate();
                }
            }
            else
            {
                App.Log("Error: Tile {0} does not have spawn behaviour", Tile.Path);
            }

            PushState(new SpawnMarkerState(
                ESpawnMarkerState.Initial, CurrentTime,
                1.0f, 1.0f, CurrentTime
            ));
        }

        public bool IsBlocked()
        {
            for (int y = 0; y < Tile.Height; ++y)
            {
                var coords = Location.Move(Direction.Up, y);
                if (Level.Tiles[coords].IsOccupied(Level, coords))
                {
                    return true;
                }
            }
            return false;
        }

        private void Spawn()
        {
            // Create the entity
            if (Tile.Behaviour is SpawnTileBehaviour)
            {
                Level.Entities.Add(
                    ((SpawnTileBehaviour)Tile.Behaviour).CreateRobot(Level, Location, m_direction, RobotActions.BeamDown)
                );
            }
            else
            {
                App.Log("Error: Tile {0} does not have spawn behaviour", Tile.Path);
            }
            PushState(new SpawnMarkerState(
                ESpawnMarkerState.Spawned, CurrentTime,
                CalculateCurrentGhostAlpha(), 0.0f, CurrentTime
            ));

            // Notify listeners
            if (OnSpawn != null)
            {
                OnSpawn.Invoke(this, EventArgs.Empty);
            }
        }

        public override void Update()
        {
            base.Update();
            if (m_ghostModel != null)
            {
                if (Tile.Behaviour is SpawnTileBehaviour)
                {
                    var spawnTile = (SpawnTileBehaviour)Tile.Behaviour;
                    var animset = spawnTile.RobotAnimSet;
                    if (animset != null)
                    {
                        m_ghostModel.Animation = Highlight ? animset.GetAnim("ghost_highlight") : animset.GetAnim("ghost");
                        m_ghostModel.AnimTime = Level.TimeMachine.RealTime;
                        m_ghostModel.Animate();
                    }
                }
            }
        }

        public float CalculateCurrentGhostAlpha()
        {
            var timer = CurrentTime - CurrentState.GhostAlphaTimeStamp;
            return
                CurrentState.GhostAlpha +
                Math.Min(timer / FADE_TIME, 1.0f) * (CurrentState.TargetGhostAlpha - CurrentState.GhostAlpha);
        }

        protected override void OnUpdate(float dt)
        {
            switch (CurrentState.State)
            {
                case ESpawnMarkerState.Initial:
                    {
                        if (m_spawnDelay > 0)
                        {
                            // Go to countdown
                            if (CurrentTime > CurrentState.TimeStamp + Robot.Robot.STEP_TIME)
                            {
                                PlaySound("sound/countdown_loop.wav", true);
                                PushState(new SpawnMarkerState(
                                    ESpawnMarkerState.Countdown, CurrentTime,
                                    1.0f, 1.0f, CurrentTime
                                ));
                            }
                        }
                        else
                        {
                            // Spawn instantly
                            if (CurrentTime > SpawnTime && !IsBlocked())
                            {
                                Spawn();
                            }
                        }
                        break;
                    }
                case ESpawnMarkerState.Countdown:
                    {
                        if (CurrentTime >= CurrentState.TimeStamp + (m_spawnDelay * STEP_MULTIPLIER * Robot.Robot.STEP_TIME))
                        {
                            StopSound("sound/countdown_loop.wav");
                            if (!IsBlocked())
                            {
                                Spawn();
                            }
                            else
                            {
                                PushState(
                                    new SpawnMarkerState(
                                        ESpawnMarkerState.Blocked, CurrentTime,
                                        CalculateCurrentGhostAlpha(), 0.0f, CurrentTime
                                    )
                                );
                            }
                        }
                        else
                        {
                            var targetAlpha = IsBlocked() ? 0.0f : 1.0f;
                            if (CurrentState.TargetGhostAlpha != targetAlpha)
                            {
                                PushState(
                                    new SpawnMarkerState(
                                        CurrentState.State, CurrentState.TimeStamp,
                                        CalculateCurrentGhostAlpha(), targetAlpha, CurrentTime
                                    )
                                );
                            }
                        }
                        break;
                    }
                case ESpawnMarkerState.Blocked:
                    {
                        if (!IsBlocked())
                        {
                            Spawn();
                        }
                        break;
                    }
            }
        }

        protected override void OnShutdown()
        {
        }

        protected override void OnLocationChanged()
        {
            if (m_ghostModel != null)
            {
                m_ghostModel.Transform = BuildTransform();
            }
        }

        public override bool NeedsRenderPass(RenderPass pass)
        {
            return pass == RenderPass.Translucent;
        }

        protected override void OnDraw(ModelEffectInstance effect, RenderPass pass)
        {
            var alpha = CalculateCurrentGhostAlpha();
            if (alpha > 0.0f)
            {
                m_ghostModel.Alpha = alpha;
                m_ghostModel.Draw(effect);
            }

            effect.ModelMatrix = m_ghostModel.Transform;
            effect.UVOffset = Vector2.Zero;
            effect.UVScale = Vector2.One;
            effect.DiffuseColour = Vector4.One;
            effect.DiffuseTexture = Texture.White;
            effect.SpecularColour = Vector3.Zero;
            effect.SpecularTexture = Texture.Black;
            effect.NormalTexture = Texture.Black;
            effect.EmissiveColour = Vector3.One;
            effect.EmissiveTexture = Texture.White;
            effect.Bind();
            //m_shell.Draw();
        }

        protected override void OnDrawShadows(ShadowEffectInstance shadowEffect)
        {
        }

        public override bool Raycast(Ray ray, out Direction o_side, out float o_distance)
        {
            var location = Location;
            return ray.TestVersusBox(
                new Vector3(location.X, location.Y * 0.5f, location.Z),
                new Vector3(location.X + 1.0f, location.Y * 0.5f + 0.5f, location.Z + 1.0f),
                out o_side,
                out o_distance
            );
        }

        private Matrix4 BuildTransform()
        {
            return
                Matrix4.CreateRotationY(
                    m_direction.ToYaw()
                ) *
                Matrix4.CreateTranslation(
                    Location.X + 0.5f,
                    Location.Y * 0.5f,
                    Location.Z + 0.5f
                );
        }
    }
}

