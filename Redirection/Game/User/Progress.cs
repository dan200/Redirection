using Dan200.Core.Assets;
using Dan200.Core.Main;
using Dan200.Core.Network;
using Dan200.Core.Util;
using System;
using System.Text;

namespace Dan200.Game.User
{
    public class Progress
    {
        private INetwork m_network;
        private KeyValuePairFile m_kvp;

        public bool UsedCheats
        {
            get
            {
                return m_kvp.GetBool("used_cheats", false);
            }
            set
            {
                m_kvp.Set("used_cheats", value);
            }
        }

        public int LastEditedLevel
        {
            get
            {
                return m_kvp.GetInteger("levels.last_edited", 0);
            }
            set
            {
                m_kvp.Set("levels.last_edited", value);
            }
        }

        public bool UsedLevelEditor
        {
            get
            {
                return m_kvp.GetBool("used_level_editor", false);
            }
            set
            {
                m_kvp.Set("used_level_editor", value);
            }
        }

        public Version LastPlayedVersion
        {
            set
            {
                m_kvp.Set("last_played_version", value);
            }
        }

        public Progress(INetwork network, string fileName)
        {
            m_network = network;

            // Load KVP and check for tampering
            m_kvp = new KeyValuePairFile(m_network.LocalUser.RemoteSaveStore, fileName);
            m_kvp.Comment = "Game progress. NOTE: Tampering with this file will disable achievements";
            var tamperCode = ComputeTamperCode();
            if (m_kvp.GetInteger("tamper_code", 0) != tamperCode)
            {
                App.Log("Error: progress.txt has been tampered with! Setting used_cheats to true");
                m_kvp.Set("used_cheats", true);
                tamperCode = ComputeTamperCode();
            }
            m_kvp.Set("tamper_code", tamperCode);
            m_kvp.SaveIfModified();

            if (m_network != null && !App.Demo)
            {
                // Send stat and achivement updates to the network
                foreach (Statistic stat in Enum.GetValues(typeof(Statistic)))
                {
                    int value = GetStatistic(stat);
                    stat.UnlockLinkedAchievements(this, value, value);
                    if (!UsedCheats && m_network.SupportsStatistics)
                    {
                        m_network.LocalUser.SetStatistic(stat.GetID(), value);
                    }
                }
                foreach (Achievement achievement in Enum.GetValues(typeof(Achievement)))
                {
                    if (IsAchievementUnlocked(achievement))
                    {
                        if (!UsedCheats && m_network.SupportsAchievements)
                        {
                            m_network.LocalUser.UnlockAchievement(achievement.GetID());
                        }
                    }
                }
            }
        }

        public void Reset()
        {
            m_kvp.Clear();
        }

        public void SetLevelCompleted(int levelID, int obstaclesUsed)
        {
            m_kvp.Set("levels." + levelID + ".completed", true);

            var obstaclesUsedKey = "levels." + levelID + ".obstacles_used";
            if (m_kvp.ContainsKey(obstaclesUsedKey))
            {
                var oldObstaclesUsed = m_kvp.GetInteger(obstaclesUsedKey, obstaclesUsed);
                m_kvp.Set(obstaclesUsedKey, Math.Min(obstaclesUsed, oldObstaclesUsed));
            }
            else
            {
                m_kvp.Set(obstaclesUsedKey, obstaclesUsed);
            }
        }

        public bool IsLevelCompleted(int levelID)
        {
            return m_kvp.GetBool("levels." + levelID + ".completed", false);
        }

        public int GetObstaclesUsed(int levelID, int _default)
        {
            return m_kvp.GetInteger("levels." + levelID + ".obstacles_used", _default);
        }

        public float GetPlaytime(int levelID)
        {
            var key = "levels." + levelID + ".playtime";
            return m_kvp.GetFloat(key, 0.0f);
        }

        public void AddPlaytime(int levelID, float seconds)
        {
            var key = "levels." + levelID + ".playtime";
            var existing = m_kvp.GetFloat(key, 0.0f);
            m_kvp.Set(key, existing + seconds);

            var total = m_kvp.GetFloat("total_playtime", 0.0f);
            m_kvp.Set("total_playtime", total + seconds);
        }

        public void SetLastArcadeGamePlayed(int diskID)
        {
            m_kvp.Set("arcade.last_played_game", diskID);
        }

        public int GetLastPlayedArcadeGame()
        {
            return m_kvp.GetInteger("arcade.last_played_game", 0);
        }

