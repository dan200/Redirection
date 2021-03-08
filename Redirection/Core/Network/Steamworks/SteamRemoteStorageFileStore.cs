using Dan200.Core.Assets;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dan200.Core.Network.Steamworks
{
    public class SteamRemoteStorageFileStore : IWritableFileStore
    {
        public SteamRemoteStorageFileStore()
        {
        }

        public void ReloadIndex()
        {
        }

        public bool FileExists(string path)
        {
            return SteamRemoteStorage.FileExists(path);
        }

        public bool DirectoryExists(string path)
        {
            // Iterate all files, looking for one in the given path
            int count = SteamRemoteStorage.GetFileCount();
            for (int i = 0; i < count; ++i)
            {
                int size;
                string file = SteamRemoteStorage.GetFileNameAndSize(i, out size);
                if (file.StartsWith(path + "/"))
                {
                    return true;
                }
            }
            return false;
        }

        public long GetFileSize(string path)
        {
            return SteamRemoteStorage.GetFileSize(path);
        }

        public DateTime GetFileModificationTime(string path)
        {
            var timeStamp = SteamRemoteStorage.GetFileTimestamp(path);
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timeStamp);
        }

        public Stream OpenFile(string path)
        {
            byte[] bytes = new byte[SteamRemoteStorage.GetFileSize(path)];
            int bytesRead = SteamRemoteStorage.FileRead(path, bytes, bytes.Length);
            if (bytesRead != bytes.Length)
            {
                Array.Resize(ref bytes, bytesRead);
            }
            return new MemoryStream(bytes);
        }

        public TextReader OpenTextFile(string path)
        {
            return new StreamReader(OpenFile(path), Encoding.UTF8);
        }

        public IEnumerable<string> ListFiles(string path)
        {
            // Iterate all files, identify those in the given path
            int count = SteamRemoteStorage.GetFileCount();
            for (int i = 0; i < count; ++i)
            {
                int size;
                string file = SteamRemoteStorage.GetFileNameAndSize(i, out size);
                if (AssetPath.GetDirectoryName(file) == path)
                {
                    yield return AssetPath.GetFileName(file);
                }
            }
        }

        public IEnumerable<string> ListDirectories(string path)
        {
            // Iterate all files, get their directories, identify the directories in the given path
            int count = SteamRemoteStorage.GetFileCount();
            for (int i = 0; i < count; ++i)
            {
                int size;
                string file = SteamRemoteStorage.GetFileNameAndSize(i, out size);
                string dir = AssetPath.GetDirectoryName(file);
                if (AssetPath.GetDirectoryName(dir) == path)
                {
                    yield return AssetPath.GetFileName(dir);
                }
            }
        }

        public void SaveFile(string path, byte[] bytes)
        {
            SteamRemoteStorage.FileWrite(path, bytes, bytes.Length);
        }

        public void SaveTextFile(string path, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            SteamRemoteStorage.FileWrite(path, bytes, bytes.Length);
        }

        public void DeleteFile(string path)
        {
            SteamRemoteStorage.FileDelete(path);
        }

        public void CreateDirectory(string path)
        {
            // Not necessary on remote storage, the directory will create itself when a file is saved
        }

        public void DeleteDirectory(string path)
        {
            // Find all the files below the directory
            IList<string> deleteList = new List<string>();
            int count = SteamRemoteStorage.GetFileCount();
            for (int i = 0; i < count; ++i)
            {
                int size;
                string file = SteamRemoteStorage.GetFileNameAndSize(i, out size);
                if (file.StartsWith(path + "/"))
                {
                    deleteList.Add(file);
                }
            }

            // Delete them all. The directory will delete itself when all the files are gone
            for (int i = 0; i < deleteList.Count; ++i)
            {
                SteamRemoteStorage.FileDelete(deleteList[i]);
            }
        }
    }
}

