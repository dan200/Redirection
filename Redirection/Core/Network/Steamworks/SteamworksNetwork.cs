using Dan200.Core.Async;
using Dan200.Core.Input;
using Dan200.Core.Input.Steamworks;
using Dan200.Core.Main;
using Dan200.Core.Util;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Dan200.Core.Network.Steamworks
{
    public class SteamworksNetwork : INetwork
    {
        private string[] m_achievementNames;
        private string[] m_statNames;

        private SteamworksLocalUser m_localUser;
        private IWorkshop m_workshop;

        private ISet<string> m_achievements;
        private IDictionary<string, int> m_stats;
        private IDictionary<string, long> m_globalStats;
        private IDictionary<string, bool> m_earlyAchievementChanges;
        private IDictionary<string, int> m_earlyStatAdditions;
        private IDictionary<string, int> m_earlyStatSets;
        private int m_concurrentPlayers;
        private bool m_statsNeedUpload;
        private bool m_earlyUpload;
        private bool m_initialised;

        public bool SupportsAchievements
        {
            get
            {
                return true;
            }
        }

        public bool SupportsStatistics
        {
            get
            {
                return true;
            }
        }

        public bool SupportsWorkshop
        {
            get
            {
                return true;
            }
        }

        public bool SupportsLeaderboards
        {
            get
            {
                return true;
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
                return m_workshop;
            }
        }

        public SteamworksNetwork(string[] achievementIDs, string[] statisticIDs)
        {
            m_achievementNames = achievementIDs.Select(id => GetAchievementName(id)).ToArray();
            m_statNames = statisticIDs.Select(id => GetStatName(id)).ToArray();

            m_localUser = new SteamworksLocalUser(this);
            m_workshop = new SteamworksWorkshop(this);
            m_achievements = new HashSet<string>();
            m_stats = new Dictionary<string, int>();
            m_globalStats = new Dictionary<string, long>();
            m_earlyAchievementChanges = new Dictionary<string, bool>();
            m_earlyStatAdditions = new Dictionary<string, int>();
            m_earlyStatSets = new Dictionary<string, int>();
            m_statsNeedUpload = false;
            m_earlyUpload = false;
            m_initialised = false;

            RegisterCallback<UserStatsReceived_t>(OnUserStatsReceived);
            RegisterCallback<GlobalStatsReceived_t>(OnGlobalStatsReceived);
            RegisterCallback<UserStatsStored_t>(OnUserStatsStored);
            RegisterCallback<UserAchievementStored_t>(OnAchievementStored);
            RequestStats();
        }

        public void SetAchievementCorner(AchievementCorner corner)
        {
            ENotificationPosition position;
            switch (corner)
            {
                case AchievementCorner.TopLeft:
                    {
                        position = ENotificationPosition.k_EPositionTopLeft;
                        break;
                    }
                case AchievementCorner.TopRight:
                    {
                        position = ENotificationPosition.k_EPositionTopRight;
                        break;
                    }
                case AchievementCorner.BottomLeft:
                    {
                        position = ENotificationPosition.k_EPositionBottomLeft;
                        break;
                    }
                case AchievementCorner.BottomRight:
                default:
                    {
                        position = ENotificationPosition.k_EPositionBottomRight;
                        break;
                    }
            }
            SteamUtils.SetOverlayNotificationPosition(position);
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

        private bool OpenOverlayWebBrowser(string url)
        {
            if (SteamUtils.IsOverlayEnabled())
            {
                App.Log("Opening steam web browser to {0}", url);
                SteamFriends.ActivateGameOverlayToWebPage(url);
                return true;
            }
            return false;
        }

        private bool OpenExternalWebBrowser(string url)
        {
            if (App.Platform != Platform.Linux) // Steam's Linux sandbox doesn't like browsers :(
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
            return false;
        }

        public bool OpenWebBrowser(string url, WebBrowserType prefferedType)
        {
            if (prefferedType == WebBrowserType.Overlay)
            {
                return OpenOverlayWebBrowser(url) || OpenExternalWebBrowser(url);
            }
            else
            {
                return OpenExternalWebBrowser(url) || OpenOverlayWebBrowser(url);
            }
        }

        public bool OpenWorkshopItem(ulong id)
        {
            return OpenWebBrowser(string.Format("http://steamcommunity.com/sharedfiles/filedetails/?id={0}", id), WebBrowserType.Overlay);
        }

        public bool OpenWorkshopHub()
        {
            return OpenWebBrowser(string.Format("http://steamcommunity.com/workshop/browse/?appid={0}", App.Info.SteamAppID), WebBrowserType.Overlay);
        }

        public bool OpenWorkshopHub(string[] filterTags)
        {
            var url = new StringBuilder();
            url.Append(string.Format("http://steamcommunity.com/workshop/browse/?appid={0}", App.Info.SteamAppID));
            for (int i = 0; i < filterTags.Length; ++i)
            {
                var tag = filterTags[i];
                url.Append(string.Format("&requiredtags[]={0}", tag.URLEncode()));
            }
            return OpenWebBrowser(url.ToString(), WebBrowserType.Overlay);
        }

        public bool OpenAchievementsHub()
        {
            if (SteamUtils.IsOverlayEnabled())
            {
                SteamFriends.ActivateGameOverlay("Achievements");
                return true;
            }
            else
            {
                return OpenWebBrowser(string.Format("http://steamcommunity.com/stats/{0}/achievements/", App.Info.SteamAppID), WebBrowserType.Overlay);
            }
        }

        public bool OpenSteamControllerConfig(ISteamController controller)
        {
            if (controller is SteamworksSteamController)
            {
                return SteamController.ShowBindingPanel(
                    ((SteamworksSteamController)controller).Handle
                );
            }
            return false;
        }

        public bool OpenLeaderboard(ulong id)
        {
            return OpenWebBrowser(string.Format("http://steamcommunity.com/stats/{0}/leaderboards/{1}", App.Info.SteamAppID, id), WebBrowserType.Overlay);
        }

        public Promise<ulong> GetLeaderboardID(string name, bool createIfAbsent)
        {
            var result = new SimplePromise<ulong>();
            MakeCall(
                createIfAbsent ?
                    SteamUserStats.FindOrCreateLeaderboard(name, ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending, ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric) :
                    SteamUserStats.FindLeaderboard(name),
                delegate (LeaderboardFindResult_t args, bool ioFailure)
                {
                    if (ioFailure || args.m_bLeaderboardFound == 0)
                    {
                        result.Fail("Failed to get leaderboard ID");
                    }
                    else
                    {
                        var id = args.m_hSteamLeaderboard.m_SteamLeaderboard;
                        result.Succeed(id);
                    }
                }
            );
            return result;
        }

        public Promise<Leaderboard> DownloadLeaderboard(ulong id, LeaderboardType type, int maxEntries)
        {
            var result = new SimplePromise<Leaderboard>();
            if (maxEntries > 0)
            {
                ELeaderboardDataRequest requestType;
                int rangeStart, rangeEnd;
                switch (type)
                {
                    case LeaderboardType.Global:
                    default:
                        {
                            requestType = ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal;
                            rangeStart = 1;
                            rangeEnd = maxEntries;
                            break;
                        }
                    case LeaderboardType.Local:
                        {
                            requestType = ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser;
                            rangeStart = -(maxEntries / 2);
                            rangeEnd = rangeStart + maxEntries - 1;
                            break;
                        }
                    case LeaderboardType.Friends:
                        {
                            requestType = ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends;
                            rangeStart = 1;
                            rangeEnd = maxEntries;
                            break;
                        }
                }
                MakeCall(
                    SteamUserStats.DownloadLeaderboardEntries(new SteamLeaderboard_t(id), requestType, rangeStart, rangeEnd),
                    delegate (LeaderboardScoresDownloaded_t args2, bool ioFailure2)
                    {
                        if (ioFailure2)
                        {
                            result.Fail("Failed to download leaderboard");
                        }
                        else
                        {
                            var leaderboard = new Leaderboard(id, type);
                            for (int i = 0; i < Math.Min(args2.m_cEntryCount, maxEntries); ++i)
                            {
                                LeaderboardEntry_t entry;
                                if (SteamUserStats.GetDownloadedLeaderboardEntry(args2.m_hSteamLeaderboardEntries, i, out entry, null, 0))
                                {
                                    var rank = entry.m_nGlobalRank;
                                    var username = SteamFriends.GetFriendPersonaName(entry.m_steamIDUser);
                                    var score = entry.m_nScore;
                                    leaderboard.Entries.Add(new LeaderboardEntry(rank, username, score));
                                }
                            }
                            result.Succeed(leaderboard);
                        }
                    }
                );
            }
            else
            {
                result.Succeed(new Leaderboard(id, type));
            }
            return result;
        }

        public Promise SubmitLocalUserLeaderboardScore(ulong id, int score)
        {
            var result = new SimplePromise();
            MakeCall(
                SteamUserStats.UploadLeaderboardScore(new SteamLeaderboard_t(id), ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, score, null, 0),
                delegate (LeaderboardScoreUploaded_t args2, bool ioFailure2)
                {
                    if (ioFailure2 || args2.m_bSuccess == 0)
                    {
                        result.Fail("Failed to submit leaderboard score");
                    }
                    else
                    {
                        result.Succeed();
                    }
                }
            );
            return result;
        }

        public string GetAchievementName(string achievementID)
        {
            return "ACH_" + achievementID.ToUpperInvariant();
        }

        public void UnlockLocalUserAchievement(string steamAchievementName)
        {
            if (m_initialised)
            {
                if (!m_achievements.Contains(steamAchievementName))
                {
                    if (SteamUserStats.SetAchievement(steamAchievementName) && SteamUserStats.StoreStats())
                    {
                        m_achievements.Add(steamAchievementName);
                        m_statsNeedUpload = false;
                    }
                }
            }
            else
            {
                m_earlyAchievementChanges[steamAchievementName] = true;
            }
        }

        public void RemoveLocalUserAchievement(string steamAchievementName)
        {
            if (m_initialised)
            {
                if (m_achievements.Contains(steamAchievementName))
                {
                    if (SteamUserStats.ClearAchievement(steamAchievementName) && SteamUserStats.StoreStats())
                    {
                        m_achievements.Remove(steamAchievementName);
                        m_statsNeedUpload = false;
                    }
                }
            }
            else
            {
                m_earlyAchievementChanges[steamAchievementName] = false;
            }
        }

        public void IndicateLocalUserAchievementProgress(string steamAchievementName, int currentValue, int unlockValue)
        {
            if (m_initialised)
            {
                if (!m_achievements.Contains(steamAchievementName))
                {
                    SteamUserStats.IndicateAchievementProgress(steamAchievementName, (uint)currentValue, (uint)unlockValue);
                }
            }
        }

        public string GetStatName(string statisticID)
        {
            return "STAT_" + statisticID.ToUpperInvariant();
        }

        public void AddLocalUserStat(string steamStatName, int count)
        {
            if (m_initialised)
            {
                int currentValue;
                if (SteamUserStats.GetStat(steamStatName, out currentValue))
                {
                    int newValue = Math.Max(currentValue + count, currentValue);
                    if (newValue != currentValue && SteamUserStats.SetStat(steamStatName, newValue))
                    {
                        m_statsNeedUpload = true;
                        m_stats[steamStatName] = newValue;
                        if (m_globalStats.ContainsKey(steamStatName))
                        {
                            m_globalStats[steamStatName] += newValue - currentValue;
                        }
                    }
                }
            }
            else
            {
                if (m_earlyStatAdditions.ContainsKey(steamStatName))
                {
                    m_earlyStatAdditions[steamStatName] += count;
                }
                else
                {
                    m_earlyStatAdditions[steamStatName] = count;
                }
            }
        }

        public void SetLocalUserStat(string steamStatName, int value)
        {
            if (m_initialised)
            {
                int currentValue;
                if (SteamUserStats.GetStat(steamStatName, out currentValue))
                {
                    int newValue = Math.Max(value, currentValue);
                    if (newValue != currentValue && SteamUserStats.SetStat(steamStatName, newValue))
                    {
                        m_statsNeedUpload = true;
                        m_stats[steamStatName] = newValue;
                        if (m_globalStats.ContainsKey(steamStatName))
                        {
                            m_globalStats[steamStatName] += newValue - currentValue;
                        }
                    }
                }
            }
            else
            {
                m_earlyStatSets[steamStatName] = value;
            }
        }

        public int GetLocalUserStat(string steamStatName)
        {
            if (m_initialised)
            {
                if (m_stats.ContainsKey(steamStatName))
                {
                    return m_stats[steamStatName];
                }
            }
            return 0;
        }

        public long GetGlobalStatistic(string statisticID)
        {
            return GetGlobalStat(GetStatName(statisticID));
        }

        public void UploadStatistics()
        {
            if (m_initialised)
            {
                if (m_statsNeedUpload && SteamUserStats.StoreStats())
                {
                    m_statsNeedUpload = false;
                }
            }
            else
            {
                m_earlyUpload = true;
            }
        }

        public int GetConcurrentPlayers()
        {
            return m_concurrentPlayers;
        }

        private long GetGlobalStat(string steamStatName)
        {
            long globalStat = 0;
            if (m_globalStats.ContainsKey(steamStatName))
            {
                globalStat = m_globalStats[steamStatName];
            }
            long localStat = GetLocalUserStat(steamStatName);
            return Math.Max(localStat, globalStat);
        }

        private void RequestStats()
        {
            SteamUserStats.RequestCurrentStats();
            MakeCall(SteamUserStats.RequestGlobalStats(0), delegate (GlobalStatsReceived_t param, bool bIOFailure)
            {
                if (!bIOFailure)
                {
                    OnGlobalStatsReceived(param);
                }
            });
            MakeCall(SteamUserStats.GetNumberOfCurrentPlayers(), delegate (NumberOfCurrentPlayers_t param, bool bIOFailure)
           {
               if (!bIOFailure)
               {
                   OnNumberOfCurrentPlayersReceived(param);
               }
           });
        }

        private HashSet<object> m_allCallbacks = new HashSet<object>();

        public void RegisterCallback<TCallbackType>(Callback<TCallbackType>.DispatchDelegate callbackDelegate)
        {
            var callback = new Callback<TCallbackType>(callbackDelegate);
            m_allCallbacks.Add(callback);
        }

        private HashSet<object> m_pendingCallResults = new HashSet<object>();

        public void MakeCall<TCallResultType>(SteamAPICall_t apiCall, CallResult<TCallResultType>.APIDispatchDelegate callResultDelegate)
        {
            CallResult<TCallResultType> callResult = null;
            callResult = new CallResult<TCallResultType>(delegate (TCallResultType param, bool ioFailure)
           {
               callResultDelegate.Invoke(param, ioFailure);
               m_pendingCallResults.Remove(callResult);
           });
            m_pendingCallResults.Add(callResult);
            callResult.Set(apiCall);
        }

        private void OnUserStatsReceived(UserStatsReceived_t args)
        {
            if (args.m_nGameID == SteamUtils.GetAppID().m_AppId)
            {
                if (args.m_eResult == EResult.k_EResultOK)
                {
                    // Get currently unlocked achivements and stats
                    m_initialised = true;
                    m_achievements.Clear();
                    for (uint i = 0; i < m_achievementNames.Length; ++i)
                    {
                        var achievementName = m_achievementNames[i];
                        bool achieved = false;
                        if (SteamUserStats.GetAchievement(achievementName, out achieved) && achieved)
                        {
                            m_achievements.Add(achievementName);
                        }
                    }
                    m_stats.Clear();
                    for (uint i = 0; i < m_statNames.Length; ++i)
                    {
                        var statName = m_statNames[i];
                        int value = 0;
                        if (SteamUserStats.GetStat(statName, out value))
                        {
                            m_stats[statName] = value;
                        }
                    }

                    // Ping achivements and stats unlocked before we initialised
                    if (m_earlyAchievementChanges.Count > 0)
                    {
                        foreach (var entry in m_earlyAchievementChanges)
                        {
                            if (entry.Value)
                            {
                                UnlockLocalUserAchievement(entry.Key);
                            }
                            else
                            {
                                RemoveLocalUserAchievement(entry.Key);
                            }
                        }
                        m_earlyAchievementChanges.Clear();
                    }
                    if (m_earlyStatAdditions.Count > 0)
                    {
                        foreach (var entry in m_earlyStatAdditions)
                        {
                            AddLocalUserStat(entry.Key, entry.Value);
                        }
                        m_earlyStatAdditions.Clear();
                    }
                    if (m_earlyStatSets.Count > 0)
                    {
                        foreach (var entry in m_earlyStatSets)
                        {
                            SetLocalUserStat(entry.Key, entry.Value);
                        }
                        m_earlyStatSets.Clear();
                    }
                    if (m_earlyUpload)
                    {
                        UploadStatistics();
                        m_earlyUpload = false;
                    }
                }
            }
        }

        private void OnGlobalStatsReceived(GlobalStatsReceived_t args)
        {
            if (args.m_nGameID == SteamUtils.GetAppID().m_AppId)
            {
                if (args.m_eResult == EResult.k_EResultOK)
                {
                    // Collect global stats
                    m_globalStats.Clear();
                    for (uint i = 0; i < m_statNames.Length; ++i)
                    {
                        var statName = m_statNames[i];
                        long globalValue = 0;
                        if (SteamUserStats.GetGlobalStat(statName, out globalValue))
                        {
                            m_globalStats[statName] = globalValue;
                        }
                    }
                }
            }
        }

        private void OnNumberOfCurrentPlayersReceived(NumberOfCurrentPlayers_t args)
        {
            if (args.m_bSuccess == 1)
            {
                m_concurrentPlayers = Math.Max(args.m_cPlayers, 1);
            }
        }

        private void OnUserStatsStored(UserStatsStored_t args)
        {
            if (args.m_nGameID == SteamUtils.GetAppID().m_AppId)
            {
                if (args.m_eResult == EResult.k_EResultOK)
                {
                    // Steamworks stats and achievements stored
                    //App.LogDebug( "Steamworks user stats stored." );
                }
            }
        }

        private void OnAchievementStored(UserAchievementStored_t args)
        {
            if (args.m_nGameID == SteamUtils.GetAppID().m_AppId)
            {
                if (args.m_nCurProgress == 0 && args.m_nMaxProgress == 0)
                {
                    // Steamworks achievement completed
                    m_achievements.Add(args.m_rgchAchievementName);
                }
                else
                {
                    // Steamworks achievement progressed
                }
            }
        }
    }
}

