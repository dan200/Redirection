namespace Dan200.Core.Network
{
    public struct WorkshopPublishResult
    {
        public readonly ulong ID;
        public bool AgreementNeeded;

        public WorkshopPublishResult(ulong id)
        {
            ID = id;
            AgreementNeeded = false;
        }
    }
}

