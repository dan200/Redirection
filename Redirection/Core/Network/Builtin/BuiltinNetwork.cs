using Dan200.Core.Async;
using Dan200.Core.Input;
using Dan200.Core.Main;
using System;
using System.Diagnostics;

namespace Dan200.Core.Network.Builtin
{
    public class BuiltinNetwork : INetwork
    {
        private BuiltinLocalUser m_localUser;

        public bool SupportsAchievements
        {
            get
            {
                return false;
            }
        }

        public bool SupportsStatistics
        {
            get
            {
                return false;
            }
        }

        public bool SupportsWorkshop
        {
            get
            {
                return false;
            }
        }

        public bool SupportsLeaderboards
        {
            get
            {
                return false;
            }
        }

        public ILocalUser LocalUser
        {
            get
            {
                return m_localUser;
            }
        }

        public IWorkshop Workshop
        {
            get
            {
                return null;
            }
        }

        public BuiltinNetwork()
        {
            m_localUser = new BuiltinLocalUser();
        }

        public long GetGlobalStatistic(string statisticID)
        {
            return 0;
        }

        public void UploadStatistics()
        {
        }

        public int GetConcurrentPlayers()
        {
            return 1;
        }

        public void SetAchievementCorner(AchievementCorner corner)
        {
        }

        public bool OpenFileBrowser(string path)
        {
            App.Log("Opening file browser to {0}", path);
            return Process.Start(path) != null;
        }

        public bool OpenTextEditor(string path)
        {
            App.Log("Opening {0} in text editor", path);
            var info = new ProcessStartInfo(path);
            info.UseShellExecute = true;
            return Process.Start(info) != null;
        }

        public bool OpenWebBrowser(string url, WebBrowserType preferredType)
        {
            App.Log("Opening web browser to {0}", url);
            try
            {
                Process.Start(url);
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

        public bool OpenWorkshopItem(ulong id)
        {
            return false;
        }

        public bool OpenWorkshopHub()
        {
            return false;
        }

        public bool OpenWorkshopHub(string[] filterTags)
        {
            return false;
        }

        public bool OpenAchievementsHub()
        {
            return false;
        }

        public bool OpenSteamControllerConfig(ISteamController controller)
        {
            return false;
        }

        public bool OpenLeaderboard(ulong id)
        {
            return false;
        }

        public Promise<ulong> GetLeaderboardID(string name, bool createIfAbsent)
        {
            throw new NotImplementedException();
        }

        public Promise<Leaderboard> DownloadLeaderboard(ulong id, LeaderboardType type, int maxEntries)
        {
            throw new NotImplementedException();
        }
    }
}
