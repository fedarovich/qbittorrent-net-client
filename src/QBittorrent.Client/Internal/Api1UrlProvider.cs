using System;
using System.Collections.Generic;
using System.Text;
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

        public Uri GetTorrentDownloadLimit()
        {
            throw new NotImplementedException();
        }

        public Uri SetTorrentDownloadLimit()
        {
            throw new NotImplementedException();
        }

        public Uri GetTorrentUploadLimit()
        {
            throw new NotImplementedException();
        }

        public Uri SetTorrentUploadLimit()
        {
            throw new NotImplementedException();
        }

        public Uri GetGlobalDownloadLimit()
        {
            throw new NotImplementedException();
        }

        public Uri SetGlobalDownloadLimit()
        {
            throw new NotImplementedException();
        }

        public Uri GetGlobalUploadLimit()
        {
            throw new NotImplementedException();
        }

        public Uri SetGlobalUploadLimit()
        {
            throw new NotImplementedException();
        }

        public Uri MinTorrentPriority()
        {
            throw new NotImplementedException();
        }

        public Uri MaxTorrentPriority()
        {
            throw new NotImplementedException();
        }

        public Uri IncTorrentPriority()
        {
            throw new NotImplementedException();
        }

        public Uri DecTorrentPriority()
        {
            throw new NotImplementedException();
        }

        public Uri SetFilePriority()
        {
            throw new NotImplementedException();
        }

        public Uri DeleteTorrent()
        {
            throw new NotImplementedException();
        }

        public Uri SetLocation()
        {
            throw new NotImplementedException();
        }

        public Uri Rename()
        {
            throw new NotImplementedException();
        }

        public Uri SetTrackers()
        {
            throw new NotImplementedException();
        }

        public Uri Recheck()
        {
            throw new NotImplementedException();
        }

        public Uri GetLog()
        {
            throw new NotImplementedException();
        }

        public Uri GetAlternativeSpeedLimitsEnabled()
        {
            throw new NotImplementedException();
        }

        public Uri ToggleAlternativeSpeedLimits()
        {
            throw new NotImplementedException();
        }

        public Uri SetAutomaticTorrentManagement()
        {
            throw new NotImplementedException();
        }

        public Uri SetForceStart()
        {
            throw new NotImplementedException();
        }

        public Uri SetSuperSeeding()
        {
            throw new NotImplementedException();
        }

        public Uri ToggleFirstLastPiecePrioritized()
        {
            throw new NotImplementedException();
        }

        public Uri ToggleSequentialDownload()
        {
            throw new NotImplementedException();
        }

        public Uri GetPreferences()
        {
            throw new NotImplementedException();
        }

        public Uri SetPreferences()
        {
            throw new NotImplementedException();
        }
    }
}
