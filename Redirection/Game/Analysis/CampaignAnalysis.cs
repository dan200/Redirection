using Dan200.Game.Game;
using Dan200.Game.Level;
using Dan200.Game.User;
using System.Collections.Generic;

namespace Dan200.Game.Analysis
{
    public class CampaignAnalysis
    {
        public static CampaignAnalysis Analyse(Campaign campaign, Progress[] users)
        {
            var results = new List<LevelAnalysis>();
            for (int i = 0; i < campaign.Levels.Count; ++i)
            {
                var levelPath = campaign.Levels[i];
                var levelData = LevelData.Get(levelPath);
                var id = levelData.ID;
                results.Add(LevelAnalysis.Analyse(id, users));
            }
            return new CampaignAnalysis(campaign.ID, results.ToArray());
        }

        public readonly int CampaignID;
        public readonly LevelAnalysis[] Levels;

        private CampaignAnalysis(int campaignID, LevelAnalysis[] levels)
        {
            CampaignID = campaignID;
            Levels = levels;
        }
    }
}

