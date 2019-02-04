﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QBittorrent.Client.Extensions;
using QBittorrent.Client.Internal;
using static QBittorrent.Client.Internal.Utils;

namespace QBittorrent.Client
{
    public partial class QBittorrentClient : IQBittorrentClient2
    {
        private static readonly IEnumerable<string> All = new[] { "all" };

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static readonly ApiVersion Version_2_0_2 = new ApiVersion(2, 0, 2);

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static readonly ApiVersion Version_2_1_0 = new ApiVersion(2, 1);

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static readonly ApiVersion Version_2_1_1 = new ApiVersion(2, 1, 1);

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static readonly ApiVersion Version_2_2_0 = new ApiVersion(2, 2, 0);

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
        [ApiLevel(ApiLevel.V2, MinVersion = "2.0.2")]
        public Task ReannounceAsync(
            CancellationToken token = default)
        {
            return PostAsync(p => p.Reannounce(All), token, ApiLevel.V2, Version_2_0_2);
        }

        /// <summary>
        /// Reannounces the torrents.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.0.2")]
        public Task ReannounceAsync(
            [NotNull, ItemNotNull] IEnumerable<string> hashes,
            CancellationToken token = default)
        {
            ValidateHashes(ref hashes);
            return PostAsync(p => p.Reannounce(hashes), token, ApiLevel.V2, Version_2_0_2);
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
        /// Adds the category.
        /// </summary>
        /// <param name="category">The category name.</param>
        /// <param name="savePath">The save path for the torrents belonging to this category.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        public Task AddCategoryAsync(string category, string savePath, CancellationToken token = default)
        {
            return PostAsync(p => p.AddCategory(category, savePath), token, ApiLevel.V2, Version_2_1_0);
        }

        /// <summary>
        /// Changes the category save path.
        /// </summary>
        /// <param name="category">The category name.</param>
        /// <param name="savePath">The save path for the torrents belonging to this category.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        public Task EditCategoryAsync(string category, string savePath, CancellationToken token = default)
        {
            return PostAsync(p => p.EditCategory(category, savePath), token, ApiLevel.V2, Version_2_1_0);
        }

        /// <summary>
        /// Gets all categories.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.1")]
        public async Task<IReadOnlyDictionary<string, Category>> GetCategoriesAsync(CancellationToken token = default)
        {
            var provider = await _requestProvider.GetValueAsync(token).ConfigureAwait(false);
            await EnsureApiVersionAsync(provider, token, ApiLevel.V2, Version_2_1_1);

            var uri = provider.Url.GetCategories();
            var json = await _client.GetStringAsync(uri, token).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Dictionary<string, Category>>(json);
        }

        /// <summary>
        /// Changes tracker URL.
        /// </summary>
        /// <param name="hash">The hash of the torrent.</param>
        /// <param name="trackerUrl">The tracker URL you want to edit.</param>
        /// <param name="newTrackerUrl">The new URL to replace the <paramref name="trackerUrl"/>.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        public Task EditTrackerAsync(string hash, Uri trackerUrl, Uri newTrackerUrl, CancellationToken token = default)
        {
            ValidateHash(hash);
            if (trackerUrl == null)
                throw new ArgumentNullException(nameof(trackerUrl));
            if (newTrackerUrl == null)
                throw new ArgumentNullException(nameof(newTrackerUrl));
            if (!trackerUrl.IsAbsoluteUri)
                throw new ArgumentException(nameof(trackerUrl) + " must be an absolute URI.", nameof(trackerUrl));
            if (!newTrackerUrl.IsAbsoluteUri)
                throw new ArgumentException(nameof(newTrackerUrl) + " must be an absolute URI.", nameof(newTrackerUrl));

            return PostAsync(p => p.EditTracker(hash, trackerUrl, newTrackerUrl), token, ApiLevel.V2, Version_2_2_0);
        }

        /// <summary>
        /// Removes the trackers from the torrent.
        /// </summary>
        /// <param name="hash">The hash of the torrent.</param>
        /// <param name="trackerUrls">The tracker URLs you want to remove.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        public Task DeleteTrackersAsync(string hash, IEnumerable<Uri> trackerUrls, CancellationToken token = default)
        {
            ValidateHash(hash);
            if (trackerUrls == null)
                throw new ArgumentNullException(nameof(trackerUrls));
            var urls = trackerUrls.ToList();
            if (urls.Any(u => u == null))
                throw new ArgumentException("The tracker URL cannot be null.", nameof(trackerUrls));
            if (urls.Any(u => !u.IsAbsoluteUri))
                throw new ArgumentException("The tracker URLs must be absolute.", nameof(trackerUrls));

            return PostAsync(p => p.DeleteTrackers(hash, urls), token, ApiLevel.V2, Version_2_2_0);
        }

        /// <summary>
        /// Sets the file priority for multiple files.
        /// </summary>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="fileIds">The file identifiers.</param>
        /// <param name="priority">The priority.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        public Task SetFilePriorityAsync(string hash, IEnumerable<int> fileIds, TorrentContentPriority priority,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            if (fileIds == null)
                throw new ArgumentNullException(nameof(fileIds));
            if (!Enum.GetValues(typeof(TorrentContentPriority)).Cast<TorrentContentPriority>().Contains(priority))
                throw new ArgumentOutOfRangeException(nameof(priority));

            var ids = fileIds.ToList();
            if (ids.Any(id => id < 0))
                throw new ArgumentException("The file IDs must be non-negative numbers.", nameof(fileIds));

            return PostAsync(p => p.SetFilePriority(hash, ids, priority), token, ApiLevel.V2, Version_2_2_0);
        }

