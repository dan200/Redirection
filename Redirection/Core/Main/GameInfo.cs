using System;

namespace Dan200.Core.Main
{
    public class GameInfo
    {
        public string Title;
        public Version Version;
        public string Website;
        public string DeveloperName;
        public string DeveloperEmail;
        public string DeveloperTwitter;
        public uint SteamAppID;

        public GameInfo()
        {
            Title = "Untitled";
            Version = new Version(0, 0, 0);
            Website = "";
            DeveloperName = "";
            DeveloperEmail = "";
            DeveloperTwitter = "";
            SteamAppID = 0;
        }
    }
}

