using Dan200.Core.Assets;
using Dan200.Core.Audio;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.GUI;
using Dan200.Game.Input;
using Dan200.Game.Level;
using Dan200.Game.Robot;
using Dan200.Game.Script;
using Dan200.Game.User;
using OpenTK;
using System;
using System.Collections.Generic;

namespace Dan200.Game.Game
{
    public enum GameState
    {
        Intro,
        Planning,
        Playing,
        Rewinding,
        Outro,
    }

    public abstract class InGameState : LevelState
    {
        public const float INTRO_DURATION = RobotPreSpawnAction.DURATION + RobotSpawnAction.DURATION;
        private const float MAX_REWIND_DURATION = 0.6f;

        private Button m_playButton;
        private Button m_fastForwardButton;
        private Button m_menuButton;
        private Text m_timerText;
        private InputPrompt m_tweakPrompt;
        private InputPrompt m_placePrompt;
        private InputPrompt m_tweakUpPrompt;
        private InputPrompt m_tweakDownPrompt;
        private InWorldInputPrompt m_playButtonHint;
        private InWorldInputPrompt m_fastForwardButtonHint;

        private DialogueBox m_dialogueBox;
        private LevelCompleteDetails m_levelCompleteDetails;
        private LevelCompleteMessage m_levelCompleteMessage;

        private List<InWorldInputPrompt> m_placementHints;
        private Dictionary<Robot.Robot, RobotIndicator> m_robotIndicators;
        private Dictionary<SpawnMarker, SpawnMarkerIndicator> m_spawnMarkerIndicators;
        private SpawnMarker m_selectedSpawnMarker;
        private SpawnMarker m_highligtedSpawnMarker;
        private HashSet<TileCoordinates> m_spawnMarkerLocations;

        private SpawnMarkerPreview m_previewEntity;

        private GameState m_gameState;
        private float m_outroTimer;

        public GameState State
        {
            get
            {
                return m_gameState;
            }
        }

        public SpawnMarker SelectedSpawnMarker
        {
            get
            {
                return m_selectedSpawnMarker;
            }
        }

        public bool PlayDisabled = false;
        public bool RewindDisabled = false;
        public bool FastForwardDisabled = false;
        public bool PlaceDisabled = false;
        public bool RemoveDisabled = false;
        public bool TweakDisabled = false;
        public bool ShowPlayHint = false;
        public bool ShowRewindHint = false;
        public bool ShowFastForwardHint = false;

