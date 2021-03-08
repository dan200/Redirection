using Dan200.Core.Animation;
using Dan200.Core.Audio;
using Dan200.Core.Render;
using Dan200.Core.Utils;
using Dan200.Game.Level;
using OpenTK;
using System;

namespace Dan200.Game.Robot
{
    public class Robot : Entity
    {
        public const float STEP_TIME = 0.5f;

        private string m_colour;
        private bool m_required;
        private bool m_immobile;

        private RobotAction m_spawnAction;
        private TileCoordinates m_spawnPosition;
        private FlatDirection m_spawnDirection;
        private TurnDirection m_turnPreference;
        private Vector3 m_guiColour;

        private ModelInstance m_modelInstance;
        private AnimSet m_animations;
        private SoundSet m_sounds;
        private Vector3 m_lightColour;
        private PointLight m_light;

        private RenderPass m_renderPass;
        private bool m_castShadows;

        public event EventHandler OnFall;
        public event EventHandler OnDrown;

        private new RobotState CurrentState
        {
            get
            {
                return (RobotState)base.CurrentState;
            }
        }

        public bool IsStopped
        {
            get
            {
                return CurrentState != null &&
                       CurrentState.Action is RobotGoalAction &&
                       CurrentTime - CurrentState.TimeStamp >= RobotGoalAction.DURATION;
            }
        }

        public bool IsSaved
        {
            get
            {
                return CurrentState != null &&
                       CurrentState.Action is RobotBeamUpAction &&
                       CurrentTime > CurrentState.TimeStamp + RobotBeamUpAction.DURATION + STEP_TIME;
            }
        }

        public bool IsVacating
        {
            get
            {
                return CurrentState != null &&
                       CurrentState.Action is RobotWalkAction &&
                    (Level.TimeMachine.Time - CurrentState.TimeStamp) > (0.5f * STEP_TIME);
            }
        }

        public bool IsMoving
        {
            get
            {
                return !(CurrentState.Action is RobotGoalAction || CurrentState.Action is RobotWaitAction);
            }
        }

        public bool IsTurning
        {
            get
            {
                return CurrentState != null &&
                       CurrentState.Action is RobotTurnAction;
            }
        }

        public TileCoordinates Location
        {
            get
            {
                return CurrentState.Position;
            }
        }

        public override Matrix4 Transform
        {
            get
            {
                var transform = m_modelInstance.Transform;
                var anim = CurrentState.Action.GetAnimation(CurrentState);
                if (anim != null)
                {
                    bool visible;
                    Matrix4 partTransform;
                    Vector2 uvOffset;
                    Vector2 uvScale;
                    Vector4 colour;
                    float fov;
                    var animTime = CurrentState.Action.GetAnimTime(CurrentState);
                    anim.Animate("Center", animTime, out visible, out partTransform, out uvOffset, out uvScale, out colour, out fov);
                    transform = partTransform * transform;
                }
                return transform;
            }
        }

        public FlatDirection Direction
        {
            get
            {
                return CurrentState.Direction;
            }
        }

        public string Colour
        {
            get
            {
                return m_colour;
            }
        }

        public bool Required
        {
            get
            {
                return m_required;
            }
        }

        public bool Immobile
        {
            get
            {
                return m_immobile;
            }
        }

        public Vector3 GUIColour
        {
            get
            {
                return m_guiColour;
            }
        }

        public Robot(TileCoordinates position, FlatDirection direction, string colour, bool immobile, bool required, RobotAction initialAction, Vector3 guiColour, TurnDirection turnPreference, Model model, AnimSet animations, SoundSet sounds, Vector3? lightColour, float? lightRadius, RenderPass renderPass, bool castShadows)
        {
            m_colour = colour;
            m_immobile = immobile;
            m_required = required;
            m_spawnAction = initialAction;
            m_spawnPosition = position;
            m_spawnDirection = direction;
            m_turnPreference = turnPreference;
            if (lightColour.HasValue && lightRadius.HasValue)
            {
                m_lightColour = lightColour.Value;
                m_light = new PointLight(Vector3.Zero, lightColour.Value, lightRadius.Value);
            }
            m_modelInstance = new ModelInstance(model, Matrix4.Identity);
            m_animations = animations;
            m_sounds = sounds;
            m_guiColour = guiColour;
            m_renderPass = renderPass;
            m_castShadows = castShadows;
        }

        public void BeamUp()
        {
            if (CurrentState != null && CurrentState.Action != RobotActions.BeamUp)
            {
                PushState(RobotActions.BeamUp.Init(CurrentState));
            }
        }

        protected override void OnInit()
        {
            var state = new RobotState(this, Level.Random.Next(), m_spawnPosition, m_spawnDirection, m_turnPreference, m_spawnAction);
            state = m_spawnAction.Init(state);
            if (m_light != null)
            {
                Level.Lights.PointLights.Add(m_light);
            }
            PushState(state);
            Reoccupy();
            UpdateLight();
            UpdateTransform();
            UpdateAnimation();
        }

        protected override void OnShutdown()
        {
            Unoccupy();
            if (m_light != null)
            {
                Level.Lights.PointLights.Remove(m_light);
                m_light = null;
            }
        }

        public IAnimation GetAnim(string name)
        {
            if (m_animations != null)
            {
                return m_animations.GetAnim(name);
            }
            return null;
        }

