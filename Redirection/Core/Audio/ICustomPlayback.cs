namespace Dan200.Core.Audio
{
    public interface ICustomPlayback : IPlayback, IStoppable
    {
        ICustomAudioSource Source { get; }
        int Channels { get; }
        int SampleRate { get; }
    }
}
