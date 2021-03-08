using Dan200.Core.Async;
using Dan200.Core.Input;
using Dan200.Core.Util;

namespace Dan200.Core.Network
{
    public enum AchievementCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }

    public enum WebBrowserType
    {
        Overlay,
        External
    }

    public interface INetwork
    {
        bool SupportsAchievements { get; }
        bool SupportsStatistics { get; }
        bool SupportsWorkshop { get; }
        bool SupportsLeaderboards { get; }

        ILocalUser LocalUser { get; }
        IWorkshop Workshop { get; }

        long GetGlobalStatistic(string statisticID);
        void UploadStatistics();
        int GetConcurrentPlayers();

        void SetAchievementCorner(AchievementCorner corner);

        bool OpenFileBrowser(string path);
        bool OpenTextEditor(string path);
        bool OpenWebBrowser(string url, WebBrowserType preferredType);
        bool OpenWorkshopItem(ulong id);
        bool OpenWorkshopHub();
        bool OpenWorkshopHub(string[] filterTags);
        bool OpenAchievementsHub();
        bool OpenSteamControllerConfig(ISteamController controller);
        bool OpenLeaderboard(ulong id);

        Promise<ulong> GetLeaderboardID(string name, bool createIfAbsent);
        Promise<Leaderboard> DownloadLeaderboard(ulong id, LeaderboardType type, int maxEntries);
    }

    public static class NetworkExtensions
    {
        public static void OpenTwitter(this INetwork network, string handle)
        {
            network.OpenWebBrowser(
                string.Format("http://www.twitter.com/{0}", handle.URLEncode()),
                WebBrowserType.External
            );
        }

        public static void OpenComposeTweet(this INetwork network, string tweet)
        {
            network.OpenWebBrowser(
                string.Format("http://www.twitter.com/intent/tweet?text={0}", tweet.URLEncode()),
                WebBrowserType.External
            );
        }
    }
}
