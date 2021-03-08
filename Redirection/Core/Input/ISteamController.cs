using Dan200.Core.Util;

namespace Dan200.Core.Input
{
    public interface ISteamController
    {
        bool Connected { get; }
        string ActionSet { get; set; }
        IReadOnlyDictionary<string, IButton> Buttons { get; }
        IReadOnlyDictionary<string, IAxis> Axes { get; }
        IReadOnlyDictionary<string, IJoystick> Joysticks { get; }
        void Rumble(float duration);
    }
}

