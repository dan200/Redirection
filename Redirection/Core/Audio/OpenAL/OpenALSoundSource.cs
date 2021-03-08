using Dan200.Core.Main;
using OpenTK.Audio.OpenAL;
using System;

namespace Dan200.Core.Audio.OpenAL
{
    public class OpenALSoundSource : IDisposable
    {
        private uint m_source;
        private OpenALSoundPlayback m_playback;

        public uint ALSource
        {
            get
            {
                return m_source;
            }
        }

        public ISoundPlayback CurrentPlayback
        {
            get
            {
                return m_playback;
            }
        }

        public OpenALSoundSource()
        {
            AL.GenSource(out m_source);
            App.CheckOpenALError();
            m_playback = null;
        }

        public void Dispose()
        {
            if (m_playback != null)
            {
                m_playback.Stop();
                m_playback = null;
            }
            AL.DeleteSource(ref m_source);
            App.CheckOpenALError();
        }

        public ISoundPlayback Play(OpenALSound sound, bool looping)
        {
            if (m_playback != null && !m_playback.Stopped)
            {
                m_playback.Stop();
            }
            m_playback = new OpenALSoundPlayback(this, sound, looping);
            return m_playback;
        }

        public void Update(float dt)
        {
            if (m_playback != null)
            {
                m_playback.Update(dt);
                if (m_playback.Stopped)
                {
                    m_playback = null;
                }
            }
        }

        public void UpdateVolume()
        {
            if (m_playback != null)
            {
                m_playback.UpdateVolume();
            }
        }
    }
}