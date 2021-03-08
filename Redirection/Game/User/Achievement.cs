using Dan200.Core.Util;
using System;

namespace Dan200.Game.User
{
    // Remember to add new achievements to the Steam parter site:
    // https://partner.steamgames.com/apps/achievements/305760
    public enum Achievement
    {
        WatchIntro = 0,
        CompleteFirstLevel,
        CompleteAllLevels,
        WatchOutro,
        Complete_1_WorkshopLevel,
        Complete_10_WorkshopLevels,
        Complete_50_WorkshopLevels,
        Rewind_100_Times,
        Drown_5_Robots,
        Lose_5_Robots,
        Place_100_Obstacles,
        Rescue_100_Robots,
        Optimisation,
        UnlockAllGameplayAchievements,
        CreateLevel,
        CreatePopularMod,
        PlayManyArcadeGames,
        WormHighScore,
        InvadersHighScore,
        TennisHighScore,
        QuestHighScore,
    }

    public static class AchievementExtensions
    {
        public static bool IsGameplay(this Achievement achievement)
        {
            return
                achievement != Achievement.UnlockAllGameplayAchievements &&
                achievement != Achievement.CreateLevel &&
                achievement != Achievement.CreatePopularMod;
        }

        public static string GetID(this Achievement achievement)
        {
            return achievement.ToString().ToLowerUnderscored();
        }

        public static string[] GetAllIDs()
        {
            var values = Enum.GetValues(typeof(Achievement));
            var ids = new string[values.Length];
            foreach (Achievement achievement in values)
            {
                ids[(int)achievement] = achievement.GetID();
            }
            return ids;
        }
    }
}

