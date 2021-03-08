namespace Dan200.Game.Game
{
    public class Playthrough
    {
        public readonly Campaign Campaign;
        public int Level;
        public int JustCompletedLevel;
        public bool CampaignCompleted;

        public Playthrough(Campaign campaign, int level)
        {
            Campaign = campaign;
            Level = level;
            JustCompletedLevel = -1;
            CampaignCompleted = false;
        }
    }
}

