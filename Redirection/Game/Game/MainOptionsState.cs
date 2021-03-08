using Dan200.Core.Animation;
using Dan200.Core.Assets;
using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Game.Options;
using System.IO;
using System.Linq;

namespace Dan200.Game.Game
{
    public class MainOptionsState : RobotOptionsState
    {
        public MainOptionsState(Game game) : base(game, "menus.options.title")
        {
        }

        protected override IOption[] GetOptions()
        {
            return new IOption[]
            {
                new ActionOption( "menus.options.graphics_options", GraphicsOptions ),
                new ActionOption( "menus.options.sound_options", SoundOptions ),
                new ActionOption( "menus.options.input_options", InputOptions ),
                new ActionOption( "menus.options.change_language", SelectLanguage ),
                new ActionOption( "menus.options.reset_progress", ResetProgress ),
                new ActionOption( "menus.options.statistics", Statistics ),
                new ActionOption( "menus.options.credits", Credits )
            };
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);

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
                        return new MainOptionsState(Game);
                    });
                }
            }
        }

        private void GraphicsOptions()
        {
            FuzzToState(new GraphicsOptionsState(Game));
        }

        private void SoundOptions()
        {
            FuzzToState(new SoundOptionsState(Game));
        }

        private void InputOptions()
        {
            FuzzToState(new InputOptionsState(Game));
        }

        private void SelectLanguage()
        {
            // Select Language
            var languages = Language.GetAll()
                .Where(l => !l.IsDebug)
                .OrderBy(l => (l.CustomFont != null && l.CustomFont != Game.Language.CustomFont) ? l.EnglishName : l.Name)
                .ToArray();

            var titles = languages
                .Select(l => string.Format("{0}", (l.CustomFont != null && l.CustomFont != Game.Language.CustomFont) ? l.EnglishName : l.Name))
                .ToList();
            if (App.Steam)
            {
                titles.Add(Game.Language.Translate("menus.campaign_select.open_steam_workshop"));
            }
            //            else
            //          {
            //            titles.Add(Game.Language.Translate("menus.campaign_select.open_mod_directory"));
            //      }

            var dialog = DialogBox.CreateMenuBox(
                Game.Language.Translate("menus.select_language.title"),
                titles.ToArray(),
                false
            );
            dialog.OnClosed += delegate (object sender2, DialogBoxClosedEventArgs e2)
            {
                if (e2.Result >= 0 && e2.Result < languages.Length)
                {
                    var language = languages[e2.Result];
                    if (language.Code != Game.Language.Code)
                    {
                        Game.User.Settings.Language = language.Code;
                        Game.User.Settings.Save();
                        Game.SelectLanguage();
                        WipeToState(new MainOptionsState(Game));
                    }
                }
                else if (e2.Result == languages.Length)
                {
                    if (App.Steam)
                    {
                        Game.Network.OpenWorkshopHub(new string[] { "Localisation" });
                    }
                    else
                    {
                        Mods.InitLocalDirectory();
                        Game.Network.OpenFileBrowser(Path.Combine(App.SavePath, "mods"));
                    }
                }
            };
            ShowDialog(dialog);
        }

        private void ResetProgress()
        {
            // Reset progress
            var dialog = DialogBox.CreateQueryBox(
                Game.Screen,
                Game.Language.Translate("menus.reset_progress_prompt.title"),
                Game.Language.Translate("menus.reset_progress_prompt.info"),
                new string[] {
                    Game.Language.Translate( "menus.yes" ),
                    Game.Language.Translate( "menus.no" ),
                },
                true
            );
            dialog.OnClosed += delegate (object sender2, DialogBoxClosedEventArgs e)
            {
                switch (e.Result)
                {
                    case 0:
                        {
                            // YES
                            Game.User.Progress.Reset();
                            Game.User.Progress.ResetAllStatistics();
                            if (Game.Keyboard.Keys[Key.LeftShift].Held || Game.Keyboard.Keys[Key.RightShift].Held)
                            {
                                Game.User.Progress.RemoveAllAchievements();
                            }
                            Game.User.Progress.Save();
                            GoBack();
                            break;
                        }
                    case 1:
                        {
                            // NO
                            break;
                        }
                }
            };
            ShowDialog(dialog);
        }

        private void Credits()
        {
            WipeToState(new CutsceneState(Game, null, "levels/credits.level", CutsceneContext.Credits));
        }

        private void Statistics()
        {
            WipeToState(new StatisticsState(Game));
        }

        protected override void GoBack()
        {
            // Animate
            StartCameraAnimation("animation/menus/options/camera_fromscreen.anim.lua");
            Robot.StartAnimation(LuaAnimation.Get("animation/menus/options/robot_fromscreen.anim.lua"), false);

            // Show options
            CutToState(new MainMenuState(Game), 1.25f);
        }
    }
}

