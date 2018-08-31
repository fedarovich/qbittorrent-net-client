using JetBrains.Annotations;
using Newtonsoft.Json;
using QBittorrent.Client.Converters;
using QBittorrent.Client.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using QBittorrent.Client.Internal;
using static QBittorrent.Client.Utils;

namespace QBittorrent.Client
{
    /// <summary>
    /// Provides access to qBittorrent remote API.
    /// </summary>
    /// <seealso cref="IDisposable" />
    /// <seealso cref="IQBittorrentClient"/>
    /// <seealso cref="QBittorrentClientExtensions"/>
    public class QBittorrentClient : IQBittorrentClient, IDisposable
    {
        private const int NewApiLegacyVersion = 18;

        private readonly Uri _uri;
        private readonly HttpClient _client;

        private IUrlProvider _urlProvider;
        private int _legacyVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="QBittorrentClient"/> class.
        /// </summary>
        /// <param name="uri">qBittorrent remote server URI.</param>
        public QBittorrentClient([NotNull] Uri uri)
            : this(uri, new HttpClient())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QBittorrentClient"/> class.
        /// </summary>
        /// <param name="uri">qBittorrent remote server URI.</param>
        /// <param name="handler">Custom HTTP message handler.</param>
        /// <param name="disposeHandler">The value indicating whether the <paramref name="handler"/> must be disposed when disposing this object.</param>
        public QBittorrentClient([NotNull] Uri uri, HttpMessageHandler handler, bool disposeHandler)
            : this(uri, new HttpClient(handler, disposeHandler))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QBittorrentClient"/> class.
        /// </summary>
        /// <param name="uri">The qBittorrent remote server URI.</param>
        /// <param name="client">Custom <see cref="HttpClient"/> instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="uri"/> or <paramref name="client"/> is <see langword="null"/>.
        /// </exception>
        private QBittorrentClient([NotNull] Uri uri, [NotNull] HttpClient client)
        {
            _uri = uri ?? throw new ArgumentNullException(nameof(uri));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _client.DefaultRequestHeaders.ExpectContinue = true;
        }

        #region Properties

        /// <summary>
        /// Gets or sets the timespan to wait before the request times out.
        /// </summary>
        public TimeSpan Timeout
        {
            get => _client.Timeout;
            set => _client.Timeout = value;
        }

        /// <summary>
        /// Gets the headers which should be sent with each request.
        /// </summary>
        public HttpRequestHeaders DefaultRequestHeaders => _client.DefaultRequestHeaders;

        #endregion  

        #region Authentication

        /// <summary>
        /// Authenticates this client with the remote qBittorrent server.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task LoginAsync(
                    string username,
                    string password,
                    CancellationToken token = default)
        {
            var uri = await BuildUriAsync(p => p.Login(), token).ConfigureAwait(false);
            var response = await _client.PostAsync(uri,
                BuildForm(
                    ("username", username),
                    ("password", password)
                ),
                token)
                .ConfigureAwait(false);
            using (response)
            {
                response.EnsureSuccessStatusCodeEx();
            }
        }

