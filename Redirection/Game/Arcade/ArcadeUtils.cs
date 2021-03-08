using Dan200.Core.Assets;
using Dan200.Core.Main;
using Dan200.Core.Modding;
using Dan200.Game.Game;
using Dan200.Game.Level;
using Dan200.Game.User;
using System.Collections.Generic;
using System.Linq;

namespace Dan200.Game.Arcade
{
    public class DiskWithMod
    {
        public readonly ArcadeDisk Disk;
        public readonly Mod Mod;

        public DiskWithMod(ArcadeDisk disk, Mod mod)
        {
            Disk = disk;
            Mod = mod;
        }
    }

    public class ArcadeUtils
    {
        public static bool IsArcadeUnlocked(Progress progress)
        {
            return IsDiskUnlocked(ArcadeDisk.Get("arcade/tennis.disk"), null, progress);
        }

        public static bool IsDiskUnlocked(ArcadeDisk disk, Mod mod, Progress progress)
        {
            if (App.Arguments.GetBool("unlockall") ||
                disk.UnlockCampaign == null ||
                disk.UnlockLevelCount == 0)
            {
                return true;
            }

            var campaignPath = disk.UnlockCampaign;
            var campaign = (mod != null) ? mod.Assets.Load<Campaign>(campaignPath) : Campaign.Get(campaignPath);

            int levelsCompleted = 0;
            for (int i = 0; i < campaign.Levels.Count; ++i)
            {
                var levelPath = campaign.Levels[i];
                var levelData = (mod != null) ? mod.Assets.Load<LevelData>(levelPath) : Assets.Get<LevelData>(levelPath);
                if (progress.IsLevelCompleted(levelData.ID))
                {
                    ++levelsCompleted;
                }
            }
            return levelsCompleted >= disk.UnlockLevelCount;
        }

        public static List<DiskWithMod> GetAllDisks()
        {
            var results = new List<DiskWithMod>();

            // Find disks in the base game
            foreach (var disk in Assets.List<ArcadeDisk>("arcade"))
            {
                var sources = Assets.GetSources(disk.Path);
                if (!sources.Where(source => source.Mod != null).Any())
                {
                    results.Add(new DiskWithMod(disk, null));
                }
            }
            results.Sort((a, b) => a.Disk.UnlockLevelCount.CompareTo(b.Disk.UnlockLevelCount));

            // Find disks in mods
            foreach (var mod in Mods.AllMods)
            {
                foreach (var disk in mod.Assets.LoadAll<ArcadeDisk>("arcade"))
                {
                    results.Add(new DiskWithMod(disk, mod));
                }
            }

            // Sort by path
            return results;
        }
    }
}
