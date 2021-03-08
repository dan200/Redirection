using Dan200.Core.Assets;
using Dan200.Core.Util;

namespace Dan200.Core.Input
{
    // Values must match SDL_GameControllerAxis
    public enum GamepadAxis
    {
        None = -1,
        LeftStickX = 0,
        LeftStickY = 1,
        RightStickX = 2,
        RightStickY = 3,
        LeftTrigger = 4,
        RightTrigger = 5,
    }

    public static class GamepadAxisExtensions
    {
        public static string GetPrompt(this GamepadAxis axis, GamepadType type)
        {
            if (axis == GamepadAxis.None)
            {
                return "?";
            }

            return '[' + axis.GetPromptImagePath(type) + ']';
        }

        private static string GetPromptImagePath(this GamepadAxis axis, GamepadType type)
        {
            if (type == GamepadType.Unknown)
            {
                type = GamepadType.Xbox360;
            }

            string buttonName;
            switch (axis)
            {
                case GamepadAxis.LeftStickX:
                case GamepadAxis.LeftStickY:
                    {
                        buttonName = "left_stick";
                        break;
                    }
                case GamepadAxis.RightStickX:
                case GamepadAxis.RightStickY:
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

