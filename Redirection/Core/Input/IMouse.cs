namespace Dan200.Core.Input
{
    public interface IMouse
    {
        int X { get; }
        int Y { get; }
        int DX { get; }
        int DY { get; }
        int Wheel { get; }
        Dan200.Core.Util.IReadOnlyDictionary<MouseButton, IButton> Buttons { get; }
    }
}
