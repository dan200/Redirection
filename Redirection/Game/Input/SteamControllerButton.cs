using Dan200.Core.Input;
using Dan200.Core.Input.Steamworks;
using Dan200.Core.Util;
using System;

namespace Dan200.Game.Input
{
    public enum SteamControllerButton
    {
        MenuUp,
        MenuDown,
        MenuLeft,
        MenuRight,
        MenuNextPage,
        MenuPreviousPage,
        MenuSelect,
        MenuAltSelect,
        MenuBack,
        MenuToGame,

        InGamePlace,
        InGameRemove,
        InGameTweak,
        InGameTweakUp,
        InGameTweakDown,

        InGamePlay,
        InGameRewind,
        InGameFastForward,
        InGameBack,
        InGameToMenu,

        ArcadeUp,
        ArcadeDown,
        ArcadeLeft,
        ArcadeRight,
        ArcadeA,
        ArcadeB,
        ArcadeBack,
        ArcadeSwapDisk,
    }

    public static class SteamControllerButtonExtensions
    {
        public static string GetID(this SteamControllerButton button)
        {
            return button.ToString().ToLowerUnderscored();
        }

        public static string[] GetAllIDs()
        {
            var values = Enum.GetValues(typeof(SteamControllerButton));
            var ids = new string[values.Length];
            foreach (SteamControllerButton button in values)
            {
                ids[(int)button] = button.GetID();
            }
            return ids;
        }

        public static SteamControllerActionSet GetActionSet(this SteamControllerButton button)
        {
            if (button <= SteamControllerButton.MenuToGame)
            {
                return SteamControllerActionSet.Menu;
            }
            else if (button <= SteamControllerButton.InGameToMenu)
            {
                return SteamControllerActionSet.InGame;
            }
            else
            {
                return SteamControllerActionSet.Arcade;
            }
        }

        public static string GetPrompt(this SteamControllerButton button, ISteamController controller)
        {
            var path = button.GetPromptImagePath(controller);
            if (path != null)
            {
                return '[' + path + ']';
            }
            else
            {
                return "?";
            }
        }

        private static string GetPromptImagePath(this SteamControllerButton button, ISteamController controller)
        {
            if (controller is SteamworksSteamController)
            {
                return ((SteamworksSteamController)controller).GetButtonPromptPath(button.GetID(), button.GetActionSet().GetID());
            }
            return null;
        }
    }
}
