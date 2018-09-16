using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using QBittorrent.Client.Extensions;
using static QBittorrent.Client.Internal.Utils;

namespace QBittorrent.Client
{
    public partial class QBittorrentClient : IQBittorrentClient2
    {
        private static readonly IEnumerable<string> All = new[] { "all" };

        /// <summary>
        /// Gets the peer log.
        /// </summary>
        [ApiLevel(ApiLevel.V2)]
        public async Task<IEnumerable<PeerLogEntry>> GetPeerLogAsync(
            int afterId = -1,
            CancellationToken token = default)
        {
            var uri = await BuildUriAsync(p => p.GetPeerLog(afterId), token).ConfigureAwait(false);
            var json = await _client.GetStringAsync(uri, token).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<IEnumerable<PeerLogEntry>>(json);
        }

        /// <summary>
        /// Adds the torrents to download.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task AddTorrentsAsync(
            [NotNull] AddTorrentsRequest request,
            CancellationToken token = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return PostAsync(p => p.AddTorrents(request), token);
        }

        /// <summary>
        /// Deletes all torrents.
        /// </summary>
        /// <param name="deleteDownloadedData"><see langword="true"/> to delete the downloaded data.</param>
        /// <param name="token">The cancellation token.</param>
        [ApiLevel(ApiLevel.V2)]
        public Task DeleteAsync(
            bool deleteDownloadedData = false,
            CancellationToken token = default)
        {
            return PostAsync(p => p.DeleteTorrents(All, deleteDownloadedData), token, ApiLevel.V2);
        }

