using Dan200.Core.Assets;
using Dan200.Core.GUI;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Core.Utils;
using Dan200.Game.GUI;
using Dan200.Game.Level;
using Dan200.Game.Script;
using OpenTK;
using System;

namespace Dan200.Game.Game
{
    public abstract class LevelState : State
    {
        private string m_levelLoadPath;
        private LevelOptions m_levelOptions;
        private Level.Level m_level;

        private Text m_placementPreviewText;
        private TileSelect m_placementPreview;

        private InGameCameraController m_cameraController;
        private ScriptController m_scriptController;

        private Text m_titleText;
        private DialogBox m_dialog;
        private bool m_disposeDialogOnClose;
        private float m_timeInState;

        private float m_skyAnimTimeOffset;

        public string LevelLoadPath
        {
            get
            {
                return m_levelLoadPath;
            }
        }

        public Level.Level Level
        {
            get
            {
                return m_level;
            }
        }

        public InGameCameraController CameraController
        {
            get
            {
                return m_cameraController;
            }
        }

        protected ScriptController ScriptController
        {
            get
            {
                return m_scriptController;
            }
        }

        protected bool ShowLevelTitle
        {
            get
            {
                return m_titleText.Visible;
            }
            set
            {
                m_titleText.Visible = value;
            }
        }

        protected bool ShowPlacementsLeft
        {
            get
            {
                return m_placementPreview.Visible;
            }
            set
            {
                m_placementPreview.Visible = value;
                m_placementPreviewText.Visible = value;
            }
        }

        public float TimeInState
        {
            get
            {
                return m_timeInState;
            }
        }

        protected bool EnableTimeEffects;

        public DialogBox Dialog
        {
            get
            {
                return m_dialog;
            }
        }

        public LevelState(Game game, string levelLoadPath, LevelOptions options) : base(game)
        {
            // Load level
            m_levelLoadPath = levelLoadPath;
            m_levelOptions = options;
            var data = Assets.Get<LevelData>(m_levelLoadPath);
            m_level = Dan200.Game.Level.Level.Load(data, m_levelOptions);
            m_level.Audio = game.Audio;
            m_level.Visible = false;

            // Load GUI
            m_placementPreviewText = new Text(UIFonts.Default, "", UIColours.Text, TextAlignment.Left);
            m_placementPreviewText.Visible = false;
            m_placementPreviewText.Anchor = Anchor.TopLeft;
            m_placementPreviewText.LocalPosition = new Vector2(
                96.0f + (this is EditorState ? 80.0f : 0.0f),
                32.0f
            );

            m_placementPreview = new TileSelect(Game, Tile.Get(Level.Info.ItemPath));
            m_placementPreview.Visible = false;
            m_placementPreview.Anchor = Anchor.TopLeft;
            m_placementPreview.LocalPosition = new Vector2(
                48.0f + (this is EditorState ? 80.0f : 0.0f),
                48.0f
            );

            UpdatePlacementsLeft();
            EnableTimeEffects = true;

            m_titleText = new Text(UIFonts.Default, "", UIColours.Text, TextAlignment.Right);
            m_titleText.Anchor = Anchor.TopRight;
            m_titleText.LocalPosition = new Vector2(-24.0f, 16.0f);
            m_titleText.Visible = false;

            m_dialog = null;
            m_disposeDialogOnClose = false;
            m_cameraController = new InGameCameraController(Game);
            m_timeInState = 0.0f;

            if (!m_level.Info.InEditor && m_level.Info.ScriptPath != null)
            {
                m_scriptController = new ScriptController(this);
            }
        }

        protected bool CheckBack()
        {
            return Dialog == null && Game.Screen.CheckBack();
        }