        /// <summary>
        /// Clears authentication on the remote qBittorrent server.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task LogoutAsync(
                    CancellationToken token = default)
        {
            var uri = await BuildUriAsync(p => p.Logout(), token).ConfigureAwait(false);
            var response = await _client.PostAsync(uri, BuildForm(), token).ConfigureAwait(false);
            using (response)
            {
                response.EnsureSuccessStatusCodeEx();
            }
        }

        #endregion

        #region Get

        /// <summary>
        /// Gets the current API version of the server.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <remarks>
        /// <para>
        /// For qBittorrent versions before 4.1.0 this method returns version <c>1.x</c>
        /// where <c>x</c> is the value returned by <see cref="GetLegacyApiVersionAsync"/> method.
        /// </para>
        /// <para>
        /// For qBittorrent version starting from 4.1.0 this method returns version <c>x.y</c> or <c>x.y.z</c>
        /// where <c>x >= 2</c>. 
        /// </para>
        /// </remarks>
        /// <returns></returns>
        public async Task<Version> GetApiVersionAsync(CancellationToken token = default)
        {
            var legacyVersion = await GetLegacyApiVersionAsync(token).ConfigureAwait(false);
            if (legacyVersion < NewApiLegacyVersion)
                return new Version(1, legacyVersion);

            var uri = BuildUri("/api/v2/app/webapiVersion");
            var version = await _client.GetStringAsync(uri, token).ConfigureAwait(false);
            return new Version(version);
        }

        /// <summary>
        /// Gets the current API version of the server for qBittorrent versions up to 4.0.4.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task<int> GetLegacyApiVersionAsync(CancellationToken token = default)
        {
            int version = Interlocked.CompareExchange(ref _legacyVersion, 0, 0);
            if (version > 0)
                return version;

            var uri = BuildUri("/version/api");
            version = Convert.ToInt32(await _client.GetStringAsync(uri, token).ConfigureAwait(false));
            return version;
        }

        /// <summary>
        /// Get the minimum API version supported by server. Any application designed to work with an API version greater than or equal to the minimum API version is guaranteed to work.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task<int> GetLegacyMinApiVersionAsync(CancellationToken token = default)
        {
            var uri = BuildUri("/version/api_min");
            var version = await _client.GetStringAsync(uri, token).ConfigureAwait(false);
            return Convert.ToInt32(version);
        }

        /// <summary>
        /// Gets the qBittorrent version.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task<Version> GetQBittorrentVersionAsync(CancellationToken token = default)
        {
            var uri = await BuildUriAsync(p => p.QBittorrentVersion(), token).ConfigureAwait(false);
            var version = await _client.GetStringAsync(uri, token).ConfigureAwait(false);
            var match = Regex.Match(version, @"\d+\.\d+(?:\.\d+(?:\.\d+)?)?");
            return match.Success ? new Version(match.Value) : null;
        }

        /// <summary>
        /// Gets the torrent list.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task<IReadOnlyList<TorrentInfo>> GetTorrentListAsync(
            TorrentListQuery query = null,
            CancellationToken token = default)
        {
            query = query ?? new TorrentListQuery();
            var uri = await BuildUriAsync(p => p.GetTorrentList(
                    query.Filter,
                    query.Category,
                    query.SortBy,
                    query.ReverseSort,
                    query.Limit,
                    query.Offset), token)
                .ConfigureAwait(false);

            var json = await _client.GetStringAsync(uri, token).ConfigureAwait(false);
            var result = JsonConvert.DeserializeObject<TorrentInfo[]>(json);
            return result;
        }

        /// <summary>
        /// Gets the torrent generic properties.
        /// </summary>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task<TorrentProperties> GetTorrentPropertiesAsync(
            string hash,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return ExecuteAsync();

            async Task<TorrentProperties> ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.GetTorrentProperties(hash), token).ConfigureAwait(false);
                var json = await _client.GetStringAsync(uri, true, token).ConfigureAwait(false);
                var result = JsonConvert.DeserializeObject<TorrentProperties>(json);
                return result;
            }
        }

        /// <summary>
        /// Gets the torrent contents.
        /// </summary>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task<IReadOnlyList<TorrentContent>> GetTorrentContentsAsync(
            string hash,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return ExecuteAsync();

            async Task<IReadOnlyList<TorrentContent>> ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.GetTorrentContents(hash), token).ConfigureAwait(false);
                var json = await _client.GetStringAsync(uri, true, token).ConfigureAwait(false);
                var result = JsonConvert.DeserializeObject<TorrentContent[]>(json);
                return result;
            }
        }

        /// <summary>
        /// Gets the torrent trackers.
        /// </summary>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task<IReadOnlyList<TorrentTracker>> GetTorrentTrackersAsync(
            string hash,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return ExecuteAsync();

            async Task<IReadOnlyList<TorrentTracker>> ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.GetTorrentTrackers(hash), token);
                var json = await _client.GetStringAsync(uri, true, token).ConfigureAwait(false);
                var result = JsonConvert.DeserializeObject<TorrentTracker[]>(json);
                return result;
            }
        }

        /// <summary>
        /// Gets the torrent web seeds.
        /// </summary>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task<IReadOnlyList<Uri>> GetTorrentWebSeedsAsync(
            string hash,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return ExecuteAsync();

            async Task<IReadOnlyList<Uri>> ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.GetTorrentWebSeeds(hash), token).ConfigureAwait(false);
                var json = await _client.GetStringAsync(uri, true, token).ConfigureAwait(false);
                var result = JsonConvert.DeserializeObject<UrlItem[]>(json);
                return result?.Select(x => x.Url).ToArray();
            }
        }

        /// <summary>
        /// Gets the states of the torrent pieces.
        /// </summary>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task<IReadOnlyList<TorrentPieceState>> GetTorrentPiecesStatesAsync(
            string hash,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return ExecuteAsync();

            async Task<IReadOnlyList<TorrentPieceState>> ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.GetTorrentPiecesStates(hash), token).ConfigureAwait(false);
                var json = await _client.GetStringAsync(uri, true, token).ConfigureAwait(false);
                var result = JsonConvert.DeserializeObject<TorrentPieceState[]>(json);
                return result;
            }
        }

        /// <summary>
        /// Gets the hashes of the torrent pieces.
        /// </summary>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task<IReadOnlyList<string>> GetTorrentPiecesHashesAsync(
            string hash,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return ExecuteAsync();

            async Task<IReadOnlyList<string>> ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.GetTorrentPiecesHashes(hash), token).ConfigureAwait(false);
                var json = await _client.GetStringAsync(uri, true, token).ConfigureAwait(false);
                var result = JsonConvert.DeserializeObject<string[]>(json);
                return result;
            }
        }

        /// <summary>
        /// Gets the global transfer information.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task<GlobalTransferInfo> GetGlobalTransferInfoAsync(
            CancellationToken token = default)
        {
            var uri = await BuildUriAsync(p => p.GetGlobalTransferInfo(), token).ConfigureAwait(false);
            var json = await _client.GetStringAsync(uri, token).ConfigureAwait(false);
            var result = JsonConvert.DeserializeObject<GlobalTransferInfo>(json);
            return result;
        }

        /// <summary>
        /// Gets the partial data.
        /// </summary>
        /// <param name="responseId">The response identifier.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task<PartialData> GetPartialDataAsync(
            int responseId = 0,
            CancellationToken token = default)
        {
            var uri = await BuildUriAsync(p => p.GetPartialData(responseId), token).ConfigureAwait(false);
            var json = await _client.GetStringAsync(uri, token).ConfigureAwait(false);
            var result = JsonConvert.DeserializeObject<PartialData>(json);
            return result;
        }

        /// <summary>
        /// Gets the peer partial data.
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="responseId">The response identifier.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task<PeerPartialData> GetPeerPartialDataAsync(
            string hash, 
            int responseId = 0,
            CancellationToken token = default )
        {
            ValidateHash(hash);
            return ExecuteAsync();
            
            async Task<PeerPartialData> ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.GetPeerPartialData(hash, responseId), token).ConfigureAwait(false);
                var json = await _client.GetStringAsync(uri, true, token).ConfigureAwait(false);
                var result = JsonConvert.DeserializeObject<PeerPartialData>(json);
                return result;
            }
        }

        /// <summary>
        /// Get the path to the folder where the downloaded files are saved by default.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task<string> GetDefaultSavePathAsync(
            CancellationToken token = default)
        {
            var uri = await BuildUriAsync(p => p.GetDefaultSavePath(), token).ConfigureAwait(false);
            return await _client.GetStringAsync(uri, token).ConfigureAwait(false);
        }

        #endregion

        #region Add

        /// <summary>
        /// Adds the torrent files to download.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task AddTorrentsAsync(
            AddTorrentFilesRequest request,
            CancellationToken token = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.AddTorrentFiles(), token).ConfigureAwait(false);
                var data = new MultipartFormDataContent();
                foreach (var file in request.TorrentFiles)
                {
                    data.AddFile("torrents", file, "application/x-bittorrent");
                }

                await AddTorrentsCoreAsync(uri, data, request, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Adds the torrent URLs or magnet-links to download.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task AddTorrentsAsync(
            AddTorrentUrlsRequest request,
            CancellationToken token = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.AddTorrentUrls(), token).ConfigureAwait(false);
                var urls = string.Join("\n", request.TorrentUrls.Select(url => url.AbsoluteUri));
                var data = new MultipartFormDataContent().AddValue("urls", urls);
                await AddTorrentsCoreAsync(uri, data, request, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Adds the torrents.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="data">The data.</param>
        /// <param name="request">The request.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        protected async Task AddTorrentsCoreAsync(
            Uri uri,
            MultipartFormDataContent data,
            AddTorrentRequest request,
            CancellationToken token)
        {
            data
                .AddNonEmptyString("savepath", request.DownloadFolder)
                .AddNonEmptyString("cookie", request.Cookie)
                .AddNonEmptyString("category", request.Category)
                .AddValue("skip_checking", request.SkipHashChecking)
                .AddValue("paused", request.Paused)
                .AddNotNullValue("root_folder", request.CreateRootFolder)
                .AddNonEmptyString("rename", request.Rename)
                .AddNotNullValue("upLimit", request.UploadLimit)
                .AddNotNullValue("dlLimit", request.DownloadLimit)
                .AddValue("sequentialDownload", request.SequentialDownload)
                .AddValue("firstLastPiecePrio", request.FirstLastPiecePrioritized);

            using (var response = await _client.PostAsync(uri, data, token).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCodeEx();
            }
        }

        #endregion

        #region Pause/Resume

        /// <summary>
        /// Pauses the torrent.
        /// </summary>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task PauseAsync(
            string hash,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.Pause(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(uri, BuildForm(("hash", hash)), token).ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        /// <summary>
        /// Pauses all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task PauseAllAsync(
            CancellationToken token = default)
        {
            var uri = await BuildUriAsync(p => p.PauseAll(), token).ConfigureAwait(false);
            var response = await GetLegacyApiVersionAsync(token) < NewApiLegacyVersion
                ? await _client.PostAsync(uri, BuildForm(), token).ConfigureAwait(false)
                : await _client.PostAsync(uri, BuildForm(("hash", "all")), token).ConfigureAwait(false);
            using (response)
            {
                response.EnsureSuccessStatusCodeEx();
            }
        }

        /// <summary>
        /// Resumes the torrent.
        /// </summary>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task ResumeAsync(
            string hash,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.Resume(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(uri, BuildForm(("hash", hash)), token).ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        /// <summary>
        /// Resumes all torrents.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task ResumeAllAsync(
            CancellationToken token = default)
        {
            var uri = await BuildUriAsync(p => p.ResumeAll(), token).ConfigureAwait(false);
            var response = await GetLegacyApiVersionAsync(token) < NewApiLegacyVersion
                ? await _client.PostAsync(uri, BuildForm(), token).ConfigureAwait(false)
                : await _client.PostAsync(uri, BuildForm(("hash", "all")), token).ConfigureAwait(false);
            using (response)
            {
                response.EnsureSuccessStatusCodeEx();
            }
        }

        #endregion

        #region Categories

        /// <summary>
        /// Adds the category.
        /// </summary>
        /// <param name="category">The category name.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task AddCategoryAsync(
            string category,
            CancellationToken token = default)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("The category cannot be empty.", nameof(category));

            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.AddCategory(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(uri, BuildForm(("category", category)), token).ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        /// <summary>
        /// Deletes the categories.
        /// </summary>
        /// <param name="categories">The list of categories' names.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task DeleteCategoriesAsync(
            IEnumerable<string> categories,
            CancellationToken token = default)
        {
            var names = GetNames();
            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.DeleteCategories(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(
                        uri,
                        BuildForm(("categories", names)),
                        token)
                    .ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }

            string GetNames()
            {
                if (categories == null)
                    throw new ArgumentNullException(nameof(categories));

                var builder = new StringBuilder(4096);
                foreach (var category in categories)
                {
                    if (string.IsNullOrWhiteSpace(category))
                        throw new ArgumentException("The collection must not contain nulls or empty strings.", nameof(categories));

                    if (builder.Length > 0)
                    {
                        builder.Append('\n');
                    }

                    builder.Append(category);
                }

                if (builder.Length == 0)
                    throw new ArgumentException("The collection must contain at least one category.", nameof(categories));

                return builder.ToString();
            }
        }

        /// <summary>
        /// Sets the torrent category.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="category">The category.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task SetTorrentCategoryAsync(
            IEnumerable<string> hashes,
            string category,
            CancellationToken token = default)
        {
            if (hashes == null)
                throw new ArgumentNullException(nameof(hashes));
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.SetCategory(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(uri,
                    BuildForm(
                        ("hashes", string.Join("|", hashes)),
                        ("category", category)
                    ), token).ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        #endregion

        #region Limits

        /// <summary>
        /// Gets the torrent download speed limit.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task<IReadOnlyDictionary<string, long?>> GetTorrentDownloadLimitAsync(
            IEnumerable<string> hashes,
            CancellationToken token = default)
        {
            var hashesString = JoinHashes(hashes);
            return ExecuteAsync();

            async Task<IReadOnlyDictionary<string, long?>> ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.GetTorrentDownloadLimit(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(
                        uri,
                        BuildForm(("hashes", hashesString)),
                        token)
                    .ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();

                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, long?>>(json, new NegativeToNullConverter());
                    return dict;
                }
            }
        }

        /// <summary>
        /// Sets the torrent download speed limit.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task SetTorrentDownloadLimitAsync(
            IEnumerable<string> hashes,
            long limit,
            CancellationToken token = default)
        {
            var hashesString = JoinHashes(hashes);
            if (limit < 0)
                throw new ArgumentOutOfRangeException(nameof(limit));

            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.SetTorrentDownloadLimit(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(
                        uri,
                        BuildForm(
                            ("hashes", hashesString),
                            ("limit", limit.ToString())),
                        token)
                    .ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        /// <summary>
        /// Gets the torrent upload speed limit.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task<IReadOnlyDictionary<string, long?>> GetTorrentUploadLimitAsync(
            IEnumerable<string> hashes,
            CancellationToken token = default)
        {
            var hashesString = JoinHashes(hashes);
            return ExecuteAsync();

            async Task<IReadOnlyDictionary<string, long?>> ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.GetTorrentUploadLimit(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(
                        uri,
                        BuildForm(("hashes", hashesString)),
                        token)
                    .ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();

                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, long?>>(json, new NegativeToNullConverter());
                    return dict;
                }
            }
        }

        /// <summary>
        /// Sets the torrent upload speed limit.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="limit">The limit.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task SetTorrentUploadLimitAsync(
            IEnumerable<string> hashes,
            long limit,
            CancellationToken token = default)
        {
            var hashesString = JoinHashes(hashes);
            if (limit < 0)
                throw new ArgumentOutOfRangeException(nameof(limit));

            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.SetTorrentUploadLimit(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(
                        uri,
                        BuildForm(
                            ("hashes", hashesString),
                            ("limit", limit.ToString())),
                        token)
                    .ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        /// <summary>
        /// Gets the global download speed limit.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task<long?> GetGlobalDownloadLimitAsync(
            CancellationToken token = default)
        {
            var uri = await BuildUriAsync(p => p.GetGlobalDownloadLimit(), token).ConfigureAwait(false);
            using (var response = await _client.PostAsync(uri, null, token).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCodeEx();
                var strValue = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return long.TryParse(strValue, out long value) ? value : 0;
            }
        }

        /// <summary>
        /// Sets the global download speed limit.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task SetGlobalDownloadLimitAsync(
            long limit,
            CancellationToken token = default)
        {
            if (limit < 0)
                throw new ArgumentOutOfRangeException(nameof(limit));

            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.SetGlobalDownloadLimit(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(uri, BuildForm(("limit", limit.ToString())), token).ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        /// <summary>
        /// Gets the global upload speed limit.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task<long?> GetGlobalUploadLimitAsync(
            CancellationToken token = default)
        {
            var uri = await BuildUriAsync(p => p.GetGlobalUploadLimit(), token).ConfigureAwait(false);
            using (var response = await _client.PostAsync(uri, null, token).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCodeEx();
                var strValue = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return long.TryParse(strValue, out long value) ? value : 0;
            }
        }

        /// <summary>
        /// Sets the global upload speed limit.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task SetGlobalUploadLimitAsync(
            long limit,
            CancellationToken token = default)
        {
            if (limit < 0)
                throw new ArgumentOutOfRangeException(nameof(limit));

            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.SetGlobalUploadLimit(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(uri, BuildForm(("limit", limit.ToString())), token).ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        #endregion

        #region Priority

        /// <summary>
        /// Changes the torrent priority.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="change">The priority change.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task ChangeTorrentPriorityAsync(
            IEnumerable<string> hashes,
            TorrentPriorityChange change,
            CancellationToken token = default)
        {
            var hashesString = JoinHashes(hashes);
            if (!Enum.IsDefined(typeof(TorrentPriorityChange), change))
                throw new ArgumentOutOfRangeException(nameof(change), change, null);

            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(GetUrl, token).ConfigureAwait(false);
                var response = await _client.PostAsync(
                        uri,
                        BuildForm(("hashes", hashesString)),
                        token)
                    .ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }

            Uri GetUrl(IUrlProvider provider)
            {
                switch (change)
                {
                    case TorrentPriorityChange.Minimal:
                        return provider.MinTorrentPriority();
                    case TorrentPriorityChange.Increase:
                        return provider.IncTorrentPriority();
                    case TorrentPriorityChange.Decrease:
                        return provider.DecTorrentPriority();
                    case TorrentPriorityChange.Maximal:
                        return provider.MaxTorrentPriority();
                    default:
                        throw new ArgumentOutOfRangeException(nameof(change), change, null);
                }
            }
        }

        /// <summary>
        /// Sets the file priority.
        /// </summary>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="fileId">The file identifier.</param>
        /// <param name="priority">The priority.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task SetFilePriorityAsync(
            string hash,
            int fileId,
            TorrentContentPriority priority,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            if (fileId < 0)
                throw new ArgumentOutOfRangeException(nameof(fileId));
            if (!Enum.GetValues(typeof(TorrentContentPriority)).Cast<TorrentContentPriority>().Contains(priority))
                throw new ArgumentOutOfRangeException(nameof(priority));

            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.SetFilePriority(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(
                        uri,
                        BuildForm(
                            ("hash", hash),
                            ("id", fileId.ToString()),
                            ("priority", priority.ToString("D"))),
                        token)
                    .ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        #endregion

        #region Other

        /// <summary>
        /// Deletes the torrents.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="deleteDownloadedData"><see langword="true"/> to delete the downloaded data.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task DeleteAsync(
            IEnumerable<string> hashes,
            bool deleteDownloadedData = false,
            CancellationToken token = default)
        {
            var hashesString = JoinHashes(hashes);
            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.DeleteTorrent(deleteDownloadedData), token).ConfigureAwait(false);
                HttpResponseMessage response;
                if (await GetLegacyApiVersionAsync(token) < NewApiLegacyVersion)
                {
                    response = await _client.PostAsync(
                            uri,
                            BuildForm(("hashes", hashesString)),
                            token)
                        .ConfigureAwait(false);
                }
                else
                {
                    response = await _client.PostAsync(
                            uri,
                            BuildForm(
                                ("hashes", hashesString),
                                ("deleteFiles", deleteDownloadedData.ToLowerString())),
                            token)
                        .ConfigureAwait(false);
                }

                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        /// <summary>
        /// Sets the location of the torrents.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="newLocation">The new location.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task SetLocationAsync(
            IEnumerable<string> hashes,
            string newLocation,
            CancellationToken token = default)
        {
            var hashesString = JoinHashes(hashes);
            if (newLocation == null)
                throw new ArgumentNullException(nameof(newLocation));
            if (string.IsNullOrEmpty(newLocation))
                throw new ArgumentException("The location cannot be an empty string.", nameof(newLocation));

            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.SetLocation(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(uri,
                    BuildForm(
                        ("hashes", hashesString),
                        ("location", newLocation)
                    ), token).ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        /// <summary>
        /// Renames the torrent.
        /// </summary>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="newName">The new name.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task RenameAsync(
            string hash,
            string newName,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            if (newName == null)
                throw new ArgumentNullException(nameof(newName));
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("The name cannot be an empty string.", nameof(newName));

            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.Rename(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(uri,
                    BuildForm(
                        ("hash", hash),
                        ("name", newName)
                    ), token).ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        /// <summary>
        /// Adds the trackers to the torrent.
        /// </summary>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="trackers">The trackers.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task AddTrackersAsync(
            string hash,
            IEnumerable<Uri> trackers,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            var urls = GetUrls();

            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.AddTrackers(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(uri,
                    BuildForm(
                        ("hash", hash),
                        ("urls", urls)
                    ), token).ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }

            string GetUrls()
            {
                if (trackers == null)
                    throw new ArgumentNullException(nameof(trackers));

                var builder = new StringBuilder(4096);
                foreach (var tracker in trackers)
                {
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (tracker == null)
                        throw new ArgumentException("The collection must not contain nulls.", nameof(trackers));
                    if (!tracker.IsAbsoluteUri)
                        throw new ArgumentException("The collection must contain absolute URIs.", nameof(trackers));

                    if (builder.Length > 0)
                    {
                        builder.Append('\n');
                    }

                    builder.Append(tracker.AbsoluteUri);
                }

                if (builder.Length == 0)
                    throw new ArgumentException("The collection must contain at least one URI.", nameof(trackers));

                return builder.ToString();
            }
        }

        /// <summary>
        /// Rechecks the torrent.
        /// </summary>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task RecheckAsync(
            string hash,
            CancellationToken token = default)
        {
            ValidateHash(hash);
            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.Recheck(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(uri, BuildForm(("hash", hash)), token).ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        /// <summary>
        /// Gets the server log.
        /// </summary>
        /// <param name="severity">The severity of log entries to return. <see cref="TorrentLogSeverity.All"/> by default.</param>
        /// <param name="afterId">Return the entries with the ID greater than the specified one.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task<IEnumerable<TorrentLogEntry>> GetLogAsync(
            TorrentLogSeverity severity = TorrentLogSeverity.All,
            int afterId = -1,
            CancellationToken token = default)
        {
            var uri = await BuildUriAsync(p => p.GetLog(severity, afterId), token).ConfigureAwait(false);
            var json = await _client.GetStringAsync(uri, token).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<IEnumerable<TorrentLogEntry>>(json);
        }

        /// <summary>
        /// Gets the value indicating whether the alternative speed limits are enabled.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task<bool> GetAlternativeSpeedLimitsEnabledAsync(
            CancellationToken token = default)
        {
            var uri = await BuildUriAsync(p => p.GetAlternativeSpeedLimitsEnabled(), token).ConfigureAwait(false);
            var result = await _client.GetStringAsync(uri, token).ConfigureAwait(false);
            return result == "1";
        }

        /// <summary>
        /// Toggles the alternative speed limits.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task ToggleAlternativeSpeedLimitsAsync(
            CancellationToken token = default)
        {
            var uri = await BuildUriAsync(p => p.ToggleAlternativeSpeedLimits(), token).ConfigureAwait(false);
            using (var result = await _client.PostAsync(uri, BuildForm(), token).ConfigureAwait(false))
            {
                result.EnsureSuccessStatusCodeEx();
            }
        }

        /// <summary>
        /// Sets the automatic torrent management.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="enabled"></param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task SetAutomaticTorrentManagementAsync(
            IEnumerable<string> hashes,
            bool enabled,
            CancellationToken token = default)
        {
            var hashesString = JoinHashes(hashes);
            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.SetAutomaticTorrentManagement(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(uri,
                    BuildForm(
                        ("hashes", hashesString),
                        ("enable", enabled.ToLowerString())
                    ), token).ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        /// <summary>
        /// Sets the force start.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="enabled"></param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task SetForceStartAsync(
            IEnumerable<string> hashes,
            bool enabled,
            CancellationToken token = default)
        {
            var hashesString = JoinHashes(hashes);
            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.SetForceStart(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(uri,
                    BuildForm(
                        ("hashes", hashesString),
                        ("value", enabled.ToLowerString())
                    ), token).ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        /// <summary>
        /// Sets the super seeding asynchronous.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="enabled"></param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task SetSuperSeedingAsync(
            IEnumerable<string> hashes,
            bool enabled,
            CancellationToken token = default)
        {
            var hashesString = JoinHashes(hashes);
            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.SetSuperSeeding(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(uri,
                    BuildForm(
                        ("hashes", hashesString),
                        ("value", enabled.ToLowerString())
                    ), token).ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }


        /// <summary>
        /// Toggles the first and last piece priority.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task ToggleFirstLastPiecePrioritizedAsync(
            IEnumerable<string> hashes,
            CancellationToken token = default)
        {
            var hashesString = JoinHashes(hashes);
            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.ToggleFirstLastPiecePrioritized(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(uri,
                    BuildForm(("hashes", hashesString)), token).ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        /// <summary>
        /// Toggles the sequential download.
        /// </summary>
        /// <param name="hashes">The torrent hashes.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task ToggleSequentialDownloadAsync(
            IEnumerable<string> hashes,
            CancellationToken token = default)
        {
            var hashesString = JoinHashes(hashes);
            return ExecuteAsync();

            async Task ExecuteAsync()
            {
                var uri = await BuildUriAsync(p => p.ToggleSequentialDownload(), token).ConfigureAwait(false);
                var response = await _client.PostAsync(uri,
                    BuildForm(("hashes", hashesString)), token).ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        /// <summary>
        /// Gets qBittorrent preferences.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public async Task<Preferences> GetPreferencesAsync(
            CancellationToken token = default)
        {
            var uri = await BuildUriAsync(p => p.GetPreferences(), token).ConfigureAwait(false);
            var response = await _client.GetStringAsync(uri, token).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Preferences>(response);
        }

        /// <summary>
        /// Gets qBittorrent preferences.
        /// </summary>
        /// <param name="preferences">
        /// The prefences to set.
        /// You can set only the properties you want to change and leave the other ones as <see langword="null"/>.
        /// </param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        public Task SetPreferencesAsync(
            Preferences preferences,
            CancellationToken token = default)
        {
            if (preferences == null)
                throw new ArgumentNullException(nameof(preferences));
            return Execute();

            async Task Execute()
            {
                var uri = await BuildUriAsync(p => p.SetPreferences(), token).ConfigureAwait(false);
                var json = JsonConvert.SerializeObject(preferences);
                var response = await _client.PostAsync(uri, BuildForm(("json", json)), token).ConfigureAwait(false);
                using (response)
                {
                    response.EnsureSuccessStatusCodeEx();
                }
            }
        }

        #endregion

        /// <inheritdoc />
        public void Dispose()
        {
            _client?.Dispose();
        }

        private HttpContent BuildForm(params (string key, string value)[] fields)
        {
            return new CompatibleFormUrlEncodedContent(fields);
        }

        private Uri BuildUri(string path, params (string key, string value)[] parameters)
        {
            var builder = new UriBuilder(_uri)
            {
                Path = path,
                Query = string.Join("&", parameters
                    .Where(t => t.value != null)
                    .Select(t => $"{Uri.EscapeDataString(t.key)}={Uri.EscapeDataString(t.value)}"))
            };
            return builder.Uri;
        }

        private async Task<Uri> BuildUriAsync(Func<IUrlProvider, Uri> builder, CancellationToken token = default)
        {
            var provider = Interlocked.CompareExchange(ref _urlProvider, null, null);
            if (provider == null)
            {
                var version = await GetLegacyApiVersionAsync(token).ConfigureAwait(false);
                var newProvider = version < NewApiLegacyVersion 
                    ? (IUrlProvider)new Api1UrlProvider(_uri) 
                    : (IUrlProvider)new Api2UrlProvider(_uri);
                provider = Interlocked.CompareExchange(ref _urlProvider, newProvider, null) ?? newProvider;
            }

            return builder(provider);
        }

        private struct UrlItem
        {
            [JsonProperty("url")]
            public Uri Url { get; set; }
        }
    }
}
