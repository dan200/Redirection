namespace Dan200.Core.Audio
{
    public interface ISoundPlayback : IPlayback, IStoppable
    {
        bool Looping { get; }
        float Rate { get; set; }
        Sound Sound { get; }
    }
}

