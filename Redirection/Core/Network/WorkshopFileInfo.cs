namespace Dan200.Core.Network
{
    public struct WorkshopFileInfo
    {
        public readonly ulong ID;
        public bool Installed;
        public string InstallPath;
        public ulong Size;
        public ulong DownloadedSize;

        public WorkshopFileInfo(ulong id)
        {
            ID = id;
            Installed = false;
            InstallPath = null;
            Size = 0;
            DownloadedSize = 0;
        }
    }
}

