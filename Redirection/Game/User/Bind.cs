using Dan200.Core.Input;
using System;

namespace Dan200.Game.User
{
    public enum Bind
    {
        Place,
        Remove,
        Tweak,
        Play,
        Rewind,
        FastForward,
        IncreaseDelay,
        DecreaseDelay
    }

    public static class BindExtensions
    {
        public static MouseButton GetDefaultMouseButton(this Bind bind)
        {
            switch (bind)
            {
                case Bind.Place:
                    {
                        return MouseButton.Left;
                    }
                case Bind.Remove:
                    {
                        return MouseButton.Right;
                    }
                case Bind.Tweak:
                    {
                        return MouseButton.Left;
                    }
                default:
                    {
                        return MouseButton.None;
                    }
            }
        }

        public static Key GetDefaultKey(this Bind bind)
        {
            switch (bind)
            {
                case Bind.Place:
                case Bind.Remove:
                case Bind.Tweak:
                    {
                        return Key.None;
                    }
                case Bind.Play:
                    {
                        return Key.Z.RemapToLocal();
                    }
                case Bind.Rewind:
                    {
                        return Key.Z.RemapToLocal();
                    }
                case Bind.FastForward:
                    {
                        return Key.X.RemapToLocal();
                    }
                case Bind.IncreaseDelay:
                    {
                        return Key.Right;
                    }
                case Bind.DecreaseDelay:
                    {
                        return Key.Left;
                    }
                default:
                    {
                        throw new Exception("Bind " + bind + " has no default key!");
                    }
            }
        }

        public static GamepadButton GetDefaultPadButton(this Bind bind)
        {
            switch (bind)
            {
                case Bind.Place:
                    {
                        return GamepadButton.A;
                    }
                case Bind.Remove:
                    {
                        return GamepadButton.X;
                    }
                case Bind.Tweak:
                    {
                        return GamepadButton.A;
                    }
                case Bind.Play:
                    {
                        return GamepadButton.Y;
                    }
                case Bind.Rewind:
                    {
                        return GamepadButton.LeftTrigger;
                    }
                case Bind.FastForward:
                    {
                        return GamepadButton.RightTrigger;
                    }
                case Bind.IncreaseDelay:
                    {
                        return GamepadButton.RightBumper;
                    }
                case Bind.DecreaseDelay:
                    {
                        return GamepadButton.LeftBumper;
                    }
                default:
                    {
                        throw new Exception("Bind " + bind + " has no default button!");
                    }
            }
        }
    }
}