        public new void PlaySound(string name, bool looping = false)
        {
            var path = m_sounds.GetSound(name);
            if (path != null)
            {
                base.PlaySound(path, looping);
            }
        }

        public new void StopSound(string name)
        {
            var path = m_sounds.GetSound(name);
            if (path != null)
            {
                base.StopSound(path);
            }
        }

        private void Unoccupy()
        {
            if (CurrentState != null)
            {
                // Occupy the current tile
                var currentCoords = CurrentState.Position;
                var current = Level.Tiles[currentCoords];
                if (current.GetOccupant(Level, currentCoords) == this)
                {
                    current.SetOccupant(Level, currentCoords, null);
                }

                var aboveCoords = currentCoords.Above();
                var above = Level.Tiles[aboveCoords];
                if (above.GetOccupant(Level, aboveCoords) == this)
                {
                    above.SetOccupant(Level, aboveCoords, null);
                }

                // Occupy the destination tile
                var destinationCoords = CurrentState.Action.GetDestination(CurrentState);
                var destination = Level.Tiles[destinationCoords];
                if (destination.GetOccupant(Level, destinationCoords) == this)
                {
                    destination.SetOccupant(Level, destinationCoords, null);
                }

                var aboveDestinationCoords = destinationCoords.Above();
                var aboveDestination = Level.Tiles[aboveDestinationCoords];
                if (aboveDestination.GetOccupant(Level, aboveDestinationCoords) == this)
                {
                    aboveDestination.SetOccupant(Level, aboveDestinationCoords, null);
                }
            }
        }

        private void Reoccupy()
        {
            if (CurrentState != null)
            {
                // Occupy the current tile
                var currentCoords = CurrentState.Position;
                var current = Level.Tiles[currentCoords];
                current.SetOccupant(Level, currentCoords, this);

                var aboveCoords = currentCoords.Above();
                var above = Level.Tiles[aboveCoords];
                above.SetOccupant(Level, aboveCoords, this);

                // Occupy the destination tile
                var destinationCoords = CurrentState.Action.GetDestination(CurrentState);
                var destination = Level.Tiles[destinationCoords];
                destination.SetOccupant(Level, destinationCoords, this);

                var aboveDestinationCoords = destinationCoords.Above();
                var aboveDestination = Level.Tiles[aboveDestinationCoords];
                aboveDestination.SetOccupant(Level, aboveDestinationCoords, this);
            }
        }

        public override void Update()
        {
            Unoccupy();
            base.Update();
            if (CurrentState != null)
            {
                Reoccupy();
                UpdateLight();
                UpdateTransform();
                UpdateAnimation();
            }
        }

        protected override void OnUpdate(float dt)
        {
            PushState(CurrentState.Action.Update(CurrentState, dt));
        }

        public override bool NeedsRenderPass(RenderPass pass)
        {
            return pass == m_renderPass;
        }

        protected override void OnDraw(ModelEffectInstance modelEffect, RenderPass pass)
        {
            if (Level.InEditor && Level.Tiles[m_spawnPosition].IsHidden(Level, m_spawnPosition))
            {
                return;
            }
            m_modelInstance.Draw(modelEffect);
        }

        protected override void OnDrawShadows(ShadowEffectInstance shadowEffect)
        {
            if (Level.InEditor && Level.Tiles[m_spawnPosition].IsHidden(Level, m_spawnPosition))
            {
                return;
            }
            if (m_castShadows)
            {
                m_modelInstance.DrawShadows(shadowEffect);
            }
        }

        public override bool Raycast(Ray ray, out Direction o_side, out float o_distance)
        {
            var position = Position;
            return ray.TestVersusBox(
                position + new Vector3(-0.375f, 0.0f, -0.375f),
                position + new Vector3(0.375f, 0.75f, 0.375f),
                out o_side,
                out o_distance
            );
        }

        private void UpdateLight()
        {
            if (m_light != null)
            {
                Vector3 lightPos;
                Vector3 lightColour;
                CurrentState.Action.GetLightInfo(CurrentState, out lightPos, out lightColour);
                m_light.Colour = new Vector3(lightColour.X * m_lightColour.X, lightColour.Y * m_lightColour.Y, lightColour.Z * m_lightColour.Z);
                m_light.Position = lightPos + new Vector3(0.0f, 1.0f, 0.0f);
            }
        }

        private void UpdateTransform()
        {
            // Determine position and angle
            Vector3 position = CurrentState.Action.GetPosition(CurrentState);
            float yaw = CurrentState.Action.GetYaw(CurrentState);

            // Set transform
            m_modelInstance.Transform =
                Matrix4.CreateRotationY(yaw) *
                Matrix4.CreateTranslation(position.X, position.Y, position.Z);
        }

        private void UpdateAnimation()
        {
            // Animate
            m_modelInstance.Animation = CurrentState.Action.GetAnimation(CurrentState);
            m_modelInstance.AnimTime = CurrentState.Action.GetAnimTime(CurrentState);
            m_modelInstance.Animate();
        }

        public void EmitOnFall()
        {
            if (OnFall != null)
            {
                OnFall.Invoke(this, EventArgs.Empty);
            }
        }

        public void EmitOnDrown()
        {
            if (OnDrown != null)
            {
                OnDrown.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