        /// <summary>
        /// Adds the RSS folder.
        /// </summary>
        /// <param name="path">Full path of added folder.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        public Task AddRssFolderAsync(string path, CancellationToken token = default)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("The path cannot be an empty string.", nameof(path));

            return PostAsync(p => p.AddRssFolder(path), token, ApiLevel.V2, Version_2_1_0);
        }

        /// <summary>
        /// Adds the RSS feed.
        /// </summary>
        /// <param name="url">The URL of the RSS feed.</param>
        /// <param name="path">The full path of added folder.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        public Task AddRssFeedAsync(Uri url, string path = "", CancellationToken token = default)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            return PostAsync(p => p.AddRssFeed(url, path), token, ApiLevel.V2, Version_2_1_0);
        }

        /// <summary>
        /// Removes the RSS folder or feed.
        /// </summary>
        /// <param name="path">The full path of removed folder or feed.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        public Task DeleteRssItemAsync(string path, CancellationToken token = default)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("The path cannot be an empty string.", nameof(path));

            return PostAsync(p => p.DeleteRssItem(path), token, ApiLevel.V2, Version_2_1_0);
        }

        /// <summary>
        /// Moves or renames the RSS folder or feed.
        /// </summary>
        /// <param name="path">The current full path of the folder or feed.</param>
        /// <param name="newPath">The new path.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        public Task MoveRssItemAsync(string path, string newPath, CancellationToken token = default)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("The path cannot be an empty string.", nameof(path));
            if (newPath == null)
                throw new ArgumentNullException(nameof(newPath));

            return PostAsync(p => p.MoveRssItem(path, newPath), token, ApiLevel.V2, Version_2_1_0);
        }

        /// <summary>
        /// Gets all RSS folders and feeds.
        /// </summary>
        /// <param name="withData">
        ///   <see langword="true" /> if you need current feed articles.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        public async Task<RssFolder> GetRssItemsAsync(bool withData = false, CancellationToken token = default)
        {
            var provider = await _requestProvider.GetValueAsync(token).ConfigureAwait(false);
            if (provider.ApiLevel < ApiLevel.V2 
                || await GetApiVersionAsync(token).ConfigureAwait(false) < Version_2_1_0)
                throw new ApiNotSupportedException(ApiLevel.V2);

            var uri = provider.Url.GetRssItems(withData);
            var json = await _client.GetStringAsync(uri, token).ConfigureAwait(false);
            var rootObject = JObject.Parse(json);
            return RssParser.Parse(rootObject);
        }

        /// <summary>
        /// Sets the RSS auto-downloading rule.
        /// </summary>
        /// <param name="name">The rule name.</param>
        /// <param name="rule">The rule definition.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        public Task SetRssAutoDownloadingRuleAsync(string name, RssAutoDownloadingRule rule, CancellationToken token = default)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            var ruleDefinition = JsonConvert.SerializeObject(rule);
            return PostAsync(p => p.SetRssAutoDownloadingRule(name, ruleDefinition), token, ApiLevel.V2, Version_2_1_0);
        }

        /// <summary>
        /// Renames the RSS auto-downloading rule.
        /// </summary>
        /// <param name="name">The rule name.</param>
        /// <param name="newName">The new rule name.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        public Task RenameRssAutoDownloadingRuleAsync(string name, string newName, CancellationToken token = default)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (newName == null)
                throw new ArgumentNullException(nameof(newName));

            return PostAsync(p => p.RenameRssAutoDownloadingRule(name, newName), token, ApiLevel.V2, Version_2_1_0);
        }

        /// <summary>
        /// Deletes the RSS auto-downloading rule.
        /// </summary>
        /// <param name="name">The rule name.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        public Task DeleteRssAutoDownloadingRuleAsync(string name, CancellationToken token = default)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return PostAsync(p => p.DeleteRssAutoDownloadingRule(name), token, ApiLevel.V2, Version_2_1_0);
        }

        /// <summary>
        /// Gets the RSS auto-downloading rules.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        public async Task<IReadOnlyDictionary<string, RssAutoDownloadingRule>> GetRssAutoDownloadingRulesAsync(CancellationToken token = default)
        {
            var provider = await _requestProvider.GetValueAsync(token).ConfigureAwait(false);
            await EnsureApiVersionAsync(provider, token, ApiLevel.V2, Version_2_1_0).ConfigureAwait(false);

            var uri = provider.Url.GetRssAutoDownloadingRules();
            var json = await _client.GetStringAsync(uri, token).ConfigureAwait(false);
            var result = JsonConvert.DeserializeObject<Dictionary<string, RssAutoDownloadingRule>>(json);
            return result;
        }
    }
}
