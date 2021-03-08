using Dan200.Core.Async;
using Dan200.Core.Main;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dan200.Core.Network.Steamworks
{
    public class SteamworksWorkshop : IWorkshop
    {
        private SteamworksNetwork m_network;

        public SteamworksWorkshop(SteamworksNetwork network)
        {
            m_network = network;
        }

        public Promise<WorkshopPublishResult> CreateItem(string filePath, string previewImagePath, string title, string description, string[] tags, bool visibility)
        {
            var result = new SimplePromise<WorkshopPublishResult>();
            m_network.MakeCall(
                SteamUGC.CreateItem(new AppId_t(App.Info.SteamAppID), EWorkshopFileType.k_EWorkshopFileTypeCommunity),
                delegate (CreateItemResult_t args, bool ioFailure)
                {
                    if (ioFailure || args.m_eResult != EResult.k_EResultOK)
                    {
                        if (!ioFailure)
                        {
                            result.Fail("Failed to create file: " + args.m_eResult);
                        }
                        else
                        {
                            result.Fail("Failed to create file");
                        }
                    }
                    else
                    {
                        ulong fileID = args.m_nPublishedFileId.m_PublishedFileId;
                        UpdateItem(fileID, "Initial upload", filePath, previewImagePath, title, description, tags, visibility, result);
                    }
                }
            );
            return result;
        }

        public Promise<WorkshopPublishResult> UpdateItem(ulong itemID, string changeMessage, string filePath = null, string previewImagePath = null, string title = null, string description = null, string[] tags = null, bool? visibility = null)
        {
            var result = new SimplePromise<WorkshopPublishResult>();
            UpdateItem(itemID, changeMessage, filePath, previewImagePath, title, description, tags, visibility, result);
            return result;
        }

        private void UpdateItem(ulong itemID, string changeMessage, string filePath, string previewImagePath, string title, string description, string[] tags, bool? visibility, SimplePromise<WorkshopPublishResult> promise)
        {
            UGCUpdateHandle_t handle = SteamUGC.StartItemUpdate(new AppId_t(App.Info.SteamAppID), new PublishedFileId_t(itemID));
            if (filePath != null)
            {
                SteamUGC.SetItemContent(handle, filePath + Path.DirectorySeparatorChar);
            }
            if (previewImagePath != null)
            {
                SteamUGC.SetItemPreview(handle, previewImagePath);
            }
            if (title != null)
            {
                SteamUGC.SetItemTitle(handle, title);
            }
            if (description != null)
            {
                SteamUGC.SetItemDescription(handle, description);
            }
            if (tags != null)
            {
                SteamUGC.SetItemTags(handle, new List<string>(tags));
            }
            if (visibility.HasValue)
            {
                SteamUGC.SetItemVisibility(handle, visibility.Value ?
                    (App.Debug ?
                        ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityFriendsOnly :
                        ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic) :
                    ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate
                );
            }
            m_network.MakeCall(
                SteamUGC.SubmitItemUpdate(handle, changeMessage),
                delegate (SubmitItemUpdateResult_t args, bool ioFailure)
                {
                    if (ioFailure || args.m_eResult != EResult.k_EResultOK)
                    {
                        if (!ioFailure)
                        {
                            promise.Fail("Failed to update file: " + args.m_eResult);
                        }
                        else
                        {
                            promise.Fail("Failed to update file");
                        }
                    }
                    else
                    {
                        var result = new WorkshopPublishResult(itemID);
                        result.AgreementNeeded = args.m_bUserNeedsToAcceptWorkshopLegalAgreement;
                        promise.Succeed(result);
                    }
                }
            );
        }

        public Promise SubscribeToItem(ulong itemID)
        {
            var promise = new SimplePromise();
            m_network.MakeCall(
                SteamUGC.SubscribeItem(new PublishedFileId_t(itemID)),
                delegate (RemoteStorageSubscribePublishedFileResult_t args, bool ioFailure)
                {
                    if (ioFailure || args.m_eResult != EResult.k_EResultOK)
                    {
                        if (!ioFailure)
                        {
                            promise.Fail("Failed to subscribe to item: " + args.m_eResult);
                        }
                        else
                        {
                            promise.Fail("Failed to subscribe to item");
                        }
                    }
                    else
                    {
                        promise.Succeed();
                    }
                }
            );
            return promise;
        }

        public Promise UnsubscribeFromItem(ulong itemID)
        {
            var promise = new SimplePromise();
            m_network.MakeCall(
                SteamUGC.UnsubscribeItem(new PublishedFileId_t(itemID)),
                delegate (RemoteStorageUnsubscribePublishedFileResult_t args, bool ioFailure)
                {
                    if (ioFailure || args.m_eResult != EResult.k_EResultOK)
                    {
                        if (!ioFailure)
                        {
                            promise.Fail("Failed to unsubscribe from item: " + args.m_eResult);
                        }
                        else
                        {
                            promise.Fail("Failed to unsubscribe from item");
                        }
                    }
                    else
                    {
                        promise.Succeed();
                    }
                }
            );
            return promise;
        }

        public ulong[] GetSubscribedItems()
        {
            uint numSubscribedFiles = SteamUGC.GetNumSubscribedItems();
            PublishedFileId_t[] files = new PublishedFileId_t[numSubscribedFiles];
            numSubscribedFiles = SteamUGC.GetSubscribedItems(files, numSubscribedFiles);
            ulong[] results = new ulong[numSubscribedFiles];
            for (int i = 0; i < results.Length; ++i)
            {
                results[i] = files[i].m_PublishedFileId;
            }
            return results;
        }

        public WorkshopFileInfo[] GetFileInfo(ulong[] itemIDs)
        {
            var results = new WorkshopFileInfo[itemIDs.Length];
            for (int i = 0; i < results.Length; ++i)
            {
                var itemID = itemIDs[i];
                ulong sizeOnDisk;
                string directory = new string('\0', 1024);
                uint timeStamp = 0;
                if (SteamUGC.GetItemInstallInfo(new PublishedFileId_t(itemID), out sizeOnDisk, out directory, (uint)directory.Length, out timeStamp))
                {
                    var fileInfo = new WorkshopFileInfo(itemID);
                    fileInfo.Installed = true;
                    fileInfo.InstallPath = directory;
                    fileInfo.Size = sizeOnDisk;
                    fileInfo.DownloadedSize = sizeOnDisk;
                    results[i] = fileInfo;
                }
                else
                {
                    ulong bytesDownloaded;
                    ulong bytesTotal;
                    if (SteamUGC.GetItemDownloadInfo(new PublishedFileId_t(itemID), out bytesDownloaded, out bytesTotal))
                    {
                        var fileInfo = new WorkshopFileInfo(itemID);
                        fileInfo.Installed = false;
                        fileInfo.InstallPath = null;
                        fileInfo.Size = bytesTotal;
                        fileInfo.DownloadedSize = bytesDownloaded;
                        results[i] = fileInfo;
                    }
                    else
                    {
                        var fileInfo = new WorkshopFileInfo(itemID);
                        fileInfo.Installed = false;
                        fileInfo.InstallPath = null;
                        fileInfo.Size = 0;
                        fileInfo.DownloadedSize = 0;
                        results[i] = fileInfo;
                    }
                }
            }
            return results;
        }

        public Promise<WorkshopItemInfo[]> GetItemInfo(ulong[] itemIDs)
        {
            var result = new SimplePromise<WorkshopItemInfo[]>();

            var publishedFileIDs = new PublishedFileId_t[itemIDs.Length];
            for (int i = 0; i < itemIDs.Length; ++i)
            {
                publishedFileIDs[i] = new PublishedFileId_t(itemIDs[i]);
            }

            var query = SteamUGC.CreateQueryUGCDetailsRequest(publishedFileIDs, (uint)publishedFileIDs.Length);
            m_network.MakeCall(
                SteamUGC.SendQueryUGCRequest(query),
                delegate (SteamUGCQueryCompleted_t args, bool ioFailure)
                {
                    if (ioFailure || args.m_eResult != EResult.k_EResultOK)
                    {
                        if (!ioFailure)
                        {
                            result.Fail("Failed to get item info: " + args.m_eResult);
                        }
                        else
                        {
                            result.Fail("Failed to get item info");
                        }
                    }
                    else
                    {
                        var results = new List<WorkshopItemInfo>((int)args.m_unNumResultsReturned);
                        for (uint i = 0; i < args.m_unNumResultsReturned; ++i)
                        {
                            SteamUGCDetails_t details;
                            uint subscribers, totalSubscribers;
                            if (SteamUGC.GetQueryUGCResult(query, i, out details) &&
                                SteamUGC.GetQueryUGCStatistic(query, i, EItemStatistic.k_EItemStatistic_NumSubscriptions, out subscribers) &&
                                SteamUGC.GetQueryUGCStatistic(query, i, EItemStatistic.k_EItemStatistic_NumUniqueSubscriptions, out totalSubscribers))
                            {
                                var id = details.m_nPublishedFileId.m_PublishedFileId;
                                var info = new WorkshopItemInfo(id);
                                info.AuthorID = details.m_ulSteamIDOwner;
                                info.Title = details.m_rgchTitle;
                                info.Description = details.m_rgchDescription;
                                info.Subscribers = (int)subscribers;
                                info.TotalSubscribers = (int)Math.Max(subscribers, totalSubscribers);
                                info.UpVotes = (int)details.m_unVotesUp;
                                info.DownVotes = (int)details.m_unVotesDown;
                                results.Add(info);
                            }
                        }
                        result.Succeed(results.ToArray());
                    }
                    SteamUGC.ReleaseQueryUGCRequest(query);
                }
            );

            return result;
        }

        public Promise SubmitItemVote(ulong itemID, WorkshopVote vote)
        {
            var result = new SimplePromise();
            m_network.MakeCall(
                SteamUGC.SetUserItemVote(new PublishedFileId_t(itemID), (vote == WorkshopVote.Up)),
                delegate (SetUserItemVoteResult_t args, bool ioFailure)
                {
                    if (ioFailure || args.m_eResult != EResult.k_EResultOK)
                    {
                        if (!ioFailure)
                        {
                            result.Fail("Failed to submit item vote: " + args.m_eResult);
                        }
                        else
                        {
                            result.Fail("Failed to submit item vote");
                        }
                    }
                    else
                    {
                        result.Succeed();
                    }
                }
            );
            return result;
        }

        public Promise SetItemPlayed(ulong itemID)
        {
            var result = new SimplePromise();
            m_network.MakeCall(
                SteamRemoteStorage.SetUserPublishedFileAction(new PublishedFileId_t(itemID), EWorkshopFileAction.k_EWorkshopFileActionPlayed),
                delegate (RemoteStorageSetUserPublishedFileActionResult_t args, bool ioFailure)
                {
                    if (ioFailure || args.m_eResult != EResult.k_EResultOK)
                    {
                        if (!ioFailure)
                        {
                            result.Fail("Failed to set item played: " + args.m_eResult);
                        }
                        else
                        {
                            result.Fail("Failed to set item played");
                        }
                    }
                    else
                    {
                        result.Succeed();
                    }
                }
            );
            return result;
        }

        public Promise SetItemCompleted(ulong itemID)
        {
            var result = new SimplePromise();
            m_network.MakeCall(
                SteamRemoteStorage.SetUserPublishedFileAction(new PublishedFileId_t(itemID), EWorkshopFileAction.k_EWorkshopFileActionCompleted),
                delegate (RemoteStorageSetUserPublishedFileActionResult_t args, bool ioFailure)
                {
                    if (ioFailure || args.m_eResult != EResult.k_EResultOK)
                    {
                        if (!ioFailure)
                        {
                            result.Fail("Failed to set item completed: " + args.m_eResult);
                        }
                        else
                        {
                            result.Fail("Failed to set item completed");
                        }
                    }
                    else
                    {
                        result.Succeed();
                    }
                }
            );
            return result;
        }
    }
}