        public InGameState(Game game, string levelLoadPath) : base(game, levelLoadPath, LevelOptions.InGame)
        {
            ShowLevelTitle = true;

            m_robotIndicators = new Dictionary<Robot.Robot, RobotIndicator>();
            m_spawnMarkerIndicators = new Dictionary<SpawnMarker, SpawnMarkerIndicator>();
            m_selectedSpawnMarker = null;
            m_highligtedSpawnMarker = null;
            m_spawnMarkerLocations = new HashSet<TileCoordinates>();

            // Create buttons
            // Play/Rewind
            m_playButton = new Button(Texture.Get("gui/play.png", true), 64.0f, 64.0f);
            m_playButton.Region = new Quad(0.0f, 0.0f, 0.375f, 0.375f);
            m_playButton.HighlightRegion = new Quad(0.5f, 0.0f, 0.375f, 0.375f);
            m_playButton.DisabledRegion = new Quad(0.0f, 0.5f, 0.375f, 0.375f);
            m_playButton.ShortcutKey = Game.User.Settings.GetKeyBind(Bind.Play);
            m_playButton.ShortcutButton = Game.User.Settings.GetPadBind(Bind.Play);
            m_playButton.ShortcutSteamControllerButton = SteamControllerButton.InGamePlay;
            m_playButton.ShowShortcutPrompt = true;
            m_playButton.Anchor = Anchor.BottomLeft;
            m_playButton.LocalPosition = new Vector2(16.0f, -16.0f - m_playButton.Height);
            m_playButton.Visible = false;

            // Fastforward
            m_fastForwardButton = new Button(Texture.Get("gui/fastforward.png", true), 48.0f, 48.0f);
            m_fastForwardButton.Region = new Quad(0.0f, 0.0f, 0.375f, 0.375f);
            m_fastForwardButton.HighlightRegion = new Quad(0.5f, 0.0f, 0.375f, 0.375f);
            m_fastForwardButton.DisabledRegion = new Quad(0.0f, 0.5f, 0.375f, 0.375f);
            m_fastForwardButton.ShortcutKey = Game.User.Settings.GetKeyBind(Bind.FastForward);
            m_fastForwardButton.ShortcutButton = Game.User.Settings.GetPadBind(Bind.FastForward);
            m_fastForwardButton.ShortcutSteamControllerButton = SteamControllerButton.InGameFastForward;
            m_fastForwardButton.ShowShortcutPrompt = true;
            m_fastForwardButton.Anchor = Anchor.BottomLeft;
            m_fastForwardButton.LocalPosition = new Vector2(16.0f + m_playButton.Width + 8.0f, -16.0f - m_fastForwardButton.Height);
            m_fastForwardButton.Visible = false;

            // Menu
            m_menuButton = new Button(Texture.Get((this is TestState) ? "gui/menu_editor.png" : "gui/menu.png", true), 48.0f, 48.0f);
            m_menuButton.Region = new Quad(0.0f, 0.0f, 0.375f, 0.375f);
            m_menuButton.HighlightRegion = new Quad(0.5f, 0.0f, 0.375f, 0.375f);
            m_menuButton.DisabledRegion = new Quad(0.0f, 0.5f, 0.375f, 0.375f);
            m_menuButton.ShortcutKey = Key.Escape;
            m_menuButton.ShortcutButton = GamepadButton.Start;
            m_menuButton.ShortcutSteamControllerButton = SteamControllerButton.InGameToMenu;
            m_menuButton.ShowShortcutPrompt = true;
            m_menuButton.AllowDuringDialogue = true;
            m_menuButton.Anchor = Anchor.BottomRight;
            m_menuButton.LocalPosition = new Vector2(-16.0f - m_menuButton.Width, -16.0f - m_menuButton.Height);
            m_menuButton.Visible = false;

            // Timer
            m_timerText = new Text(UIFonts.Smaller, "0:00", UIColours.Text, TextAlignment.Left);
            m_timerText.Anchor = Anchor.BottomLeft;
            m_timerText.LocalPosition = new Vector2(16.0f + m_playButton.Width + 8.0f + m_fastForwardButton.Width + 8.0f, -16.0f - m_timerText.Font.Height);
            m_timerText.Visible = true;

            // Create prompts
            // Tweak
            m_tweakPrompt = new InputPrompt(UIFonts.Smaller, game.Language.Translate("inputs.tweak.name"), TextAlignment.Right);
            m_tweakPrompt.MouseButton = Game.User.Settings.GetMouseBind(Bind.Tweak);
            m_tweakPrompt.GamepadButton = Game.User.Settings.GetPadBind(Bind.Tweak);
            m_tweakPrompt.SteamControllerButton = SteamControllerButton.InGameTweak;
            m_tweakPrompt.Anchor = Anchor.BottomRight;
            m_tweakPrompt.LocalPosition = new Vector2(-16.0f, -16.0f);
            m_tweakPrompt.Visible = false;

            // Place/Remove
            m_placePrompt = new InputPrompt(UIFonts.Smaller, game.Language.Translate("inputs.place.name"), TextAlignment.Right);
            m_placePrompt.MouseButton = Game.User.Settings.GetMouseBind(Bind.Place);
            m_placePrompt.GamepadButton = Game.User.Settings.GetPadBind(Bind.Place);
            m_placePrompt.SteamControllerButton = SteamControllerButton.InGamePlace;
            m_placePrompt.Anchor = Anchor.BottomRight;
            m_placePrompt.LocalPosition = new Vector2(-16.0f, -16.0f);
            m_placePrompt.Visible = false;

            // Increase delay
            m_tweakUpPrompt = new InputPrompt(UIFonts.Smaller, game.Language.Translate("inputs.tweak_up.name"), TextAlignment.Right);
            m_tweakUpPrompt.GamepadButton = Game.User.Settings.GetPadBind(Bind.IncreaseDelay);
            m_tweakUpPrompt.SteamControllerButton = SteamControllerButton.InGameTweakUp;
            m_tweakUpPrompt.Anchor = Anchor.BottomRight;
            m_tweakUpPrompt.LocalPosition = new Vector2(-16.0f, -16.0f);
            m_tweakUpPrompt.Visible = false;

            // Decrease delay
            m_tweakDownPrompt = new InputPrompt(UIFonts.Smaller, game.Language.Translate("inputs.tweak_down.name"), TextAlignment.Right);
            m_tweakDownPrompt.GamepadButton = Game.User.Settings.GetPadBind(Bind.DecreaseDelay);
            m_tweakDownPrompt.SteamControllerButton = SteamControllerButton.InGameTweakDown;
            m_tweakDownPrompt.Anchor = Anchor.BottomRight;
            m_tweakDownPrompt.LocalPosition = new Vector2(-16.0f, -16.0f);
            m_tweakDownPrompt.Visible = false;

            // Create hints
            // Play/Rewind
            m_playButtonHint = new InWorldInputPrompt(Level, Game.Camera, UIFonts.Default, "", TextAlignment.Center);
            m_playButtonHint.MouseButton = MouseButton.Left;
            m_playButtonHint.UsePosition3D = false;
            m_playButtonHint.Position2DAnchor = m_playButton.Anchor;
            m_playButtonHint.Position2D = m_playButton.LocalPosition + new Vector2(0.5f * m_playButton.Width, 0.0f);

            // Fastforward
            m_fastForwardButtonHint = new InWorldInputPrompt(Level, Game.Camera, UIFonts.Default, "", TextAlignment.Center);
            m_fastForwardButtonHint.MouseButton = MouseButton.Left;
            m_fastForwardButtonHint.UsePosition3D = false;
            m_fastForwardButtonHint.Position2DAnchor = m_fastForwardButton.Anchor;
            m_fastForwardButtonHint.Position2D = m_fastForwardButton.LocalPosition + new Vector2(0.5f * m_fastForwardButton.Width, 0.0f);

            // Init state
            m_gameState = GameState.Intro;
            Level.TimeMachine.Rate = 0.0f;
            EnableTimeEffects = true;
            ShowPlacementsLeft = true;
        }

        protected override string GetMusicPath(State previous, Transition transition)
        {
            return null;
        }

        public override void OnReloadAssets()
        {
            base.OnReloadAssets();
            UpdatePrompts();
        }

        public void ShowDialogue(string character, string dialogue, bool modal)
        {
            // Create dialogue box
            if (m_dialogueBox == null)
            {
                m_dialogueBox = DialogueBox.Create(Game.Screen);
            }

            // Populate dialogue box
            m_dialogueBox.CharacterName = Game.Screen.Language.Translate("character." + character + ".name");
            m_dialogueBox.CharacterImage = Texture.Get("gui/portraits/" + character + ".png", true);
            m_dialogueBox.Dialogue = dialogue;
            m_dialogueBox.Modal = modal;
            if (m_dialogueBox.IsClosing || m_dialogueBox.IsClosed)
            {
                m_dialogueBox.Open();
            }

            // Show dialogue box
            if (Dialog == null && Dialog != m_dialogueBox)
            {
                ShowDialog(m_dialogueBox, false);
            }

            // Make a noise
            var sound = "sound/message/" + character + ".wav";
            if (Assets.Exists<Sound>(sound))
            {
                Game.Screen.Audio.PlaySound(sound);
            }
        }

        public void HideDialogue()
        {
            // Close the dialogue box
            if (m_dialogueBox != null)
            {
                m_dialogueBox.Close(-1);
            }
        }

        public bool IsDialogueReady()
        {
            return m_dialogueBox != null && m_dialogueBox.Modal;
        }

        public bool IsDialogueVisible()
        {
            return m_dialogueBox != null && !m_dialogueBox.IsClosed;
        }

