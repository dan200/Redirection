using Dan200.Core.Async;

namespace Dan200.Core.Network
{
    public interface IWorkshop
    {
        // Create
        Promise<WorkshopPublishResult> CreateItem(string filePath, string previewImagePath, string title, string description, string[] tags, bool visibility);
        Promise<WorkshopPublishResult> UpdateItem(ulong itemID, string changeMessage, string filePath = null, string previewImagePath = null, string title = null, string description = null, string[] tags = null, bool? visibility = null);

        // Query
        Promise SubscribeToItem(ulong itemID);
        Promise UnsubscribeFromItem(ulong itemID);
        ulong[] GetSubscribedItems();
        WorkshopFileInfo[] GetFileInfo(ulong[] itemIDs);
        Promise<WorkshopItemInfo[]> GetItemInfo(ulong[] itemIDs);

        // Feedback
        Promise SubmitItemVote(ulong itemID, WorkshopVote rating);
        Promise SetItemPlayed(ulong itemID);
        Promise SetItemCompleted(ulong itemID);
    }
}

