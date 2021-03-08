using Dan200.Core.Assets;
using Dan200.Core.Main;
using Dan200.Core.Network;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Modding
{
    public class Mods
    {
        private static HashSet<string> s_ignoredMods = new HashSet<string>();
        private static SortedDictionary<string, Mod> s_availableMods = new SortedDictionary<string, Mod>();
        private static List<Mod> s_removedLoadedMods = new List<Mod>();
        private static int s_pendingModCount = 0;
        private static float s_pendingModProgress = 0.0f;

        public static int Count
        {
            get
            {
                return s_availableMods.Count;
            }
        }

        public static IEnumerable<Mod> AllMods
        {
            get
            {
                return s_availableMods.Values;
            }
        }

        public static IEnumerable<Mod> AllLoadedMods
        {
            get
            {
                foreach (var mod in s_availableMods.Values)
                {
                    if (mod.Loaded)
                    {
                        yield return mod;
                    }
                }
            }
        }

        public static IEnumerable<Mod> RemovedLoadedMods
        {
            get
            {
                for (int i = s_removedLoadedMods.Count - 1; i >= 0; --i)
                {
                    var mod = s_removedLoadedMods[i];
                    if (mod.Loaded)
                    {
                        yield return mod;
                    }
                    else
                    {
                        s_removedLoadedMods.RemoveAt(i);
                    }
                }
            }
        }

        public static int PendingModCount
        {
            get
            {
                return s_pendingModCount;
            }
        }

        public static float PendingModProgress
        {
            get
            {
                return s_pendingModProgress;
            }
        }

        public static void InitLocalDirectory()
        {
            string saveDir = App.SavePath;
            string modsDir = Path.Combine(saveDir, "mods");
            if (!Directory.Exists(modsDir))
            {
                Directory.CreateDirectory(modsDir);
                File.WriteAllText(
                    Path.Combine(modsDir, "readme.txt"),
                    "Place downloaded mods in this directory to customise " + App.Info.Title + "."
                );
            }
        }

        public static void InitEditorDirectory()
        {
            string saveDir = App.SavePath;
            string modsDir = Path.Combine(saveDir, "editor" + Path.DirectorySeparatorChar + "mods");
            if (!Directory.Exists(modsDir))
            {
                Directory.CreateDirectory(modsDir);
                File.WriteAllText(
                    Path.Combine(modsDir, "readme.txt"),
                    "Mods created in the " + App.Info.Title + " mod editor will be placed in this folder."
                );
            }
        }

        private static bool TryAddMod(string path, ModSource source, out Mod o_mod)
        {
            Mod mod;
            try
            {
                // (re)load the mod
                if (s_availableMods.ContainsKey(path))
                {
                    mod = s_availableMods[path];
                    mod.ReloadInfo();
                }
                else
                {
                    mod = new Mod(path, source);
                }
            }
            catch (Exception)
            {
                bool changed = false;
                if (!s_ignoredMods.Contains(path))
                {
                    App.Log("Error loading mod {0}. Ignoring", path);
                    s_ignoredMods.Add(path);
                    changed = true;
                }
                if (s_availableMods.ContainsKey(path))
                {
                    App.Log("Error loading mod {0}. Disabling", path);
                    mod = s_availableMods[path];
                    s_availableMods.Remove(path);
                    if (mod.Loaded)
                    {
                        s_removedLoadedMods.Add(mod);
                    }
                    changed = true;
                }
                o_mod = null;
                return changed;
            }

            // Check if the mod is too new
            if (App.Info.Version < mod.MinimumGameVersion)
            {
                // Remove and ignore the mod
                bool changed = false;
                if (!s_ignoredMods.Contains(path))
                {
                    App.Log("Error: Ignoring mod {0} ({1}): Requires game version {2}", mod.Title, mod.Path, mod.MinimumGameVersion);
                    s_ignoredMods.Add(path);
                    changed = true;
                }
                if (s_availableMods.ContainsKey(path))
                {
                    App.Log("Error: Disabling mod {0} ({1}): Requires game version {2}", mod.Title, mod.Path, mod.MinimumGameVersion);
                    s_availableMods.Remove(path);
                    if (mod.Loaded)
                    {
                        s_removedLoadedMods.Add(mod);
                    }
                    changed = true;
                }
                o_mod = null;
                return changed;
            }
            else
            {
                // Add the mod
                bool changed = false;
                if (s_ignoredMods.Contains(path))
                {
                    s_ignoredMods.Remove(path);
                    changed = true;
                }
                if (!s_availableMods.ContainsKey(path))
                {
                    App.Log("Found mod {0} ({1})", mod.Title, mod.Path);
                    s_availableMods[path] = mod;
                    changed = true;
                }
                o_mod = mod;
                return changed;
            }
        }

        private static bool TryRemoveMod(string path)
        {
            bool changed = false;
            if (s_availableMods.ContainsKey(path))
            {
                var mod = s_availableMods[path];
                App.Log("Removed mod {0} ({1})", mod.Title, mod.Path);
                s_availableMods.Remove(path);
                if (mod.Loaded)
                {
                    s_removedLoadedMods.Add(mod);
                }
                changed = true;
            }
            if (s_ignoredMods.Contains(path))
            {
                s_ignoredMods.Remove(path);
                changed = true;
            }
            return changed;
        }

        public static bool Refresh(INetwork network)
        {
            if (App.Arguments.GetBool("nomods"))
            {
                return false;
            }

            bool changed = false;

            var oldMods = new List<string>();
            oldMods.AddRange(s_ignoredMods);
            oldMods.AddRange(s_availableMods.Keys);

            // Find mods in the mods directory
            string saveDir = App.SavePath;
            string modsDir = Path.Combine(saveDir, "mods");
            if (!Directory.Exists(modsDir))
            {
                InitLocalDirectory();
            }
            else
            {
                string[] filePaths = Directory.GetFileSystemEntries(modsDir);
                foreach (string filePath in filePaths)
                {
                    var path = filePath;
                    if (Directory.Exists(path) || (File.Exists(path) && path.EndsWith(".zip")))
                    {
                        Mod mod;
                        if (TryAddMod(path, ModSource.Local, out mod))
                        {
                            changed = true;
                        }
                        oldMods.Remove(path);
                    }
                }
            }

            // Find mods in the editor mods directory
            string editorModsDir = Path.Combine(saveDir, "editor" + Path.DirectorySeparatorChar + "mods");
            if (!Directory.Exists(editorModsDir))
            {
                InitEditorDirectory();
            }
            else
            {
                string[] filePaths = Directory.GetFileSystemEntries(editorModsDir);
                foreach (string filePath in filePaths)
                {
                    var path = filePath;
                    if (Directory.Exists(path))
                    {
                        Mod mod;
                        if (TryAddMod(path, ModSource.Editor, out mod))
                        {
                            changed = true;
                        }
                        oldMods.Remove(path);
                    }
                }
            }

            // Find mods on the workshop
            if (network.SupportsWorkshop)
            {
                IWorkshop workshop = network.Workshop;
                ulong[] subscribedMods = workshop.GetSubscribedItems();
                int pendingModCount = 0;
                ulong pendingModTotalSize = 0;
                ulong pendingModTotalDownloaded = 0;
                var fileInfos = workshop.GetFileInfo(subscribedMods);
                for (int i = 0; i < fileInfos.Length; ++i)
                {
                    var fileInfo = fileInfos[i];
                    if (fileInfo.Installed)
                    {
                        var path = fileInfo.InstallPath;
                        if (Directory.Exists(path) || (File.Exists(path) && path.EndsWith(".zip")))
                        {
                            Mod mod;
                            if (TryAddMod(path, ModSource.Workshop, out mod))
                            {
                                changed = true;
                            }
                            if (mod != null)
                            {
                                mod.SteamWorkshopID = fileInfo.ID;
                            }
                            oldMods.Remove(path);
                        }
                    }
                    else
                    {
                        pendingModCount++;
                        pendingModTotalSize += fileInfo.Size;
                        pendingModTotalDownloaded += fileInfo.DownloadedSize;
                    }
                }
                s_pendingModCount = pendingModCount;
                s_pendingModProgress = (pendingModTotalSize > 0) ?
                    (float)pendingModTotalDownloaded / (float)pendingModTotalSize :
                    0.0f;
            }
            else
            {
                s_pendingModCount = 0;
                s_pendingModProgress = 0.0f;
            }

            // Remove old mods that we didn't find when re-scanning
            foreach (string path in oldMods)
            {
                if (TryRemoveMod(path))
                {
                    changed = true;
                }
            }
            return changed;
        }

        public static Mod Create(string title)
        {
            // Determine a unique path
            var shortTitle = title.ToSafeAssetName(false);
            var modsPath = Path.Combine(App.SavePath, "editor" + Path.DirectorySeparatorChar + "mods");
            var path = Path.Combine(modsPath, shortTitle);

            int i = 2;
            while (Directory.Exists(path))
            {
                path = Path.Combine(modsPath, shortTitle + i);
                ++i;
            }

            // Create directory
            App.Log("Creating mod {0} ({1})", title, path);
            Mods.InitEditorDirectory();
            Directory.CreateDirectory(path);

            // Create info.txt
            var kvp = new KeyValuePairs();
            kvp.Comment = "Mod information";
            kvp.Set("title", title);
            kvp.Set("version", new Version(1, 0, 0));
            kvp.Set("game_version", App.Info.Version);
            kvp.Set("autoload", false);

            var infoPath = Path.Combine(path, "info.txt");
            using (var infoStream = new StreamWriter(infoPath))
            {
                kvp.Save(infoStream);
            }

            // Create assets directory
            var assetsPath = Path.Combine(path, "assets");
            Directory.CreateDirectory(assetsPath);

            // Load and return our new mod
            Mod mod;
            if (TryAddMod(path, ModSource.Editor, out mod))
            {
                return mod;
            }
            else
            {
                throw new IOException(string.Format("Failed to load newly created mod {0}", path));
            }
        }

        public static void Delete(Mod mod)
        {
            TryRemoveMod(mod.Path);
            App.Log("Deleting mod {0} ({1})", mod.Title, mod.Path);
            DeleteRecursive(mod.Path);
        }

        private static void DeleteRecursive(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else if (Directory.Exists(path))
            {
                foreach (var subPath in Directory.EnumerateFileSystemEntries(path))
                {
                    DeleteRecursive(subPath);
                }
                Directory.Delete(path, true);
            }
        }

        public static void Export(Mod mod, string outputPath)
        {
            App.Log("Exporting mod {0} ({1}) to {2}", mod.Title, mod.Path, outputPath);
            using (var zipFile = new ZipFile())
            {
                // Build the zip
                AddFolderToZip(zipFile, mod.Path, "");

                // Save the zip
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                using (var stream = File.OpenWrite(outputPath))
                {
                    zipFile.Save(stream);
                }
            }
        }

        private static bool ShouldIgnoreDirectory(string path)
        {
            return false;
        }

        private static bool ShouldIgnoreFile(string path)
        {
            var name = Path.GetFileName(path);
            return name == ".DS_Store" || name == "Thumbs.db";
        }

        private static void AddFolderToZip(ZipFile zipFile, string inputPath, string outputPath)
        {
            var directoryPaths = Directory.GetDirectories(inputPath);
            foreach (string directoryPath in directoryPaths)
            {
                if (!ShouldIgnoreDirectory(directoryPath))
                {
                    var directoryOutputPath = Path.Combine(outputPath, Path.GetFileName(directoryPath));
                    zipFile.AddDirectoryByName(directoryOutputPath);
                    AddFolderToZip(zipFile, directoryPath, directoryOutputPath);
                }
            }

            var filePaths = Directory.GetFiles(inputPath);
            foreach (string filePath in filePaths)
            {
                if (!ShouldIgnoreFile(filePath))
                {
                    var fileOutputPath = Path.Combine(outputPath, Path.GetFileName(filePath));
                    byte[] contents = File.ReadAllBytes(filePath);
                    zipFile.AddEntry(fileOutputPath, contents);
                }
            }
        }
    }
}

