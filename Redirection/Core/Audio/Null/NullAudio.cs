namespace Dan200.Core.Audio.Null
{
    public class NullAudio : IAudio
    {
        public bool EnableSound
        {
            get;
            set;
        }

        public float SoundVolume
        {
            get;
            set;
        }

        public bool EnableMusic
        {
            get;
            set;
        }

        public float MusicVolume
        {
            get;
            set;
        }

        public NullAudio()
        {
            EnableSound = true;
            SoundVolume = 1.0f;
            EnableMusic = true;
            MusicVolume = 1.0f;
        }

        public ISoundPlayback PlaySound(Sound sound, bool looping)
        {
            return null;
        }

        public IMusicPlayback PlayMusic(Music music, bool looping, float fadeInTime)
        {
            return null;
        }

        public ICustomPlayback PlayCustom(ICustomAudioSource source, int channels, int sampleRate)
        {
            return null;
        }
    }
}

