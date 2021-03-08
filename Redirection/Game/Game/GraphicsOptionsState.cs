using Dan200.Core.Render;
using Dan200.Game.Options;

namespace Dan200.Game.Game
{
    public class GraphicsOptionsState : RobotOptionsState
    {
        private class FullscreenOption : IOptionValue<bool>
        {
            private Game m_game;

            public FullscreenOption(Game game)
            {
                m_game = game;
            }

            public bool Value
            {
                get
                {
                    return m_game.User.Settings.Fullscreen;
                }
                set
                {
                    m_game.Window.Fullscreen = value;
                    m_game.User.Settings.Fullscreen = value;
                    m_game.User.Settings.Save();
                }
            }
        }

        private class ResolutionOption : IOptionValue<Resolution>
        {
            private Game m_game;

            public ResolutionOption(Game game)
            {
                m_game = game;
            }

            public Resolution Value
            {
                get
                {
                    return new Resolution(
                        m_game.User.Settings.FullscreenWidth,
                        m_game.User.Settings.FullscreenHeight
                    );
                }
                set
                {
                    m_game.User.Settings.FullscreenWidth = value.Width;
                    m_game.User.Settings.FullscreenHeight = value.Height;
                    m_game.User.Settings.Save();
                    m_game.Resize();
                }
            }
        }

        private class AntiAliasingOption : IOptionValue<AntiAliasingMode>
        {
            private Game m_game;

            public AntiAliasingOption(Game game)
            {
                m_game = game;
            }

            public AntiAliasingMode Value
            {
                get
                {
                    return m_game.User.Settings.AAMode;
                }
                set
                {
                    m_game.User.Settings.AAMode = value;
                    m_game.User.Settings.Save();
                    m_game.PostEffect.Effect = PostEffectInstance.ChooseEffect(m_game.User.Settings);
                    m_game.Resize();
                }
            }
        }

        private class ShadowsOption : IOptionValue<bool>
        {
            private Game m_game;

            public ShadowsOption(Game game)
            {
                m_game = game;
            }

            public bool Value
            {
                get
                {
                    return m_game.User.Settings.Shadows;
                }
                set
                {
                    m_game.User.Settings.Shadows = value;
                    m_game.User.Settings.Save();
                }
            }
        }

        private class VSyncOption : IOptionValue<bool>
        {
            private Game m_game;

            public VSyncOption(Game game)
            {
                m_game = game;
            }

            public bool Value
            {
                get
                {
                    return m_game.User.Settings.VSync;
                }
                set
                {
                    m_game.Window.VSync = value;
                    m_game.User.Settings.VSync = value;
                    m_game.User.Settings.Save();
                }
            }
        }

        private class FancyRewindOption : IOptionValue<bool>
        {
            private Game m_game;

            public FancyRewindOption(Game game)
            {
                m_game = game;
            }

            public bool Value
            {
                get
                {
                    return m_game.User.Settings.FancyRewind;
                }
                set
                {
                    m_game.User.Settings.FancyRewind = value;
                    m_game.User.Settings.Save();
                    m_game.PostEffect.Effect = PostEffectInstance.ChooseEffect(m_game.User.Settings);
                }
            }
        }

        public GraphicsOptionsState(Game game) : base(game, "menus.graphics_options.title")
        {
        }

        protected override IOption[] GetOptions()
        {
            return new IOption[]
            {
                new ToggleOption( "menus.graphics_options.fullscreen", new FullscreenOption( Game ) ),
                new MultipleChoiceOption<Resolution>(
                    "menus.graphics_options.resolution",
                    new ResolutionOption( Game ),
                    Resolution.StandardResolutions,
                    delegate( Resolution arg ) {
                        return arg.Name;
                    }
                ),
                new MultipleChoiceOption<AntiAliasingMode>(
                    "menus.graphics_options.aa",
                    new AntiAliasingOption( Game ),
                    new AntiAliasingMode[] {
                        AntiAliasingMode.None,
                        AntiAliasingMode.FXAA,
                        AntiAliasingMode.SSAA
                    },
                    delegate( AntiAliasingMode arg ) {
                        if( arg == AntiAliasingMode.None )
                        {
                            return Game.Language.Translate( "menus.graphics_options.aa.none" );
                        }
                        else
                        {
                            return arg.ToString();
                        }
                    }
                ),
                new ToggleOption( "menus.graphics_options.shadows", new ShadowsOption( Game ) ),
                //new ToggleOption( "menus.graphics_options.lighting", new LightingOption( Game ) ),
                //new CycleOption( "menus.graphics_options.brightness", new BrightnessOption( Game ), 0, 100, 10 ),
                //new CycleOption( "menus.graphics_options.contrast", new ContrastOption( Game ), 0, 100, 10 ),
                new ToggleOption( "menus.graphics_options.fancy_rewind", new FancyRewindOption( Game ) ),
                new ToggleOption( "menus.graphics_options.vsync", new VSyncOption( Game ) )
            };
        }

        protected override void GoBack()
        {
            FuzzToState(new MainOptionsState(Game));
        }
    }
}

