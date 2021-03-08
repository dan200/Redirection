using Dan200.Core.Animation;
using Dan200.Core.Assets;
using Dan200.Core.Async;
using Dan200.Core.Audio;
using Dan200.Core.Computer;
using Dan200.Core.Computer.Devices.DiskDrive;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.Game;
using Dan200.Game.GUI;
using Dan200.Game.Input;
using Dan200.Game.Level;
using Dan200.Game.User;
using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dan200.Game.Arcade
{
    public class ArcadeState : LevelState
    {
        private const long RAM = 512000;
        private const float FPS = 60.0f;
        private const int SPEAKER_SAMPLE_RATE = 16384;

        private AnimatedCameraController m_animatedCamera;
        private ArcadeRobot m_robot;

        private Computer m_computer;
        private RobotDevices m_devices;
        private float m_frameTimer;

        private Bitmap m_displayBitmap;
        private BitmapTexture m_displayTexture;

        private ICustomPlayback m_speakerOutput;

        private InputPrompt m_diskSelectPrompt;
        private InputPrompt m_backPrompt;

        private InputPrompt m_aPrompt;
        private InputPrompt m_bPrompt;

        private Key m_zKey;
        private Key m_xKey;
        private Key m_wKey;
        private Key m_sKey;
        private Key m_aKey;
        private Key m_dKey;

        private DiskSelector m_diskSelector;
        private ArcadeDisk m_activeDisk;
        private int m_cachedScore;
        private Mod m_activeDiskMod;

        private Guid GetComputerID()
        {
            var guid = Game.User.Settings.ArcadeGUID;
            if (guid.Equals(Guid.Empty))
            {
                guid = Guid.NewGuid();
                Game.User.Settings.ArcadeGUID = guid;
                Game.User.Settings.Save();
            }
            return guid;
        }

        public ArcadeState(Game.Game game) : base(game, "levels/startscreen.level", LevelOptions.Menu)
        {
            // Create computer
            m_computer = new Computer(GetComputerID());
            m_computer.Memory.TotalMemory = RAM;
            m_computer.Host = App.Info.Title + " " + App.Info.Version;
            m_computer.Output = new LogWriter(LogLevel.User);
            m_computer.ErrorOutput = new LogWriter(LogLevel.Error);
            m_computer.SetPowerStatus(PowerStatus.Charged, 1.0);

            m_devices = new RobotDevices();
            m_computer.Ports.Add(m_devices);

            // Create some textures
            m_displayBitmap = new Bitmap(m_devices.Display.Width, m_devices.Display.Height);
            m_displayTexture = new BitmapTexture(m_displayBitmap);
            UpdateDisplay();

            // Create camera
            m_animatedCamera = new AnimatedCameraController(Level.TimeMachine);

            // Create prompts
            {
                m_zKey = Key.Z.RemapToLocal();
                m_xKey = Key.X.RemapToLocal();
                m_wKey = Key.W.RemapToLocal();
                m_sKey = Key.S.RemapToLocal();
                m_aKey = Key.A.RemapToLocal();
                m_dKey = Key.D.RemapToLocal();

                m_backPrompt = new InputPrompt(UIFonts.Smaller, Game.Language.Translate("menus.back"), TextAlignment.Right);
                m_backPrompt.MouseButton = MouseButton.Left;
                m_backPrompt.Key = Key.Escape;
                m_backPrompt.GamepadButton = GamepadButton.Back;
                m_backPrompt.SteamControllerButton = SteamControllerButton.ArcadeBack;
                m_backPrompt.Anchor = Anchor.BottomRight;
                m_backPrompt.LocalPosition = new Vector2(-16.0f, -16.0f - m_backPrompt.Font.Height);
                m_backPrompt.OnClick += delegate
                {
                    GoBack();
                };

                m_diskSelectPrompt = new InputPrompt(UIFonts.Smaller, Game.Language.Translate("menus.arcade.swap_disk"), TextAlignment.Right);
                m_diskSelectPrompt.Key = Key.Tab;
                m_diskSelectPrompt.GamepadButton = GamepadButton.Start;
                m_diskSelectPrompt.SteamControllerButton = SteamControllerButton.ArcadeSwapDisk;
                m_diskSelectPrompt.Anchor = Anchor.BottomRight;
                m_diskSelectPrompt.LocalPosition = new Vector2(-16.0f, -16.0f - m_backPrompt.Font.Height - m_diskSelectPrompt.Height);

                m_bPrompt = new InputPrompt(UIFonts.Smaller, Game.Language.Translate("menus.arcade.b"), TextAlignment.Left);
                m_bPrompt.Key = m_xKey;
                m_bPrompt.GamepadButton = GamepadButton.B;
                m_bPrompt.SteamControllerButton = SteamControllerButton.ArcadeB;
                m_bPrompt.Anchor = Anchor.BottomLeft;
                m_bPrompt.LocalPosition = new Vector2(16.0f, -16.0f - m_bPrompt.Height);

                m_aPrompt = new InputPrompt(UIFonts.Smaller, Game.Language.Translate("menus.arcade.a"), TextAlignment.Left);
                m_aPrompt.Key = m_zKey;
                m_aPrompt.GamepadButton = GamepadButton.A;
                m_aPrompt.SteamControllerButton = SteamControllerButton.ArcadeA;
                m_aPrompt.Anchor = Anchor.BottomLeft;
                m_aPrompt.LocalPosition = new Vector2(16.0f, -16.0f - 2.0f * m_bPrompt.Height);
            }

            // Create disk selector
            m_diskSelector = null;
            m_activeDisk = null;
            m_activeDiskMod = null;
            m_cachedScore = 0;
        }

        protected override string GetMusicPath(State previous, Transition transition)
        {
            return null;
        }

        protected override void OnReveal()
        {
            base.OnReveal();

            // Create robot
            m_robot = new ArcadeRobot(
                Model.Get("models/entities/new/red_robot.obj")
            );
            Level.Entities.Add(m_robot);

            // Start animation
            StartCameraAnimation("animation/menus/options/camera.anim.lua");
            m_robot.StartAnimation(LuaAnimation.Get("animation/menus/options/robot_arcade.anim.lua"), false);
            m_robot.ScreenTexture = m_displayTexture;

            // Reposition sky
            Game.Sky.ForegroundModelTransform = Matrix4.CreateTranslation(-5.0f, 5.0f, -20.0f);
        }

        protected void StartCameraAnimation(string animPath)
        {
            m_animatedCamera.Play(LuaAnimation.Get(animPath));
        }

        protected override void OnInit()
        {
            base.OnInit();

            // Add GUI elements
            Game.Screen.Elements.Add(m_backPrompt);
            Game.Screen.Elements.Add(m_diskSelectPrompt);

            Game.Screen.Elements.Add(m_aPrompt);
            Game.Screen.Elements.Add(m_bPrompt);

            // Start audio
            m_speakerOutput = Game.Audio.Audio.PlayCustom(m_devices, 1, SPEAKER_SAMPLE_RATE);

            // Choose a disk
            var allDisks = ArcadeUtils.GetAllDisks();
            var lastPlayedDiskID = Game.User.Progress.GetLastPlayedArcadeGame();
            DiskWithMod lastPlayedDisk = null;
            if (lastPlayedDiskID != 0)
            {
                for (int i = 0; i < allDisks.Count; ++i)
                {
                    var disk = allDisks[i];
                    if (ArcadeUtils.IsDiskUnlocked(disk.Disk, disk.Mod, Game.User.Progress) &&
                        disk.Disk.ID == lastPlayedDiskID)
                    {
                        lastPlayedDisk = disk;
                        break;
                    }
                }
            }
            if (lastPlayedDisk == null)
            {
                lastPlayedDisk = allDisks.FirstOrDefault(
                    disk => ArcadeUtils.IsDiskUnlocked(disk.Disk, disk.Mod, Game.User.Progress )
                );
            }
            if (lastPlayedDisk != null)
            {
                SelectDisk(lastPlayedDisk.Disk, lastPlayedDisk.Mod);
            }

            // Turn on the computer
            m_computer.TurnOn();
            UpdateDisplay();
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();

            // Steam controller
            if (Game.ActiveSteamController != null)
            {
                Game.ActiveSteamController.ActionSet = SteamControllerActionSet.Menu.GetID();
            }

            // Shutdown the computer
            m_computer.Dispose();
            m_computer = null;

            // Stop audio
            if (m_speakerOutput != null)
            {
                m_speakerOutput.Stop();
            }

            // Dispose elements
            m_displayTexture.Dispose();
            m_displayBitmap.Dispose();

            Game.Screen.Elements.Remove(m_backPrompt);
            m_backPrompt.Dispose();

            Game.Screen.Elements.Remove(m_diskSelectPrompt);
            m_diskSelectPrompt.Dispose();

            Game.Screen.Elements.Remove(m_aPrompt);
            m_aPrompt.Dispose();

            Game.Screen.Elements.Remove(m_bPrompt);
            m_bPrompt.Dispose();

            // Remove disk selector
            if (m_diskSelector != null)
            {
                Game.Screen.Elements.Remove(m_diskSelector);
                m_diskSelector.Dispose();
                m_diskSelector = null;
            }
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

            // Update steam controller
            if (Game.ActiveSteamController != null)
            {
                Game.ActiveSteamController.ActionSet = (Game.Screen.ModalDialog != null) ?
                    SteamControllerActionSet.Menu.GetID() :
                    SteamControllerActionSet.Arcade.GetID();
            }

            // Update 
            if (CheckBack())
            {
                GoBack();
            }
            if (CheckDiskSelect())
            {
                ShowDiskSelect();
            }

            var display = m_devices.Display;
            var displayImage = display.Image;
            var displayVer = (displayImage != null) ? displayImage.Version : 0;

            // Input
            // Gampad
            var gamepad = m_devices.Gamepad;
            gamepad.UpdateAxis(0, CheckX());
            gamepad.UpdateAxis(1, CheckY());
            gamepad.UpdateButton(0, CheckA());
            gamepad.UpdateButton(1, CheckB());

            // Keyboard
            var keyboard = m_devices.Keyboard;
            if (Game.Screen.ModalDialog == null)
            {
                // Text
                if (Game.Keyboard.Text.Length > 0)
                {
                    Game.Screen.InputMethod = InputMethod.Keyboard;
                    foreach (var c in Game.Keyboard.Text)
                    {
                        if (!char.IsControl(c) && !char.IsSurrogate(c))
                        {
                            keyboard.Char(c);
                        }
                    }
                }

                // Keys
                foreach (Key key in Enum.GetValues(typeof(Key)))
                {
                    int? code = TranslateKey(key);
                    if (code.HasValue)
                    {
                        if (Game.Keyboard.Keys[key].Pressed)
                        {
                            Game.Screen.InputMethod = InputMethod.Keyboard;
                            keyboard.KeyDown(code.Value, false);
                        }
                        else if (Game.Keyboard.Keys[key].Repeated)
                        {
                            Game.Screen.InputMethod = InputMethod.Keyboard;
                            keyboard.KeyDown(code.Value, true);
                        }
                        else if (Game.Keyboard.Keys[key].Released)
                        {
                            Game.Screen.InputMethod = InputMethod.Keyboard;
                            keyboard.KeyUp(code.Value);
                        }
                    }
                }

                // Terminate/Reboot
                if (Game.Keyboard.Keys[Key.LeftCtrl].Held || Game.Keyboard.Keys[Key.RightCtrl].Held)
                {
                    if (Game.Keyboard.Keys[Key.T].Pressed)
                    {
                        Game.Screen.InputMethod = InputMethod.Keyboard;
                        m_computer.Events.Queue("terminate");
                    }
                    if (Game.Keyboard.Keys[Key.R].Pressed)
                    {
                        Game.Screen.InputMethod = InputMethod.Keyboard;
                        m_computer.Reboot();
                    }
                    if (Game.Keyboard.Keys[Key.S].Pressed)
					{
						Game.Screen.InputMethod = InputMethod.Keyboard;
						m_computer.TurnOff();
					}
				}
            }
            else
            {
                // De-press
                foreach (Key key in Enum.GetValues(typeof(Key)))
                {
                    int? code = TranslateKey(key);
                    if (code.HasValue)
                    {
                        keyboard.KeyUp(code.Value);
                    }
                }
            }

            // Update
            var frameTime = 1.0f / FPS;
            m_frameTimer -= dt;
            while (m_frameTimer <= 0.0f)
            {
                m_computer.Update(TimeSpan.FromSeconds(frameTime));
                m_frameTimer += frameTime;
            }

            // Detect score increase
            if (m_activeDisk != null && m_devices.Score.Score > m_cachedScore)
            {
                var score = m_devices.Score.Score;
                if (m_activeDisk.ID != 0)
                {
                    // Save the score and check achivements
                    Game.User.Progress.SubmitArcadeGameScore(m_activeDisk.ID, score);
                    UnlockHighscoreAchievements(m_activeDiskMod, m_activeDisk, score);

                    // Submit the score to the leaderboards
                    if (Game.Network.SupportsLeaderboards)
                    {
                        string leaderboardName = null;
                        if (m_activeDiskMod == null)
                        {
                            leaderboardName = "arcade.main." + m_activeDisk.ID;
                        }
                        else
                        {
                            leaderboardName = "arcade.mod." + m_activeDisk.ID;
                        }
                        if (leaderboardName != null)
                        {
                            Game.QueuePromiseTask(
                                Game.Network.GetLeaderboardID(leaderboardName, true),
                                delegate (Promise<ulong> promise)
                                {
                                    if (promise.Status == Status.Complete)
                                    {
                                        Game.Network.LocalUser.SubmitLeaderboardScore(promise.Result, score);
                                    }
                                }
                            );
                        }
                    }
                }
                m_cachedScore = score;
            }

            // Detect eject
            if (m_activeDisk != null && m_devices.DiskDrive.Disk == null)
            {
                SelectDisk(null, null);
            }

            // Output
            // Video
            var newDisplayImage = m_devices.Display.Image;
            var newDisplayVer = (newDisplayImage != null) ? newDisplayImage.Version : 0;
            if (newDisplayImage != displayImage || newDisplayVer != displayVer)
            {
                UpdateDisplay();
            }

            // See if any new mods have been added
            if (Mods.Refresh(Game.Network))
            {
                bool needsLoad = false;
                foreach (var mod in Mods.RemovedLoadedMods)
                {
                    Assets.RemoveSource(mod.Assets);
                    mod.Loaded = false;
                    needsLoad = true;
                }
                foreach (var mod in Mods.AllMods)
                {
                    if (mod.AutoLoad && !mod.Loaded)
                    {
                        Assets.AddSource(mod.Assets);
                        mod.Loaded = true;
                        needsLoad = true;
                    }
                }
                if (needsLoad)
                {
                    LoadToState(delegate ()
                    {
                        return new ArcadeState(Game);
                    });
                }
            }
        }

        private void UnlockHighscoreAchievements( Mod mod, ArcadeDisk disk, int score )
        {
            if (mod == null)
            {
                if (disk.Path == "arcade/worm.disk" && score >= 60)
                {
                    Game.User.Progress.UnlockAchievement(Achievement.WormHighScore);
                }
                else if (disk.Path == "arcade/invaders.disk" && score >= 750)
                {
                    Game.User.Progress.UnlockAchievement(Achievement.InvadersHighScore);
                }
                else if (disk.Path == "arcade/tennis.disk" && score >= 10)
                {
                    Game.User.Progress.UnlockAchievement(Achievement.TennisHighScore);
                }
                else if (disk.Path == "arcade/quest.disk" && score >= 1)
                {
                    Game.User.Progress.UnlockAchievement(Achievement.QuestHighScore);
                }
            }
            Game.User.Progress.Save();
        }

        private void ShowDiskSelect()
        {
            if (m_diskSelector == null)
            {
                m_diskSelector = new DiskSelector(Game.Screen, (m_activeDisk != null) ? m_activeDisk.Path : null, m_activeDiskMod, Game.User.Progress);
                m_diskSelector.OnSelect += delegate (object o, DiskSelectEventArgs args)
                {
                    // Reboot computer with new disk
                    m_computer.TurnOff();
                    SelectDisk(args.Disk, args.Mod);
                    m_computer.TurnOn();
                    UpdateDisplay();
                };
                m_diskSelector.OnBrowseWorkshop += delegate (object o, EventArgs args)
                {
                    Game.Network.OpenWorkshopHub(new string[] { "Arcade Games" });
                };
                m_diskSelector.OnClose += delegate (object o, EventArgs args)
                {
                    if (m_diskSelector == o)
                    {
                        Game.Screen.Elements.Remove(m_diskSelector);
                        m_diskSelector.Dispose();
                        m_diskSelector = null;
                    }
                };
                Game.Screen.Elements.Add(m_diskSelector);
            }
        }

        private void SelectDisk(ArcadeDisk disk, Mod mod)
        {
            if (m_activeDisk == disk)
            {
                return;
            }

            // Mount the disk
            m_activeDisk = disk;
            m_activeDiskMod = mod;
            if (m_activeDisk != null)
            {
                IEnumerable<IAssetSource> sources;
                if (mod != null)
                {
                    sources = new IAssetSource[] { mod.Assets };
                }
                else
                {
                    sources = Assets.Sources.Where(source => source.Mod == null);
                }
                m_devices.DiskDrive.Disk = new Disk(new AssetMount("disk", sources, m_activeDisk.ContentPath));
                App.Log("Started disk {0}", m_activeDisk.Path);
            }
            else
            {
                m_devices.DiskDrive.Disk = null;
            }

            // Update UI
            UpdatePrompts();

            if (disk != null)
            {
                // Initialise the disk ID
                if (disk.ID == 0)
                {
                    if ((mod != null && mod.Source == ModSource.Editor) ||
                        (mod == null && App.Debug))
                    {
                        disk.ID = MathUtils.GenerateLevelID(disk.Path);
                        if (mod != null)
                        {
                            disk.Save(Path.Combine(mod.Path, "assets/" + disk.Path));
                        }
                        else
                        {
                            disk.Save(Path.Combine(App.AssetPath, "main/" + disk.Path));
                        }
                    }
                }

                // Mark the disk as played
                if (disk.ID != 0)
                {
                    Game.User.Progress.SetLastArcadeGamePlayed(disk.ID);
                    if (!Game.User.Progress.IsArcadeGamePlayed(disk.ID))
                    {
                        Game.User.Progress.SetArcadeGamePlayed(disk.ID);
                        Game.User.Progress.IncrementStatistic(Statistic.ArcadeGamesPlayed);
                    }
                    Game.User.Progress.Save();
                }
                if (App.Steam && mod != null && mod.Source == ModSource.Workshop && mod.SteamWorkshopID.HasValue)
                {
                    Game.Network.Workshop.SetItemPlayed(mod.SteamWorkshopID.Value);
                }

                // Initialise the score chip
                m_devices.Score.Score = disk.ID != 0 ? Game.User.Progress.GetArcadeGameScore(disk.ID) : 0;
                m_cachedScore = m_devices.Score.Score;
                UnlockHighscoreAchievements(mod, disk, m_devices.Score.Score);
            }
        }

        private string TranslatePrompt(string prompt)
        {
            if (prompt.StartsWith("#", StringComparison.InvariantCulture))
            {
                return Game.Language.Translate(prompt.Substring(1));
            }
            else
            {
                return prompt;
            }
        }

        private void UpdatePrompts()
        {
            var aPrompt = (m_activeDisk != null) ? m_activeDisk.Button0Prompt : "A";
            var bPrompt = (m_activeDisk != null) ? m_activeDisk.Button1Prompt : "B";

            var baseLine = -16.0f;
            if (!string.IsNullOrEmpty(bPrompt))
            {
                m_bPrompt.Visible = true;
                m_bPrompt.String = TranslatePrompt(bPrompt);
                m_bPrompt.LocalPosition = new Vector2(16.0f, baseLine - m_bPrompt.Height);
                baseLine -= m_bPrompt.Height;
            }
            else
            {
                m_bPrompt.Visible = false;
            }
            if (!string.IsNullOrEmpty(aPrompt))
            {
                m_aPrompt.Visible = true;
                m_aPrompt.String = TranslatePrompt(aPrompt);
                m_aPrompt.LocalPosition = new Vector2(16.0f, baseLine - m_aPrompt.Height);
                baseLine -= m_aPrompt.Height;
            }
            else
            {
                m_aPrompt.Visible = false;
            }
        }

        protected override void OnPopulateCamera(Camera camera)
        {
            // Sample the animation
            m_animatedCamera.Populate(camera);

            // Transform from level to world space
            MathUtils.FastInvert(ref camera.Transform);
            camera.Transform = camera.Transform * Level.Transform;
            MathUtils.FastInvert(ref camera.Transform);
        }

        protected void GoBack()
        {
            // Animate
            StartCameraAnimation("animation/menus/options/camera_fromscreen.anim.lua");
            Game.Audio.PlaySound("sound/arcade_shutdown.wav", false);
            m_robot.StartAnimation(LuaAnimation.Get("animation/menus/options/robot_fromscreen.anim.lua"), false);
            m_robot.ScreenTexture = null;

            // Show options
            CutToState(new MainMenuState(Game), 1.25f);
        }

        private bool CheckLeft()
        {
            if (Game.Screen.ModalDialog != null)
            {
                return false;
            }
            if (Game.ActiveSteamController != null)
            {
                if (Game.ActiveSteamController.Buttons[SteamControllerButton.ArcadeLeft.GetID()].Held)
                {
                    Game.Screen.InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Game.ActiveGamepad != null)
            {
                if (Game.ActiveGamepad.Buttons[GamepadButton.Left].Held ||
                    Game.ActiveGamepad.Buttons[GamepadButton.LeftStickLeft].Held)
                {
                    Game.Screen.InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Game.Keyboard.Keys[Key.Left].Held ||
                Game.Keyboard.Keys[m_aKey].Held)
            {
                Game.Screen.InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }

        private bool CheckRight()
        {
            if (Game.Screen.ModalDialog != null)
            {
                return false;
            }
            if (Game.ActiveSteamController != null)
            {
                if (Game.ActiveSteamController.Buttons[SteamControllerButton.ArcadeRight.GetID()].Held)
                {
                    Game.Screen.InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Game.ActiveGamepad != null)
            {
                if (Game.ActiveGamepad.Buttons[GamepadButton.Right].Held ||
                    Game.ActiveGamepad.Buttons[GamepadButton.LeftStickRight].Held)
                {
                    Game.Screen.InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Game.Keyboard.Keys[Key.Right].Held ||
                Game.Keyboard.Keys[m_dKey].Held)
            {
                Game.Screen.InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }

        private bool CheckUp()
        {
            if (Game.Screen.ModalDialog != null)
            {
                return false;
            }
            if (Game.ActiveSteamController != null)
            {
                if (Game.ActiveSteamController.Buttons[SteamControllerButton.ArcadeUp.GetID()].Held)
                {
                    Game.Screen.InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Game.ActiveGamepad != null)
            {
                if (Game.ActiveGamepad.Buttons[GamepadButton.Up].Held ||
                    Game.ActiveGamepad.Buttons[GamepadButton.LeftStickUp].Held)
                {
                    Game.Screen.InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Game.Keyboard.Keys[Key.Up].Held ||
                Game.Keyboard.Keys[m_wKey].Held)
            {
                Game.Screen.InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }

        private bool CheckDown()
        {
            if (Game.Screen.ModalDialog != null)
            {
                return false;
            }
            if (Game.ActiveSteamController != null)
            {
                if (Game.ActiveSteamController.Buttons[SteamControllerButton.ArcadeDown.GetID()].Held)
                {
                    Game.Screen.InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Game.ActiveGamepad != null)
            {
                if (Game.ActiveGamepad.Buttons[GamepadButton.Down].Held ||
                    Game.ActiveGamepad.Buttons[GamepadButton.LeftStickDown].Held)
                {
                    Game.Screen.InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Game.Keyboard.Keys[Key.Down].Held ||
                Game.Keyboard.Keys[m_sKey].Held)
            {
                Game.Screen.InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }

        private float CheckX()
        {
            var x = 0.0f;
            if (CheckLeft())
            {
                x -= 1.0f;
            }
            if (CheckRight())
            {
                x += 1.0f;
            }
            return x;
        }

        private float CheckY()
        {
            var y = 0.0f;
            if (CheckUp())
            {
                y -= 1.0f;
            }
            if (CheckDown())
            {
                y += 1.0f;
            }
            return y;
        }

        private bool CheckA()
        {
            if (Game.Screen.ModalDialog != null)
            {
                return false;
            }
            if (Game.ActiveSteamController != null)
            {
                if (Game.ActiveSteamController.Buttons[SteamControllerButton.ArcadeA.GetID()].Held)
                {
                    Game.Screen.InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Game.ActiveGamepad != null)
            {
                if (Game.ActiveGamepad.Buttons[GamepadButton.A].Held)
                {
                    Game.Screen.InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Game.Keyboard.Keys[m_zKey].Held)
            {
                Game.Screen.InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }

        private bool CheckB()
        {
            if (Game.Screen.ModalDialog != null)
            {
                return false;
            }
            if (Game.ActiveSteamController != null)
            {
                if (Game.ActiveSteamController.Buttons[SteamControllerButton.ArcadeB.GetID()].Held)
                {
                    Game.Screen.InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Game.ActiveGamepad != null)
            {
                if (Game.ActiveGamepad.Buttons[GamepadButton.B].Held)
                {
                    Game.Screen.InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Game.Keyboard.Keys[m_xKey].Held)
            {
                Game.Screen.InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }

        private new bool CheckBack()
        {
            if (Game.Screen.ModalDialog != null)
            {
                return false;
            }
            if (Game.ActiveSteamController != null)
            {
                if (Game.ActiveSteamController.Buttons[SteamControllerButton.ArcadeBack.GetID()].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Game.ActiveGamepad != null)
            {
                if (Game.ActiveGamepad.Buttons[GamepadButton.Back].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Game.Keyboard.Keys[Key.Escape].Pressed)
            {
                Game.Screen.InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }

        private bool CheckDiskSelect()
        {
            if (Game.Screen.ModalDialog != null || !m_diskSelectPrompt.Visible)
            {
                return false;
            }
            if (Game.ActiveSteamController != null)
            {
                if (Game.ActiveSteamController.Buttons[SteamControllerButton.ArcadeSwapDisk.GetID()].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.SteamController;
                    return true;
                }
            }
            if (Game.ActiveGamepad != null)
            {
                if (Game.ActiveGamepad.Buttons[GamepadButton.Start].Pressed)
                {
                    Game.Screen.InputMethod = InputMethod.Gamepad;
                    return true;
                }
            }
            if (Game.Keyboard.Keys[Key.Tab].Pressed)
            {
                Game.Screen.InputMethod = InputMethod.Keyboard;
                return true;
            }
            return false;
        }

        private void UpdateDisplay()
        {
            var image = m_devices.Display.Image;
            var white = Vector4.One;
            var black = Vector4.Zero;
            using (var bits = m_displayBitmap.Lock())
            {
                for (int x = 0; x < bits.Width; ++x)
                {
                    for (int y = 0; y < bits.Height; ++y)
                    {
                        if (image != null && x < image.Width && y < image.Height)
                        {
                            var c = image[x, y];
                            bits.SetPixel(x, y, c >= 1 ? white : black);
                        }
                        else
                        {
                            bits.SetPixel(x, y, black);
                        }
                    }
                }
            }
            if (m_displayTexture != null)
            {
                m_displayTexture.Update();
            }
        }

        private int? TranslateKey(Key key)
        {
            switch (key)
            {
                case Key.One: return 2;
                case Key.Two: return 3;
                case Key.Three: return 4;
                case Key.Four: return 5;
                case Key.Five: return 6;
                case Key.Six: return 7;
                case Key.Seven: return 8;
                case Key.Eight: return 9;
                case Key.Nine: return 10;
                case Key.Zero: return 11;
                case Key.Minus: return 12;
                case Key.Equals: return 13;
                case Key.Backspace: return 14;
                case Key.Tab: return 15;
                case Key.Q: return 16;
                case Key.W: return 17;
                case Key.E: return 18;
                case Key.R: return 19;
                case Key.T: return 20;
                case Key.Y: return 21;
                case Key.U: return 22;
                case Key.I: return 23;
                case Key.O: return 24;
                case Key.P: return 25;
                case Key.LeftBracket: return 26;
                case Key.RightBracket: return 27;
                case Key.Return: return 28;
                case Key.LeftCtrl: return 29;
                case Key.A: return 30;
                case Key.S: return 31;
                case Key.D: return 32;
                case Key.F: return 33;
                case Key.G: return 34;
                case Key.H: return 35;
                case Key.J: return 36;
                case Key.K: return 37;
                case Key.L: return 38;
                case Key.Semicolon: return 39;
                case Key.Apostrophe: return 40;
                case Key.BackQuote: return 41;
                case Key.LeftShift: return 42;
                case Key.BackSlash: return 43;
                case Key.Z: return 44;
                case Key.X: return 45;
                case Key.C: return 46;
                case Key.V: return 47;
                case Key.B: return 48;
                case Key.N: return 49;
                case Key.M: return 50;
                case Key.Comma: return 51;
                case Key.Period: return 52;
                case Key.Slash: return 53;
                case Key.RightShift: return 54;
                case Key.NumpadMultiply: return 55;
                case Key.LeftAlt: return 56;
                case Key.Space: return 57;
                case Key.CapsLock: return 58;
				/*
                case Key.F1: return 59;
                case Key.F2: return 60;
                case Key.F3: return 61;
                case Key.F4: return 62;
                case Key.F5: return 63;
                case Key.F6: return 64;
                case Key.F7: return 65;
                case Key.F8: return 66;
                case Key.F9: return 67;
                case Key.F10: return 68;
                case Key.NumLock: return 69;
                case Key.ScrollLock: return 70;
                */
                case Key.NumpadSeven: return 71;
                case Key.NumpadEight: return 72;
                case Key.NumpadNine: return 73;
                case Key.NumpadMinus: return 74;
                case Key.NumpadFour: return 75;
                case Key.NumpadFive: return 76;
                case Key.NumpadSix: return 77;
                case Key.NumpadPlus: return 78;
                case Key.NumpadOne: return 79;
                case Key.NumpadTwo: return 80;
                case Key.NumpadThree: return 81;
                case Key.NumpadZero: return 82;
                case Key.NumpadPeriod: return 83;
				/*
                case Key.F11: return 87;
                case Key.F12: return 88;
                */
                case Key.NumpadEquals: return 141;
                case Key.Colon: return 146;
                case Key.Underscore: return 147;
                case Key.NumpadEnter: return 152;
                case Key.RightCtrl: return 153;
                case Key.NumpadComma: return 179;
                case Key.NumpadDivide: return 181;
                case Key.RightAlt: return 184;
                case Key.Pause: return 197;
                case Key.Home: return 199;
                case Key.Up: return 200;
                case Key.PageUp: return 201;
                case Key.Left: return 203;
                case Key.Right: return 205;
                case Key.End: return 207;
                case Key.Down: return 208;
                case Key.PageDown: return 209;
                case Key.Insert: return 210;
                case Key.Delete: return 211;
                default: return null;
            }
        }
    }
}
