using Dan200.Core.Animation;
using Dan200.Core.Assets;
using Dan200.Core.Async;
using Dan200.Core.Audio;
using Dan200.Core.Audio.Null;
using Dan200.Core.Audio.OpenAL;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Input.SDL2;
using Dan200.Core.Input.Steamworks;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Core.Network;
using Dan200.Core.Network.Builtin;
using Dan200.Core.Network.Steamworks;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Core.Window;
using Dan200.Core.Window.SDL2;
using Dan200.Game.Analysis;
using Dan200.Game.Arcade;
using Dan200.Game.GUI;
using Dan200.Game.Input;
using Dan200.Game.Level;
using Dan200.Game.User;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SDL2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Dan200.Game.Game
{
    public class Game : IGame
    {
        public const float SCREEN_HEIGHT = 480.0f;
        public const float DEFAULT_FOV = 0.52f;

        private User.User m_user;
        private SDL2Window m_window;
        private IAudio m_audio;
        private INetwork m_network;
        private Language m_language;

        private Camera m_camera;
        private GameAudio m_gameAudio;

        private DebugCameraController m_debugCameraController;
        private DebugMenu m_debugMenu;
        private AxisMarker m_cameraAxisMarker;

        private SkyInstance m_sky;
        private Screen m_screen;
        private Cursor m_cursor;

        private SDL2Keyboard m_keyboard;
        private SDL2Mouse m_mouse;
        private SDL2GamepadCollection m_gamepads;
        private IGamepad m_activeGamepad;
        private SteamworksSteamControllerCollection m_steamControllers;
        private IReadOnlyCollection<ISteamController> m_readOnlySteamControllers;
        private ISteamController m_activeSteamController;

        private PostEffectInstance m_postEffect;
        private RenderTexture m_worldRenderTexture;
        private UpscaleEffectInstance m_upscaleEffect;
        private BackgroundEffectInstance m_backgroundEffect;
        private RenderTexture m_upscaleRenderTexture;
        private Geometry m_fullScreenQuad;

        private State m_pendingPendingState;
        private Transition m_pendingTransition;

        private Transition m_transition;
        private State m_currentState;
        private State m_pendingState;

        private Screenshot m_pendingScreenshot;
        private SimplePromise<Screenshot> m_pendingScreenshotPromise;

        private struct PromiseTask
        {
            public Promise Promise;
            public Action OnComplete;
        }
        private List<PromiseTask> m_promiseTasks;

        private bool m_over;

        public IWindow Window
        {
            get
            {
                return m_window;
            }
        }

        public GameAudio Audio
        {
            get
            {
                return m_gameAudio;
            }
        }

        public SkyInstance Sky
        {
            get
            {
                return m_sky;
            }
            set
            {
                m_sky = value;
            }
        }

        public Camera Camera
        {
            get
            {
                return m_camera;
            }
        }

        public Screen Screen
        {
            get
            {
                return m_screen;
            }
        }

        public User.User User
        {
            get
            {
                return m_user;
            }
        }

        public INetwork Network
        {
            get
            {
                return m_network;
            }
        }

        public Language Language
        {
            get
            {
                return m_language;
            }
            set
            {
                m_language = value;
                if (m_screen != null)
                {
                    m_screen.Language = m_language;
                }
                UIFonts.FontOverride = m_language.CustomFont;
            }
        }

        public IKeyboard Keyboard
        {
            get
            {
                return m_keyboard;
            }
        }

        public IMouse Mouse
        {
            get
            {
                return m_mouse;
            }
        }

        public IReadOnlyCollection<IGamepad> Gamepads
        {
            get
            {
                return m_gamepads;
            }
        }

        public IGamepad ActiveGamepad
        {
            get
            {
                return m_activeGamepad;
            }
            set
            {
                m_activeGamepad = value;
                m_screen.Gamepad = m_activeGamepad;
            }
        }

        public IReadOnlyCollection<ISteamController> SteamControllers
        {
            get
            {
                return m_readOnlySteamControllers;
            }
        }

        public ISteamController ActiveSteamController
        {
            get
            {
                return m_activeSteamController;
            }
            set
            {
                m_activeSteamController = value;
                m_screen.SteamController = m_activeSteamController;
            }
        }

        public Cursor Cursor
        {
            get
            {
                return m_cursor;
            }
        }

        public bool RenderUI;
        public bool UseDebugCamera;

        public bool Over
        {
            get
            {
                return m_over;
            }
            set
            {
                m_over = value;
            }
        }

        public State CurrentState
        {
            get
            {
                return m_currentState;
            }
        }

        public PostEffectInstance PostEffect
        {
            get
            {
                return m_postEffect;
            }
        }

        private User.User LoadUser()
        {
            var user = new User.User(Network);
            if (App.Arguments.GetBool("defaults"))
            {
                App.Log("Using default settings");
                user.Settings.Reset();
                user.Settings.Save();
            }
            if (App.Demo)
            {
                App.Log("Demo mode. Using default progress and will not save");
                user.Progress.Reset();
            }
            user.Progress.LastPlayedVersion = App.Info.Version;
            user.Progress.Save();
            return user;
        }

        public Game()
        {
            m_promiseTasks = new List<PromiseTask>();

            m_over = false;
            RenderUI = true;
            UseDebugCamera = false;

            // Init network
            if (App.Steam)
            {
                m_network = new SteamworksNetwork(
                    AchievementExtensions.GetAllIDs(),
                    StatisticExtensions.GetAllIDs()
                );
            }
            else
            {
                m_network = new BuiltinNetwork();
            }
            if (m_network.SupportsAchievements)
            {
                m_network.SetAchievementCorner(AchievementCorner.TopRight);
            }

            // Init user
            m_user = LoadUser();

            // Init window
            var title = App.Info.Title + " " + App.Info.Version.ToString();
            if (App.Debug && App.Steam)
            {
                title += " (Steam Debug build)";
            }
            else if (App.Debug)
            {
                title += " (Debug build)";
            }

            bool fullscreen = m_user.Settings.Fullscreen;
            bool vsync = m_user.Settings.VSync;
            using (var icon = new Bitmap(Path.Combine(App.AssetPath, "icon.png")))
            {
                m_window = new SDL2Window(
                    title,
                    m_user.Settings.WindowWidth,
                    m_user.Settings.WindowHeight,
                    m_user.Settings.Fullscreen,
                    m_user.Settings.WindowMaximised,
                    m_user.Settings.VSync
                );
                m_window.SetIcon(icon);
            }
            m_window.OnClosed += delegate (object sender, EventArgs e)
            {
                Over = true;
            };
            m_window.OnResized += delegate (object sender, EventArgs e)
            {
                Resize();
                if (!m_window.Fullscreen)
                {
                    if (m_window.Maximised)
                    {
                        m_user.Settings.WindowMaximised = true;
                    }
                    else
                    {
                        m_user.Settings.WindowMaximised = false;
                        m_user.Settings.WindowWidth = m_window.Width;
                        m_user.Settings.WindowHeight = m_window.Height;
                    }
                    m_user.Settings.Save();
                }
            };

            // Init audio
            if (App.Arguments.GetBool("nosound"))
            {
                m_audio = new NullAudio();
            }
            else
            {
                m_audio = new OpenALAudio();
            }
            m_audio.EnableSound = m_user.Settings.EnableSound;
            m_audio.SoundVolume = m_user.Settings.SoundVolume / 11.0f;
            m_audio.EnableMusic = m_user.Settings.EnableMusic;
            m_audio.MusicVolume = m_user.Settings.MusicVolume / 11.0f;
            m_gameAudio = new GameAudio(m_audio);

            // Init input
            m_keyboard = new SDL2Keyboard(m_window);
            m_mouse = new SDL2Mouse(m_window);
            m_gamepads = new SDL2GamepadCollection(m_window);
            m_activeGamepad = null;
            if (App.Steam)
            {
                m_steamControllers = new SteamworksSteamControllerCollection(
                    m_window,
                    SteamControllerActionSetExtensions.GetAllIDs(),
                    SteamControllerButtonExtensions.GetAllIDs(),
                    SteamControllerJoystickExtensions.GetAllIDs(),
                    SteamControllerAxisExtensions.GetAllIDs()
                );
                m_readOnlySteamControllers = m_steamControllers;
                m_activeSteamController = null;
            }
            else
            {
                m_steamControllers = null;
                m_readOnlySteamControllers = new List<ISteamController>(0).ToReadOnly();
                m_activeSteamController = null;
            }

            // Init tiles
            Tiles.Init();

            // Load early assets
            var earlyAssetFileStore = new FolderFileStore(Path.Combine(App.AssetPath, "early"));
            var earlyAssets = new FileAssetSource("early", earlyAssetFileStore);
            Assets.AddSource(earlyAssets);
            Assets.LoadAll();

            // Find mods
            Mods.Refresh(Network);
            if (Network.SupportsWorkshop)
            {
                // See if any mods are worthy of the popular mod achievement
                var myModIDs = new List<ulong>();
                foreach (var mod in Mods.AllMods)
                {
                    if (mod.Source == ModSource.Editor &&
                        mod.SteamWorkshopID.HasValue)
                    {
                        myModIDs.Add(mod.SteamWorkshopID.Value);
                    }
                }
                if (myModIDs.Count > 0)
                {
                    QueuePromiseTask(
                        m_network.Workshop.GetItemInfo(myModIDs.ToArray()),
                        delegate (Promise<WorkshopItemInfo[]> result)
                        {
                            if (result.Status == Status.Complete)
                            {
                                var infos = result.Result;
                                int subs = 0;
                                for (int i = 0; i < infos.Length; ++i)
                                {
                                    var info = infos[i];
									if (info.AuthorID == m_network.LocalUser.ID &&
									    info.UpVotes >= info.DownVotes)
                                    {
                                        subs = Math.Max(info.TotalSubscribers, subs);
                                    }
                                }
                                int oldSubs = User.Progress.GetStatistic(Statistic.MostPopularModSubscriptions);
                                if (subs >= 25)
                                {
                                    User.Progress.SetStatistic(Statistic.MostPopularModSubscriptions, subs);
                                    User.Progress.UnlockAchievement(Achievement.CreatePopularMod);
                                    User.Progress.Save();
                                }
                                else if (subs > oldSubs)
                                {
                                    User.Progress.SetStatistic(Statistic.MostPopularModSubscriptions, subs);
                                    User.Progress.IndicateAchievementProgress(Achievement.CreatePopularMod, subs, 25);
                                    User.Progress.Save();
                                }
                            }
                        }
                    );
                }
            }

            // Load language
            SelectLanguage();

            // Load debug stuff
            m_debugCameraController = new DebugCameraController(this);

            // Load game
            Load();
        }

        public void Dispose()
        {
            // Dispose state
            if (m_pendingState == null)
            {
                m_currentState.Shutdown();
            }
            m_currentState.PostShutdown();

            // Dispose the rest
            m_fullScreenQuad.Dispose();
            if (m_audio is IDisposable)
            {
                ((IDisposable)m_audio).Dispose();
            }
            m_screen.Dispose();
            m_window.Dispose();
            if (m_steamControllers != null)
            {
                m_steamControllers.Dispose();
            }
            if (m_pendingScreenshot != null)
            {
                m_pendingScreenshot.Dispose();
            }
            m_cameraAxisMarker.Dispose();

            // Shutdown animation
            LuaAnimation.UnloadAll();
        }

        public void SelectLanguage()
        {
            // Choose language
            var languageCode = User.Settings.Language;
            if (User.Settings.Language == "system")
            {
                languageCode = Network.LocalUser.Language;
            }
            var newLanguage = Language.GetMostSimilarTo(languageCode);
            if (m_language == null || m_language.Code != newLanguage.Code)
            {
                App.Log("Using language {0} ({1})", newLanguage.Code, newLanguage.EnglishName);
            }

            // Set language
            Language = newLanguage;
        }

        public void Resize()
        {
            m_window.MakeCurrent();

            var aspectRatio = (float)Window.Width / (float)Window.Height;
            m_screen.Height = SCREEN_HEIGHT;
            m_screen.Width = aspectRatio * SCREEN_HEIGHT;
            m_camera.AspectRatio = aspectRatio;

            var width = Math.Min(Window.Width, User.Settings.FullscreenWidth);
            var height = Math.Min(Window.Height, User.Settings.FullscreenHeight);
            int scale = User.Settings.AAMode == AntiAliasingMode.SSAA ? 2 : 1;
            App.Log("Resolution changed to {0}x{1} (AA:{2})", width, height, User.Settings.AAMode);
            m_worldRenderTexture.Resize(scale * width, scale * height);
            m_upscaleRenderTexture.Resize(width, height);

            m_screen.PixelWidth = width;
            m_screen.PixelHeight = height;
        }

        public void QueuePromiseTask<TPromise>(TPromise promise, Action<TPromise> onComplete) where TPromise : Promise
        {
            var task = new PromiseTask();
            task.Promise = promise;
            task.OnComplete = delegate ()
            {
                onComplete.Invoke(promise);
            };
            m_promiseTasks.Add(task);
        }

        private Geometry CreateFullscreenQuad()
        {
            var geometry = new Geometry(Primitive.Triangles, 4, 6);
            geometry.Add2DQuad(
                new Vector2(-1.0f, 1.0f), new Vector2(1.0f, -1.0f),
                new Quad(0.0f, 1.0f, 1.0f, -1.0f)
            );
            geometry.Rebuild();
            return geometry;
        }

        private void Load()
        {
            // Bind OpenGL
            m_window.MakeCurrent();

            // Set default OpenGL options
            GL.Viewport(0, 0, Window.Width, Window.Height);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(true);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            GL.Enable(EnableCap.AlphaTest);
            GL.AlphaFunc(AlphaFunction.Greater, 0.0f);

            GL.LineWidth(2.0f);

            // Create render stuff
            var width = Math.Min(Window.Width, User.Settings.FullscreenWidth);
            var height = Math.Min(Window.Height, User.Settings.FullscreenHeight);
            var aamode = User.Settings.AAMode;
            m_postEffect = new PostEffectInstance(User.Settings);
            m_postEffect.Gamma = User.Settings.Gamma;

            int scale = (aamode == AntiAliasingMode.SSAA) ? 2 : 1;
            m_worldRenderTexture = new RenderTexture(scale * width, scale * height, true);
            m_upscaleEffect = new UpscaleEffectInstance();
            m_backgroundEffect = new BackgroundEffectInstance();
            m_upscaleRenderTexture = new RenderTexture(width, height, true);
            m_fullScreenQuad = CreateFullscreenQuad();
            m_cameraAxisMarker = new AxisMarker();

            // Create camera
            var aspectRatio = (float)Window.Width / (float)Window.Height;
            m_camera = new Camera(Matrix4.Identity, DEFAULT_FOV, aspectRatio);

            // Create screen
            m_screen = new Screen(
                Mouse, Keyboard, Language, m_window, m_audio,
                aspectRatio * SCREEN_HEIGHT, SCREEN_HEIGHT,
                width, height
            );

            m_cursor = new Cursor();
            m_cursor.Visible = false;
            m_screen.Elements.Add(m_cursor);

            m_debugMenu = new DebugMenu(this);
            m_debugMenu.Visible = false;
            m_screen.Elements.Add(m_debugMenu);

            // Add the rest of the asset sources:
            // Add the base assets
            var baseAssetFileStore = new FolderFileStore(Path.Combine(App.AssetPath, "base"));
            var baseAssets = new FileAssetSource("base", baseAssetFileStore);
            Assets.AddSource(baseAssets);

            // Add the main assets
            var mainAssetFileStore = new FolderFileStore(Path.Combine(App.AssetPath, "main"));
            var mainAssets = new FileAssetSource("main", mainAssetFileStore);
            Assets.AddSource(mainAssets);

            // Add the temp assets (used by the editor)
            var tempAssetFileStore = new LocatedFileStore(
                new FolderFileStore(Path.Combine(App.SavePath, "editor/temp")),
                "temp"
            );
            var tempAssets = new FileAssetSource("temp", tempAssetFileStore);
            Assets.AddSource(tempAssets);

            // Add autoload mod assets
            foreach (Mod mod in Mods.AllMods)
            {
                if (mod.AutoLoad)
                {
                    Assets.AddSource(mod.Assets);
                    mod.Loaded = true;
                }
            }

            // Create initial loading state
            m_currentState = CreateInitialState();
            m_pendingState = null;
            m_currentState.PreInit(null, null);
            m_currentState.Reveal();
            m_currentState.Init();
        }

        private void GuessCampaign(string levelPath, Mod mod, out Campaign o_campaign, out int o_levelIndex)
        {
            foreach (var campaign in Assets.List<Campaign>("campaigns", (mod != null) ? mod.Assets : null))
            {
                for (int i = 0; i < campaign.Levels.Count; ++i)
                {
                    if (campaign.Levels[i] == levelPath)
                    {
                        o_campaign = campaign;
                        o_levelIndex = i;
                        return;
                    }
                }
            }
            o_campaign = null;
            o_levelIndex = -1;
        }

        private State CreateInitialState()
        {
            // Preload a mod from the command line
            Mod startupMod = null;
            if (App.Arguments.ContainsKey("mod"))
            {
                var modName = App.Arguments.GetString("mod");
                foreach (var mod in Mods.AllMods)
                {
                    if (mod.Source == ModSource.Editor &&
                        Path.GetFileName(modName) == modName)
                    {
                        startupMod = mod;
                        if (!startupMod.Loaded)
                        {
                            Assets.AddSource(startupMod.Assets);
                            startupMod.Loaded = true;
                        }
                        break;
                    }
                }
                if (startupMod == null)
                {
                    App.Log("Error: No editor mod named {0}", modName);
                }
            }

            return new LoadState(this, delegate ()
            {
                if (App.Debug && App.Arguments.ContainsKey("analysis"))
                {
                    // Analysis
                    return new AnalysisState(this);
                }

                /*
                if (App.Debug && App.Arguments.ContainsKey("sky"))
                {
                    // Edit sky
                    var levelPath = App.Arguments.GetString("sky");
                    return new SkyEditor(this, levelPath);
                }
				*/

                if (App.Arguments.ContainsKey("arcade"))
                {
                    // Arcade
                    return new ArcadeState(this);
                }

                if (startupMod != null || App.Debug)
                {
                    if (App.Arguments.ContainsKey("level"))
                    {
                        // Edit level
                        var levelPath = App.Arguments.GetString("level");
                        Campaign campaign;
                        int levelIndex;
                        GuessCampaign(levelPath, startupMod, out campaign, out levelIndex);
                        if (Assets.Exists<LevelData>(levelPath))
                        {
                            return new TestState(this, startupMod, campaign, levelIndex, levelPath, levelPath);
                        }
                        else
                        {
                            return new TestState(this, startupMod, campaign, levelIndex, "levels/template.level", levelPath);
                        }
                    }
                    else if (App.Arguments.ContainsKey("cutscene"))
                    {
                        // Preview cutscene
                        var cutscenePath = App.Arguments.GetString("cutscene");
                        return new CutsceneState(this, startupMod, cutscenePath, CutsceneContext.Test);
                    }
                    else if (startupMod != null)
                    {
                        // Edit mod
                        return new ModEditorState(this, startupMod);
                    }
                }

                // Normal startup
                return new StartScreenState(this);
            });
        }

        public void ChangeState(State newState, Transition transition)
        {
            m_pendingPendingState = newState;
            m_pendingTransition = transition;
        }

        public void HandleEvent(ref SDL.SDL_Event e)
        {
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_QUIT:
                    {
                        Over = true;
                        break;
                    }
                default:
                    {
                        m_window.HandleEvent(ref e);
                        m_keyboard.HandleEvent(ref e);
                        m_mouse.HandleEvent(ref e);
                        m_gamepads.HandleEvent(ref e);
                        break;
                    }
            }
        }

        public void Update(float dt)
        {
            dt = Math.Min(dt, 0.1f);

            // Bind OpenGL
            m_window.MakeCurrent();

            // Update input
            m_keyboard.Update();
            m_mouse.Update();
            m_gamepads.Update();
            if (m_steamControllers != null)
            {
                m_steamControllers.Update();
            }

            if (m_steamControllers != null)
            {
                // Choose an active steampad
                if (ActiveSteamController == null || !ActiveSteamController.Connected)
                {
                    // No steampad connected
                    if (User.Settings.EnableSteamController && m_currentState.EnableGamepad)
                    {
                        ActiveSteamController = SteamControllers.FirstOrDefault(pad => pad.Connected);
                        if (ActiveSteamController != null && !(m_currentState is TestState))
                        {
                            Screen.InputMethod = InputMethod.SteamController;
                        }
                    }
                    else
                    {
                        ActiveSteamController = null;
                    }
                }
                else
                {
                    // Steampad connected
                    if (!User.Settings.EnableSteamController || !m_currentState.EnableGamepad)
                    {
                        // Disable pad if necessary
                        ActiveSteamController = null;
                    }
                    else
                    {
                        // Switch pad if Accept pressed
                        foreach (var steampad in SteamControllers)
                        {
                            if (steampad.Connected &&
                                steampad.Buttons[SteamControllerButton.MenuSelect.GetID()].Pressed)
                            {
                                ActiveSteamController = steampad;
                                Screen.InputMethod = InputMethod.SteamController;
                                break;
                            }
                        }
                    }
                }
            }

            // Choose an active gamepad
            if (ActiveGamepad == null || !ActiveGamepad.Connected)
            {
                // No Pad connected
                if (User.Settings.EnableGamepad && m_currentState.EnableGamepad)
                {
                    ActiveGamepad = Gamepads.FirstOrDefault(pad => pad.Connected);
                    if (ActiveGamepad != null)
                    {
                        ActiveGamepad.EnableRumble = User.Settings.EnableGamepadRumble;
                        if (User.Settings.GamepadPromptType != GamepadType.Unknown)
                        {
                            ActiveGamepad.Type = User.Settings.GamepadPromptType;
                        }
                        if (!(m_currentState is TestState) && Screen.InputMethod != InputMethod.SteamController)
                        {
                            Screen.InputMethod = InputMethod.Gamepad;
                        }
                    }
                }
                else
                {
                    ActiveGamepad = null;
                }
            }
            else
            {
                // Pad connected
                if (!User.Settings.EnableGamepad || !m_currentState.EnableGamepad)
                {
                    // Disable pad if necessary
                    ActiveGamepad = null;
                }
                else
                {
                    // Switch pad if Start or A pressed
                    foreach (var gamepad in Gamepads)
                    {
                        if (gamepad.Connected &&
                            gamepad.Buttons[GamepadButton.Start].Pressed ||
                            gamepad.Buttons[GamepadButton.A].Pressed)
                        {
                            ActiveGamepad = gamepad;
                            ActiveGamepad.EnableRumble = User.Settings.EnableGamepadRumble;
                            if (User.Settings.GamepadPromptType != GamepadType.Unknown)
                            {
                                ActiveGamepad.Type = User.Settings.GamepadPromptType;
                            }
                            if (Screen.InputMethod != InputMethod.SteamController)
                            {
                                Screen.InputMethod = InputMethod.Gamepad;
                            }
                            break;
                        }
                    }
                }
            }

            // Update tasks
            for (int i = m_promiseTasks.Count - 1; i >= 0; --i)
            {
                var task = m_promiseTasks[i];
                if (task.Promise.Status != Status.Waiting)
                {
                    task.OnComplete.Invoke();
                    m_promiseTasks.RemoveAt(i);
                }
            }

            // Update sound
            if (m_audio is OpenALAudio)
            {
                ((OpenALAudio)m_audio).Update(dt);
            }

            // Toggle fullscreen
            if ((
                    (Keyboard.Keys[Key.LeftAlt].Held || Keyboard.Keys[Key.RightAlt].Held) &&
                    Keyboard.Keys[Key.Return].Pressed
                ) ||
                (
                    App.Platform == Platform.OSX &&
                    (Keyboard.Keys[Key.LeftGUI].Held || Keyboard.Keys[Key.RightGUI].Held) &&
                    Keyboard.Keys[Key.W].Pressed
                ))
            {
                Window.Fullscreen = !Window.Fullscreen;
                User.Settings.Fullscreen = Window.Fullscreen;
                User.Settings.Save();
            }

            // Update screen
            m_screen.Update(dt);

            // Update state
            if (m_pendingPendingState != null)
            {
                m_pendingState = m_pendingPendingState;
                m_pendingState.PreInit(m_currentState, m_pendingTransition);
                m_currentState.Shutdown();
                m_pendingPendingState = null;

                m_transition = m_pendingTransition;
                m_transition.Init(m_currentState, m_pendingState);
                m_pendingTransition = null;
            }

            if (m_transition != null)
            {
                if (m_transition.Complete)
                {
                    m_currentState.PostShutdown();
                    m_pendingState.Init();
                    m_transition.Shutdown();
                    m_currentState = m_pendingState;
                    m_pendingState = null;
                    m_transition = null;
                }
                else
                {
                    m_currentState.PostUpdate(dt);
                    m_pendingState.PreUpdate(dt);
                    m_transition.Update(dt);
                }
            }

            if (m_transition == null)
            {
                m_currentState.Update(dt);
                m_currentState.PopulateCamera(Camera);
            }
            else
            {
                m_transition.PopulateCamera(Camera);
            }

            var cameraTransInv = Camera.Transform;
            MathUtils.FastInvert(ref cameraTransInv);
            Audio.Listener.Transform = cameraTransInv;
            m_cameraAxisMarker.Transform = cameraTransInv;

            // Update sky
            if (m_sky != null)
            {
                m_sky.Animate();
            }

            // Update sound
            m_gameAudio.Update();

            // Update debug camera
            if (UseDebugCamera)
            {
                m_debugCameraController.Update(dt);
                m_debugCameraController.Populate(Camera);
            }
        }

        public void Render()
        {
            // Bind OpenGL
            m_window.MakeCurrent();

            // ----------
            // DRAW WORLD
            // ----------

            // Clear world render texture
            m_worldRenderTexture.Bind();
            var clearColor = (m_sky != null) ? m_sky.BackgroundColour : Vector3.Zero;
            GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            App.CheckOpenGLError();

            // Draw the skybox
            if (m_sky != null)
            {
                var backgroundImage = m_sky.Sky.BackgroundImage;
                if (backgroundImage != null)
                {
                    // Draw background image
                    GL.DepthMask(false);
                    var backgroundTexture = Texture.Get(backgroundImage, true);
                    m_backgroundEffect.Texture = backgroundTexture;
                    m_backgroundEffect.Bind();
                    m_fullScreenQuad.Draw();
                    GL.DepthMask(true);
                }

                m_sky.DrawBackground(m_camera);
                GL.Clear(ClearBufferMask.DepthBufferBit);
            }

            // Draw states
            if (m_transition != null)
            {
                m_currentState.PostDraw();
                m_pendingState.PreDraw();
            }
            else
            {
                m_currentState.Draw();
            }

            // Draw the camera marker
            if (UseDebugCamera && m_debugMenu.Visible)
            {
                m_cameraAxisMarker.Draw(m_camera);
            }

            m_worldRenderTexture.Unbind();

            // -----------------
            // POSTPROCESS WORLD
            // -----------------

            // Clear screen
            m_upscaleRenderTexture.Bind();
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Draw texture to screen
            m_postEffect.Texture = m_worldRenderTexture;
            m_postEffect.Bind();
            m_fullScreenQuad.Draw();

            // --------
            // DRAW GUI
            // --------

            if (RenderUI)
            {
                // Draw GUI
                m_screen.DrawExcept(m_debugMenu, m_cursor);
                m_screen.DrawOnly(m_cursor);
            }

            // Draw debug GUI
            m_screen.DrawOnly(m_debugMenu);

            if (RenderUI)
            {
                // -----------
                // DRAW 3D GUI
                // -----------

                // Clear world render texture
                m_worldRenderTexture.Bind();
                GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                App.CheckOpenGLError();

                // Draw 3D GUI to offscreen buffer
                m_screen.Draw3D();

                // ------------------
                // POSTPROCESS 3D GUI
                // ------------------

                // Clear screen
                m_upscaleRenderTexture.Bind();

                // Draw texture to screen
                m_postEffect.Texture = m_worldRenderTexture;
                m_postEffect.Bind();
                m_fullScreenQuad.Draw();
            }

            // -----------------
            // UPSCALE TO SCREEN
            // -----------------

            // Clear screen
            m_upscaleRenderTexture.Unbind();
            GL.Viewport(0, 0, Window.Width, Window.Height);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            // Draw texture to screen
            m_upscaleEffect.Texture = m_upscaleRenderTexture;
            m_upscaleEffect.Bind();
            m_fullScreenQuad.Draw();

            // ------
            // FINISH
            // ------

            if (m_pendingScreenshot != null)
            {
                // Take screenshot
                GL.Finish();
                TakeScreenshot();
            }

            // Flip OpenGL
            m_window.SwapBuffers();
        }

        public Promise<Screenshot> QueueScreenshot(int customWidth = 0, int customHeight = 0)
        {
            m_pendingScreenshot = new Screenshot(customWidth, customHeight);
            m_pendingScreenshotPromise = new SimplePromise<Screenshot>();
            return m_pendingScreenshotPromise;
        }

        private void TakeScreenshot()
        {
            // Capture image
            var bitmap = new Bitmap(Window.Width, Window.Height);
            using (var bits = bitmap.Lock())
            {
                try
                {
                    GL.PixelStore(PixelStoreParameter.UnpackRowLength, bits.Stride / bits.BytesPerPixel);
                    GL.ReadPixels(
                        0, 0,
                        Window.Width, Window.Height,
                        (bits.BytesPerPixel == 4) ? PixelFormat.Rgba : PixelFormat.Rgb,
                        PixelType.UnsignedByte,
                        bits.Data
                    );
                }
                finally
                {
                    GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
                    App.CheckOpenGLError();
                }
            }

            // Flip image
            bitmap.FlipY();

            if (m_pendingScreenshot.Width > 0 && m_pendingScreenshot.Height > 0)
            {
                // Resize image
                var resizedBitmap = bitmap.Resize(m_pendingScreenshot.Width, m_pendingScreenshot.Height, true, true);
                bitmap.Dispose();
                bitmap = resizedBitmap;
            }
            else
            {
                // Use original size
                m_pendingScreenshot.Width = bitmap.Width;
                m_pendingScreenshot.Height = bitmap.Height;
            }

            // Complete the promise
            m_pendingScreenshot.Bitmap = bitmap;
            m_pendingScreenshotPromise.Succeed(m_pendingScreenshot);
            m_pendingScreenshotPromise = null;
            m_pendingScreenshot = null;
        }
    }
}
