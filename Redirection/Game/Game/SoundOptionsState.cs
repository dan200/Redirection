using Dan200.Game.Options;
using System;

namespace Dan200.Game.Game
{
    public class SoundOptionsState : RobotOptionsState
    {
        private class SoundEnabledOption : IOptionValue<bool>
        {
            private Game m_game;

            public SoundEnabledOption(Game game)
            {
                m_game = game;
            }

            public bool Value
            {
                get
                {
                    return m_game.User.Settings.EnableSound;
                }
                set
                {
                    m_game.User.Settings.EnableSound = value;
                    m_game.User.Settings.Save();
                    m_game.Audio.Audio.EnableSound = value;
                }
            }
        }

        private class SoundVolumeOption : IOptionValue<int>
        {
            private Game m_game;

            public SoundVolumeOption(Game game)
            {
                m_game = game;
            }

            public int Value
            {
                get
                {
                    return (int)Math.Round(m_game.User.Settings.SoundVolume);
                }
                set
                {
                    m_game.User.Settings.SoundVolume = (float)value;
                    m_game.User.Settings.Save();
                    m_game.Audio.Audio.SoundVolume = (float)value / 11.0f;
                    m_game.Audio.PlaySound("sound/tweak_countdown.wav");
                }
            }
        }

        private class MusicEnabledOption : IOptionValue<bool>
        {
            private Game m_game;

            public MusicEnabledOption(Game game)
            {
                m_game = game;
            }

            public bool Value
            {
                get
                {
                    return m_game.User.Settings.EnableMusic;
                }
                set
                {
                    m_game.User.Settings.EnableMusic = value;
                    m_game.User.Settings.Save();
                    m_game.Audio.Audio.EnableMusic = value;
                }
            }
        }

        private class MusicVolumeOption : IOptionValue<int>
        {
            private Game m_game;

            public MusicVolumeOption(Game game)
            {
                m_game = game;
            }

            public int Value
            {
                get
                {
                    return (int)Math.Round(m_game.User.Settings.MusicVolume);
                }
                set
                {
                    m_game.User.Settings.MusicVolume = (float)value;
                    m_game.User.Settings.Save();
                    m_game.Audio.Audio.MusicVolume = (float)value / 11.0f;
                }
            }
        }

        public SoundOptionsState(Game game) : base(game, "menus.sound_options.title")
        {
        }

        protected override IOption[] GetOptions()
        {
            return new IOption[]
            {
                new ToggleOption( "menus.sound_options.sound_enabled", new SoundEnabledOption( Game ) ),
                new CycleOption( "menus.sound_options.sound_volume", new SoundVolumeOption( Game ), 0, 11 ),
                new ToggleOption( "menus.sound_options.music_enabled", new MusicEnabledOption( Game ) ),
                new CycleOption( "menus.sound_options.music_volume", new MusicVolumeOption( Game ), 0, 11 ),
            };
        }

        protected override void GoBack()
        {
            FuzzToState(new MainOptionsState(Game));
        }
    }
}

