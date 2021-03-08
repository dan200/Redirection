namespace Dan200.Core.Audio
{
    public interface IMusicPlayback : IPlayback, IStoppable
    {
        bool Looping { get; }
        Music Music { get; }
        void FadeToVolume(float target, float duration, bool thenStop = false);
    }
}

