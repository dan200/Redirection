using Dan200.Core.Assets;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Game.Level;

namespace Dan200.Game.Game
{
    public static class ProgressUtils
    {
        public static bool IsLevelCompleted(Campaign campaign, Mod mod, int levelIndex, User.User user, int ignoreLevel = -1)
        {
            if (levelIndex < 0 || levelIndex >= campaign.Levels.Count || levelIndex == ignoreLevel)
            {
                return false;
            }
            else
            {
                var levelPath = campaign.Levels[levelIndex];
                var levelData = (mod != null && !mod.Loaded) ? mod.Assets.Load<LevelData>(levelPath) : LevelData.Get(levelPath);
                return user.Progress.IsLevelCompleted(levelData.ID);
            }
        }

        public static bool IsCampaignCompleted(Campaign campaign, Mod mod, User.User user, int ignoreLevel = -1)
        {
            for (int i = 0; i < campaign.Levels.Count; ++i)
            {
                if (!IsLevelCompleted(campaign, mod, i, user, ignoreLevel))
                {
                    return false;
                }
            }
            return true;
        }

        public static int CountLevelsUnlocked(Campaign campaign, Mod mod, User.User user, int ignoreLevel = -1)
        {
            var count = 0;
            for (int i = 0; i < campaign.Levels.Count; ++i)
            {
                if (IsLevelUnlocked(campaign, mod, i, user, ignoreLevel))
                {
                    ++count;
                }
            }
            return count;
        }

        public static int CountLevelsCompleted(Campaign campaign, Mod mod, User.User user, int ignoreLevel = -1)
        {
            var count = 0;
            for (int i = 0; i < campaign.Levels.Count; ++i)
            {
                if (IsLevelCompleted(campaign, mod, i, user, ignoreLevel))
                {
                    ++count;
                }
            }
            return count;
        }

        public static int CountRobotsRescued(Campaign campaign, Mod mod, User.User user, out int o_totalRobots, int ignoreLevel = -1)
        {
            int robotsRescued = 0;
            int totalRobots = 0;
            for (int j = 0; j < campaign.Levels.Count; ++j)
            {
                var levelPath = campaign.Levels[j];
                var levelData = (mod != null && !mod.Loaded) ? mod.Assets.Load<LevelData>(levelPath) : Assets.Get<LevelData>(levelPath);
                totalRobots += levelData.RobotCount;
                if (user.Progress.IsLevelCompleted(levelData.ID))
                {
                    robotsRescued += levelData.RobotCount;
                }
            }
            o_totalRobots = totalRobots;
            return robotsRescued;
        }

        public static bool IsLevelUnlocked(Campaign campaign, Mod mod, int levelIndex, User.User user, int ignoreLevel = -1)
        {
            if (App.Arguments.GetBool("unlockall"))
            {
                return true;
            }
            else if (levelIndex < 0 || levelIndex >= campaign.Levels.Count)
            {
                return false;
            }
            else
            {
                // Get the most recent checkpoint
                int mostRecentCheckpoint = 0;
                for (int i = 0; i < campaign.Checkpoints.Count; ++i)
                {
                    var checkpoint = campaign.Checkpoints[i];
                    if (checkpoint <= levelIndex && checkpoint > mostRecentCheckpoint)
                    {
                        mostRecentCheckpoint = checkpoint;
                    }
                }

                // Check that all the levels before the checkpoint have been completed
                for (int i = 0; i < mostRecentCheckpoint; ++i)
                {
                    if (!IsLevelCompleted(campaign, mod, i, user, ignoreLevel))
                    {
                        return false;
                    }
                }

                // Count the number of levels unlocked since the checkpoint
                int levelsUnlocked = campaign.InitialLevelsUnlocked;
                for (int i = mostRecentCheckpoint; i < campaign.Levels.Count; ++i)
                {
                    if (IsLevelCompleted(campaign, mod, i, user, ignoreLevel))
                    {
                        levelsUnlocked++;
                    }
                }

                // See if the requested level is past the point
                if (levelIndex < (mostRecentCheckpoint + levelsUnlocked))
                {
                    return true;
                }
                return false;
            }
        }
    }
}
