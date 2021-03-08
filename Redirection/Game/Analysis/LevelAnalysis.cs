using Dan200.Game.User;
using System.Collections.Generic;

namespace Dan200.Game.Analysis
{
    public class LevelAnalysis
    {
        public static LevelAnalysis Analyse(int levelID, Progress[] users)
        {
            var analysis = new LevelAnalysis(levelID);

            var playtimes = new List<float>();
            foreach (var user in users)
            {
                if (user.IsLevelCompleted(levelID))
                {
                    playtimes.Add(user.GetPlaytime(levelID));
                }
            }

            if (playtimes.Count > 0)
            {
                playtimes.Sort();
                analysis.PercentCompleted = ((float)playtimes.Count / (float)users.Length) * 100.0f;
                analysis.MinPlaytime = playtimes[0];
                analysis.MaxPlaytime = playtimes[playtimes.Count - 1];
                analysis.MedianPlaytime = playtimes[playtimes.Count / 2];
            }

            return analysis;
        }

        public readonly int LevelID;
        public float PercentCompleted;
        public float MinPlaytime;
        public float MaxPlaytime;
        public float MedianPlaytime;

        public LevelAnalysis(int levelID)
        {
            LevelID = levelID;
            PercentCompleted = 0.0f;
            MinPlaytime = 0.0f;
            MaxPlaytime = 0.0f;
            MedianPlaytime = 0.0f;
        }
    }
}