        public void SetArcadeGamePlayed(int diskID)
        {
            m_kvp.Set("arcade." + diskID + ".played", true);
        }

        public bool IsArcadeGamePlayed(int diskID)
        {
            return m_kvp.GetBool("arcade." + diskID + ".played", false);
        }

        public void SubmitArcadeGameScore(int diskID, int score)
        {
            var key = "arcade." + diskID + ".score";
            var oldScore = m_kvp.GetInteger(key, 0);
            if (score > oldScore)
            {
                m_kvp.Set(key, score);
            }
        }

        public int GetArcadeGameScore(int diskID)
        {
            var key = "arcade." + diskID + ".score";
            return m_kvp.GetInteger(key, 0);
        }

        public void IndicateAchievementProgress(Achievement achievement, int currentValue, int unlockValue)
        {
            var achievementID = achievement.GetID();
            if (m_network != null && m_network.SupportsAchievements && !UsedCheats && !App.Demo)
            {
                m_network.LocalUser.IndicateAchievementProgress(achievementID, currentValue, unlockValue);
            }
        }

        public void UnlockAchievement(Achievement achievement)
        {
            var achievementID = achievement.GetID();
            string key = "achievements." + achievementID + ".unlocked";
            if (!m_kvp.GetBool(key))
            {
                App.Log("Unlocked achievement " + achievementID);
                m_kvp.Set(key, true);
                if (achievement.IsGameplay())
                {
                    IncrementStatistic(Statistic.GameplayAchievements);
                }
            }
            if (m_network != null && m_network.SupportsAchievements && !UsedCheats && !App.Demo)
            {
                m_network.LocalUser.UnlockAchievement(achievementID);
            }
        }

        public bool IsAchievementUnlocked(Achievement achievement)
        {
            var achievementID = achievement.GetID();
            var key = "achievements." + achievementID + ".unlocked";
            return m_kvp.GetBool(key);
        }

        public void RemoveAchievement(Achievement achievement)
        {
            var achievementID = achievement.GetID();
            var key = "achievements." + achievementID + ".unlocked";
            if (m_kvp.ContainsKey(key))
            {
                m_kvp.Remove(key);
                if (achievement.IsGameplay())
                {
                    AddStatistic(Statistic.GameplayAchievements, -1);
                }
            }
            if (m_network != null && m_network.SupportsAchievements && !UsedCheats && !App.Demo)
            {
                m_network.LocalUser.RemoveAchievement(achievementID);
            }
        }

        public void RemoveAllAchievements()
        {
            foreach (Achievement achievement in Enum.GetValues(typeof(Achievement)))
            {
                RemoveAchievement(achievement);
            }
        }

        public void IncrementStatistic(Statistic stat)
        {
            AddStatistic(stat, 1);
        }

        public void AddStatistic(Statistic stat, int count)
        {
            SetStatistic(stat, GetStatistic(stat) + count);
        }

        public void SetStatistic(Statistic stat, int value)
        {
            var statID = stat.GetID();
            var key = "stats." + statID + ".count";
            int oldValue = m_kvp.GetInteger(key, 0);
            m_kvp.Set(key, value);
            if (m_network != null && m_network.SupportsStatistics && !UsedCheats && !App.Demo)
            {
                m_network.LocalUser.SetStatistic(statID, value);
            }
            stat.UnlockLinkedAchievements(this, oldValue, value);
        }

        public int GetStatistic(Statistic stat)
        {
            var statID = stat.GetID();
            return m_kvp.GetInteger("stats." + statID + ".count", 0);
        }

        public void ResetAllStatistics()
        {
            foreach (Statistic stat in Enum.GetValues(typeof(Statistic)))
            {
                SetStatistic(stat, 0);
            }
        }

        public void Save()
        {
            if (!App.Demo)
            {
                if (m_network != null && m_network.SupportsAchievements && !UsedCheats && !App.Demo)
                {
                    m_network.UploadStatistics();
                }
                m_kvp.Set("tamper_code", ComputeTamperCode());
                m_kvp.SaveIfModified();
            }
        }

        private int ComputeTamperCode()
        {
            var builder = new StringBuilder();
            foreach (string key in m_kvp.Keys)
            {
                if (key != "tamper_code")
                {
                    builder.Append(key);
                    builder.Append(m_kvp.GetString(key));
                }
            }
            return MathUtils.SimpleStableHash(builder.ToString());
        }
    }
}
