using Dan200.Core.Main;
using System;
using System.Reflection;

namespace Dan200.Game.Main
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            args = new string[] {
				//"-demo",
                //"-default_settings",
                //"-nomods",
                //"-nosound",
                //"-unlockall",
                //"-mod", "UntitledMod",
                //"-cutscene", "levels/main/outro/part1.level",
                //"-level", "levels/main/41.level",
                //"-sky", "levels/main/10.level"
            };
#endif

            var info = new GameInfo();
            info.Title = ((AssemblyTitleAttribute)typeof(Program).Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0]).Title;
            info.Website = "http://www.redirectiongame.com";
            info.DeveloperName = "Daniel Ratcliffe";
            info.DeveloperEmail = "dratcliffe@gmail.com";
            info.DeveloperTwitter = "DanTwoHundred";
            info.SteamAppID = 305760;

            var version = typeof(Program).Assembly.GetName().Version;
            info.Version = new Version(version.Major, version.Minor, version.Build);

            App.Run<Game.Game>(info, args);
        }
    }
}

