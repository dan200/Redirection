using Dan200.Core.Main;
using Dan200.Core.Render;
using Dan200.Core.Util;
using SDL2;

namespace Dan200.Core.Input
{
    // Values match SDL_Keycode 
    // Add new ones when needed from https://wiki.libsdl.org/SDLKeycodeLookup
    public enum Key
    {
        None = 0,
        Backspace = 8,
        Tab = 9,
        Return = 13,
        Escape = 27,
        Space = 32,
        ExclamationMark = 33, // No image
        DoubleQuote = 34, // No image
        Hash = 35, // No image
        Dollar = 36, // No image
        Percent = 37, // No image
        Ampersand = 38, // No image
        Apostrophe = 39,
        LeftParenthesis = 40, // No image
        RightParenthesis = 41, // No image
        Asterisk = 42,
        Plus = 43,
        Comma = 44,
        Minus = 45,
        Period = 46,
        Slash = 47,
        Zero = 48,
        One = 49,
        Two = 50,
        Three = 51,
        Four = 52,
        Five = 53,
        Six = 54,
        Seven = 55,
        Eight = 56,
        Nine = 57,
        Colon = 58,
        Semicolon = 59,
        LessThan = 60,
        Equals = 61,
        GreaterThan = 62,
        QuestionMark = 63,
        LeftBracket = 91,
        BackSlash = 92,
        RightBracket = 93,
        Caret = 94, // No image
        Underscore = 95, // No image
        BackQuote = 96,
        A = 97,
        B = 98,
        C = 99,
        D = 100,
        E = 101,
        F = 102,
        G = 103,
        H = 104,
        I = 105,
        J = 106,
        K = 107,
        L = 108,
        M = 109,
        N = 110,
        O = 111,
        P = 112,
        Q = 113,
        R = 114,
        S = 115,
        T = 116,
        U = 117,
        V = 118,
        W = 119,
        X = 120,
        Y = 121,
        Z = 122,
        Delete = 127,
        CapsLock = 1073741881, // No image
        F1 = 1073741882, // No image
        F2 = 1073741883, // No image
        F3 = 1073741884, // No image
        F4 = 1073741885, // No image
        F5 = 1073741886, // No image
        F6 = 1073741887, // No image
        F7 = 1073741888, // No image
        F8 = 1073741889, // No image
        F9 = 1073741890, // No image
        F10 = 1073741891, // No image
        F11 = 1073741892, // No image
        F12 = 1073741893, // No image
        PrintScreen = 1073741894,
        ScrollLock = 1073741895, // No image
        Pause = 1073741896, // No image
        Insert = 1073741897,
        Home = 1073741898,
        PageUp = 1073741899,
        End = 1073741901,
        PageDown = 1073741902,
        Right = 1073741903,
        Left = 1073741904,
        Down = 1073741905,
        Up = 1073741906,
        NumLock = 1073741907,
        NumpadDivide = 1073741908,
        NumpadMultiply = 1073741909,
        NumpadMinus = 1073741910,
        NumpadPlus = 1073741911,
        NumpadEnter = 1073741912,
        NumpadOne = 1073741913,
        NumpadTwo = 1073741914,
        NumpadThree = 1073741915,
        NumpadFour = 1073741916,
        NumpadFive = 1073741917,
        NumpadSix = 1073741918,
        NumpadSeven = 1073741919,
        NumpadEight = 1073741920,
        NumpadNine = 1073741921,
        NumpadZero = 1073741922,
        NumpadPeriod = 1073741923,
        NumpadEquals = 1073741927,
        NumpadComma = 1073741957,
        LeftCtrl = 1073742048,
        LeftShift = 1073742049,
        LeftAlt = 1073742050,
        LeftGUI = 1073742051,
        RightCtrl = 1073742052,
        RightShift = 1073742053,
        RightAlt = 1073742054,
        RightGUI = 1073742055,
    }

