using Dan200.Core.Assets;
using Dan200.Core.Async;
using Dan200.Core.Main;
using Steamworks;
using System.Collections.Generic;

namespace Dan200.Core.Network.Steamworks
{
    public class SteamworksLocalUser : ILocalUser
    {
        private SteamworksNetwork m_network;
        private FolderFileStore m_localSaveStore;
        private SteamRemoteStorageFileStore m_remoteSaveStore;

        public ulong ID
        {
            get
            {
                return SteamUser.GetSteamID().m_SteamID;
            }
        }

        public string DisplayName
        {
            get
            {
                return SteamFriends.GetPersonaName();
            }
        }

        private IDictionary<string, string> s_steamLanguageCodes = new Dictionary<string, string>() {
            { "brazilian", "pt_BR" },
            { "bulgarian", "bg" },
            { "czech", "cs" },
            { "danish", "da" },
            { "dutch", "nl" },
            { "english", "en" },
            { "finnish", "fi" },
            { "french", "fr" },
            { "german", "de" },
            { "greek", "el" },
            { "hungarian", "hu" },
            { "italian", "it" },
            { "japanese", "ja" },
            { "koreana", "ko" },
            { "norwegian", "no" },
            { "polish", "pl" },
            { "portuguese", "pt" },
            { "romanian", "ro" },
            { "russian", "ru" },
            { "schinese", "zh_CHS" },
            { "spanish", "es" },
            { "swedish", "sv" },
            { "tchinese", "zh_CHT" },
            { "thai", "th" },
            { "turkish", "tr" },
            { "ukrainian", "uk" },
        };

        public string Language
        {
            get
            {
                var steamLanguage = SteamApps.GetCurrentGameLanguage();
                if (s_steamLanguageCodes.ContainsKey(steamLanguage))
                {
                    var code = s_steamLanguageCodes[steamLanguage];
                    return code;
                }
                return "en";
            }
        }

        public IWritableFileStore LocalSaveStore
        {
            get
            {
                return m_localSaveStore;
            }
        }

        public IWritableFileStore RemoteSaveStore
        {
            get
            {
                return m_remoteSaveStore;
            }
        }

        public SteamworksLocalUser(SteamworksNetwork platform)
        {
            m_network = platform;
            m_localSaveStore = new FolderFileStore(App.SavePath);
            m_remoteSaveStore = new SteamRemoteStorageFileStore();
        }

        public void UnlockAchievement(string achievementID)
        {
            m_network.UnlockLocalUserAchievement(m_network.GetAchievementName(achievementID));
        }

        public void RemoveAchievement(string achievementID)
        {
            m_network.RemoveLocalUserAchievement(m_network.GetAchievementName(achievementID));
        }

        public void IndicateAchievementProgress(string achievementID, int currentValue, int unlockValue)
        {
            m_network.IndicateLocalUserAchievementProgress(m_network.GetAchievementName(achievementID), currentValue, unlockValue);
        }

        public void AddStatistic(string statisticID, int count)
        {
            m_network.AddLocalUserStat(m_network.GetStatName(statisticID), count);
        }

        public void SetStatistic(string statisticID, int count)
        {
            m_network.SetLocalUserStat(m_network.GetStatName(statisticID), count);
        }

        public int GetStatistic(string statisticID)
        {
            return m_network.GetLocalUserStat(m_network.GetStatName(statisticID));
        }

        public Promise SubmitLeaderboardScore(ulong id, int score)
        {
            return m_network.SubmitLocalUserLeaderboardScore(id, score);
        }
    }
}
