using Dan200.Core.Audio;
using System.Collections.Generic;

namespace Dan200.Game.Game
{
    public class GameAudio
    {
        private const float MUSIC_FADE_TIME = 0.5f;

        public IAudio Audio
        {
            get
            {
                return m_audio;
            }
        }

        public AudioListener Listener
        {
            get
            {
                return m_listener;
            }
        }

        public float Rate
        {
            get
            {
                return m_rate;
            }
            set
            {
                if (m_rate != value)
                {
                    m_rate = value;
                    foreach (var sound in m_sounds)
                    {
                        if (!sound.UI)
                        {
                            sound.Playback.Rate = m_rate;
                        }
                    }
                }
            }
        }

        private class PlayingSound : IStoppable
        {
            public readonly ISoundPlayback Playback;
            public readonly int StartFrame;
            public readonly bool UI;
            private int m_refCount;

            public bool Looping
            {
                get
                {
                    return Playback.Looping;
                }
            }

            public bool Stopped
            {
                get
                {
                    return Playback.Stopped;
                }
            }

            public PlayingSound(ISoundPlayback playback, int startFrame, bool ui)
            {
                Playback = playback;
                StartFrame = startFrame;
                UI = ui;
                m_refCount = 1;
            }

            public void Stop()
            {
                if (m_refCount > 0)
                {
                    m_refCount--;
                    if (m_refCount == 0)
                    {
                        Playback.Stop();
                    }
                }
            }

            public void AddRef()
            {
                m_refCount++;
            }
        }

        private IAudio m_audio;
        private AudioListener m_listener;
        private float m_rate;

        private IMusicPlayback m_music;
        private List<PlayingSound> m_sounds;

        private int m_frame;

        public GameAudio(IAudio audio)
        {
            m_audio = audio;
            m_listener = new AudioListener();
            m_rate = 1.0f;

            m_music = null;
            m_sounds = new List<PlayingSound>();

            m_frame = 0;
        }

        public void PlayMusic(string path, float transitionTime, bool looping = true)
        {
            if (path == null)
            {
                // Stop music
                if (m_music != null)
                {
                    m_music.FadeToVolume(0.0f, transitionTime, true);
                    m_music = null;
                }
            }
            else
            {
                // Switch or continue music
                if (m_music == null || m_music.Music.Path != path || m_music.Stopped)
                {
                    if (m_music != null)
                    {
                        m_music.FadeToVolume(0.0f, transitionTime, true);
                        m_music = null;
                    }
                    m_music = m_audio.PlayMusic(path, looping, transitionTime);
                }
            }
        }

        public IStoppable PlaySound(string path, bool looping = false)
        {
            return PlaySound(path, looping, false);
        }

        public IStoppable PlayUISound(string path, bool looping = false)
        {
            return PlaySound(path, looping, true);
        }

        public IStoppable PlaySound(string path, bool looping, bool ui)
        {
            foreach (var sound in m_sounds)
            {
                if (sound.Playback.Sound.Path == path &&
                    sound.Looping == looping &&
                    sound.UI == ui &&
                    (looping || sound.StartFrame == m_frame))
                {
                    sound.AddRef();
                    return sound;
                }
            }

            var playback = m_audio.PlaySound(path, looping);
            if (playback != null)
            {
                if (!ui)
                {
                    playback.Rate = m_rate;
                }
                var sound = new PlayingSound(playback, m_frame, ui);
                m_sounds.Add(sound);
                return sound;
            }
            return null;
        }

        public void Update()
        {
            for (int i = m_sounds.Count - 1; i >= 0; --i)
            {
                var sound = m_sounds[i];
                if (sound.Stopped)
                {
                    m_sounds.RemoveAt(i);
                }
            }
            m_frame++;
        }
    }
}

