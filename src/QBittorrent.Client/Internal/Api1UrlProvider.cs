using System;
using QBittorrent.Client.Extensions;

namespace QBittorrent.Client.Internal
{
    internal class Api1UrlProvider : BaseUrlProvider, IUrlProvider
    {
        public Api1UrlProvider(Uri baseUri) : base(baseUri)
        {
        }

        public Uri Login() => Create("/login");

        public Uri Logout() => Create("/logout");

        public Uri QBittorrentVersion() => Create("/version/qbittorrent");

        public Uri GetTorrentList(
            TorrentListFilter filter, 
            string category, 
            string sort, 
            bool reverse, 
            int? limit, 
            int? offset)
        {
            return Create("/query/torrents",
                ("filter", filter.ToString().ToLowerInvariant()),
                ("category", category),
                ("sort", sort),
                ("reverse", reverse.ToLowerString()),
                ("limit", limit?.ToString()),
                ("offset", offset?.ToString()));
        }

        public Uri GetTorrentProperties(string hash) => Create($"/query/propertiesGeneral/{hash}");

        public Uri GetTorrentContents(string hash) => Create($"/query/propertiesFiles/{hash}");

        public Uri GetTorrentTrackers(string hash) => Create($"/query/propertiesTrackers/{hash}");

        public Uri GetTorrentWebSeeds(string hash) => Create($"/query/propertiesWebSeeds/{hash}");

        public Uri GetTorrentPiecesStates(string hash) => Create($"/query/getPieceStates/{hash}");

        public Uri GetTorrentPiecesHashes(string hash) => Create($"/query/getPieceHashes/{hash}");

        public Uri GetGlobalTransferInfo() => Create("/query/transferInfo");

        public Uri GetPartialData(int responseId) => Create("/sync/maindata", ("rid", responseId.ToString()));

        public Uri GetPeerPartialData(string hash, int responseId) => Create("/sync/torrent_peers",
            ("rid", responseId.ToString()),
            ("hash", hash));

        public Uri GetDefaultSavePath() => Create("/command/getSavePath");

        public Uri AddTorrentFiles() => Create("/command/upload");

        public Uri AddTorrentUrls() => Create("/command/download");

        public Uri Pause() => Create("/command/pause");

        public Uri PauseAll() => Create("/command/pauseAll");

        public Uri Resume() => Create("/command/resume");

        public Uri ResumeAll() => Create("/command/resumeAll");

        public Uri AddCategory() => Create("/command/addCategory");

        public Uri DeleteCategories() => Create("/command/removeCategories");

        public Uri SetCategory() => Create("/command/setCategory");

        public Uri GetTorrentDownloadLimit() => Create("/command/getTorrentsDlLimit");

        public Uri SetTorrentDownloadLimit() => Create("/command/setTorrentsDlLimit");

        public Uri GetTorrentUploadLimit() => Create("/command/getTorrentsUpLimit");

        public Uri SetTorrentUploadLimit() => Create("/command/setTorrentsUpLimit");

        public Uri GetGlobalDownloadLimit() => Create("/command/getGlobalDlLimit");

        public Uri SetGlobalDownloadLimit() => Create("/command/setGlobalDlLimit");

        public Uri GetGlobalUploadLimit() => Create("/command/getGlobalUpLimit");

        public Uri SetGlobalUploadLimit() => Create("/command/setGlobalUpLimit");

        public Uri MinTorrentPriority() => Create("/command/bottomPrio");

        public Uri MaxTorrentPriority() => Create("/command/topPrio");

        public Uri IncTorrentPriority() => Create("/command/increasePrio");

        public Uri DecTorrentPriority() => Create("/command/decreasePrio");

        public Uri SetFilePriority() => Create("/command/setFilePrio");

        public Uri DeleteTorrent(bool withFiles) => withFiles ? Create("/command/deletePerm") : Create("/command/delete");

        public Uri SetLocation() => Create("/command/setLocation");

        public Uri Rename() => Create("/command/rename");

        public Uri AddTrackers() => Create("/command/addTrackers");

        public Uri Recheck() => Create("/command/recheck");

        public Uri GetLog(TorrentLogSeverity severity, int afterId)
        {
            return Create("/query/getLog",
                ("normal", severity.HasFlag(TorrentLogSeverity.Normal).ToLowerString()),
                ("info", severity.HasFlag(TorrentLogSeverity.Info).ToLowerString()),
                ("warning", severity.HasFlag(TorrentLogSeverity.Warning).ToLowerString()),
                ("critical", severity.HasFlag(TorrentLogSeverity.Critical).ToLowerString()),
                ("last_known_id", afterId.ToString()));
        }

        public Uri GetAlternativeSpeedLimitsEnabled() => Create("/command/alternativeSpeedLimitsEnabled");

        public Uri ToggleAlternativeSpeedLimits() => Create("/command/toggleAlternativeSpeedLimits");

        public Uri SetAutomaticTorrentManagement() => Create("/command/setAutoTMM");

        public Uri SetForceStart() => Create("/command/setForceStart");

        public Uri SetSuperSeeding() => Create("/command/setSuperSeeding");

        public Uri ToggleFirstLastPiecePrioritized() => Create("/command/toggleFirstLastPiecePrio");

        public Uri ToggleSequentialDownload() => Create("/command/toggleSequentialDownload");

        public Uri GetPreferences() => Create("/query/preferences");

        public Uri SetPreferences() => Create("/command/setPreferences");
    }
}
