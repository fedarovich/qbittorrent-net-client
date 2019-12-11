using System;
using System.Collections.Generic;
using System.Text;

namespace QBittorrent.Client.Internal
{
    internal interface IUrlProvider
    {
        Uri Login();

        Uri Logout();

        Uri QBittorrentVersion();

        Uri GetTorrentList(TorrentListFilter filter,
            string category,
            string sort,
            bool reverse,
            int? limit,
            int? offset, 
            IEnumerable<string> hashes);

        Uri GetTorrentProperties(string hash);

        Uri GetTorrentContents(string hash);

        Uri GetTorrentTrackers(string hash);

        Uri GetTorrentWebSeeds(string hash);

        Uri GetTorrentPiecesStates(string hash);

        Uri GetTorrentPiecesHashes(string hash);

        Uri GetGlobalTransferInfo();

        Uri GetPartialData(int responseId);

        Uri GetPeerPartialData(string hash, int responseId);

        Uri GetDefaultSavePath();

        Uri AddTorrentFiles();

        Uri AddTorrentUrls();

        Uri Pause();

        Uri PauseAll();

        Uri Resume();

        Uri ResumeAll();

        Uri AddCategory();

        Uri EditCategory();

        Uri DeleteCategories();

        Uri SetCategory();

        Uri GetTorrentDownloadLimit();

        Uri SetTorrentDownloadLimit();

        Uri GetTorrentUploadLimit();
                      
        Uri SetTorrentUploadLimit();

        Uri GetGlobalDownloadLimit();

        Uri SetGlobalDownloadLimit();

        Uri GetGlobalUploadLimit();

        Uri SetGlobalUploadLimit();

        Uri MinTorrentPriority();

        Uri MaxTorrentPriority();

        Uri IncTorrentPriority();

        Uri DecTorrentPriority();

        Uri SetFilePriority();

        Uri DeleteTorrents(bool withFiles);

        Uri SetLocation();

        Uri Rename();

        Uri AddTrackers();

        Uri Recheck();

        Uri GetLog(TorrentLogSeverity severity, int afterId);

        Uri GetAlternativeSpeedLimitsEnabled();

        Uri ToggleAlternativeSpeedLimits();

        Uri SetAutomaticTorrentManagement();

        Uri SetForceStart();

        Uri SetSuperSeeding();

        Uri ToggleFirstLastPiecePrioritized();

        Uri ToggleSequentialDownload();

        Uri GetPreferences();

        Uri SetPreferences();

        Uri ShutdownApplication();

        // API 2.0

        Uri GetPeerLog(int afterId = -1);

        Uri Reannounce();

        Uri GetCategories();

        Uri EditTracker();

        Uri DeleteTrackers();

        Uri SetShareLimits();

        // API 2.3

        Uri GetNetworkInterfaces();

        Uri GetNetworkInterfaceAddresses(string networkInterfaceId);

        Uri GetBuildInfo();

        // RSS

        Uri AddRssFolder();

        Uri AddRssFeed();

        Uri DeleteRssItem();

        Uri MoveRssItem();

        Uri GetRssItems(bool withData);

        Uri SetRssAutoDownloadingRule();

        Uri RenameRssAutoDownloadingRule();

        Uri DeleteRssAutoDownloadingRule();

        Uri GetRssAutoDownloadingRules();

        // Search

        Uri StartSearch();

        Uri StopSearch();

        Uri GetSearchStatus();

        Uri GetSearchStatus(int id);

        Uri GetSearchResults(int id, int offset, int limit);

        Uri DeleteSearch();

        Uri GetSearchCategories(string plugin);

        Uri GetSearchPlugins();

        Uri InstallSearchPlugins();

        Uri UninstallSearchPlugins();

        Uri EnableDisableSearchPlugins();

        Uri UpdateSearchPlugins();
    }
}
