using Dan200.Core.Main;
using OpenTK.Audio.OpenAL;
using System;

namespace Dan200.Core.Audio.OpenAL
{
    public class OpenALSoundPlayback : ISoundPlayback
    {
        private OpenALSoundSource m_source;
        private Sound m_sound;

        private bool m_looping;
        private float m_rate;
        private float m_volume;
        private bool m_complete;

        public Sound Sound
        {
            get
            {
                return m_sound;
            }
        }

        public bool Looping
        {
            get
            {
                return m_looping;
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
                if (!m_complete && m_rate != value)
                {
                    m_rate = value;
                    UpdateSpeed();
                }
            }
        }

        public float Volume
        {
            get
            {
                return m_volume;
            }
            set
            {
                if (!m_complete && m_volume != value)
                {
                    m_volume = value;
                    UpdateVolume();
                }
            }
        }

        public bool Stopped
        {
            get
            {
                return m_complete;
            }
        }

        public OpenALSoundPlayback(OpenALSoundSource source, OpenALSound sound, bool looping)
        {
            m_source = source;
            m_sound = sound;

            m_looping = looping;
            m_rate = 1.0f;
            m_volume = 1.0f;
            m_complete = false;

            AL.Source(m_source.ALSource, ALSourcei.Buffer, (int)sound.ALBuffer);
            AL.Source(m_source.ALSource, ALSourceb.Looping, looping);
            App.CheckOpenALError();

            UpdateSpeed();
            UpdateVolume();
        }

        public void Stop()
        {
            if (!m_complete)
            {
                AL.SourceStop(m_source.ALSource);
                AL.Source(m_source.ALSource, ALSourcei.Buffer, 0);
                App.CheckOpenALError();
                m_complete = true;
            }
        }

        public void Update(float dt)
        {
            if (!m_complete)
            {
                CheckComplete();
            }
        }

        public void UpdateVolume()
        {
            if (!m_complete)
            {
                var globalVolume = OpenALAudio.Instance.EnableSound ? OpenALAudio.Instance.SoundVolume : 0.0f;
                AL.Source(m_source.ALSource, ALSourcef.Gain, m_volume * globalVolume);
                App.CheckOpenALError();
            }
        }

        private float ClampPitch(float pitch)
        {
            return Math.Min(Math.Max(pitch, 0.5f), 2.0f);
        }

        private void UpdateSpeed()
        {
            if (m_rate >= 0.01f)
            {
                var state = AL.GetSourceState(m_source.ALSource);
                AL.Source(m_source.ALSource, ALSourcef.Pitch, ClampPitch(m_rate));
                if (state != ALSourceState.Playing)
                {
                    AL.SourcePlay(m_source.ALSource);
                }
                App.CheckOpenALError();
            }
            else
            {
                var state = AL.GetSourceState(m_source.ALSource);
                if (state == ALSourceState.Playing)
                {
                    AL.SourcePause(m_source.ALSource);
                }
                App.CheckOpenALError();
            }
        }

        private void CheckComplete()
        {
            var state = AL.GetSourceState(m_source.ALSource);
            App.CheckOpenALError();
            if (state == ALSourceState.Stopped)
            {
                AL.Source(m_source.ALSource, ALSourcei.Buffer, 0);
                App.CheckOpenALError();
                m_complete = true;
            }
        }
    }
}

