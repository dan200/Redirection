namespace Dan200.Core.Audio
{
    public interface ICustomAudioSource
    {
        void GenerateSamples(ICustomPlayback playback, short[] data, int start, int numSamples);
    }
}
