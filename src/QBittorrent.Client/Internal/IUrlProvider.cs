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
            int? offset);

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

        Uri DeleteTorrent(bool withFiles);

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
    }
}
