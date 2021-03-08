using Dan200.Core.Util;
using System;
using System.Collections.Generic;

namespace Dan200.Game.User
{
    // Remember to add new statistics to the Steam parter site:
    // https://partner.steamgames.com/apps/stats/305760
    public enum Statistic
    {
        ObstaclesPlaced = 0,
        RobotsRescued,
        Rewinds,
        WorkshopLevelsCompleted,
        CampaignLevelsCompleted,
        RobotsDrowned,
        RobotsLost,
        GameplayAchievements,
        LevelsCompleted,
        MostPopularModSubscriptions,
        ArcadeGamesPlayed
    }

    public static class StatisticExtensions
    {
        static StatisticExtensions()
        {
            // When linking statistics to achievements, make sure you update "Progress Stat" on the Steam parter site:
            // https://partner.steamgames.com/apps/achievements/305760
            Statistic.ObstaclesPlaced.LinkToAchievement(
                Achievement.Place_100_Obstacles,
                100,
                new int[] { 10, 50 }
            );
            Statistic.RobotsRescued.LinkToAchievement(
                Achievement.Rescue_100_Robots,
                100,
                new int[] { 10, 50 }
            );
            Statistic.Rewinds.LinkToAchievement(
                Achievement.Rewind_100_Times,
                100,
                new int[] { 10, 50 }
            );
            Statistic.WorkshopLevelsCompleted.LinkToAchievement(
                Achievement.Complete_1_WorkshopLevel,
                1
            );
            Statistic.WorkshopLevelsCompleted.LinkToAchievement(
                Achievement.Complete_10_WorkshopLevels,
                10
            );
            Statistic.WorkshopLevelsCompleted.LinkToAchievement(
                Achievement.Complete_50_WorkshopLevels,
                50
            );
            Statistic.RobotsDrowned.LinkToAchievement(
                Achievement.Drown_5_Robots,
                5
            );
            Statistic.RobotsLost.LinkToAchievement(
                Achievement.Lose_5_Robots,
                5
            );
            Statistic.ArcadeGamesPlayed.LinkToAchievement(
                Achievement.PlayManyArcadeGames,
                6
            );

            int numGameplayAchievements = 0;
            foreach (Achievement achievement in Enum.GetValues(typeof(Achievement)))
            {
                if (achievement.IsGameplay())
                {
                    ++numGameplayAchievements;
                }
            }
            Statistic.GameplayAchievements.LinkToAchievement(
                Achievement.UnlockAllGameplayAchievements,
                numGameplayAchievements
            );
        }

        public static string GetID(this Statistic stat)
        {
            return stat.ToString().ToLowerUnderscored();
        }

        public static string[] GetAllIDs()
        {
            var values = Enum.GetValues(typeof(Statistic));
            var ids = new string[values.Length];
            foreach (Statistic statistic in values)
            {
                ids[(int)statistic] = statistic.GetID();
            }
            return ids;
        }

        private static void LinkToAchievement(this Statistic stat, Achievement achievement, int unlockValue, int[] notifyValues = null)
        {
            if (!s_linkedAchivements.ContainsKey(stat))
            {
                s_linkedAchivements[stat] = new List<LinkedAchievement>(1);
            }

            var linkedAchivement = new LinkedAchievement();
            linkedAchivement.Achievement = achievement;
            linkedAchivement.UnlockValue = unlockValue;
            linkedAchivement.NotifyValues = notifyValues;
            s_linkedAchivements[stat].Add(linkedAchivement);
        }

        public static void UnlockLinkedAchievements(this Statistic stat, Progress progress, int oldValue, int newValue)
        {
            if (s_linkedAchivements.ContainsKey(stat))
            {
                var list = s_linkedAchivements[stat];
                for (int i = 0; i < list.Count; ++i)
                {
                    var linkedAchievement = list[i];
                    if (newValue >= linkedAchievement.UnlockValue)
                    {
                        progress.UnlockAchievement(linkedAchievement.Achievement);
                        progress.Save();
                    }
                    else
                    {
                        var notifyList = linkedAchievement.NotifyValues;
                        if (notifyList != null)
                        {
                            for (int j = notifyList.Length - 1; j >= 0; --j)
                            {
                                var notifyValue = notifyList[j];
                                if (oldValue < notifyValue && newValue >= notifyValue)
                                {
                                    progress.IndicateAchievementProgress(linkedAchievement.Achievement, newValue, linkedAchievement.UnlockValue);
                                    progress.Save();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static Dictionary<Statistic, List<LinkedAchievement>> s_linkedAchivements = new Dictionary<Statistic, List<LinkedAchievement>>();

        private class LinkedAchievement
        {
            public Achievement Achievement;
            public int UnlockValue;
            public int[] NotifyValues;
        }
    }
}