    public static class KeyExtensions
    {
        private static SDL.SDL_Scancode GetEquivalentScancode(Key key)
        {
            switch (key)
            {
                case Key.Backspace: return SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE;
                case Key.Tab: return SDL.SDL_Scancode.SDL_SCANCODE_TAB;
                case Key.Return: return SDL.SDL_Scancode.SDL_SCANCODE_RETURN;
                case Key.Escape: return SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE;
                case Key.Space: return SDL.SDL_Scancode.SDL_SCANCODE_SPACE;
                case Key.Apostrophe: return SDL.SDL_Scancode.SDL_SCANCODE_APOSTROPHE;
                case Key.Comma: return SDL.SDL_Scancode.SDL_SCANCODE_COMMA;
                case Key.Minus: return SDL.SDL_Scancode.SDL_SCANCODE_MINUS;
                case Key.Period: return SDL.SDL_Scancode.SDL_SCANCODE_PERIOD;
                case Key.Slash: return SDL.SDL_Scancode.SDL_SCANCODE_SLASH;
                case Key.Zero: return SDL.SDL_Scancode.SDL_SCANCODE_0;
                case Key.One: return SDL.SDL_Scancode.SDL_SCANCODE_1;
                case Key.Two: return SDL.SDL_Scancode.SDL_SCANCODE_2;
                case Key.Three: return SDL.SDL_Scancode.SDL_SCANCODE_3;
                case Key.Four: return SDL.SDL_Scancode.SDL_SCANCODE_4;
                case Key.Five: return SDL.SDL_Scancode.SDL_SCANCODE_5;
                case Key.Six: return SDL.SDL_Scancode.SDL_SCANCODE_6;
                case Key.Seven: return SDL.SDL_Scancode.SDL_SCANCODE_7;
                case Key.Eight: return SDL.SDL_Scancode.SDL_SCANCODE_8;
                case Key.Nine: return SDL.SDL_Scancode.SDL_SCANCODE_9;
                case Key.Semicolon: return SDL.SDL_Scancode.SDL_SCANCODE_SEMICOLON;
                case Key.Equals: return SDL.SDL_Scancode.SDL_SCANCODE_EQUALS;
                case Key.LeftBracket: return SDL.SDL_Scancode.SDL_SCANCODE_LEFTBRACKET;
                case Key.BackSlash: return SDL.SDL_Scancode.SDL_SCANCODE_BACKSLASH;
                case Key.RightBracket: return SDL.SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET;
                case Key.BackQuote: return SDL.SDL_Scancode.SDL_SCANCODE_GRAVE;
                case Key.A: return SDL.SDL_Scancode.SDL_SCANCODE_A;
                case Key.B: return SDL.SDL_Scancode.SDL_SCANCODE_B;
                case Key.C: return SDL.SDL_Scancode.SDL_SCANCODE_C;
                case Key.D: return SDL.SDL_Scancode.SDL_SCANCODE_D;
                case Key.E: return SDL.SDL_Scancode.SDL_SCANCODE_E;
                case Key.F: return SDL.SDL_Scancode.SDL_SCANCODE_F;
                case Key.G: return SDL.SDL_Scancode.SDL_SCANCODE_G;
                case Key.H: return SDL.SDL_Scancode.SDL_SCANCODE_H;
                case Key.I: return SDL.SDL_Scancode.SDL_SCANCODE_I;
                case Key.J: return SDL.SDL_Scancode.SDL_SCANCODE_J;
                case Key.K: return SDL.SDL_Scancode.SDL_SCANCODE_K;
                case Key.L: return SDL.SDL_Scancode.SDL_SCANCODE_L;
                case Key.M: return SDL.SDL_Scancode.SDL_SCANCODE_M;
                case Key.N: return SDL.SDL_Scancode.SDL_SCANCODE_N;
                case Key.O: return SDL.SDL_Scancode.SDL_SCANCODE_O;
                case Key.P: return SDL.SDL_Scancode.SDL_SCANCODE_P;
                case Key.Q: return SDL.SDL_Scancode.SDL_SCANCODE_Q;
                case Key.R: return SDL.SDL_Scancode.SDL_SCANCODE_R;
                case Key.S: return SDL.SDL_Scancode.SDL_SCANCODE_S;
                case Key.T: return SDL.SDL_Scancode.SDL_SCANCODE_T;
                case Key.U: return SDL.SDL_Scancode.SDL_SCANCODE_U;
                case Key.V: return SDL.SDL_Scancode.SDL_SCANCODE_V;
                case Key.W: return SDL.SDL_Scancode.SDL_SCANCODE_W;
                case Key.X: return SDL.SDL_Scancode.SDL_SCANCODE_X;
                case Key.Y: return SDL.SDL_Scancode.SDL_SCANCODE_Y;
                case Key.Z: return SDL.SDL_Scancode.SDL_SCANCODE_Z;
                case Key.Delete: return SDL.SDL_Scancode.SDL_SCANCODE_DELETE;
                case Key.CapsLock: return SDL.SDL_Scancode.SDL_SCANCODE_CAPSLOCK;
                case Key.F1: return SDL.SDL_Scancode.SDL_SCANCODE_F1;
                case Key.F2: return SDL.SDL_Scancode.SDL_SCANCODE_F2;
                case Key.F3: return SDL.SDL_Scancode.SDL_SCANCODE_F3;
                case Key.F4: return SDL.SDL_Scancode.SDL_SCANCODE_F4;
                case Key.F5: return SDL.SDL_Scancode.SDL_SCANCODE_F5;
                case Key.F6: return SDL.SDL_Scancode.SDL_SCANCODE_F6;
                case Key.F7: return SDL.SDL_Scancode.SDL_SCANCODE_F7;
                case Key.F8: return SDL.SDL_Scancode.SDL_SCANCODE_F8;
                case Key.F9: return SDL.SDL_Scancode.SDL_SCANCODE_F9;
                case Key.F10: return SDL.SDL_Scancode.SDL_SCANCODE_F10;
                case Key.F11: return SDL.SDL_Scancode.SDL_SCANCODE_F11;
                case Key.F12: return SDL.SDL_Scancode.SDL_SCANCODE_F12;
                case Key.PrintScreen: return SDL.SDL_Scancode.SDL_SCANCODE_PRINTSCREEN;
                case Key.ScrollLock: return SDL.SDL_Scancode.SDL_SCANCODE_SCROLLLOCK;
                case Key.Pause: return SDL.SDL_Scancode.SDL_SCANCODE_PAUSE;
                case Key.Insert: return SDL.SDL_Scancode.SDL_SCANCODE_INSERT;
                case Key.Home: return SDL.SDL_Scancode.SDL_SCANCODE_HOME;
                case Key.PageUp: return SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP;
                case Key.End: return SDL.SDL_Scancode.SDL_SCANCODE_END;
                case Key.PageDown: return SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN;
                case Key.Right: return SDL.SDL_Scancode.SDL_SCANCODE_RIGHT;
                case Key.Left: return SDL.SDL_Scancode.SDL_SCANCODE_LEFT;
                case Key.Down: return SDL.SDL_Scancode.SDL_SCANCODE_DOWN;
                case Key.Up: return SDL.SDL_Scancode.SDL_SCANCODE_UP;
                case Key.NumLock: return SDL.SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR;
                case Key.NumpadDivide: return SDL.SDL_Scancode.SDL_SCANCODE_KP_DIVIDE;
                case Key.NumpadMultiply: return SDL.SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY;
                case Key.NumpadMinus: return SDL.SDL_Scancode.SDL_SCANCODE_KP_MINUS;
                case Key.NumpadPlus: return SDL.SDL_Scancode.SDL_SCANCODE_KP_PLUS;
                case Key.NumpadEnter: return SDL.SDL_Scancode.SDL_SCANCODE_KP_ENTER;
                case Key.NumpadOne: return SDL.SDL_Scancode.SDL_SCANCODE_KP_1;
                case Key.NumpadTwo: return SDL.SDL_Scancode.SDL_SCANCODE_KP_2;
                case Key.NumpadThree: return SDL.SDL_Scancode.SDL_SCANCODE_KP_3;
                case Key.NumpadFour: return SDL.SDL_Scancode.SDL_SCANCODE_KP_4;
                case Key.NumpadFive: return SDL.SDL_Scancode.SDL_SCANCODE_KP_5;
                case Key.NumpadSix: return SDL.SDL_Scancode.SDL_SCANCODE_KP_6;
                case Key.NumpadSeven: return SDL.SDL_Scancode.SDL_SCANCODE_KP_7;
                case Key.NumpadEight: return SDL.SDL_Scancode.SDL_SCANCODE_KP_8;
                case Key.NumpadNine: return SDL.SDL_Scancode.SDL_SCANCODE_KP_9;
                case Key.NumpadZero: return SDL.SDL_Scancode.SDL_SCANCODE_KP_0;
                case Key.NumpadPeriod: return SDL.SDL_Scancode.SDL_SCANCODE_KP_PERIOD;
                case Key.NumpadEquals: return SDL.SDL_Scancode.SDL_SCANCODE_KP_EQUALS;
                case Key.NumpadComma: return SDL.SDL_Scancode.SDL_SCANCODE_KP_COMMA;
                case Key.LeftCtrl: return SDL.SDL_Scancode.SDL_SCANCODE_LCTRL;
                case Key.LeftShift: return SDL.SDL_Scancode.SDL_SCANCODE_LSHIFT;
                case Key.LeftAlt: return SDL.SDL_Scancode.SDL_SCANCODE_LALT;
                case Key.LeftGUI: return SDL.SDL_Scancode.SDL_SCANCODE_LGUI;
                case Key.RightCtrl: return SDL.SDL_Scancode.SDL_SCANCODE_RCTRL;
                case Key.RightShift: return SDL.SDL_Scancode.SDL_SCANCODE_RSHIFT;
                case Key.RightAlt: return SDL.SDL_Scancode.SDL_SCANCODE_RALT;
                case Key.RightGUI: return SDL.SDL_Scancode.SDL_SCANCODE_RGUI;
                default: return SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN;
            }
        }

