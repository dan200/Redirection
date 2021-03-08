using Dan200.Core.Input;
using Dan200.Core.Lua;
using Dan200.Core.Main;
using Dan200.Core.Util;
using Dan200.Game.Game;
using Dan200.Game.Input;
using Dan200.Game.User;
using System.Text;

namespace Dan200.Game.Script
{
    public class IOAPI : API
    {
        private LevelState m_state;

        public IOAPI(LevelState state)
        {
            m_state = state;
        }

        [LuaMethod]
        public LuaArgs print(LuaArgs args)
        {
            var output = new StringBuilder();
            for (int i = 0; i < args.Length; ++i)
            {
                output.Append(args.ToString(i));
                if (i < args.Length - 1)
                {
                    output.Append(", ");
                }
            }
            App.UserLog(output.ToString());
            return LuaArgs.Empty;
        }

        [LuaMethod]
        public LuaArgs getLanguage(LuaArgs args)
        {
            return new LuaArgs(m_state.Game.Language.Code);
        }

        [LuaMethod]
        public LuaArgs canTranslate(LuaArgs args)
        {
            var language = m_state.Game.Language;
            var key = args.GetString(0);
            return new LuaArgs(language.CanTranslate(key));
        }

        [LuaMethod]
        public LuaArgs translate(LuaArgs args)
        {
            var language = m_state.Game.Language;
            var key = args.GetString(0);
            if (args.Length > 1)
            {
                object[] strings = new object[args.Length - 1];
                for (int i = 1; i < args.Length; ++i)
                {
                    strings[i - 1] = args.ToString(i);
                }
                return new LuaArgs(
                    language.Translate(key, strings)
                );
            }
            else
            {
                return new LuaArgs(
                    language.Translate(key)
                );
            }
        }

        [LuaMethod]
        public LuaArgs getTime(LuaArgs args)
        {
            return new LuaArgs(m_state.Level.TimeMachine.RealTime);
        }

        [LuaMethod]
        public LuaArgs getInputMethod(LuaArgs args)
        {
            return new LuaArgs(m_state.Game.Screen.InputMethod.ToString().ToLowerUnderscored());
        }

        private string GetPrompt(Bind bind, SteamControllerButton steamControllerButton)
        {
            if (m_state.Game.Screen.InputMethod == InputMethod.SteamController)
            {
                var controller = m_state.Game.ActiveSteamController;
                return steamControllerButton.GetPrompt(controller);
            }
            else if (m_state.Game.Screen.InputMethod == InputMethod.Gamepad)
            {
                var padType = m_state.Game.ActiveGamepad.Type;
                return m_state.Game.User.Settings.GetPadBind(bind).GetPrompt(padType);
            }
            else
            {
                var button = m_state.Game.User.Settings.GetMouseBind(bind);
                if (button != MouseButton.None)
                {
                    return button.GetPrompt();
                }
                else
                {
                    return m_state.Game.User.Settings.GetKeyBind(bind).GetPrompt();
                }
            }
        }

        [LuaMethod]
        public LuaArgs getPlayPrompt(LuaArgs args)
        {
            if (m_state.Game.Screen.InputMethod == InputMethod.Mouse)
            {
                return new LuaArgs("[gui/prompts/play.png]");
            }
            else
            {
                return new LuaArgs(
                    GetPrompt(Bind.Play, SteamControllerButton.InGamePlay)
                );
            }
        }

        [LuaMethod]
        public LuaArgs getRewindPrompt(LuaArgs args)
        {
            if (m_state.Game.Screen.InputMethod == InputMethod.Mouse)
            {
                return new LuaArgs("[gui/prompts/rewind.png]");
            }
            else
            {
                return new LuaArgs(
                    GetPrompt(Bind.Rewind, SteamControllerButton.InGameRewind)
                );
            }
        }

        [LuaMethod]
        public LuaArgs getFastforwardPrompt(LuaArgs args)
        {
            if (m_state.Game.Screen.InputMethod == InputMethod.Mouse)
            {
                return new LuaArgs("[gui/prompts/fastforward.png]");
            }
            else
            {
                return new LuaArgs(
                    GetPrompt(Bind.FastForward, SteamControllerButton.InGameFastForward)
                );
            }
        }

        [LuaMethod]
        public LuaArgs getPlacePrompt(LuaArgs args)
        {
            return new LuaArgs(
                GetPrompt(Bind.Place, SteamControllerButton.InGamePlace)
            );
        }

        [LuaMethod]
        public LuaArgs getRemovePrompt(LuaArgs args)
        {
            return new LuaArgs(
                GetPrompt(Bind.Remove, SteamControllerButton.InGameRemove)
            );
        }

        [LuaMethod]
        public LuaArgs getTweakPrompt(LuaArgs args)
        {
            return new LuaArgs(
                GetPrompt(Bind.Tweak, SteamControllerButton.InGameTweak)
            );
        }

        [LuaMethod]
        public LuaArgs getIncreaseDelayPrompt(LuaArgs args)
        {
            return new LuaArgs(
                GetPrompt(Bind.IncreaseDelay, SteamControllerButton.InGameTweakUp)
            );
        }

        [LuaMethod]
        public LuaArgs getDecreaseDelayPrompt(LuaArgs args)
        {
            return new LuaArgs(
                GetPrompt(Bind.DecreaseDelay, SteamControllerButton.InGameTweakDown)
            );
        }

        [LuaMethod]
        public LuaArgs getRotateCameraPrompt(LuaArgs args)
        {
            if (m_state.Game.Screen.InputMethod == InputMethod.Gamepad)
            {
                return new LuaArgs(GamepadJoystick.Right.GetPrompt(m_state.Game.ActiveGamepad.Type));
            }
            else if (m_state.Game.Screen.InputMethod == InputMethod.SteamController)
            {
                return new LuaArgs(SteamControllerJoystick.InGameCamera.GetPrompt(m_state.Game.ActiveSteamController));
            }
            else
            {
                return new LuaArgs(MouseButton.Right.GetPrompt());
            }
        }
    }
}