        protected override void OnReveal()
        {
            // Set the skybox
            Level.Visible = true;
            if (Game.Sky == null || Game.Sky.Sky.Path != m_level.Info.SkyPath)
            {
                Game.Sky = new SkyInstance(Sky.Get(m_level.Info.SkyPath));
                m_skyAnimTimeOffset = 0.0f;
            }
            else
            {
                if (!(this is CutsceneState))
                {
                    m_skyAnimTimeOffset = Game.Sky.AnimTime - m_level.TimeMachine.RealTime;
                }
                else
                {
                    m_skyAnimTimeOffset = 0.0f;
                }
                Game.Sky.ForegroundModelTransform = Matrix4.Identity;
            }
            Level.Sky = Game.Sky;
        }

        protected override void OnHide()
        {
            if (Level != null)
            {
                Level.Visible = false;
            }
        }

        protected string TranslateTitle(string title)
        {
            if (title.StartsWith("#", StringComparison.InvariantCulture))
            {
                return Game.Language.Translate(title.Substring(1));
            }
            return title;
        }

        protected override void OnPreInit(State previous, Transition transition)
        {
            // Position the camera
            CameraController.Pitch = MathHelper.DegreesToRadians(m_level.Info.CameraPitch);
            CameraController.Yaw = MathHelper.DegreesToRadians(m_level.Info.CameraYaw);
            CameraController.Distance = m_level.Info.CameraDistance;
            CameraController.TargetDistance = m_level.Info.CameraDistance;

            // Position the level
            var center = GetCentre(m_level);
            m_level.Transform = Matrix4.CreateTranslation(-center);
            m_titleText.String = TranslateTitle(m_level.Info.Title);
            UpdateCameraBounds();

            // Start level
            var tiles = m_level.Tiles;
            for (int x = tiles.MinX; x < tiles.MaxX; ++x)
            {
                for (int y = tiles.MinY; y < tiles.MaxY; ++y)
                {
                    for (int z = tiles.MinZ; z < tiles.MaxZ; ++z)
                    {
                        var coords = new TileCoordinates(x, y, z);
                        var tile = tiles[coords];
                        if (!tile.IsExtension())
                        {
                            tiles[coords].OnLevelStart(m_level, coords);
                        }
                    }
                }
            }

            // Start music
            Game.Audio.PlayMusic(GetMusicPath(previous, transition), transition != null ? transition.Duration : 0.0f);
        }

        protected virtual string GetMusicPath(State previous, Transition transition)
        {
            return Level.Info.MusicPath;
        }

        protected void UpdateCameraBounds()
        {
            var levelPos = m_level.Transform.Row3;
            m_cameraController.Bounds = new Quad(
                levelPos.X + m_level.Tiles.MinX - 2.0f,
                levelPos.Z + m_level.Tiles.MinZ - 2.0f,
                m_level.Tiles.Width + 4.0f,
                m_level.Tiles.Depth + 4.0f
            );
        }

        protected override void OnPreUpdate(float dt)
        {
            // Update level(s)
            CommonUpdate(dt);
        }

        private void CommonUpdate(float dt)
        {
            // Update level(s)
            m_level.Update(dt);
            UpdatePlacementsLeft();

            // Update script
            if (m_scriptController != null)
            {
                m_scriptController.Update(dt);
            }

            // Update effects
            UpdateTimeEffects(dt);
        }

        private void UpdateTimeEffects(float dt)
        {
            float scaledDT = m_level.TimeMachine.Rate * dt;
            if (m_level.Visible)
            {
                var rate = m_level.TimeMachine.Rate;
                bool warp = (rate < 0.0f || rate > 1.0f) && EnableTimeEffects && Game.RenderUI;
                Game.PostEffect.Desaturation = 0.75f * Game.PostEffect.Desaturation + 0.25f * (warp ? 0.75f : 0.0f);
                Game.PostEffect.Warp = 0.75f * Game.PostEffect.Warp + 0.25f * (warp ? 1.0f : 0.0f);
                Game.PostEffect.Time += scaledDT;
                Game.Sky.AnimTime = m_level.TimeMachine.RealTime + m_skyAnimTimeOffset;
                Game.Audio.Rate = m_level.TimeMachine.Rate;
            }
        }

