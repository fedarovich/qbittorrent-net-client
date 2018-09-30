﻿using System;
using System.Collections.Generic;
using System.Text;
using QBittorrent.Client.Extensions;

namespace QBittorrent.Client.Internal
{
    internal class Api2UrlProvider : BaseUrlProvider, IUrlProvider
    {
        public Api2UrlProvider(Uri baseUri) : base(baseUri)
        {
        }

        public Uri Login() => Create("/api/v2/auth/login");

        public Uri Logout() => Create("/api/v2/auth/logout");

        public Uri QBittorrentVersion() => Create("/api/v2/app/version");

        public Uri GetTorrentList(
            TorrentListFilter filter, 
            string category, 
            string sort, 
            bool reverse, 
            int? limit,
            int? offset, 
            IEnumerable<string> hashes)
        {
            var hashesString = hashes != null ? string.Join("|", hashes) : null;
            return Create("/api/v2/torrents/info",
                ("filter", filter.ToString().ToLowerInvariant()),
                ("category", category),
                ("sort", sort),
                ("reverse", reverse.ToLowerString()),
                ("limit", limit?.ToString()),
                ("offset", offset?.ToString()),
                ("hashes", hashesString));
        }

        public Uri GetTorrentProperties(string hash) => Create("/api/v2/torrents/properties", ("hash", hash));

        public Uri GetTorrentContents(string hash) => Create("/api/v2/torrents/files", ("hash", hash));

        public Uri GetTorrentTrackers(string hash) => Create("/api/v2/torrents/trackers", ("hash", hash));

        public Uri GetTorrentWebSeeds(string hash) => Create("/api/v2/torrents/webseeds", ("hash", hash));

        public Uri GetTorrentPiecesStates(string hash) => Create("/api/v2/torrents/pieceStates", ("hash", hash));

        public Uri GetTorrentPiecesHashes(string hash) => Create("/api/v2/torrents/pieceHashes", ("hash", hash));

        public Uri GetGlobalTransferInfo() => Create("/api/v2/transfer/info");

        public Uri GetPartialData(int responseId) => Create("/api/v2/sync/maindata", ("rid", responseId.ToString()));

        public Uri GetPeerPartialData(string hash, int responseId) =>
            Create("/api/v2/sync/torrentPeers",
                ("rid", responseId.ToString()),
                ("hash", hash));

        public Uri GetDefaultSavePath() => Create("/api/v2/app/defaultSavePath");

        public Uri AddTorrentFiles() => Create("/api/v2/torrents/add");

        public Uri AddTorrentUrls() => Create("/api/v2/torrents/add");

        public Uri Pause() => Create("/api/v2/torrents/pause");

        public Uri PauseAll() => Pause();

        public Uri Resume() => Create("/api/v2/torrents/resume");

        public Uri ResumeAll() => Resume();

        public Uri AddCategory() => Create("/api/v2/torrents/createCategory");

        public Uri DeleteCategories() => Create("/api/v2/torrents/removeCategories");

        public Uri SetCategory() => Create("/api/v2/torrents/setCategory");

        public Uri GetTorrentDownloadLimit() => Create("/api/v2/torrents/downloadLimit");

        public Uri SetTorrentDownloadLimit() => Create("/api/v2/torrents/setDownloadLimit");

        public Uri GetTorrentUploadLimit() => Create("/api/v2/torrents/uploadLimit");

        public Uri SetTorrentUploadLimit() => Create("/api/v2/torrents/setUploadLimit");

        public Uri GetGlobalDownloadLimit() => Create("/api/v2/transfer/downloadLimit");

        public Uri SetGlobalDownloadLimit() => Create("/api/v2/transfer/setDownloadLimit");

        public Uri GetGlobalUploadLimit() => Create("/api/v2/transfer/uploadLimit");

        public Uri SetGlobalUploadLimit() => Create("/api/v2/transfer/setUploadLimit");

        public Uri MinTorrentPriority() => Create("/api/v2/torrents/bottomPrio");

        public Uri MaxTorrentPriority() => Create("/api/v2/torrents/topPrio");

        public Uri IncTorrentPriority() => Create("/api/v2/torrents/increasePrio");

        public Uri DecTorrentPriority() => Create("/api/v2/torrents/decreasePrio");

        public Uri SetFilePriority() => Create("/api/v2/torrents/filePrio");

        public Uri DeleteTorrents(bool withFiles) => Create("/api/v2/torrents/delete");

        public Uri SetLocation() => Create("/api/v2/torrents/setLocation");

        public Uri Rename() => Create("/api/v2/torrents/rename");

        public Uri AddTrackers() => Create("/api/v2/torrents/addTrackers");

        public Uri Recheck() => Create("/api/v2/torrents/recheck");

        public Uri GetLog(TorrentLogSeverity severity, int afterId)
        {
            return Create("/api/v2/log/main",
                ("normal", severity.HasFlag(TorrentLogSeverity.Normal).ToLowerString()),
                ("info", severity.HasFlag(TorrentLogSeverity.Info).ToLowerString()),
                ("warning", severity.HasFlag(TorrentLogSeverity.Warning).ToLowerString()),
                ("critical", severity.HasFlag(TorrentLogSeverity.Critical).ToLowerString()),
                ("last_known_id", afterId.ToString()));
        }

        public Uri GetAlternativeSpeedLimitsEnabled() => Create("/api/v2/transfer/speedLimitsMode");

        public Uri ToggleAlternativeSpeedLimits() => Create("/api/v2/transfer/toggleSpeedLimitsMode");

        public Uri SetAutomaticTorrentManagement() => Create("/api/v2/torrents/setAutoManagement");

        public Uri SetForceStart() => Create("/api/v2/torrents/setForceStart");

        public Uri SetSuperSeeding() => Create("/api/v2/torrents/setSuperSeeding");

        public Uri ToggleFirstLastPiecePrioritized() => Create("/api/v2/torrents/toggleFirstLastPiecePrio");

        public Uri ToggleSequentialDownload() => Create("/api/v2/torrents/toggleSequentialDownload");

        public Uri GetPreferences() => Create("/api/v2/app/preferences");

        public Uri SetPreferences() => Create("/api/v2/app/setPreferences");

        public Uri ShutdownApplication() => Create("/api/v2/app/shutdown");

        public Uri GetPeerLog(int afterId = -1) => Create("/api/v2/log/peers", ("last_known_id", afterId.ToString()));

        public Uri Reannounce() => Create("/api/v2/torrents/reannounce");

        public Uri AddRssFolder() => Create("/api/v2/rss/addFolder");

        public Uri AddRssFeed() => Create("/api/v2/rss/addFeed");

        public Uri DeleteRssItem() => Create("/api/v2/rss/removeItem");

        public Uri MoveRssItem() => Create("/api/v2/rss/moveItem");

        public Uri GetRssItems(bool withData) => Create("/api/v2/rss/items", ("withData", withData.ToLowerString()));

        public Uri SetRssAutoDownloadingRule() => Create("/api/v2/rss/setRule");

        public Uri RenameRssAutoDownloadingRule() => Create("/api/v2/rss/renameRule");

        public Uri DeleteRssAutoDownloadingRule() => Create("/api/v2/rss/removeRule");

        public Uri GetRssAutoDownloadingRules() => Create("/api/v2/rss/rules");
    }
}