        public static Key RemapToLocal(this Key key)
        {
            // Remaps a key from the universal layout to the local layout of the system
            // Uses the SDL2 scancode to keycode mapping
            var scancode = GetEquivalentScancode(key);
            if (scancode != SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN)
            {
                var keycode = SDL.SDL_GetKeyFromScancode(scancode);
                if (keycode != SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    return (Key)keycode;
                }
            }
            return key;
        }

        public static string GetPrompt(this Key key)
        {
            var path = key.GetPromptPath();
            if (path != null)
            {
                return '[' + path + ']';
            }

            var name = SDL.SDL_GetKeyName((SDL.SDL_Keycode)key);
            if (name != null && name.Length > 0)
            {
                return name;
            }

            return key.ToString();
        }

        public static string GetPromptPath(this Key key)
        {
            string buttonName;
            switch (key)
            {
                case Key.None:
                    {
                        buttonName = "blank";
                        break;
                    }
                case Key.LeftGUI:
                case Key.RightGUI:
                    {
                        switch (App.Platform)
                        {
                            case Platform.OSX:
                                {
                                    buttonName = "cmd";
                                    break;
                                }
                            case Platform.Windows:
                            default:
                                {
                                    buttonName = "windows";
                                    break;
                                }
                        }
                        break;
                    }
                case Key.LeftShift:
                case Key.RightShift:
                    {
                        buttonName = "shift";
                        break;
                    }
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    {
                        buttonName = "ctrl";
                        break;
                    }
                case Key.LeftAlt:
                case Key.RightAlt:
                    {
                        buttonName = "alt";
                        break;
                    }
                case Key.Zero:
                case Key.NumpadZero:
                    {
                        buttonName = "zero";
                        break;
                    }
                case Key.NumpadOne:
                case Key.NumpadTwo:
                case Key.NumpadThree:
                case Key.NumpadFour:
                case Key.NumpadFive:
                case Key.NumpadSix:
                case Key.NumpadSeven:
                case Key.NumpadEight:
                case Key.NumpadNine:
                    {
                        var equivalentKey = (key - Key.NumpadOne) + Key.One;
                        buttonName = equivalentKey.ToString().ToLowerUnderscored();
                        break;
                    }
                case Key.Minus:
                case Key.NumpadMinus:
                    {
                        buttonName = "minus";
                        break;
                    }
                case Key.Period:
                case Key.NumpadPeriod:
                    {
                        buttonName = "period";
                        break;
                    }
                case Key.Comma:
                case Key.NumpadComma:
                    {
                        buttonName = "comma";
                        break;
                    }
                case Key.BackQuote:
                    {
                        buttonName = "tilde";
                        break;
                    }
                default:
                    {
                        buttonName = key.ToString().ToLowerUnderscored();
                        break;
                    }
            }

            var path = "gui/prompts/keyboard/" + buttonName + ".png";
            if (Assets.Assets.Exists<Texture>(path))
            {
                return path;
            }
            return null;
        }
    }
}

