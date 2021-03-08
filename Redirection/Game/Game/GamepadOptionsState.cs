using Dan200.Core.Input;
using Dan200.Game.Options;

namespace Dan200.Game.Game
{
    public class GamepadOptionsState : RobotOptionsState
    {
        private class GamepadEnabledOption : IOptionValue<bool>
        {
            private Game m_game;

            public GamepadEnabledOption(Game game)
            {
                m_game = game;
            }

            public bool Value
            {
                get
                {
                    return m_game.User.Settings.EnableGamepad;
                }
                set
                {
                    m_game.User.Settings.EnableGamepad = value;
                    m_game.User.Settings.Save();
                }
            }
        }

        private class GamepadRumbleEnabledOption : IOptionValue<bool>
        {
            private Game m_game;

            public GamepadRumbleEnabledOption(Game game)
            {
                m_game = game;
            }

            public bool Value
            {
                get
                {
                    return m_game.User.Settings.EnableGamepadRumble;
                }
                set
                {
                    m_game.User.Settings.EnableGamepadRumble = value;
                    m_game.User.Settings.Save();
                    if (m_game.ActiveGamepad != null)
                    {
                        m_game.ActiveGamepad.EnableRumble = value;
                        if (value && m_game.Screen.InputMethod == InputMethod.Gamepad)
                        {
                            m_game.ActiveGamepad.Rumble(0.5f, 0.3f);
                        }
                    }
                }
            }
        }

        private class GamepadTypeOption : IOptionValue<GamepadType>
        {
            private Game m_game;
            private GamepadOptionsState m_state;

            public GamepadTypeOption(Game game, GamepadOptionsState state)
            {
                m_game = game;
                m_state = state;
            }

            public GamepadType Value
            {
                get
                {
                    return m_game.User.Settings.GamepadPromptType;
                }
                set
                {
                    m_game.User.Settings.GamepadPromptType = value;
                    m_game.User.Settings.Save();
                    if (m_game.ActiveGamepad != null)
                    {
                        if (value != GamepadType.Unknown)
                        {
                            m_game.ActiveGamepad.Type = value;
                        }
                        else
                        {
                            m_game.ActiveGamepad.DetectType();
                        }
                    }
                    m_state.RefreshOptions();
                }
            }
        }

        public GamepadOptionsState(Game game) : base(game, "menus.gamepad_input_options.title")
        {
        }

        protected override IOption[] GetOptions()
        {
            return new IOption[]
            {
                new ToggleOption( "menus.gamepad_input_options.enabled", new GamepadEnabledOption( Game ) ),
                new MultipleChoiceOption<GamepadType>(
                    "menus.gamepad_input_options.prompt_type",
                    new GamepadTypeOption( Game, this ),
                    new GamepadType[] {
                        GamepadType.Unknown,
                        GamepadType.Xbox360,
                        GamepadType.PS3,
                    },
                    delegate( GamepadType arg ) {
                        switch( arg )
                        {
                            case GamepadType.Unknown:
                            {
                                return Game.Language.Translate( "menus.gamepad_input_options.prompt_type.auto" );
                            }
                            default:
                            {
                                // ABXY
                                return GamepadButton.A.GetPrompt(arg) + GamepadButton.B.GetPrompt(arg) + GamepadButton.X.GetPrompt(arg) + GamepadButton.Y.GetPrompt(arg);
                            }
                        }
                    }
                ),
//                new PadBindOption( Game, this, Bind.Play ),
  //              new PadBindOption( Game, this, Bind.Rewind ),
    //            new PadBindOption( Game, this, Bind.FastForward ),
      //          new PadBindOption( Game, this, Bind.Place ),
        //        new PadBindOption( Game, this, Bind.Remove ),
          //      new PadBindOption( Game, this, Bind.Tweak ),
                new ToggleOption( "menus.gamepad_input_options.rumble_enabled", new GamepadRumbleEnabledOption( Game ) ),
            };
        }

        protected override void GoBack()
        {
            FuzzToState(new InputOptionsState(Game));
        }
    }
}

