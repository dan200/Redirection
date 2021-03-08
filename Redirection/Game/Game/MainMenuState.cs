
using Dan200.Core.Animation;
using Dan200.Core.Assets;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Core.Render;
using Dan200.Core.Util;
using Dan200.Game.Arcade;
using Dan200.Game.Level;
using Dan200.Game.User;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dan200.Game.Game
{
    public class MainMenuState : LevelState
    {
        private AnimatedCameraController m_animatedCamera;
        private CutsceneEntity m_robot;

        public MainMenuState(Game game) : base(game, "levels/startscreen.level", LevelOptions.Menu)
        {
            // Create camera
            m_animatedCamera = new AnimatedCameraController(Level.TimeMachine);
        }

        protected void StartCameraAnimation(string animPath)
        {
            m_animatedCamera.Play(LuaAnimation.Get(animPath));
        }

        protected CutsceneEntity CreateEntity(string modelPath)
        {
            var entity = new CutsceneEntity(Model.Get(modelPath), RenderPass.Opaque);
            Level.Entities.Add(entity);
            return entity;
        }

        protected override void OnReveal()
        {
            base.OnReveal();

            // Create robot
            m_robot = CreateEntity("models/entities/new/red_robot.obj");

            // Start animation
            StartCameraAnimation("animation/menus/startscreen/camera.anim.lua");
            m_robot.StartAnimation(LuaAnimation.Get("animation/menus/startscreen/robot.anim.lua"), false);

            // Reposition sky
            Game.Sky.ForegroundModelTransform = Matrix4.CreateTranslation(-5.0f, 5.0f, -20.0f);
        }

        protected override void OnInit()
        {
            base.OnInit();

            // Start
            if (App.Demo)
            {
                Game.User.Progress.Reset();
                ContinueGame();
            }
            else if (App.FPS <= 18.0f)
            {
                ShowFPSWarning();
            }
            else
            {
                ShowMenu();
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

        private void ShowFPSWarning()
        {
            var dialog = DialogBox.CreateQueryBox(
                Game.Screen,
                Game.Language.Translate("menus.fps_warning.title"),
                Game.Language.Translate("menus.fps_warning.info"),
                new string[] {
                    Game.Language.Translate( "menus.yes" ),
                    Game.Language.Translate( "menus.no" )
                },
                true
            );
            dialog.OnClosed += delegate (object sender, DialogBoxClosedEventArgs e)
            {
                int index = e.Result;
                switch (index)
                {
                    case 0:
                        {
                            // YES
                            WipeToState(new GraphicsOptionsState(Game));
                            break;
                        }
                    case 1:
                        {
                            // NO
                            ShowMenu();
                            break;
                        }
                    default:
                        {
                            // Back
                            GoBack();
                            break;
                        }
                }
            };
            ShowDialog(dialog);
        }

        private void ShowMenu()
        {
            var lang = Game.Language;
            var options = new List<string>();
            var optionActions = new List<Action>();

            if (Game.User.Progress.GetStatistic(Statistic.LevelsCompleted) > 0)
            {
                options.Add(lang.Translate("menus.main.continue_game"));
            }
            else
            {
                options.Add(lang.Translate("menus.main.new_game"));
            }
            optionActions.Add(ContinueGame);

            if (ArcadeUtils.IsArcadeUnlocked(Game.User.Progress))
            {
                options.Add(lang.Translate("menus.main.arcade"));
                optionActions.Add(Arcade);
            }

            options.Add(lang.Translate("menus.main.mod_editor"));
            optionActions.Add(ShowGamepadWarningThenModEditor);

            options.Add(lang.Translate("menus.main.options"));
            optionActions.Add(Options);

            options.Add(lang.Translate("menus.main.quit"));
            optionActions.Add(Quit);

            var dialog = DialogBox.CreateMenuBox(
                lang.Translate("menus.main.title"),
                options.ToArray(),
                false
            );
            dialog.OnClosed += delegate (object sender, DialogBoxClosedEventArgs e)
            {
                int index = e.Result;
                if (index >= 0 && index < optionActions.Count)
                {
                    optionActions[index].Invoke();
                }
                else
                {
                    GoBack();
                }
            };
            ShowDialog(dialog);
        }

        private void ContinueGame()
        {
            // Animate the robot
            m_robot.StartAnimation(LuaAnimation.Get("animation/menus/startscreen/robot_move.anim.lua"), false);
            m_robot.PlaySoundAfterDelay("sound/new_robot/idle_loop.wav", true, 0.3f);

            var delay = 1.25f;
            if (App.Demo)
            {
                // Start the main campaign
                var campaign = Campaign.Get("campaigns/demo.campaign");
                var playthrough = new Playthrough(Campaign.Get("campaigns/demo.campaign"), 0);
                var firstLevel = campaign.Levels[0];
                var firstLevelData = LevelData.Get(firstLevel);
                if (firstLevelData.Intro != null)
                {
                    WipeToState(new CutsceneState(Game, null, firstLevelData.Intro, CutsceneContext.LevelIntro, playthrough), delay);
                }
                else
                {
                    WipeToState(new CampaignState(Game, null, playthrough));
                }
            }
            else
            {
                // Open the campaign select
                WipeToState(new CampaignSelectState(Game), delay);
            }
        }

        private List<Mod> AllEditorMods()
        {
            var results = new List<Mod>();
            foreach (var mod in Mods.AllMods)
            {
                if (mod.Source == ModSource.Editor)
                {
                    results.Add(mod);
                }
            }
            return results;
        }

        private void ModEditor()
        {
            var mods = AllEditorMods();
            var sortedMods = mods.OrderBy(m => m.Title).ToArray();
            var titles = sortedMods.Select(m => m.Title).ToList();
            titles.Add(Game.Language.Translate("menus.mod_select.create_new"));

            var dialog = DialogBox.CreateMenuBox(
                Game.Language.Translate("menus.mod_select.title"),
                titles.ToArray(),
                false
            );
            dialog.OnClosed += delegate (object sender, DialogBoxClosedEventArgs e)
            {
                if (e.Result < 0)
                {
                    // Go back
                    EnableGamepad = true;
                    ShowMenu();
                }
                else if (e.Result >= 0 && e.Result < mods.Count)
                {
                    // Select mod
                    var mod = sortedMods[e.Result];
                    EditMod(mod);
                }
                else if (e.Result == sortedMods.Length)
                {
                    // Create new
                    CreateMod();
                }
            };

            EnableGamepad = false;
            ShowDialog(dialog);
        }

        private string SuggestModTitle()
        {
            var username = Game.Network.LocalUser.DisplayName;
            if (username.EndsWith("s", StringComparison.InvariantCulture))
            {
                return username + "' Mod";
            }
            else
            {
                return username + "'s Mod";
            }
        }

        private void CreateMod()
        {
            var textEntry = TextEntryDialogBox.Create(Game.Language.Translate("menus.name_mod_prompt.title"), SuggestModTitle(), "", Game.Screen.Width - 300.0f, new string[] {
                Game.Language.Translate( "menus.ok" ),
                Game.Language.Translate( "menus.cancel" )
            });
            textEntry.OnClosed += delegate (object sender2, DialogBoxClosedEventArgs e2)
            {
                if (e2.Result == 0)
                {
                    var title = (textEntry.EnteredText.Trim().Length > 0) ? textEntry.EnteredText.Trim() : "Untitled Mod";
                    var mod = Mods.Create(title);
                    if (App.Steam)
                    {
                        mod.Author = Game.Network.LocalUser.DisplayName;
                        mod.SteamUserID = Game.Network.LocalUser.ID;
                        mod.SaveInfo();
                    }
                    EditMod(mod);
                }
                else
                {
                    ModEditor();
                }
            };
            ShowDialog(textEntry);
        }

        private void ShowGamepadWarningThenModEditor()
        {
            if (Game.Screen.InputMethod != InputMethod.Keyboard && Game.Screen.InputMethod != InputMethod.Mouse)
            {
                var warning = DialogBox.CreateQueryBox(
                    Game.Screen,
                    Game.Language.Translate("menus.editor_gamepad_warning.title"),
                    Game.Language.Translate("menus.editor_gamepad_warning.info"),
                    new string[] {
                        Game.Language.Translate( "menus.ok" ),
                        Game.Language.Translate( "menus.cancel" ),
                    },
                    true
                );
                warning.OnClosed += delegate (object sender, DialogBoxClosedEventArgs e)
                {
                    if (e.Result == 0)
                    {
                        ModEditor();
                    }
                    else
                    {
                        ShowMenu();
                    }
                };
                ShowDialog(warning);
            }
            else
            {
                ModEditor();
            }
        }

        private void EditMod(Mod mod)
        {
            // Animate the robot
            m_robot.StartAnimation(LuaAnimation.Get("animation/menus/startscreen/robot_move_alt.anim.lua"), false);
            m_robot.PlaySoundAfterDelay("sound/new_robot/beam_up.wav", false, 0.3f);

            // Open the mod editor
            Func<State> fnNextState = delegate ()
            {
                return new ModEditorState(Game, mod);
            };
            if (!mod.Loaded)
            {
                Assets.AddSource(mod.Assets);
                mod.Loaded = true;
                LoadToState(fnNextState, 1.75f);
            }
            else
            {
                WipeToState(fnNextState.Invoke(), 1.75f);
            }
        }

        private void Options()
        {
            // Animate the camera
            StartCameraAnimation("animation/menus/startscreen/camera_toscreen.anim.lua");
            m_robot.StartAnimation(LuaAnimation.Get("animation/menus/startscreen/robot_toscreen.anim.lua"), false);

            // Show options
            CutToState(new MainOptionsState(Game), 1.25f);
        }

        private void Arcade()
        {
            // Animate the camera
            StartCameraAnimation("animation/menus/startscreen/camera_toscreen.anim.lua");
            m_robot.StartAnimation(LuaAnimation.Get("animation/menus/startscreen/robot_toscreen.anim.lua"), false);
            Game.Audio.PlaySound("sound/arcade_boot.wav", false);

            // Show options
            CutToState(new ArcadeState(Game), 1.25f);
        }

        private void Quit()
        {
            // Animate the robot
            m_robot.StartAnimation(LuaAnimation.Get("animation/menus/startscreen/robot_die.anim.lua"), false);

            // Quit
            BlackoutToState(new ShutdownState(Game), 1.0f);
        }

        private void GoBack()
        {
            // Animate the camera
            StartCameraAnimation("animation/menus/startscreen/camera_toinitial.anim.lua");

            // Show startup
            CutToState(new StartScreenState(Game), 1.25f);
        }
    }
}
