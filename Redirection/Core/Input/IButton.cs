namespace Dan200.Core.Input
{
    public interface IButton
    {
        bool Held { get; }
        bool Pressed { get; }
        bool Released { get; }
        bool Repeated { get; }
    }
}

