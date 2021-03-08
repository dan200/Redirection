using Dan200.Core.Main;

namespace Dan200.Core.Audio
{
    public interface IAudio
    {
        bool EnableSound { get; set; }
        float SoundVolume { get; set; }

        bool EnableMusic { get; set; }
        float MusicVolume { get; set; }

        ISoundPlayback PlaySound(Sound sound, bool looping);
        IMusicPlayback PlayMusic(Music music, bool looping, float fadeInTime);
        ICustomPlayback PlayCustom(ICustomAudioSource source, int channels, int sampleRate);
    }

    public static class IAudioExtensions
    {
        public static ISoundPlayback PlaySound(this IAudio audio, string path, bool looping = false)
        {
            if (Assets.Assets.Exists<Sound>(path))
            {
                var sound = Assets.Assets.Get<Sound>(path);
                return audio.PlaySound(sound, looping);
            }
            else
            {
                App.Log("Error: Attempt to play missing sound {0}", path);
                return null;
            }
        }

        public static IMusicPlayback PlayMusic(this IAudio audio, string path, bool looping, float fadeInTime)
        {
            if (Assets.Assets.Exists<Music>(path))
            {
                var sound = Assets.Assets.Get<Music>(path);
                return audio.PlayMusic(sound, looping, fadeInTime);
            }
            else
            {
                App.Log("Error: Attempt to play missing music {0}", path);
                return null;
            }
        }
    }
}