        public bool IsDialogueReadyForInput()
        {
            return m_dialogueBox != null && m_dialogueBox.ReadyForInput;
        }

        public bool IsDialogueReadyToContinue()
        {
            return m_dialogueBox != null && m_dialogueBox.ContinueRequested;
        }

        private void UpdatePrompts()
        {
            bool mouseKeyboard = Game.Screen.InputMethod == InputMethod.Mouse || Game.Screen.InputMethod == InputMethod.Keyboard;
            if (mouseKeyboard)
            {
                // No prompts
                m_tweakPrompt.Visible = false;
                m_placePrompt.Visible = false;
                m_tweakUpPrompt.Visible = false;
                m_tweakDownPrompt.Visible = false;
            }
            else
            {
                // Prompts:
                // Tweak
                var tweak = GetPotentialTweak();
                if (tweak != null && tweak != m_selectedSpawnMarker)
                {
                    m_tweakPrompt.Visible = true;
                    m_tweakPrompt.String = Game.Language.Translate("inputs.tweak.name");
                }
                else
                {
                    m_tweakPrompt.Visible = false;
                }

                // Place/Remove
                if (GetPotentialPlace().HasValue && !mouseKeyboard)
                {
                    m_placePrompt.Visible = true;
                    m_placePrompt.String = Game.Language.Translate("inputs.place.name");
                    m_placePrompt.GamepadButton = Game.User.Settings.GetPadBind(Bind.Place);
                    m_placePrompt.SteamControllerButton = SteamControllerButton.InGamePlace;
                }
                else if (GetPotentialRemove() != null && !mouseKeyboard)
                {
                    m_placePrompt.Visible = true;
                    m_placePrompt.String = Game.Language.Translate("inputs.remove.name");
                    m_placePrompt.GamepadButton = Game.User.Settings.GetPadBind(Bind.Remove);
                    m_placePrompt.SteamControllerButton = SteamControllerButton.InGameRemove;
                }
                else
                {
                    m_placePrompt.Visible = false;
                }

                // Tweak up
                if (m_gameState == GameState.Planning && m_selectedSpawnMarker != null && m_selectedSpawnMarker.SpawnDelay < SpawnMarker.MAX_DELAY)
                {
                    m_tweakUpPrompt.Visible = true;
                    m_tweakUpPrompt.String = Game.Language.Translate("inputs.tweak_up.name");
                }
                else
                {
                    m_tweakUpPrompt.Visible = false;
                }

                // Tweak down
                if (m_gameState == GameState.Planning && m_selectedSpawnMarker != null && m_selectedSpawnMarker.SpawnDelay > 0)
                {
                    m_tweakDownPrompt.Visible = true;
                    m_tweakDownPrompt.String = Game.Language.Translate("inputs.tweak_down.name");
                }
                else
                {
                    m_tweakDownPrompt.Visible = false;
                }
            }

            // Buttons:
            // Play
            if (m_gameState == GameState.Planning || m_gameState == GameState.Playing || m_gameState == GameState.Rewinding)
            {
                m_playButton.Visible = true;
                m_playButton.Texture = (m_gameState == GameState.Planning) ? Texture.Get("gui/play.png", true) : Texture.Get("gui/rewind.png", true);
                m_playButton.ShortcutKey = (m_gameState == GameState.Planning) ? Game.User.Settings.GetKeyBind(Bind.Play) : Game.User.Settings.GetKeyBind(Bind.Rewind);
                m_playButton.ShortcutButton = (m_gameState == GameState.Planning) ? Game.User.Settings.GetPadBind(Bind.Play) : Game.User.Settings.GetPadBind(Bind.Rewind);
                m_playButton.ShortcutSteamControllerButton = (m_gameState == GameState.Planning) ? SteamControllerButton.InGamePlay : SteamControllerButton.InGameRewind;
                m_playButton.Disabled = (m_gameState == GameState.Rewinding) || ((m_gameState == GameState.Planning) ? PlayDisabled : RewindDisabled);
            }
            else
            {
                m_playButton.Visible = false;
            }

            // Fastforward
            if (m_gameState == GameState.Planning || m_gameState == GameState.Playing || m_gameState == GameState.Rewinding)
            {
                m_fastForwardButton.Visible = true;
                m_fastForwardButton.Disabled = (m_gameState == GameState.Rewinding) || ((m_gameState == GameState.Planning) ? (FastForwardDisabled || PlayDisabled) : FastForwardDisabled);
            }
            else
            {
                m_fastForwardButton.Visible = false;
            }

            // Menu
            if ((this is TestState) ?
                    (m_gameState != GameState.Outro) :
                    (mouseKeyboard && m_gameState != GameState.Intro && m_gameState != GameState.Outro)
                )
            {
                m_menuButton.Visible = true;
            }
            else
            {
                m_menuButton.Visible = false;
            }

            // Hints
            m_playButtonHint.Visible = m_playButton.Visible && !m_playButton.Disabled && Game.Screen.InputMethod == InputMethod.Mouse && ((m_gameState == GameState.Planning) ? ShowPlayHint : ShowRewindHint);
            m_fastForwardButtonHint.Visible = m_fastForwardButton.Visible && !m_fastForwardButton.Disabled && Game.Screen.InputMethod == InputMethod.Mouse && ShowFastForwardHint;

            // Timer
            if (m_gameState == GameState.Planning || m_gameState == GameState.Playing || m_gameState == GameState.Rewinding)
            {
                var seconds = (int)(Level.TimeMachine.Time - (INTRO_DURATION + Robot.Robot.STEP_TIME));
                m_timerText.Visible = true;
                m_timerText.String = string.Format("{0}:{1:D2}", seconds / 60, seconds % 60);
            }
            else
            {
                m_timerText.Visible = false;
            }

            // Reposition prompts:
            var top = -16.0f;
            var right = -16.0f;
            if (m_menuButton.Visible)
            {
                right = m_menuButton.LocalPosition.X - 14.0f;
            }
            if (m_placePrompt.Visible)
            {
                m_placePrompt.LocalPosition = new Vector2(right, top - m_placePrompt.Height);
                top -= m_placePrompt.Height;
            }
            if (m_tweakPrompt.Visible)
            {
                m_tweakPrompt.LocalPosition = new Vector2(right, top - m_tweakPrompt.Height);
                top -= m_tweakPrompt.Height;
            }
            if (m_tweakUpPrompt.Visible)
            {
                m_tweakUpPrompt.LocalPosition = new Vector2(right, top - m_tweakUpPrompt.Height);
                top -= m_tweakUpPrompt.Height;
            }
            if (m_tweakDownPrompt.Visible)
            {
                m_tweakDownPrompt.LocalPosition = new Vector2(right, top - m_tweakDownPrompt.Height);
                top -= m_tweakDownPrompt.Height;
            }
        }