        /// <summary>
        /// Rechecks all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task RecheckAsync(
            CancellationToken token = default)
        {
            return PostAsync(p => p.Recheck(All), token, ApiLevel.V2);
        }

        /// <summary>
        /// Rechecks the torrents.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task RecheckAsync(
            [NotNull, ItemNotNull] IEnumerable<string> hashes,
            CancellationToken token = default)
        {
            ValidateHashes(ref hashes);
            return PostAsync(p => p.Recheck(hashes), token, ApiLevel.V2);
        }

        /// <summary>
        /// Reannounces all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task ReannounceAsync(
            CancellationToken token = default)
        {
            return PostAsync(p => p.Reannounce(All), token, ApiLevel.V2);
        }

        /// <summary>
        /// Reannounces the torrents.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task ReannounceAsync(
            [NotNull, ItemNotNull] IEnumerable<string> hashes,
            CancellationToken token = default)
        {
            ValidateHashes(ref hashes);
            return PostAsync(p => p.Reannounce(hashes), token, ApiLevel.V2);
        }

        /// <summary>
        /// Pauses all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        /// <remarks>This method supersedes <see cref="IQBittorrentClient.PauseAllAsync"/>.</remarks>
        public Task PauseAsync(
            CancellationToken token = default)
        {
            return PostAsync(p => p.PauseAll(), token);
        }

        /// <summary>
        /// Resumes all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        /// <remarks>This method supersedes <see cref="IQBittorrentClient.ResumeAllAsync"/>.</remarks>
        public Task ResumeAsync(
            CancellationToken token = default)
        {
            return PostAsync(p => p.ResumeAll(), token);
        }

        /// <summary>
        /// Changes the torrent priority for all torrents.
        /// </summary>
        /// <param name="change">The priority change.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task ChangeTorrentPriorityAsync(
            TorrentPriorityChange change,
            CancellationToken token = default)
        {
            switch (change)
            {
                case TorrentPriorityChange.Minimal:
                    return PostAsync(p => p.MinTorrentPriority(All), token, ApiLevel.V2);
                case TorrentPriorityChange.Increase:
                    return PostAsync(p => p.IncTorrentPriority(All), token, ApiLevel.V2);
                case TorrentPriorityChange.Decrease:
                    return PostAsync(p => p.DecTorrentPriority(All), token, ApiLevel.V2);
                case TorrentPriorityChange.Maximal:
                    return PostAsync(p => p.MaxTorrentPriority(All), token, ApiLevel.V2);
                default:
                    throw new ArgumentOutOfRangeException(nameof(change), change, null);
            }
        }

        /// <summary>
        /// Gets the torrent download speed limit for all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task<IReadOnlyDictionary<string, long?>> GetTorrentDownloadLimitAsync(
            CancellationToken token = default)
        {
            return PostAsync(p => p.GetTorrentDownloadLimit(All), token, GetTorrentLimits, ApiLevel.V2);
        }

        /// <summary>
        /// Sets the torrent download speed limit for all torrents.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task SetTorrentDownloadLimitAsync(
            long limit,
            CancellationToken token = default)
        {
            if (limit < 0)
                throw new ArgumentOutOfRangeException(nameof(limit));

            return PostAsync(p => p.SetTorrentDownloadLimit(All, limit), token, ApiLevel.V2);
        }

        /// <summary>
        /// Gets the torrent upload speed limit for all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task<IReadOnlyDictionary<string, long?>> GetTorrentUploadLimitAsync(
            CancellationToken token = default)
        {
            return PostAsync(p => p.GetTorrentUploadLimit(All), token, GetTorrentLimits, ApiLevel.V2);
        }

        /// <summary>
        /// Sets the torrent upload speed limit for all torrents.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task SetTorrentUploadLimitAsync(
            long limit,
            CancellationToken token = default)
        {
            if (limit < 0)
                throw new ArgumentOutOfRangeException(nameof(limit));

            return PostAsync(p => p.SetTorrentUploadLimit(All, limit), token, ApiLevel.V2);
        }

        /// <summary>
        /// Sets the location of all torrents.
        /// </summary>
        /// <param name="newLocation">The new location.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task SetLocationAsync(
            [NotNull] string newLocation,
            CancellationToken token = default)
        {
            if (newLocation == null)
                throw new ArgumentNullException(nameof(newLocation));
            if (string.IsNullOrEmpty(newLocation))
                throw new ArgumentException("The location cannot be an empty string.", nameof(newLocation));

            return PostAsync(p => p.SetLocation(All, newLocation), token, ApiLevel.V2);
        }

        /// <summary>
        /// Sets the torrent category for all torrents.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task SetTorrentCategoryAsync(
            [NotNull] string category,
            CancellationToken token = default)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            return PostAsync(p => p.SetCategory(All, category), token, ApiLevel.V2);
        }

        /// <summary>
        /// Sets the automatic torrent management for all torrents.
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task SetAutomaticTorrentManagementAsync(
            bool enabled,
            CancellationToken token = default)
        {
            return PostAsync(p => p.SetAutomaticTorrentManagement(All, enabled), token, ApiLevel.V2);
        }

        /// <summary>
        /// Toggles the sequential download for all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task ToggleSequentialDownloadAsync(
            CancellationToken token = default)
        {
            return PostAsync(p => p.ToggleSequentialDownload(All), token, ApiLevel.V2);
        }

        /// <summary>
        /// Toggles the first and last piece priority for all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task ToggleFirstLastPiecePrioritizedAsync(
            CancellationToken token = default)
        {
            return PostAsync(p => p.ToggleFirstLastPiecePrioritized(All), token, ApiLevel.V2);
        }

        /// <summary>
        /// Sets the force start for all torrents.
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task SetForceStartAsync(
            bool enabled,
            CancellationToken token = default)
        {
            return PostAsync(p => p.SetForceStart(All, enabled), token, ApiLevel.V2);
        }

        /// <summary>
        /// Sets the super seeding for all torrents.
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task SetSuperSeedingAsync(
            bool enabled,
            CancellationToken token = default)
        {
            return PostAsync(p => p.SetSuperSeeding(All, enabled), token, ApiLevel.V2);
        }

        /// <summary>
        /// Adds the RSS folder.
        /// </summary>
        /// <param name="path">Full path of added folder.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task AddRssFolderAsync(string path, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds the RSS feed.
        /// </summary>
        /// <param name="url">The URL of the RSS feed.</param>
        /// <param name="path">The full path of added folder.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task AddRssFeedAsync(Uri url, string path = null, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes the RSS folder or feed.
        /// </summary>
        /// <param name="path">The full path of removed folder or feed.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task DeleteRssItemAsync(string path, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Moves or renames the RSS folder or feed.
        /// </summary>
        /// <param name="path">The current full path of the folder or feed.</param>
        /// <param name="newPath">The new path.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task MoveRssItemAsync(string path, string newPath, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets all RSS folders and feeds.
        /// </summary>
        /// <param name="withData">
        ///   <see langword="true" /> if you need current feed articles.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task<RssFolder> GetRssItemsAsync(bool withData = false, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the RSS auto-downloading rule.
        /// </summary>
        /// <param name="name">The rule name.</param>
        /// <param name="rule">The rule definition.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task SetRssAutoDownloadingRuleAsync(string name, RssAutoDownloadingRule rule, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Renames the RSS auto-downloading rule.
        /// </summary>
        /// <param name="name">The rule name.</param>
        /// <param name="newName">The new rule name.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task RenameRssAutoDownloadingRuleAsync(string name, string newName, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deletes the RSS auto-downloading rule.
        /// </summary>
        /// <param name="name">The rule name.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task DeleteRssAutoDownloadingRuleAsync(string name, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the RSS auto-downloading rules.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        public Task<IReadOnlyDictionary<string, RssAutoDownloadingRule>> GetRssAutoDownloadingRulesAsync(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}
