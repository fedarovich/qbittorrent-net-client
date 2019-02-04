using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace QBittorrent.Client
{
    /// <summary>
    /// Provides access to qBittorrent remote API.
    /// </summary>
    /// <seealso cref="QBittorrentClient"/>
    /// <seealso cref="QBittorrentClientExtensions"/>
    /// <seealso cref="IQBittorrentClient2"/>
    public interface IQBittorrentClient2 : IQBittorrentClient
    {
        /// <summary>
        /// Gets the peer log.
        /// </summary>
        [ApiLevel(ApiLevel.V2)]
        Task<IEnumerable<PeerLogEntry>> GetPeerLogAsync(
            int afterId = -1,
            CancellationToken token = default);

        /// <summary>
        /// Adds the torrents to download.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        Task AddTorrentsAsync(
            [NotNull] AddTorrentsRequest request,
            CancellationToken token = default);

        /// <summary>
        /// Deletes all torrents.
        /// </summary>
        /// <param name="deleteDownloadedData"><see langword="true"/> to delete the downloaded data.</param>
        /// <param name="token">The cancellation token.</param>
        [ApiLevel(ApiLevel.V2)]
        Task DeleteAsync(
            bool deleteDownloadedData = false,
            CancellationToken token = default);

        /// <summary>
        /// Rechecks all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        Task RecheckAsync(
            CancellationToken token = default);

        /// <summary>
        /// Rechecks the torrents.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        Task RecheckAsync(
            [NotNull, ItemNotNull] IEnumerable<string> hashes,
            CancellationToken token = default);

        /// <summary>
        /// Reannounces all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.0.2")]
        Task ReannounceAsync(
            CancellationToken token = default);

        /// <summary>
        /// Reannounces the torrents.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.0.2")]
        Task ReannounceAsync(
            [NotNull, ItemNotNull] IEnumerable<string> hashes,
            CancellationToken token = default);

        /// <summary>
        /// Pauses all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        /// <remarks>This method supersedes <see cref="IQBittorrentClient.PauseAllAsync"/>.</remarks>
        [ApiLevel(ApiLevel.V1)]
        Task PauseAsync(
            CancellationToken token = default);

        /// <summary>
        /// Resumes all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        /// <remarks>This method supersedes <see cref="IQBittorrentClient.ResumeAllAsync"/>.</remarks>
        [ApiLevel(ApiLevel.V1)]
        Task ResumeAsync(
            CancellationToken token = default);

        /// <summary>
        /// Changes the torrent priority for all torrents.
        /// </summary>
        /// <param name="change">The priority change.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        Task ChangeTorrentPriorityAsync(
            TorrentPriorityChange change,
            CancellationToken token = default);

        /// <summary>
        /// Sets the torrent download speed limit for all torrents.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        Task SetTorrentDownloadLimitAsync(
            long limit,
            CancellationToken token = default);

        /// <summary>
        /// Sets the torrent upload speed limit for all torrents.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        Task SetTorrentUploadLimitAsync(
            long limit,
            CancellationToken token = default);

        /// <summary>
        /// Sets the location of all torrents.
        /// </summary>
        /// <param name="newLocation">The new location.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        Task SetLocationAsync(
            [NotNull] string newLocation,
            CancellationToken token = default);

        /// <summary>
        /// Sets the torrent category for all torrents.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        Task SetTorrentCategoryAsync(
            [NotNull] string category,
            CancellationToken token = default);

        /// <summary>
        /// Sets the automatic torrent management for all torrents.
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        Task SetAutomaticTorrentManagementAsync(
            bool enabled,
            CancellationToken token = default);

        /// <summary>
        /// Toggles the sequential download for all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        Task ToggleSequentialDownloadAsync(
            CancellationToken token = default);

        /// <summary>
        /// Toggles the first and last piece priority for all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        Task ToggleFirstLastPiecePrioritizedAsync(
            CancellationToken token = default);

        /// <summary>
        /// Sets the force start for all torrents.
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        Task SetForceStartAsync(
            bool enabled,
            CancellationToken token = default);

        /// <summary>
        /// Sets the super seeding for all torrents.
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        Task SetSuperSeedingAsync(
            bool enabled,
            CancellationToken token = default);

        /// <summary>
        /// Adds the category.
        /// </summary>
        /// <param name="category">The category name.</param>
        /// <param name="savePath">The save path for the torrents belonging to this category.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        Task AddCategoryAsync(
            [NotNull] string category,
            string savePath,
            CancellationToken token = default);

        /// <summary>
        /// Changes the category save path.
        /// </summary>
        /// <param name="category">The category name.</param>
        /// <param name="savePath">The save path for the torrents belonging to this category.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        Task EditCategoryAsync(
            [NotNull] string category,
            string savePath,
            CancellationToken token = default);

        /// <summary>
        /// Gets all categories.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.1")]
        Task<IReadOnlyDictionary<string, Category>> GetCategoriesAsync(
            CancellationToken token = default);

        /// <summary>
        /// Changes tracker URL.
        /// </summary>
        /// <param name="hash">The hash of the torrent.</param>
        /// <param name="trackerUrl">The tracker URL you want to edit.</param>
        /// <param name="newTrackerUrl">The new URL to replace the <paramref name="trackerUrl"/>.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        Task EditTrackerAsync(
            [NotNull] string hash,
            [NotNull] Uri trackerUrl,
            [NotNull] Uri newTrackerUrl,
            CancellationToken token = default);

        /// <summary>
        /// Removes the trackers from the torrent.
        /// </summary>
        /// <param name="hash">The hash of the torrent.</param>
        /// <param name="trackerUrls">The tracker URLs you want to remove.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        Task DeleteTrackersAsync(
            [NotNull] string hash,
            [NotNull, ItemNotNull] IEnumerable<Uri> trackerUrls,
            CancellationToken token = default);

        /// <summary>
        /// Sets the file priority for multiple files.
        /// </summary>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="fileIds">The file identifiers.</param>
        /// <param name="priority">The priority.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        Task SetFilePriorityAsync(
            [NotNull] string hash,
            [NotNull] IEnumerable<int> fileIds,
            TorrentContentPriority priority,
            CancellationToken token = default);

        // RSS

        /// <summary>
        /// Adds the RSS folder.
        /// </summary>
        /// <param name="path">Full path of added folder.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        Task AddRssFolderAsync(
            string path,
            CancellationToken token = default);

        /// <summary>
        /// Adds the RSS feed.
        /// </summary>
        /// <param name="url">The URL of the RSS feed.</param>
        /// <param name="path">The full path of added folder.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        Task AddRssFeedAsync(
            Uri url,
            string path = "",
            CancellationToken token = default);

        /// <summary>
        /// Removes the RSS folder or feed.
        /// </summary>
        /// <param name="path">The full path of removed folder or feed.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        Task DeleteRssItemAsync(
            string path,
            CancellationToken token = default);

        /// <summary>
        /// Moves or renames the RSS folder or feed.
        /// </summary>
        /// <param name="path">The current full path of the folder or feed.</param>
        /// <param name="newPath">The new path.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        Task MoveRssItemAsync(
            string path,
            string newPath,
            CancellationToken token = default);

        /// <summary>
        /// Gets all RSS folders and feeds.
        /// </summary>
        /// <param name="withData"><see langword="true" /> if you need current feed articles.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        Task<RssFolder> GetRssItemsAsync(
            bool withData = false,
            CancellationToken token = default);

        /// <summary>
        /// Sets the RSS auto-downloading rule.
        /// </summary>
        /// <param name="name">The rule name.</param>
        /// <param name="rule">The rule definition.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        Task SetRssAutoDownloadingRuleAsync(
            string name,
            RssAutoDownloadingRule rule,
            CancellationToken token = default);

        /// <summary>
        /// Renames the RSS auto-downloading rule.
        /// </summary>
        /// <param name="name">The rule name.</param>
        /// <param name="newName">The new rule name.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        Task RenameRssAutoDownloadingRuleAsync(
            string name,
            string newName,
            CancellationToken token = default);

        /// <summary>
        /// Deletes the RSS auto-downloading rule.
        /// </summary>
        /// <param name="name">The rule name.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        Task DeleteRssAutoDownloadingRuleAsync(
            string name,
            CancellationToken token = default);

        /// <summary>
        /// Gets the RSS auto-downloading rules.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.0")]
        Task<IReadOnlyDictionary<string, RssAutoDownloadingRule>> GetRssAutoDownloadingRulesAsync(
            CancellationToken token = default);
    }
}
