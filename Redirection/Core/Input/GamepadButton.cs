using Dan200.Core.Assets;
using Dan200.Core.Render;
using Dan200.Core.Util;

namespace Dan200.Core.Input
{
    // Values under 100 must match SDL_GameControllerButton
    public enum GamepadButton
    {
        None = -1,

        // Real buttons
        A = 0,
        B = 1,
        X = 2,
        Y = 3,
        Back = 4,
        Start = 6,
        LeftStick = 7,
        RightStick = 8,
        LeftBumper = 9,
        RightBumper = 10,
        Up = 11,
        Down = 12,
        Left = 13,
        Right = 14,

        // Virtual buttons
        LeftStickUp = 100,
        LeftStickDown,
        LeftStickLeft,
        LeftStickRight,
        RightStickUp,
        RightStickDown,
        RightStickLeft,
        RightStickRight,
        LeftTrigger,
        RightTrigger,
    }

    public static class GamepadButtonExtensions
    {
        public static bool IsVirtual(this GamepadButton button)
        {
            return (int)button >= 100;
        }

        public static string GetPrompt(this GamepadButton button, GamepadType type)
        {
            if (button == GamepadButton.None)
            {
                return "?";
            }

            var imagePath = button.GetPromptImagePath(type);
            if (imagePath != null)
            {
                return '[' + imagePath + ']';
            }
            else
            {
                return button.ToString().ToProperSpaced();
            }
        }

        private static string GetPromptImagePath(this GamepadButton button, GamepadType type)
        {
            if (type == GamepadType.Unknown || type == GamepadType.XboxOne)
            {
                type = GamepadType.Xbox360;
            }
            else if (type == GamepadType.PS4)
            {
                type = GamepadType.PS3;
            }

            string buttonName;
            switch (button)
            {
                case GamepadButton.LeftStickUp:
                case GamepadButton.LeftStickDown:
                case GamepadButton.LeftStickLeft:
                case GamepadButton.LeftStickRight:
                    {
                        buttonName = "left_stick";
                        break;
                    }
                case GamepadButton.RightStickUp:
                case GamepadButton.RightStickDown:
                case GamepadButton.RightStickLeft:
                case GamepadButton.RightStickRight:
                    {
                        buttonName = "right_stick";
                        break;
                    }
                case GamepadButton.A:
                    {
                        switch (type)
                        {
                            case GamepadType.PS3:
                            case GamepadType.PS4:
                                {
                                    buttonName = "cross";
                                    break;
                                }
                            default:
                                {
                                    buttonName = "a";
                                    break;
                                }
                        }
                        break;
                    }
                case GamepadButton.B:
                    {
                        switch (type)
                        {
                            case GamepadType.PS3:
                            case GamepadType.PS4:
                                {
                                    buttonName = "circle";
                                    break;
                                }
                            default:
                                {
                                    buttonName = "b";
                                    break;
                                }
                        }
                        break;
                    }
                case GamepadButton.X:
                    {
                        switch (type)
                        {
                            case GamepadType.PS3:
                            case GamepadType.PS4:
                                {
                                    buttonName = "square";
                                    break;
                                }
                            default:
                                {
                                    buttonName = "x";
                                    break;
                                }
                        }
                        break;
                    }
                case GamepadButton.Y:
                    {
                        switch (type)
                        {
                            case GamepadType.PS3:
                            case GamepadType.PS4:
                                {
                                    buttonName = "triangle";
                                    break;
                                }
                            default:
                                {
                                    buttonName = "y";
                                    break;
                                }
                        }
                        break;
                    }
                case GamepadButton.Back:
                    {
                        switch (type)
                        {
                            case GamepadType.PS3:
                                {
                                    buttonName = "select";
                                    break;
                                }
                            case GamepadType.PS4:
                                {
                                    buttonName = "share";
                                    break;
                                }
                            case GamepadType.XboxOne:
                                {
                                    buttonName = "view";
                                    break;
                                }
                            default:
                                {
                                    buttonName = "back";
                                    break;
                                }
                        }
                        break;
                    }
                case GamepadButton.Start:
                    {
                        switch (type)
                        {
                            case GamepadType.PS4:
                                {
                                    buttonName = "options";
                                    break;
                                }
                            case GamepadType.XboxOne:
                                {
                                    buttonName = "menu";
                                    break;
                                }
                            default:
                                {
                                    buttonName = "start";
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    {
                        buttonName = button.ToString().ToLowerUnderscored();
                        break;
                    }
            }

            var path = AssetPath.Combine(
                "gui/prompts/" + type.ToString().ToLowerUnderscored(),
                buttonName + ".png"
            );
            if (Assets.Assets.Exists<Texture>(path))
            {
                return path;
            }
            return null;
        }
    }
}

