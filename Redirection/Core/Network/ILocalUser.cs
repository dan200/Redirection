using Dan200.Core.Assets;
using Dan200.Core.Async;

namespace Dan200.Core.Network
{
    public interface ILocalUser
    {
        ulong ID { get; }
        string DisplayName { get; }
        string Language { get; }

        IWritableFileStore LocalSaveStore { get; }
        IWritableFileStore RemoteSaveStore { get; }

        void UnlockAchievement(string achievementID);
        void RemoveAchievement(string achievementID);
        void IndicateAchievementProgress(string achievementID, int currentValue, int unlockValue);

        void AddStatistic(string statisticID, int count);
        void SetStatistic(string statisticID, int count);
        int GetStatistic(string statisticID);

        Promise SubmitLeaderboardScore(ulong id, int score);
    }
}
