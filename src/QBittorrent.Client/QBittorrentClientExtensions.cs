using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using static QBittorrent.Client.Internal.Utils;

namespace QBittorrent.Client
{
    /// <summary>
    /// Provides extension methods for <see cref="IQBittorrentClient"/>.
    /// </summary>
    public static class QBittorrentClientExtensions
    {
        /// <summary>
        /// Deletes the category.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient"/> instance.</param>
        /// <param name="category">The category name.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public static Task DeleteCategoryAsync(
            [NotNull] this IQBittorrentClient client,
            [NotNull] string category,
            CancellationToken token = default)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("The category cannot be empty.", nameof(category));

            return client.DeleteCategoriesAsync(new[] { category }, token);
        }

        /// <summary>
        /// Sets the torrent category.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient"/> instance.</param>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="category">The category.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public static Task SetTorrentCategoryAsync(
            [NotNull] this IQBittorrentClient client,
            [NotNull] string hash,
            [NotNull] string category,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return client.SetTorrentCategoryAsync(new[] { hash }, category, token);
        }

        /// <summary>
        /// Gets the torrent download speed limit.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient"/> instance.</param>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public static Task<long?> GetTorrentDownloadLimitAsync(
            [NotNull] this IQBittorrentClient client,
            [NotNull] string hash,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return ExecuteAsync();

            async Task<long?> ExecuteAsync()
            {
                var dict = await client.GetTorrentDownloadLimitAsync(new[] { hash }, token).ConfigureAwait(false);
                return dict?.Values?.SingleOrDefault();
            }
        }

        /// <summary>
        /// Sets the torrent download speed limit.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient"/> instance.</param>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public static Task SetTorrentDownloadLimitAsync(
            [NotNull] this IQBittorrentClient client,
            [NotNull] string hash,
            long limit,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return client.SetTorrentDownloadLimitAsync(new[] { hash }, limit, token);
        }

        /// <summary>
        /// Gets the torrent upload speed limit.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient"/> instance.</param>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public static Task<long?> GetTorrentUploadLimitAsync(
            [NotNull] this IQBittorrentClient client,
            [NotNull] string hash,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return ExecuteAsync();

            async Task<long?> ExecuteAsync()
            {
                var dict = await client.GetTorrentUploadLimitAsync(new[] { hash }, token).ConfigureAwait(false);
                return dict?.Values?.SingleOrDefault();
            }
        }

        /// <summary>
        /// Sets the torrent upload speed limit.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient"/> instance.</param>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public static Task SetTorrentUploadLimitAsync(
            [NotNull] this IQBittorrentClient client,
            [NotNull] string hash,
            long limit,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return client.SetTorrentUploadLimitAsync(new[] { hash }, limit, token);
        }

        /// <summary>
        /// Changes the torrent priority.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient"/> instance.</param>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="change">The priority change.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public static Task ChangeTorrentPriorityAsync(
            [NotNull] this IQBittorrentClient client,
            [NotNull] string hash,
            TorrentPriorityChange change,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return client.ChangeTorrentPriorityAsync(new[] { hash }, change, token);
        }

        /// <summary>
        /// Deletes the torrent.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient"/> instance.</param>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="deleteDownloadedData"><see langword="true"/> to delete the downloaded data.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public static Task DeleteAsync(
            [NotNull] this IQBittorrentClient client,
            [NotNull] string hash,
            bool deleteDownloadedData = false,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return client.DeleteAsync(new[] { hash }, deleteDownloadedData, token);
        }

        /// <summary>
        /// Sets the location of torrent.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient"/> instance.</param>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="newLocation">The new location.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public static Task SetLocationAsync(
            [NotNull] this IQBittorrentClient client,
            [NotNull] string hash,
            [NotNull] string newLocation,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return client.SetLocationAsync(new[] { hash }, newLocation, token);
        }

        /// <summary>
        /// Adds the tracker to the torrent.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient"/> instance.</param>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="tracker">The tracker.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public static Task AddTrackerAsync(
            [NotNull] this IQBittorrentClient client,
            [NotNull] string hash,
            [NotNull] Uri tracker,
            CancellationToken token = default)
        {
            if (tracker == null)
                throw new ArgumentNullException(nameof(tracker));
            if (!tracker.IsAbsoluteUri)
                throw new ArgumentException("The URI must be absolute.", nameof(tracker));

            return client.AddTrackersAsync(hash, new[] { tracker }, token);
        }

        /// <summary>
        /// Sets the automatic torrent management.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient"/> instance.</param>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="enabled"></param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public static Task SetAutomaticTorrentManagementAsync(
            [NotNull] this IQBittorrentClient client,
            [NotNull] string hash,
            bool enabled,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return client.SetAutomaticTorrentManagementAsync(new[] { hash }, enabled, token);
        }

        /// <summary>
        /// Sets the force start.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient"/> instance.</param>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="enabled"></param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public static Task SetForceStartAsync(
            [NotNull] this IQBittorrentClient client,
            [NotNull] string hash,
            bool enabled,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return client.SetForceStartAsync(new[] { hash }, enabled, token);
        }

        /// <summary>
        /// Sets the super seeding.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient"/> instance.</param>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="enabled"></param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public static Task SetSuperSeedingAsync(
            [NotNull] this IQBittorrentClient client,
            [NotNull] string hash,
            bool enabled,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return client.SetSuperSeedingAsync(new[] { hash }, enabled, token);
        }

        /// <summary>
        /// Toggles the first and last piece priority.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient"/> instance.</param>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public static Task ToggleFirstLastPiecePrioritizedAsync(
            [NotNull] this IQBittorrentClient client,
            [NotNull] string hash,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return client.ToggleFirstLastPiecePrioritizedAsync(new[] { hash }, token);
        }

        /// <summary>
        /// Toggles the sequential download.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient"/> instance.</param>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public static Task ToggleSequentialDownloadAsync(
            [NotNull] this IQBittorrentClient client,
            [NotNull] string hash,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return client.ToggleSequentialDownloadAsync(new[] { hash }, token);
        }

        /// <summary>
        /// Reannounces the torrent.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient2"/> instance.</param>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.0.2")]
        public static Task ReannounceAsync(
            [NotNull] this IQBittorrentClient2 client,
            [NotNull] string hash,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return client.ReannounceAsync(new[] { hash }, token);
        }

        /// <summary>
        /// Removes the trackers from the torrent.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient2"/> instance.</param>
        /// <param name="hash">The hash of the torrent.</param>
        /// <param name="trackerUrl">The tracker URL you want to remove.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        public static Task DeleteTrackerAsync(
            [NotNull] this IQBittorrentClient2 client,
            [NotNull] string hash,
            [NotNull] Uri trackerUrl,
            CancellationToken token = default)
        {
            return client.DeleteTrackersAsync(hash, new[] {trackerUrl}, token);
        }

        /// <summary>
        /// Starts torrent search job.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient2"/> instance.</param>
        /// <param name="pattern">Pattern to search for (e.g. "Ubuntu 18.04").</param>
        /// <param name="plugin">Plugin to use for searching (e.g. "legittorrents").</param>
        /// <param name="category">
        /// Categories to limit your search to (e.g. "legittorrents").
        /// Available categories depend on the specified <paramref name="plugin"/>.
        /// Also supports <c>&quot;all&quot;</c></param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The ID of the search job.</returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.1")]
        public static Task<int> StartSearchAsync(
            [NotNull] this IQBittorrentClient2 client,
            [NotNull] string pattern,
            [NotNull] string plugin,
            [NotNull] string category = "all",
            CancellationToken token = default)
        {
            if (plugin == null)
                throw new ArgumentNullException(nameof(plugin));

            return client.StartSearchAsync(pattern, new [] {plugin}, category, token);
        }

        /// <summary>
        /// Starts torrent search job using all or enabled plugins.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient2"/> instance.</param>
        /// <param name="pattern">Pattern to search for (e.g. "Ubuntu 18.04").</param>
        /// <param name="disabledPluginsToo">
        /// <see langword="false" /> to search using all enabled plugins;
        /// <see langword="true" /> to search using all (enabled and disabled) plugins.
        /// </param>
        /// <param name="category">
        /// Categories to limit your search to (e.g. "legittorrents").
        /// Also supports <c>&quot;all&quot;</c></param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The ID of the search job.</returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.1")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> StartSearchUsingEnabledPluginsAsync(
            [NotNull] this IQBittorrentClient2 client,
            [NotNull] string pattern,
            bool disabledPluginsToo = false,
            [NotNull] string category = "all",
            CancellationToken token = default)
        {
            return client.StartSearchAsync(pattern, 
                disabledPluginsToo ? SearchPlugin.All : SearchPlugin.Enabled, 
                category, 
                token);
        }

        /// <summary>
        /// Installs the search plugins.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient2"/> instance.</param>
        /// <param name="source">URL of the plugins to install.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        /// <remarks>Plugin can be installed from the local file system using <c>file:///</c> URI as <paramref name="source"/>.</remarks>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.1")]
        public static Task InstallSearchPluginAsync(
            [NotNull] this IQBittorrentClient2 client,
            [NotNull] Uri source,
            CancellationToken token = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return client.InstallSearchPluginsAsync(new[] {source}, token);
        }

        /// <summary>
        /// Uninstalls the search plugins.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient2"/> instance.</param>
        /// <param name="name">Name of the plugin to uninstall.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.1")]
        public static Task UninstallSearchPluginAsync(
            [NotNull] this IQBittorrentClient2 client,
            [NotNull] string name,
            CancellationToken token = default)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return client.UninstallSearchPluginsAsync(new[] {name}, token);
        }

        /// <summary>
        /// Enables the search plugin.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient2"/> instance.</param>
        /// <param name="name">Name of the plugin to enable.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.1")]
        public static Task EnableSearchPluginAsync(
            [NotNull] this IQBittorrentClient2 client,
            [NotNull] string name,
            CancellationToken token = default)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return client.EnableSearchPluginsAsync(new[] { name }, token);
        }

        /// <summary>
        /// Disables the search plugin.
        /// </summary>
        /// <param name="client">An <see cref="IQBittorrentClient2"/> instance.</param>
        /// <param name="name">Name of the plugin to disable.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [ApiLevel(ApiLevel.V2, MinVersion = "2.1.1")]
        public static Task DisableSearchPluginAsync(
            [NotNull] this IQBittorrentClient2 client,
            [NotNull] string name,
            CancellationToken token = default)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return client.DisableSearchPluginsAsync(new[] { name }, token);
        }
    }
}
