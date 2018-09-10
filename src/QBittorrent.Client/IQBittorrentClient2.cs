using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        // TODO: What should we use: int or long?
        Task<IEnumerable<PeerLogEntry>> GetPeerLogAsync(
            int afterId = -1,
            CancellationToken token = default);

        /// <summary>
        /// Deletes all torrents.
        /// </summary>
        /// <param name="deleteDownloadedData"><see langword="true"/> to delete the downloaded data.</param>
        /// <param name="token">The cancellation token.</param>
        [ApiLevel(ApiLevel.V2)]
        Task DeleteAllAsync(
            bool deleteDownloadedData = false,
            CancellationToken token = default);
    }
}
