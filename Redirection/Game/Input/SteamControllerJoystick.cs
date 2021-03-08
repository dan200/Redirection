using Dan200.Core.Input;
using Dan200.Core.Input.Steamworks;
using Dan200.Core.Util;
using System;

namespace Dan200.Game.Input
{
    public enum SteamControllerJoystick
    {
        InGameCursor,
        InGameCamera,
    }

    public static class SteamControllerJoystickExtensions
    {
        public static string GetID(this SteamControllerJoystick button)
        {
            return button.ToString().ToLowerUnderscored();
        }

        public static string[] GetAllIDs()
        {
            var values = Enum.GetValues(typeof(SteamControllerJoystick));
            var ids = new string[values.Length];
            foreach (SteamControllerJoystick axis in values)
            {
                ids[(int)axis] = axis.GetID();
            }
            return ids;
        }

        public static SteamControllerActionSet GetActionSet(this SteamControllerJoystick axis)
        {
            return SteamControllerActionSet.InGame;
        }

        public static string GetPrompt(this SteamControllerJoystick axis, ISteamController controller)
        {
            var path = axis.GetPromptPath(controller);
            if (path != null)
            {
                return '[' + path + ']';
            }
            else
            {
                return "?";
            }
        }

        public static string GetPromptPath(this SteamControllerJoystick axis, ISteamController controller)
        {
            if (controller is SteamworksSteamController)
            {
                return ((SteamworksSteamController)controller).GetJoystickPromptPath(axis.GetID(), axis.GetActionSet().GetID());
            }
            return null;
        }
    }
}
