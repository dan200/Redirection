namespace Dan200.Core.Network
{
    public struct WorkshopItemInfo
    {
        public readonly ulong ID;
        public ulong AuthorID;
        public string Title;
        public string Description;
        public int Subscribers;
        public int TotalSubscribers;
        public int UpVotes;
        public int DownVotes;

        public WorkshopItemInfo(ulong id)
        {
            ID = id;
            AuthorID = 0;
            Title = "";
            Description = "";
            Subscribers = 0;
            TotalSubscribers = 0;
            UpVotes = 0;
            DownVotes = 0;
        }
    }
}