        protected override void OnInit()
        {
            Game.Screen.Elements.Add(m_placementPreview);
            Game.Screen.Elements.Add(m_placementPreviewText);
            Game.Screen.Elements.Add(m_titleText);
            if (m_dialog != null)
            {
                Game.Screen.Elements.Add(m_dialog);
            }
        }

        protected override void OnUpdate(float dt)
        {
            // Update time
            m_timeInState += dt;

            // Update camera
            m_cameraController.Update(dt);

            // Update level(s)
            CommonUpdate(dt);
        }

        protected override void OnPopulateCamera(Camera camera)
        {
            m_cameraController.Populate(Game.Camera);
        }

        protected override void OnShutdown()
        {
            if (m_scriptController != null)
            {
                m_scriptController.Dispose();
                m_scriptController = null;
            }

            Game.Screen.Elements.Remove(m_placementPreview);
            m_placementPreview.Dispose();
            m_placementPreview = null;

            Game.Screen.Elements.Remove(m_placementPreviewText);
            m_placementPreviewText.Dispose();
            m_placementPreviewText = null;

            Game.Screen.Elements.Remove(m_titleText);
            m_titleText.Dispose();
            m_titleText = null;

            if (m_dialog != null)
            {
                Game.Screen.Elements.Remove(m_dialog);
                if (m_disposeDialogOnClose)
                {
                    m_dialog.Dispose();
                }
                m_dialog = null;
            }
        }

        protected override void OnPostUpdate(float dt)
        {
            // Update level(s)
            CommonUpdate(dt);
        }

        protected override void OnPostShutdown()
        {
            if (m_level != null)
            {
                m_level.Dispose();
                m_level = null;
            }
        }

        private void CommonDraw()
        {
            if (m_level != null)
            {
                // Setup level lighting
                if (Game.Sky != null)
                {
                    m_level.Lights.AmbientLight.Colour = Game.Sky.AmbientColour;
                    m_level.Lights.SkyLight.Active = (Game.Sky.LightColour.LengthSquared > 0.0f);
                    m_level.Lights.SkyLight.Colour = Game.Sky.LightColour;
                    m_level.Lights.SkyLight.Direction = Game.Sky.LightDirection;
                    m_level.Lights.SkyLight2.Active = (Game.Sky.Light2Colour.LengthSquared > 0.0f);
                    m_level.Lights.SkyLight2.Colour = Game.Sky.Light2Colour;
                    m_level.Lights.SkyLight2.Direction = Game.Sky.Light2Direction;
                }

                // Draw level
                m_level.Draw(
                    Game.Camera,
                    drawShadows: Game.User.Settings.Shadows
                );
            }
        }

        protected override void OnPreDraw()
        {
            CommonDraw();
        }

        protected override void OnDraw()
        {
            CommonDraw();
        }

        protected override void OnPostDraw()
        {
            CommonDraw();
        }

        protected void CutToState(State state, float delay = 0.0f)
        {
            Game.ChangeState(state, new CutTransition(Game, delay));
        }

        protected void WipeToState(State state, float delay = 0.0f)
        {
            Game.ChangeState(state, new WipeTransition(Game, delay));
        }

        protected void LoadToState(Func<State> stateFunction, float delay = 0.0f)
        {
            BlackoutToState(new LoadState(Game, stateFunction), delay);
        }

        protected void BlackoutToState(State state, float delay = 0.0f)
        {
            Game.ChangeState(state, new BlackTransition(Game, BlackTransitionType.ToBlack, delay));
        }

        public virtual void OnReloadAssets()
        {
            if (m_level != null)
            {
                m_level.Tiles.RequestRebuild();
            }
            if (m_placementPreview != null)
            {
                m_placementPreview.ReloadAssets();
            }
            if (Game.Sky != null)
            {
                Game.Sky.ReloadAssets();
            }
        }

