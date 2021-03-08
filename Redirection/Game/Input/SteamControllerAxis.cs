using Dan200.Core.Input;
using Dan200.Core.Input.Steamworks;
using Dan200.Core.Util;
using System;

namespace Dan200.Game.Input
{
    public enum SteamControllerAxis
    {
    }

    public static class SteamControllerAxisExtensions
    {
        public static string GetID(this SteamControllerAxis button)
        {
            return button.ToString().ToLowerUnderscored();
        }

        public static string[] GetAllIDs()
        {
            var values = Enum.GetValues(typeof(SteamControllerAxis));
            var ids = new string[values.Length];
            foreach (SteamControllerAxis axis in values)
            {
                ids[(int)axis] = axis.GetID();
            }
            return ids;
        }

        public static SteamControllerActionSet GetActionSet(this SteamControllerAxis button)
        {
            return SteamControllerActionSet.InGame;
        }

        public static string GetPrompt(this SteamControllerAxis axis, ISteamController controller)
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

        public static string GetPromptPath(this SteamControllerAxis axis, ISteamController controller)
        {
            if (controller is SteamworksSteamController)
            {
                return ((SteamworksSteamController)controller).GetAxisPromptPath(axis.GetID(), axis.GetActionSet().GetID());
            }
            return null;
        }
    }
}
