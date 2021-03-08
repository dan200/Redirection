using Dan200.Core.Util;

namespace Dan200.Core.Input
{
    public interface IGamepad
    {
        GamepadType Type { get; set; }
        bool EnableRumble { get; set; }

        bool Connected { get; }
        bool SupportsRumble { get; }
        IReadOnlyDictionary<GamepadButton, IButton> Buttons { get; }
        IReadOnlyDictionary<GamepadAxis, IAxis> Axes { get; }
        IReadOnlyDictionary<GamepadJoystick, IJoystick> Joysticks { get; }
        void Rumble(float strength, float duration);
        void DetectType();
    }
}
