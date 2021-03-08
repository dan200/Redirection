using Dan200.Core.Assets;
using Dan200.Core.Util;

namespace Dan200.Core.Input
{
    public enum GamepadJoystick
    {
        Left,
        Right,
    }

    public static class GamepadJoystickExtensions
    {
        public static string GetPrompt(this GamepadJoystick axis, GamepadType type)
        {
            return '[' + axis.GetPromptImagePath(type) + ']';
        }

        public static string GetPromptImagePath(this GamepadJoystick axis, GamepadType type)
        {
            if (type == GamepadType.Unknown)
            {
                type = GamepadType.Xbox360;
            }

            string buttonName;
            switch (axis)
            {
                case GamepadJoystick.Left:
                    {
                        buttonName = "left_stick";
                        break;
                    }
                case GamepadJoystick.Right:
                    {
                        buttonName = "right_stick";
                        break;
                    }
                default:
                    {
                        buttonName = axis.ToString().ToLowerUnderscored();
                        break;
                    }
            }
            return AssetPath.Combine(
                "gui/prompts/" + type.ToString().ToLowerUnderscored(),
                buttonName + ".png"
            );
        }
    }
}

