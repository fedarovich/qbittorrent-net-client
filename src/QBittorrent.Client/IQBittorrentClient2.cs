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
        [ApiLevel(ApiLevel.V2)]
        Task ReannounceAsync(
            CancellationToken token = default);

        /// <summary>
        /// Reannounces the torrents.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        Task ReannounceAsync(
            [NotNull, ItemNotNull] IEnumerable<string> hashes,
            CancellationToken token = default);

        /// <summary>
        /// Pauses all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        /// <remarks>This method supersedes <see cref="IQBittorrentClient.PauseAllAsync"/>.</remarks>
        [ApiLevel(ApiLevel.V2)]
        Task PauseAsync(
            CancellationToken token = default);

        /// <summary>
        /// Resumes all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        /// <remarks>This method supersedes <see cref="IQBittorrentClient.ResumeAllAsync"/>.</remarks>
        [ApiLevel(ApiLevel.V2)]
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
        /// Gets the torrent download speed limit for all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        Task<IReadOnlyDictionary<string, long?>> GetTorrentDownloadLimitAsync(
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
        /// Gets the torrent upload speed limit for all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2)]
        Task<IReadOnlyDictionary<string, long?>> GetTorrentUploadLimitAsync(
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
    }
}
