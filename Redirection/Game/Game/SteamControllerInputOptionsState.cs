using Dan200.Core.GUI;
using Dan200.Core.Input;
using Dan200.Core.Main;
using Dan200.Game.Options;
using System;
using System.Linq;

namespace Dan200.Game.Game
{
    public class SteamControllerInputOptionsState : OptionsState
    {
        private class SteamControllerEnabledOption : IOptionValue<bool>
        {
            private Game m_game;

            public SteamControllerEnabledOption(Game game)
            {
                m_game = game;
            }

            public bool Value
            {
                get
                {
                    return m_game.User.Settings.EnableSteamController;
                }
                set
                {
                    m_game.User.Settings.EnableSteamController = value;
                    m_game.User.Settings.Save();
                }
            }
        }

        private bool m_bigPicture;

        public SteamControllerInputOptionsState(Game game) : base(game, "menus.steam_controller_input_options.title", "levels/options.level")
        {
            var steamTenFoot = Environment.GetEnvironmentVariable("SteamTenfoot");
            m_bigPicture = (steamTenFoot == "1");
        }

        protected override IOption[] GetOptions()
        {
            return new IOption[]
            {
                new ToggleOption( "menus.steam_controller_input_options.enabled", new SteamControllerEnabledOption( Game ) ),
                new ActionOption( "menus.steam_controller_input_options.configure", Configure ),
            };
        }

        private void Configure()
        {
            ISteamController controller = null;
            if (Game.ActiveSteamController != null)
            {
                controller = Game.ActiveSteamController;
            }
            else if (Game.SteamControllers.Count > 0)
            {
                controller = Game.SteamControllers.First();
            }
            if( controller == null )
            {
                ShowDialog(DialogBox.CreateQueryBox(
                    Game.Screen,
                    Game.Language.Translate("menus.steam_controller_input_options.configure"),
                    Game.Language.Translate("menus.steam_controller_input_options.no_controller"),
                    new string[] {
                        Game.Language.Translate( "menus.ok" )
                    },
                    false
                ));
            }
            else if( !Game.Network.OpenSteamControllerConfig(controller) )
            {
                if (!m_bigPicture)
                {
                    ShowDialog(DialogBox.CreateQueryBox(
                        Game.Screen,
                        Game.Language.Translate("menus.steam_controller_input_options.configure"),
                        Game.Language.Translate("menus.steam_controller_input_options.no_big_picture"),
                        new string[] {
                            Game.Language.Translate( "menus.ok" )
                        },
                        false
                    ));
                }
            }
        }

        protected override void GoBack()
        {
            SlideToState(new InputOptionsState(Game), SlideDirection.Left);
        }
    }
}

