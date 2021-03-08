using Dan200.Core.Main;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System;

namespace Dan200.Core.Audio.OpenAL
{
    public class OpenALAudio : IAudio, IDisposable
    {
        private const int NUM_SOUND_SOURCES = 16;
        private const int NUM_MUSIC_SOURCES = 2;
        private const int NUM_CUSTOM_SOURCES = 1;

        public static OpenALAudio Instance
        {
            get;
            private set;
        }

        private AudioContext m_context;
        private XRamExtension m_xram;
        private OpenALSoundSource[] m_sound;
        private OpenALMusicPlayback[] m_music;
        private OpenALCustomPlayback[] m_custom;

        private bool m_enableSound;
        private float m_soundVolume;
        private bool m_enableMusic;
        private float m_musicVolume;

        public XRamExtension XRam
        {
            get
            {
                return m_xram;
            }
        }

        public bool EnableSound
        {
            get
            {
                return m_enableSound;
            }
            set
            {
                if (m_enableSound != value)
                {
                    m_enableSound = value;
                    UpdateSoundVolume();
                }
            }
        }

        public float SoundVolume
        {
            get
            {
                return m_soundVolume;
            }
            set
            {
                if (m_soundVolume != value)
                {
                    m_soundVolume = value;
                    UpdateSoundVolume();
                }
            }
        }

        public bool EnableMusic
        {
            get
            {
                return m_enableMusic;
            }
            set
            {
                if (m_enableMusic != value)
                {
                    m_enableMusic = value;
                    UpdateMusicVolume();
                }
            }
        }

        public float MusicVolume
        {
            get
            {
                return m_musicVolume;
            }
            set
            {
                if (m_musicVolume != value)
                {
                    m_musicVolume = value;
                    UpdateMusicVolume();
                }
            }
        }

        public OpenALAudio()
        {
            Instance = this;

            m_enableSound = true;
            m_soundVolume = 1.0f;
            m_enableMusic = true;
            m_musicVolume = 1.0f;

            // Init context
            m_context = new AudioContext();
            m_xram = new XRamExtension();

            // Create some sources
            m_sound = new OpenALSoundSource[NUM_SOUND_SOURCES];
            for (int i = 0; i < m_sound.Length; ++i)
            {
                m_sound[i] = new OpenALSoundSource();
            }
            m_music = new OpenALMusicPlayback[NUM_MUSIC_SOURCES];
            m_custom = new OpenALCustomPlayback[NUM_CUSTOM_SOURCES];

            UpdateSoundVolume();
            UpdateMusicVolume();
            App.CheckOpenALError();
        }

        public void Dispose()
        {
            for (int i = 0; i < m_sound.Length; ++i)
            {
                var source = m_sound[i];
                source.Dispose();
            }
            for (int i = 0; i < m_music.Length; ++i)
            {
                var source = m_music[i];
                if (source != null)
                {
                    source.Dispose();
                }
            }
            for (int i = 0; i < m_custom.Length; ++i)
            {
                var source = m_custom[i];
                if (source != null)
                {
                    source.Dispose();
                }
            }
            m_context.Dispose();

            Instance = null;
        }

        public void Update(float dt)
        {
            for (int i = 0; i < m_sound.Length; ++i)
            {
                var source = m_sound[i];
                source.Update(dt);
            }
            for (int i = 0; i < m_music.Length; ++i)
            {
                var playback = m_music[i];
                if (playback != null)
                {
                    playback.Update(dt);
                    if (playback.Stopped)
                    {
                        playback.Dispose();
                        m_music[i] = null;
                    }
                }
            }
            for (int i = 0; i < m_custom.Length; ++i)
            {
                var playback = m_custom[i];
                if (playback != null)
                {
                    playback.Update(dt);
                    if (playback.Stopped)
                    {
                        playback.Dispose();
                        m_custom[i] = null;
                    }
                }
            }
        }

        public ISoundPlayback PlaySound(Sound sound, bool looping = false)
        {
            // Find a free source and play the sound on it
            for (int i = 0; i < m_sound.Length; ++i)
            {
                var source = m_sound[i];
                if (source.CurrentPlayback == null || source.CurrentPlayback.Stopped)
                {
                    return source.Play((OpenALSound)sound, looping);
                }
            }
            return null;
        }

        public void StopSound(Sound sound)
        {
            for (int i = 0; i < m_sound.Length; ++i)
            {
                var source = m_sound[i];
                if (source.CurrentPlayback != null && source.CurrentPlayback.Sound == sound)
                {
                    source.CurrentPlayback.Stop();
                }
            }
        }

        public IMusicPlayback PlayMusic(Music music, bool looping, float fadeInTime)
        {
            for (int i = 0; i < m_music.Length; ++i)
            {
                var playback = m_music[i];
                if (playback == null || playback.Stopped)
                {
                    if (playback != null)
                    {
                        playback.Dispose();
                    }
                    m_music[i] = new OpenALMusicPlayback((OpenALMusic)music, looping, fadeInTime);
                    return m_music[i];
                }
            }
            return null;
        }

        public void StopMusic(Music music)
        {
            for (int i = 0; i < m_music.Length; ++i)
            {
                var playback = m_music[i];
                if (playback != null && playback.Music == music)
                {
                    playback.Stop();
                }
            }
        }

        public ICustomPlayback PlayCustom(ICustomAudioSource source, int channels, int sampleRate)
        {
            for (int i = 0; i < m_custom.Length; ++i)
            {
                var playback = m_custom[i];
                if (playback == null || playback.Stopped)
                {
                    if (playback != null)
                    {
                        playback.Dispose();
                    }
                    m_custom[i] = new OpenALCustomPlayback(source, channels, sampleRate);
                    return m_custom[i];
                }
            }
            return null;
        }

        private void UpdateSoundVolume()
        {
            for (int i = 0; i < m_sound.Length; ++i)
            {
                var source = m_sound[i];
                source.UpdateVolume();
            }
            for (int i = 0; i < m_custom.Length; ++i)
            {
                var source = m_custom[i];
                if (source != null)
                {
                    source.UpdateVolume();
                }
            }
        }

        private void UpdateMusicVolume()
        {
            for (int i = 0; i < m_music.Length; ++i)
            {
                var source = m_music[i];
                if (source != null)
                {
                    source.UpdateVolume();
                }
            }
        }
    }
}

