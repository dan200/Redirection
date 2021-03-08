namespace Dan200.Core.Audio
{
    public interface IStoppable
    {
        bool Stopped { get; }
        void Stop();
    }

    public interface IPlayback
    {
        float Volume { get; set; }
    }
}