        protected abstract void Reset();

        protected override void OnPreInit(State previous, Transition transition)
        {
            base.OnPreInit(previous, transition);

            // Position camera
            if (previous is InGameState || previous is EditorState)
            {
                LevelState previousInGame = (LevelState)previous;
                CameraController.Pitch = previousInGame.CameraController.Pitch;
                CameraController.Yaw = previousInGame.CameraController.Yaw;
                CameraController.Distance = previousInGame.CameraController.Distance;
                CameraController.TargetDistance = previousInGame.CameraController.TargetDistance;
            }
            else
            {
                if (Game.Screen.InputMethod == InputMethod.Gamepad)
                {
                    Game.Cursor.LocalPosition = new Vector2(Game.Screen.Width * 0.5f, Game.Screen.Height * 0.5f);
                }
            }

            // Stop the clock
            m_gameState = GameState.Intro;
            SetRate(VCRRate.Pause);
        }

        protected override void OnInit()
        {
            base.OnInit();

            // Start the clock
            SetRate(VCRRate.Play, INTRO_DURATION);

            // Enable camera control
            CameraController.AllowUserRotate = true;
            CameraController.AllowUserZoom = true;
            Game.Cursor.Visible = true;

            // Setup the preview entity
            m_previewEntity = new SpawnMarkerPreview(Tile.Get(Level.Info.ItemPath), TileCoordinates.Zero);
            m_previewEntity.Visible = false;
            Level.Entities.Add(m_previewEntity);

            // Create indicators
            foreach (var entity in Level.Entities)
            {
                if (entity is Robot.Robot)
                {
                    var robot = (Robot.Robot)entity;
                    m_robotIndicators.Add(robot, new RobotIndicator(robot, Game.Camera));
                    robot.OnDrown += delegate
                    {
                        if (robot.Required && !robot.Immobile && !(this is TestState))
                        {
                            Game.User.Progress.IncrementStatistic(Statistic.RobotsDrowned);
                        }
                    };
                    robot.OnFall += delegate
                    {
                        if (robot.Required && !robot.Immobile && !(this is TestState))
                        {
                            Game.User.Progress.IncrementStatistic(Statistic.RobotsLost);
                        }
                    };
                }
            }
            foreach (var entry in m_robotIndicators)
            {
                Game.Screen.Elements.Add(entry.Value);
            }

            // Add placement hints
            m_placementHints = new List<InWorldInputPrompt>();
            foreach (var hint in m_placementHints)
            {
                Game.Screen.Elements.Add(hint);
            }

            // Add Buttons
            Game.Screen.Elements.Add(m_playButton);
            Game.Screen.Elements.Add(m_fastForwardButton);
            Game.Screen.Elements.Add(m_menuButton);
            Game.Screen.Elements.Add(m_timerText);

            // Add Prompts
            Game.Screen.Elements.Add(m_tweakPrompt);
            Game.Screen.Elements.Add(m_placePrompt);
            Game.Screen.Elements.Add(m_tweakUpPrompt);
            Game.Screen.Elements.Add(m_tweakDownPrompt);

            // Add hints
            Game.Screen.Elements.Add(m_playButtonHint);
            Game.Screen.Elements.Add(m_fastForwardButtonHint);

            // Run the script
            if (ScriptController != null && ScriptController.HasFunction("run"))
            {
                ScriptController.StartFunction("run", LuaArgs.Empty);
            }

            UpdateSteamController();
        }