        public void ShowDialog(DialogBox dialog, bool disposeOnClose = true)
        {
            if (m_dialog == dialog)
            {
                return;
            }

            if (m_dialog != null)
            {
                Game.Screen.Elements.Remove(m_dialog);
                if (m_disposeDialogOnClose)
                {
                    m_dialog.Dispose();
                }
                m_dialog = null;
            }

            m_dialog = dialog;
            m_disposeDialogOnClose = disposeOnClose;
            Game.Screen.Elements.Add(dialog);

            m_dialog.OnClosed += delegate (object sender, DialogBoxClosedEventArgs e)
            {
                if (m_dialog == dialog)
                {
                    Game.Screen.Elements.Remove(m_dialog);
                    if (m_disposeDialogOnClose)
                    {
                        m_dialog.Dispose();
                    }
                    m_dialog = null;
                }
            };
        }

        protected Ray BuildRay(Vector2 screenPosition, float length)
        {
            // Convert screen coords to camera space direction
            float aspect = Game.Screen.Width / Game.Screen.Height;
            float x = (screenPosition.X / (0.5f * Game.Screen.Width)) - 1.0f;
            float y = (screenPosition.Y / (0.5f * Game.Screen.Height)) - 1.0f;

            Vector3 dirCS = new Vector3(
                (float)(Math.Tan(0.5f * Game.Camera.FOV)) * (x * aspect),
                -(float)(Math.Tan(0.5f * Game.Camera.FOV)) * y,
                -1.0f
            );
            dirCS.Normalize();

            // Convert camera space direction to world space ray
            Matrix4 cameraTransInv = Game.Camera.Transform;
            MathUtils.FastInvert(ref cameraTransInv);

            return new Ray(
                Vector3.TransformPosition(Vector3.Zero, cameraTransInv),
                Vector3.TransformVector(dirCS, cameraTransInv),
                length
            );
        }

        private Vector3 GetCentre(Level.Level level)
        {
            // Look for camera targets, or the lowest floor tile
            int? lowestPlacement = null;
            var cameraTargetSum = Vector3.Zero;
            var numCameraTargets = 0.0f;
            for (int x = level.Tiles.MinX; x < level.Tiles.MaxX; ++x)
            {
                for (int y = level.Tiles.MinY; y < level.Tiles.MaxY; ++y)
                {
                    for (int z = level.Tiles.MinZ; z < level.Tiles.MaxZ; ++z)
                    {
                        var coords = new TileCoordinates(x, y, z);
                        var tile = level.Tiles[coords];
                        if (tile.IsCameraTarget(level, coords))
                        {
                            var baseCoords = level.Tiles[coords].GetBase(level, coords);
                            cameraTargetSum += new Vector3((float)baseCoords.X + 0.5f, (float)baseCoords.Y * 0.5f, (float)baseCoords.Z + 0.5f);
                            numCameraTargets++;
                        }
                        else if (tile.AllowPlacement)
                        {
                            var above = coords.Move(Direction.Up, tile.Height);
                            if (!lowestPlacement.HasValue || above.Y < lowestPlacement.Value)
                            {
                                var aboveTile = level.Tiles[above];
                                if (tile.IsSolidOnSide(level, coords, Direction.Up) &&
                                    aboveTile.IsReplaceable(level, above))
                                {
                                    lowestPlacement = above.Y;
                                }
                            }
                        }
                    }
                }
            }

            if (numCameraTargets > 0)
            {
                // Use the average of the camera targets
                return cameraTargetSum / (float)numCameraTargets;
            }

            // Use the geometric center of the lowest placeable layer
            var floor = lowestPlacement.HasValue ? lowestPlacement.Value : level.Tiles.MinY + 1;
            return new Vector3(
                (float)level.Tiles.MinX + (float)level.Tiles.Width * 0.5f,
                0.5f * (float)floor,
                (float)level.Tiles.MinZ + (float)level.Tiles.Depth * 0.5f
            );
        }

        private void UpdatePlacementsLeft()
        {
            if (m_placementPreviewText != null && m_level != null)
            {
                m_placementPreviewText.String = "x" + m_level.Info.PlacementsLeft.ToString();
            }
            if (m_placementPreview != null && m_level != null)
            {
                m_placementPreview.Tile = Tile.Get(m_level.Info.ItemPath);
            }
        }
    }
}

