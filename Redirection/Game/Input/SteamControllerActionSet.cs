using Dan200.Core.Util;
using System;

namespace Dan200.Game.Input
{
    public enum SteamControllerActionSet
    {
        Menu, // First is default
        InGame,
        Arcade,
    }

    public static class SteamControllerActionSetExtensions
    {
        public static string GetID(this SteamControllerActionSet actionSet)
        {
            return actionSet.ToString().ToLowerUnderscored();
        }

        public static string[] GetAllIDs()
        {
            var values = Enum.GetValues(typeof(SteamControllerActionSet));
            var ids = new string[values.Length];
            foreach (SteamControllerActionSet actionSet in values)
            {
                ids[(int)actionSet] = actionSet.GetID();
            }
            return ids;
        }
    }
}