        protected TileCoordinates? GetPotentialPlace()
        {
            if (m_gameState == GameState.Planning &&
                Level.Info.PlacementsLeft > 0 &&
                !PlaceDisabled &&
                !CheckArrows())
            {
                TileCoordinates? tileHit;
                Entity entityHit;
                Direction sideHit;
                if (RaycastLevel(out tileHit, out entityHit, out sideHit))
                {
                    if (tileHit.HasValue)
                    {
                        // Return cube placement on tile
                        var tiles = Level.Tiles;
                        var location = tileHit.Value.Move(sideHit);
                        for (int i = 0; i < 2; ++i)
                        {
                            if (tiles[location.Below()].CanPlaceOnSide(Level, location.Below(), Direction.Up) &&
                                !tiles[location].IsOccupied(Level, location) &&
                                !tiles[location.Above()].IsOccupied(Level, location.Above()))
                            {
                                if (!m_spawnMarkerLocations.Contains(location))
                                {
                                    return location;
                                }
                            }
                            location = location.Below();
                        }
                    }
                    else if (entityHit != null)
                    {
                        // Return cube placement on entity
                        var tiles = Level.Tiles;
                        TileCoordinates location;
                        if (sideHit == Direction.Up && entityHit.CanPlaceOnTop(out location))
                        {
                            if (tiles[location].IsReplaceable(Level, location) &&
                                !tiles[location].IsOccupied(Level, location) &&
                                !tiles[location.Above()].IsOccupied(Level, location.Above()))
                            {
                                if (!m_spawnMarkerLocations.Contains(location))
                                {
                                    return location;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        protected SpawnMarker GetPotentialRemove()
        {
            if (m_gameState == GameState.Planning &&
                !RemoveDisabled &&
                !CheckArrows())
            {
                if (m_selectedSpawnMarker != null)
                {
                    var indicator = m_spawnMarkerIndicators[m_selectedSpawnMarker];
                    if (indicator.TestMouse() != 0)
                    {
                        return null;
                    }
                }

                TileCoordinates? tileHit;
                Entity entityHit;
                Direction sideHit;
                if (RaycastLevel(out tileHit, out entityHit, out sideHit))
                {
                    if (entityHit != null && entityHit is SpawnMarker)
                    {
                        return (SpawnMarker)entityHit;
                    }
                }
            }
            return null;
        }

        protected SpawnMarker GetPotentialTweak()
        {
            if (m_gameState == GameState.Planning &&
                !TweakDisabled &&
                !CheckArrows())
            {
                TileCoordinates? tileHit;
                Entity entityHit;
                Direction sideHit;
                if (RaycastLevel(out tileHit, out entityHit, out sideHit))
                {
                    if (entityHit != null && entityHit is SpawnMarker)
                    {
                        return (SpawnMarker)entityHit;
                    }
                }
            }
            return null;
        }

        protected virtual bool RaycastLevel(out TileCoordinates? o_tile, out Entity o_entity, out Direction o_side)
        {
            if ((Dialog == null || !Dialog.BlockInput) && !m_playButton.TestMouse() && !m_fastForwardButton.TestMouse() && !m_menuButton.TestMouse())
            {
                var ray = BuildRay(Game.Cursor.Position, 100.0f);

                // Test against tiles
                TileCoordinates tileHitCoords;
                Direction tileHitSide;
                float tileHitDistance;
                bool tileHit = Level.RaycastTiles(ray, out tileHitCoords, out tileHitSide, out tileHitDistance, solidOpaqueSidesOnly: true);

                // Test against entities
                Entity entity;
                Direction entityHitSide;
                float entityHitDistance;
                bool entityHit = Level.RaycastEntities(ray, out entity, out entityHitSide, out entityHitDistance);

                if (tileHit && (!entityHit || tileHitDistance <= entityHitDistance))
                {
                    o_tile = tileHitCoords;
                    o_entity = null;
                    o_side = tileHitSide;
                    return true;
                }
                else if (entityHit && (!tileHit || entityHitDistance <= tileHitDistance))
                {
                    o_tile = null;
                    o_entity = entity;
                    o_side = entityHitSide;
                    return true;
                }
            }

            o_tile = null;
            o_entity = null;
            o_side = default(Direction);
            return false;
        }

        private bool CheckSkipCheat()
        {
            if (App.Arguments.GetBool("enable_cheats"))
            {
                Game.User.Progress.UsedCheats = true;
                if (Game.Keyboard.Keys[Key.S].Pressed)
                {
                    return true;
                }
            }
            return false;
        }

        private void TryBeamUp()
        {
            // Count the stopped robots
            int robots = 0;
            int robotsStopped = 0;
            foreach (Entity entity in Level.Entities)
            {
                if (entity is Robot.Robot)
                {
                    var robot = (Robot.Robot)entity;
                    if (robot.Required)
                    {
                        robots++;
                        if (robot.IsStopped)
                        {
                            robotsStopped++;
                        }
                    }
                }
            }

            // If they're all stopped
            if (robots > 0 && (robotsStopped == robots || CheckSkipCheat()))
            {
                // Beam them all up
                foreach (Entity entity in Level.Entities)
                {
                    if (entity is Robot.Robot)
                    {
                        var robot = (Robot.Robot)entity;
                        if (!robot.Immobile)
                        {
                            robot.BeamUp();
                        }
                    }
                }

                // Start the outtro
                m_gameState = GameState.Outro;
                m_outroTimer = RobotBeamUpAction.DURATION;
                SetRate(VCRRate.Play);

                m_levelCompleteDetails = new LevelCompleteDetails();
                m_levelCompleteDetails.ObstaclesRemaining = Level.Info.PlacementsLeft;
                OnOutroStarted(robotsStopped, m_levelCompleteDetails);

                // Play the jingle
                var outroPath = AssetPath.Combine(
                    AssetPath.GetDirectoryName(Level.Info.MusicPath),
                    AssetPath.GetFileNameWithoutExtension(Level.Info.MusicPath) + "_outro.ogg"
                );
                if (Assets.Exists<Music>(outroPath))
                {
                    Game.Audio.PlayMusic(outroPath, 0.1f, false);
                }
                else if (Assets.Exists<Music>("music/space_in_time_outro.ogg"))
                {
                    Game.Audio.PlayMusic("music/space_in_time_outro.ogg", 0.1f, false);
                }

                // Rumble
                if (Game.Screen.InputMethod == InputMethod.Gamepad)
                {
                    Game.ActiveGamepad.Rumble(1.0f, 0.75f);
                }
            }
        }

        private bool CheckPlace()
        {
            if ((Dialog == null || !Dialog.BlockInput))
            {
                if (Game.ActiveSteamController != null && Game.ActiveSteamController.Buttons[SteamControllerButton.InGamePlace.GetID()].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.SteamController;
                    return true;
                }
                if (Game.ActiveGamepad != null && Game.ActiveGamepad.Buttons[Game.User.Settings.GetPadBind(Bind.Place)].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Gamepad;
                    return true;
                }
                if (Game.Mouse.Buttons[Game.User.Settings.GetMouseBind(Bind.Place)].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Mouse;
                    return true;
                }
            }
            return false;
        }

        private bool CheckRemove()
        {
            if ((Dialog == null || !Dialog.BlockInput))
            {
                if (Game.ActiveSteamController != null && Game.ActiveSteamController.Buttons[SteamControllerButton.InGameRemove.GetID()].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.SteamController;
                    return true;
                }
                if (Game.ActiveGamepad != null && Game.ActiveGamepad.Buttons[Game.User.Settings.GetPadBind(Bind.Remove)].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Gamepad;
                    return true;
                }
                if (Game.Mouse.Buttons[Game.User.Settings.GetMouseBind(Bind.Remove)].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Mouse;
                    return true;
                }
            }
            return false;
        }

        private bool CheckTweak()
        {
            if ((Dialog == null || !Dialog.BlockInput))
            {
                if (Game.ActiveSteamController != null)
                {
                    if (Game.ActiveSteamController.Buttons[SteamControllerButton.InGameTweak.GetID()].Pressed)
                    {
                        Game.Screen.InputMethod = InputMethod.SteamController;
                        return true;
                    }
                }
                if (Game.ActiveGamepad != null)
                {
                    if (Game.ActiveGamepad.Buttons[Game.User.Settings.GetPadBind(Bind.Tweak)].Pressed)
                    {
                        Game.Screen.InputMethod = InputMethod.Gamepad;
                        return true;
                    }
                }
                if (Game.Mouse.Buttons[Game.User.Settings.GetMouseBind(Bind.Tweak)].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Mouse;
                    return true;
                }
            }
            return false;
        }

        private FlatDirection CalculatePlaceDirection(TileCoordinates location)
        {
            var cameraPosWS = Vector3.TransformPosition(Vector3.Zero, MathUtils.FastInverted(Game.Camera.Transform));
            var cameraPOSLS = Vector3.TransformPosition(cameraPosWS, MathUtils.FastInverted(Level.Transform));
            var locationPosLS = new Vector3(location.X + 0.5f, location.Y * 0.5f, location.Z + 0.5f);
            var xDiff = (cameraPOSLS - locationPosLS).X;
            var zDiff = (cameraPOSLS - locationPosLS).Z;
            if (Math.Abs(xDiff) > Math.Abs(zDiff))
            {
                return xDiff > 0.0f ? FlatDirection.West : FlatDirection.East;
            }
            else
            {
                return zDiff > 0.0f ? FlatDirection.North : FlatDirection.South;
            }
        }

        private void Select(SpawnMarker marker)
        {
            if (m_selectedSpawnMarker != marker)
            {
                if (m_selectedSpawnMarker != null)
                {
                    var indicator = m_spawnMarkerIndicators[m_selectedSpawnMarker];
                    indicator.Selected = false;
                    m_selectedSpawnMarker = null;
                }
                if (marker != null)
                {
                    var indicator = m_spawnMarkerIndicators[marker];
                    indicator.Selected = true;
                    m_selectedSpawnMarker = marker;
                }
            }
        }

        private void Highlight(SpawnMarker marker)
        {
            if (m_highligtedSpawnMarker != marker)
            {
                if (m_highligtedSpawnMarker != null)
                {
                    m_highligtedSpawnMarker.Highlight = false;
                    m_highligtedSpawnMarker = null;
                }
                if (marker != null)
                {
                    marker.Highlight = true;
                    m_highligtedSpawnMarker = marker;
                }
            }
        }

        private bool CheckArrows()
        {
            if (m_selectedSpawnMarker != null && m_gameState == GameState.Planning)
            {
                var indicator = m_spawnMarkerIndicators[m_selectedSpawnMarker];
                return indicator.TestMouse() != 0;
            }
            return false;
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            UpdateSteamController();

            if (App.Demo && Game.Keyboard.Keys[Key.R].Pressed)
            {
                // Resett
                WipeToState(new StartScreenState(Game));
            }

            // Update state
            if (m_gameState == GameState.Intro)
            {
                // INTRO MODE
                if (Level.TimeMachine.Time >= INTRO_DURATION)
                {
                    m_gameState = GameState.Planning;
                    SetRate(VCRRate.Pause);
                    Game.Audio.PlayMusic(Level.Info.MusicPath, 0.0f, true);
                }
            }
            else if (m_gameState == GameState.Planning)
            {
                // PLANNING MODE
                // Place blocks
                TileCoordinates? placement;
                SpawnMarker removal;
                SpawnMarker tweak;
                if (!CheckArrows())
                {
                    if (CheckPlace() && (placement = GetPotentialPlace()).HasValue)
                    {
                        // Place a cube
                        Level.Info.PlacementsLeft = Level.Info.PlacementsLeft - 1;
                        var placeDirection = CalculatePlaceDirection(placement.Value);
                        var entity = new SpawnMarker(Tile.Get(Level.Info.ItemPath), placement.Value, placeDirection, 0);
                        entity.OnSpawn += delegate (object sender, EventArgs args)
                        {
                            if (!(this is TestState))
                            {
                                Game.User.Progress.IncrementStatistic(Statistic.ObstaclesPlaced);
                            }
                        };
                        m_spawnMarkerLocations.Add(placement.Value);
                        Level.Entities.Add(entity);

                        // And the indicator
                        var indicator = new SpawnMarkerIndicator(this, entity, Game.Camera);
                        m_spawnMarkerIndicators.Add(entity, indicator);
                        Game.Screen.Elements.Add(indicator);
                        Select(null);

                        // Make noise
                        Game.Audio.PlayUISound("sound/place.wav");
                    }
                    else if (CheckRemove() && (removal = GetPotentialRemove()) != null)
                    {
                        // Delete a cube
                        if (m_selectedSpawnMarker == removal)
                        {
                            Select(null);
                        }
                        m_spawnMarkerLocations.Remove(removal.Location);
                        Level.Info.PlacementsLeft = Level.Info.PlacementsLeft + 1;
                        Level.Entities.Remove(removal);

                        // And it's indicator
                        var indicator = m_spawnMarkerIndicators[removal];
                        m_spawnMarkerIndicators.Remove(removal);
                        Game.Screen.Elements.Remove(indicator);
                        indicator.Dispose();

                        // Make noise
                        Game.Audio.PlayUISound("sound/remove.wav");
                    }
                    else if (CheckTweak())
                    {
                        tweak = GetPotentialTweak();
                        if (tweak != null && tweak != m_selectedSpawnMarker)
                        {
                            // Select an indicator
                            Select(tweak);
                            Game.Audio.PlayUISound("sound/place.wav");
                        }
                        else
                        {
                            // Deselect an indicator
                            Select(null);
                        }
                    }
                    else if (CheckPlace())
                    {
                        // Deselect an indicator
                        Select(null);
                    }
                }

                // Play
                if (CheckPlay() || (CheckFastForwardHeld() && !PlayDisabled))
                {
                    m_gameState = GameState.Playing;
                    SetRate(VCRRate.Play);
                }
            }
            else if (m_gameState == GameState.Playing)
            {
                // PLAY MODE
                if (CheckRewind())
                {
                    m_gameState = GameState.Rewinding;
                    Game.Audio.PlayUISound("sound/rewind.wav");
                    SetRate(VCRRate.Rewind, INTRO_DURATION);
                }
                else if (CheckFastForwardHeld())
                {
                    SetRate(VCRRate.FastForward);
                    TryBeamUp();
                }
                else
                {
                    if (Dialog != null && Dialog.PauseWorld)
                    {
                        SetRate(VCRRate.Pause);
                    }
                    else
                    {
                        SetRate(VCRRate.Play);
                        TryBeamUp();
                    }
                }
            }
            else if (m_gameState == GameState.Rewinding)
            {
                // REWIND MODE
                if (Level.TimeMachine.Time <= INTRO_DURATION)
                {
                    m_gameState = GameState.Planning;
                    SetRate(VCRRate.Pause);
                    if (!(this is TestState))
                    {
                        Game.User.Progress.IncrementStatistic(Statistic.Rewinds);
                    }
                }
            }
            else if (m_gameState == GameState.Outro)
            {
                // OUTRO
                m_outroTimer -= dt;
                if (m_outroTimer <= 0.0f && m_levelCompleteMessage == null && Dialog == null)
                {
                    // Show some GUI
                    m_levelCompleteMessage = new LevelCompleteMessage(Game, m_levelCompleteDetails);
                    m_levelCompleteMessage.Anchor = Anchor.CentreMiddle;
                    m_levelCompleteMessage.LocalPosition = Vector2.Zero;
                    Game.Screen.Elements.Add(m_levelCompleteMessage);
                    ShowLevelTitle = false;
                }
                else if (m_levelCompleteMessage != null && m_levelCompleteMessage.Closed && Dialog == null)
                {
                    // Finish
                    OnOutroComplete();
                }
            }

            // Update GUI visibility
            ShowLevelTitle = (Dialog == null || !(Dialog is DialogueBox)) && (m_levelCompleteMessage == null);
            ShowPlacementsLeft = (m_gameState == GameState.Intro || m_gameState == GameState.Planning) && (Dialog == null || !(Dialog is DialogueBox));
            Game.Cursor.Visible = (m_gameState == GameState.Planning) && (!PlaceDisabled || !RemoveDisabled || !TweakDisabled) && (Dialog == null || !Dialog.BlockInput);

            // Update prompts
            UpdatePrompts();

            // Update the hover preview
            {
                TileCoordinates? placement = GetPotentialPlace();
                if (placement.HasValue)
                {
                    m_previewEntity.Visible = true;
                    m_previewEntity.Location = placement.Value;
                }
                else
                {
                    m_previewEntity.Visible = false;
                }

                SpawnMarker highlight;
                if ((highlight = GetPotentialRemove()) != null || (highlight = GetPotentialTweak()) != null)
                {
                    Highlight(highlight);
                }
                else if (CheckArrows())
                {
                    Highlight(m_selectedSpawnMarker);
                }
                else
                {
                    Highlight(null);
                }
            }

            if ((Dialog == null || Dialog is DialogueBox))
            {
                // Check pause button
                if (CheckMenu())
                {
                    // Show menu
                    OnMenuRequested();
                }
                else if (CheckBack())
                {
                    // Undo
                    if (m_gameState == GameState.Planning)
                    {
                        if (m_selectedSpawnMarker != null)
                        {
                            // Deselect
                            Select(null);
                        }
                        else
                        {
                            // Show menu
                            OnMenuRequested();
                        }
                    }
                    else if (m_gameState == GameState.Playing)
                    {
                        // Rewind
                        if (!RewindDisabled)
                        {
                            m_gameState = GameState.Rewinding;
                            Game.Audio.PlayUISound("sound/rewind.wav");
                            SetRate(VCRRate.Rewind, INTRO_DURATION);
                        }
                    }
                }
            }

            // Update the dialogue box
            if (m_dialogueBox != null && !m_dialogueBox.IsClosed && Dialog == null)
            {
                ShowDialog(m_dialogueBox, false);
            }
        }

        protected abstract void OnMenuRequested();
        protected abstract void OnOutroStarted(int robotsSaved, LevelCompleteDetails o_thingsUnlocked);
        protected abstract void OnOutroComplete();

        private void UpdateSteamController()
        {
            if (Game.ActiveSteamController != null)
            {
                if ((Dialog != null && Dialog.BlockInput) || m_levelCompleteMessage != null)
                {
                    Game.ActiveSteamController.ActionSet = SteamControllerActionSet.Menu.GetID();
                }
                else
                {
                    Game.ActiveSteamController.ActionSet = SteamControllerActionSet.InGame.GetID();
                }
            }
        }

        private void ResetSteamController()
        {
            if (Game.ActiveSteamController != null)
            {
                Game.ActiveSteamController.ActionSet = SteamControllerActionSet.Menu.GetID();
            }
        }

        protected override void OnShutdown()
        {
            // Disable camera control
            CameraController.AllowUserRotate = false;
            CameraController.AllowUserZoom = false;
            Game.Cursor.Visible = false;

            // Remove buttons
            Game.Screen.Elements.Remove(m_playButton);
            m_playButton.Dispose();

            Game.Screen.Elements.Remove(m_fastForwardButton);
            m_fastForwardButton.Dispose();

            Game.Screen.Elements.Remove(m_menuButton);
            m_menuButton.Dispose();

            Game.Screen.Elements.Remove(m_timerText);
            m_timerText.Dispose();

            // Remove prompts
            Game.Screen.Elements.Remove(m_tweakPrompt);
            m_tweakPrompt.Dispose();

            Game.Screen.Elements.Remove(m_placePrompt);
            m_placePrompt.Dispose();

            Game.Screen.Elements.Remove(m_tweakUpPrompt);
            m_tweakUpPrompt.Dispose();

            Game.Screen.Elements.Remove(m_tweakDownPrompt);
            m_tweakDownPrompt.Dispose();

            // Remove hints
            Game.Screen.Elements.Remove(m_playButtonHint);
            m_playButtonHint.Dispose();

            Game.Screen.Elements.Remove(m_fastForwardButtonHint);
            m_fastForwardButtonHint.Dispose();

            // Remove indicators
            foreach (var hint in m_placementHints)
            {
                Game.Screen.Elements.Remove(hint);
                hint.Dispose();
            }
            foreach (var entry in m_robotIndicators)
            {
                Game.Screen.Elements.Remove(entry.Value);
                entry.Value.Dispose();
            }
            foreach (var entry in m_spawnMarkerIndicators)
            {
                Game.Screen.Elements.Remove(entry.Value);
                entry.Value.Dispose();
            }

            // Remove dialogue box
            if (m_dialogueBox != null)
            {
                Game.Screen.Elements.Remove(m_dialogueBox);
                m_dialogueBox.Dispose();
                m_dialogueBox = null;
            }

            // Remove level complete
            if (m_levelCompleteMessage != null)
            {
                Game.Screen.Elements.Remove(m_levelCompleteMessage);
                m_levelCompleteMessage.Dispose();
                m_levelCompleteMessage = null;
            }

            ResetSteamController();
            base.OnShutdown();
        }

        public void ShowWorldHint(TileCoordinates coordinates, WorldHintType type)
        {
            InWorldInputPrompt hint = null;
            var position = new Vector3(coordinates.X + 0.5f, coordinates.Y * 0.5f, coordinates.Z + 0.5f);
            for (int i = 0; i < m_placementHints.Count; ++i)
            {
                var existingHint = m_placementHints[i];
                if (existingHint.Position3D == position)
                {
                    hint = existingHint;
                    break;
                }
            }

            if (hint == null)
            {
                hint = new InWorldInputPrompt(Level, Game.Camera, UIFonts.Default, "", TextAlignment.Center);
                m_placementHints.Add(hint);
                hint.Position3D = position;
                Game.Screen.Elements.Add(hint);
            }
            else
            {
                hint.Visible = true;
            }

            switch (type)
            {
                case WorldHintType.Place:
                default:
                    {
                        hint.MouseButton = Game.User.Settings.GetMouseBind(Bind.Place);
                        hint.GamepadButton = Game.User.Settings.GetPadBind(Bind.Place);
                        hint.SteamControllerButton = SteamControllerButton.InGamePlace;
                        break;
                    }
                case WorldHintType.Remove:
                    {
                        hint.MouseButton = Game.User.Settings.GetMouseBind(Bind.Remove);
                        hint.GamepadButton = Game.User.Settings.GetPadBind(Bind.Remove);
                        hint.SteamControllerButton = SteamControllerButton.InGamePlace;
                        break;
                    }
                case WorldHintType.Tweak:
                    {
                        hint.MouseButton = Game.User.Settings.GetMouseBind(Bind.Tweak);
                        hint.GamepadButton = Game.User.Settings.GetPadBind(Bind.Tweak);
                        hint.SteamControllerButton = SteamControllerButton.InGameTweak;
                        break;
                    }
            }
        }

        public void HideWorldHint(TileCoordinates coordinates)
        {
            var position = new Vector3(coordinates.X + 0.5f, coordinates.Y * 0.5f, coordinates.Z + 0.5f);
            for (int i = 0; i < m_placementHints.Count; ++i)
            {
                var existingHint = m_placementHints[i];
                if (existingHint.Position3D == position)
                {
                    existingHint.Visible = false;
                    return;
                }
            }
        }

        public bool IsComplete()
        {
            int robots = 0;
            int robotsSaved = 0;
            foreach (Entity entity in Level.Entities)
            {
                if (entity is Robot.Robot)
                {
                    var robot = (Robot.Robot)entity;
                    if (robot.Required)
                    {
                        robots++;
                        if (robot.IsSaved)
                        {
                            robotsSaved++;
                        }
                    }
                }
            }
            return (robotsSaved == robots);
        }

        private bool CheckPlay()
        {
            if (PlayDisabled)
            {
                return false;
            }
            if (Dialog == null || !Dialog.BlockInput)
            {
                if (m_playButton.Pressed)
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckRewind()
        {
            if (RewindDisabled)
            {
                return false;
            }
            if (Dialog == null || !Dialog.BlockInput)
            {
                if (m_playButton.Pressed)
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckFastForwardHeld()
        {
            if (FastForwardDisabled)
            {
                return false;
            }
            if (Dialog == null || !Dialog.BlockInput)
            {
                if (m_fastForwardButton.Held)
                {
                    return true;
                }
            }
            return false;
        }

        private bool CheckMenu()
        {
            if (m_menuButton.Pressed)
            {
                return true;
            }
            else if (Game.Screen.Keyboard.Keys[Key.Escape].Pressed)
            {
                Game.Screen.InputMethod = InputMethod.Keyboard;
                return true;
            }
            else if (Game.Screen.Gamepad != null && Game.Screen.Gamepad.Buttons[GamepadButton.Start].Pressed)
            {
                Game.Screen.InputMethod = InputMethod.Gamepad;
                return true;
            }
            else if (Game.Screen.SteamController != null && Game.Screen.SteamController.Buttons[SteamControllerButton.InGameToMenu.GetID()].Pressed)
            {
                Game.Screen.InputMethod = InputMethod.SteamController;
                return true;
            }
            return false;
        }

        private void SetRate(VCRRate rate, float? limit = null)
        {
            // Apply speed
            if (rate == VCRRate.Rewind)
            {
                if (((limit.Value - Level.TimeMachine.Time) / rate.ToFloat()) > MAX_REWIND_DURATION)
                {
                    Level.TimeMachine.Rate = (limit.Value - Level.TimeMachine.Time) / MAX_REWIND_DURATION;
                    Level.TimeMachine.Limit = limit;
                }
                else
                {
                    Level.TimeMachine.Rate = rate.ToFloat();
                    Level.TimeMachine.Limit = limit;
                }
            }
            else
            {
                Level.TimeMachine.Rate = rate.ToFloat();
                Level.TimeMachine.Limit = limit;
            }
        }
    }
}

