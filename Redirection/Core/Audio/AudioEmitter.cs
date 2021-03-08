using OpenTK;
using System.Collections.Generic;

namespace Dan200.Core.Audio
{
    public class AudioEmitter
    {
        private IAudio m_audio;
        private Vector3 m_position;
        private List<ISoundPlayback> m_sounds;

        public Vector3 Position
        {
            get
            {
                return m_position;
            }
            set
            {
                m_position = value;
            }
        }

        public AudioEmitter(IAudio audio)
        {
            m_audio = audio;
            m_position = Vector3.Zero;
            m_sounds = new List<ISoundPlayback>();
        }

        public IPlayback PlaySound(Sound sound, bool looping = false)
        {
            return StorePlayback(m_audio.PlaySound(sound, looping));
        }

        public IPlayback PlaySound(string path, bool looping = false)
        {
            return StorePlayback(m_audio.PlaySound(path, looping));
        }

        public void Update()
        {
            for (int i = m_sounds.Count - 1; i >= 0; --i)
            {
                var sound = m_sounds[i];
                if (sound.Stopped)
                {
                    // Update sound position here
                    m_sounds.RemoveAt(i);
                }
            }
        }

        private ISoundPlayback StorePlayback(ISoundPlayback playback)
        {
            if (playback != null)
            {
                // Init sound position/range here
                m_sounds.Add(playback);
            }
            return playback;
        }
    }
}

