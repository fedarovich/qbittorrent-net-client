using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QBittorrent.Client.Converters;

namespace QBittorrent.Client
{
    /// <summary>
    /// qBittorrent application preferences.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Preferences
    {
        private const string WebUiPasswordPropertyName = "web_ui_password";

        /// <summary>
        /// Currently selected language
        /// </summary>
        [JsonProperty("locale")]
        public string Locale { get; set; }

        /// <summary>
        /// Default save path for torrents, separated by slashes
        /// </summary>
        [JsonProperty("save_path")]
        public string SavePath { get; set; }

        /// <summary>
        /// True if folder for incomplete torrents is enabled
        /// </summary>
        [JsonProperty("temp_path_enabled")]
        public bool? TempPathEnabled { get; set; }

        /// <summary>
        /// Path for incomplete torrents, separated by slashes
        /// </summary>
        [JsonProperty("temp_path")]
        public string TempPath { get; set; }

        /// <summary>
        /// List of watch folders to add torrent automatically.
        /// </summary>
        [JsonProperty("scan_dirs")]
        public IDictionary<string, SaveLocation> ScanDirectories { get; set; }

        /// <summary>
        /// Path to directory to copy .torrent files.
        /// </summary>
        [JsonProperty("export_dir")]
        public string ExportDirectory { get; set; }

        /// <summary>
        /// Path to directory to copy finished .torrent files
        /// </summary>
        [JsonProperty("export_dir_fin")]
        public string ExportDirectoryForFinished { get; set; }

        /// <summary>
        /// True if e-mail notification should be enabled.
        /// </summary>
        [JsonProperty("mail_notification_enabled")]
        public bool? MailNotificationEnabled { get; set; }

        /// <summary>
        /// E-mail address to send notifications to.
        /// </summary>
        [JsonProperty("mail_notification_email")]
        public string MailNotificationEmailAddress { get; set; }

        /// <summary>
        /// SMTP server for e-mail notifications.
        /// </summary>
        [JsonProperty("mail_notification_smtp")]
        public string MailNotificationSmtpServer { get; set; }

        /// <summary>
        /// True if SMTP server requires SSL connection.
        /// </summary>
        [JsonProperty("mail_notification_ssl_enabled")]
        public bool? MailNotificationSslEnabled { get; set; }

        /// <summary>
        /// True if SMTP server requires authentication
        /// </summary>
        [JsonProperty("mail_notification_auth_enabled")]
        public bool? MailNotificationAuthenticationEnabled { get; set; }

        /// <summary>
        /// Username for SMTP authentication.
        /// </summary>
        [JsonProperty("mail_notification_username")]
        public string MailNotificationUsername { get; set; }

        /// <summary>
        /// Password for SMTP authentication.
        /// </summary>
        [JsonProperty("mail_notification_password")]
        public string MailNotificationPassword { get; set; }

        /// <summary>
        /// True if external program should be run after torrent has finished downloading.
        /// </summary>
        [JsonProperty("autorun_enabled")]
        public bool? AutorunEnabled { get; set; }

        /// <summary>
        /// Program path/name/arguments to run if <see cref="AutorunEnabled"/> is <see langword="true"/>.
        /// </summary>
        [JsonProperty("autorun_program")]
        public string AutorunProgram { get; set; }

        /// <summary>
        /// True if file preallocation should take place, otherwise sparse files are used.
        /// </summary>
        [JsonProperty("preallocate_all")]
        public bool? PreallocateAll { get; set; }

        /// <summary>
        /// True if torrent queuing is enabled
        /// </summary>
        [JsonProperty("queueing_enabled")]
        public bool? QueueingEnabled { get; set; }

        /// <summary>
        /// Maximum number of active simultaneous downloads
        /// </summary>
        [JsonProperty("max_active_downloads")]
        public int? MaxActiveDownloads { get; set; }

        /// <summary>
        /// Maximum number of active simultaneous downloads and uploads
        /// </summary>
        [JsonProperty("max_active_torrents")]
        public int? MaxActiveTorrents { get; set; }

        /// <summary>
        /// Maximum number of active simultaneous uploads
        /// </summary>
        [JsonProperty("max_active_uploads")]
        public int? MaxActiveUploads { get; set; }

        /// <summary>
        /// If true torrents w/o any activity (stalled ones) will not be counted towards max_active_* limits.
        /// </summary>
        [JsonProperty("dont_count_slow_torrents")]
        public bool? DoNotCountSlowTorrents { get; set; }

        /// <summary>
        /// True if share ratio limit is enabled
        /// </summary>
        [JsonProperty("max_ratio_enabled")]
        public bool? MaxRatioEnabled { get; set; }

        /// <summary>
        /// Get the global share ratio limit
        /// </summary>
        [JsonProperty("max_ratio")]
        public double? MaxRatio { get; set; }

        /// <summary>
        /// Action performed when a torrent reaches the maximum share ratio.
        /// </summary>
        [JsonProperty("max_ratio_act")]
        public MaxRatioAction? MaxRatioAction { get; set; }

        /// <summary>
        /// Maximal seeding time in minutes.
        /// </summary>
        [JsonProperty("max_seeding_time")]
        public int? MaxSeedingTime { get; set; }

        /// <summary>
        /// True if maximal seeding time is enabled.
        /// </summary>
        [JsonProperty("max_seeding_time_enabled")]
        public bool? MaxSeedingTimeEnabled { get; set; }

        /// <summary>
        /// If true <c>.!qB</c> extension will be appended to incomplete files.
        /// </summary>
        [JsonProperty("incomplete_files_ext")]
        public bool? AppendExtensionToIncompleteFiles { get; set; }

        /// <summary>
        /// Port for incoming connections.
        /// </summary>
        [JsonProperty("listen_port")]
        public int? ListenPort { get; set; }

        /// <summary>
        /// True if UPnP/NAT-PMP is enabled.
        /// </summary>
        [JsonProperty("upnp")]
        public bool? UpnpEnabled { get; set; }

        /// <summary>
        /// True if the port is randomly selected
        /// </summary>
        [JsonProperty("random_port")]
        public bool? RandomPort { get; set; }

        /// <summary>
        /// Global download speed limit in KiB/s; -1 means no limit is applied.
        /// </summary>
        [JsonProperty("dl_limit")]
        public int? DownloadLimit { get; set; }

        /// <summary>
        /// Global upload speed limit in KiB/s; -1 means no limit is applied.
        /// </summary>
        [JsonProperty("up_limit")]
        public int? UploadLimit { get; set; }

        /// <summary>
        /// Maximum global number of simultaneous connections.
        /// </summary>
        [JsonProperty("max_connec")]
        public int? MaxConnections { get; set; }

        /// <summary>
        /// Maximum number of simultaneous connections per torrent.
        /// </summary>
        [JsonProperty("max_connec_per_torrent")]
        public int? MaxConnectionsPerTorrent { get; set; }

        /// <summary>
        /// Maximum number of upload slots.
        /// </summary>
        [JsonProperty("max_uploads")]
        public int? MaxUploads { get; set; }

        /// <summary>
        /// Maximum number of upload slots per torrent
        /// </summary>
        [JsonProperty("max_uploads_per_torrent")]
        public int? MaxUploadsPerTorrent { get; set; }

        /// <summary>
        /// True if uTP protocol should be enabled.
        /// </summary>
        [JsonProperty("enable_utp")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [Obsolete("Use BittorrentProtocol property for qBittorrent 4.x")]
        public bool? EnableUTP { get; set; }

        /// <summary>
        /// True if <see cref="DownloadLimit"/> and <see cref="UploadLimit"/> should be applied to uTP connections.
        /// </summary>
        [JsonProperty("limit_utp_rate")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public bool? LimitUTPRate { get; set; }

        /// <summary>
        /// True if <see cref="DownloadLimit"/> and <see cref="UploadLimit"/>
        /// should be applied to estimated TCP overhead (service data: e.g. packet headers).
        /// </summary>
        [JsonProperty("limit_tcp_overhead")]
        public bool? LimitTcpOverhead { get; set; }

        /// <summary>
        /// Alternative global download speed limit in KiB/s
        /// </summary>
        [JsonProperty("alt_dl_limit")]
        public int? AlternativeDownloadLimit { get; set; }

        /// <summary>
        /// Alternative global upload speed limit in KiB/s
        /// </summary>
        [JsonProperty("alt_up_limit")]
        public int? AlternativeUploadLimit { get; set; }

        /// <summary>
        /// True if alternative limits should be applied according to schedule
        /// </summary>
        [JsonProperty("scheduler_enabled")]
        public bool? SchedulerEnabled { get; set; }

        /// <summary>
        /// Scheduler starting hour.
        /// </summary>
        [JsonProperty("schedule_from_hour")]
        public int? ScheduleFromHour { get; set; }

        /// <summary>
        /// Scheduler starting minute.
        /// </summary>
        [JsonProperty("schedule_from_min")]
        public int? ScheduleFromMinute { get; set; }

        /// <summary>
        /// Scheduler ending hour.
        /// </summary>
        [JsonProperty("schedule_to_hour")]
        public int? ScheduleToHour { get; set; }

        /// <summary>
        /// Scheduler ending minute.
        /// </summary>
        [JsonProperty("schedule_to_min")]
        public int? ScheduleToMinute { get; set; }

        /// <summary>
        /// Scheduler days.
        /// </summary>
        [JsonProperty("scheduler_days")]
        public SchedulerDay? SchedulerDays { get; set; }

        /// <summary>
        /// True if DHT is enabled
        /// </summary>
        [JsonProperty("dht")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public bool? DHT { get; set; }

        /// <summary>
        /// True if DHT port should match TCP port
        /// </summary>
        [JsonProperty("dhtSameAsBT")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public bool? DHTSameAsBT { get; set; }

        /// <summary>
        /// DHT port if <see cref="DHTSameAsBT"/> is <see langword="false"/>.
        /// </summary>
        [JsonProperty("dht_port")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public int? DHTPort { get; set; }

        /// <summary>
        /// True if peer exchange PeX is enabled.
        /// </summary>
        [JsonProperty("pex")]
        public bool? PeerExchange { get; set; }

        /// <summary>
        /// True if local peer discovery is enabled
        /// </summary>
        [JsonProperty("lsd")]
        public bool? LocalPeerDiscovery { get; set; }

        /// <summary>
        /// Encryption mode.
        /// </summary>
        [JsonProperty("encryption")]
        public Encryption? Encryption { get; set; }

        /// <summary>
        /// If true anonymous mode will be enabled.
        /// </summary>
        [JsonProperty("anonymous_mode")]
        public bool? AnonymousMode { get; set; }

        /// <summary>
        /// Proxy type.
        /// </summary>
        [JsonProperty("proxy_type")]
        public ProxyType? ProxyType { get; set; }

        /// <summary>
        /// Proxy IP address or domain name.
        /// </summary>
        [JsonProperty("proxy_ip")]
        public string ProxyAddress { get; set; }

        /// <summary>
        /// Proxy port.
        /// </summary>
        [JsonProperty("proxy_port")]
        public int? ProxyPort { get; set; }

        /// <summary>
        /// True if peer and web seed connections should be proxified.
        /// </summary>
        [JsonProperty("proxy_peer_connections")]
        public bool? ProxyPeerConnections { get; set; }

        /// <summary>
        /// True if the connections not supported by the proxy are disabled.
        /// </summary>
        [JsonProperty("force_proxy")]
        [Deprecated("2.3")]
        public bool? ForceProxy { get; set; }

        /// <summary>
        /// True if proxy should be used only for torrents.
        /// </summary>
        [JsonProperty("proxy_torrents_only")]
        [ApiLevel(ApiLevel.V2)]
        public bool ProxyTorrentsOnly { get; set; }

        /// <summary>
        /// True if proxy requires authentication; doesn't apply to SOCKS4 proxies.
        /// </summary>
        [JsonProperty("proxy_auth_enabled")]
        [Obsolete("Use ProxyType instead.")]
        public bool? ProxyAuthenticationEnabled { get; set; }

        /// <summary>
        /// Username for proxy authentication.
        /// </summary>
        [JsonProperty("proxy_username")]
        public string ProxyUsername { get; set; }

        /// <summary>
        /// Password for proxy authentication.
        /// </summary>
        [JsonProperty("proxy_password")]
        public string ProxyPassword { get; set; }

        /// <summary>
        /// True if external IP filter should be enabled.
        /// </summary>
        [JsonProperty("ip_filter_enabled")]
        public bool? IpFilterEnabled { get; set; }

        /// <summary>
        /// Path to IP filter file (.dat, .p2p, .p2b files are supported).
        /// </summary>
        [JsonProperty("ip_filter_path")]
        public string IpFilterPath { get; set; }

        /// <summary>
        /// True if IP filters are applied to trackers
        /// </summary>
        [JsonProperty("ip_filter_trackers")]
        public bool? IpFilterTrackers { get; set; }

        /// <summary>
        /// WebUI IP address. Use <c>*</c> to accept connections on any IP address.
        /// </summary>
        [JsonProperty("web_ui_address")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public string WebUIAddress { get; set; }

        /// <summary>
        /// WebUI port.
        /// </summary>
        [JsonProperty("web_ui_port")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public int? WebUIPort { get; set; }

        /// <summary>
        /// WebUI domain. Use <c>*</c> to accept connections on any domain.
        /// </summary>
        [JsonProperty("web_ui_domain_list")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public string WebUIDomain { get; set; }

        /// <summary>
        /// True if UPnP is used for the WebUI port.
        /// </summary>
        [JsonProperty("web_ui_upnp")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public bool? WebUIUpnp { get; set; }

        /// <summary>
        /// WebUI username
        /// </summary>
        [JsonProperty("web_ui_username")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public string WebUIUsername { get; set; }

        /// <summary>
        /// WebUI password. 
        /// </summary>
        /// <remarks>
        /// This property should be used for setting password.
        /// If a <see cref="Preferences"/> object is retrieved as server response, this property will be <see langword="null"/>.
        /// </remarks>
        [JsonIgnore]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public string WebUIPassword { get; set; }

        /// <summary>
        /// MD5 hash of WebUI password. 
        /// </summary>
        /// <remarks>
        /// This property can be used to get the MD5 hash of the current WebUI password.
        /// It is ignored when sending requests to the server.
        /// </remarks>
        [JsonIgnore]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public string WebUIPasswordHash { get; set; }

        /// <summary>
        /// True if WebUI HTTPS access is enabled.
        /// </summary>
        [JsonProperty("use_https")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public bool? WebUIHttps { get; set; }

        /// <summary>
        /// SSL keyfile contents (this is a not a path).
        /// </summary>
        [JsonProperty("ssl_key")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [Deprecated("2.3", Description = "Use WebUISslKeyPath on API 2.3 or later.")]
        public string WebUISslKey { get; set; }

        /// <summary>
        /// SSL certificate contents (this is a not a path).
        /// </summary>
        [JsonProperty("ssl_cert")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [Deprecated("2.3", Description = "Use WebUISslCertificatePath on API 2.3 or later.")]
        public string WebUISslCertificate { get; set; }

        /// <summary>
        /// SSL key file path on the server.
        /// </summary>
        [JsonProperty("web_ui_https_key_path")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.3")]
        public string WebUISslKeyPath { get; set; }

        /// <summary>
        /// SSL certificate file path on the server.
        /// </summary>
        [JsonProperty("web_ui_https_cert_path")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.3")]
        public string WebUISslCertificatePath { get; set; }

        /// <summary>
        /// True if WebUI clickjacking protection is enabled
        /// </summary>
        [JsonProperty("web_ui_clickjacking_protection_enabled")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.0.2")]
        public bool? WebUIClickjackingProtection { get; set; }

        /// <summary>
        /// True if WebUI CSRF protection is enabled
        /// </summary>
        [JsonProperty("web_ui_csrf_protection_enabled")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.0.2")]
        public bool? WebUICsrfProtection { get; set; }

        /// <summary>
        /// True if WebUI host header validation is enabled
        /// </summary>
        [JsonProperty("web_ui_host_header_validation_enabled")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2")]
        public bool? WebUIHostHeaderValidation { get; set; }

        /// <summary>
        /// True if authentication challenge for loopback address (127.0.0.1) should be disabled.
        /// </summary>
        [JsonProperty("bypass_local_auth")]
        public bool? BypassLocalAuthentication { get; set; }

        /// <summary>
        /// True if webui authentication should be bypassed for clients whose ip resides within (at least) one of the subnets on the whitelist.
        /// </summary>
        /// <seealso cref="BypassAuthenticationSubnetWhitelist"/>
        /// <seealso cref="BypassLocalAuthentication"/>
        [JsonProperty("bypass_auth_subnet_whitelist_enabled")]
        public bool? BypassAuthenticationSubnetWhitelistEnabled { get; set; }

        /// <summary>
        /// (White)list of ipv4/ipv6 subnets for which webui authentication should be bypassed.
        /// </summary>
        /// <seealso cref="BypassAuthenticationSubnetWhitelistEnabled"/>
        /// <seealso cref="BypassLocalAuthentication"/>
        [JsonProperty("bypass_auth_subnet_whitelist")]
        [JsonConverter(typeof(SeparatedStringToListConverter), ",", "\n")]
        public IList<string> BypassAuthenticationSubnetWhitelist { get; set; }

        /// <summary>
        /// True if server DNS should be updated dynamically.
        /// </summary>
        [JsonProperty("dyndns_enabled")]
        public bool? DynamicDnsEnabled { get; set; }

        /// <summary>
        /// Dynamic DNS service.
        /// </summary>
        [JsonProperty("dyndns_service")]
        public DynamicDnsService? DynamicDnsService { get; set; }

        /// <summary>
        /// Username for DDNS service.
        /// </summary>
        [JsonProperty("dyndns_username")]
        public string DynamicDnsUsername { get; set; }

        /// <summary>
        /// Password for DDNS service.
        /// </summary>
        [JsonProperty("dyndns_password")]
        public string DynamicDnsPassword { get; set; }

        /// <summary>
        /// Your DDNS domain name.
        /// </summary>
        [JsonProperty("dyndns_domain")]
        public string DynamicDnsDomain { get; set; }

        /// <summary>
        /// RSS refresh interval.
        /// </summary>
        [JsonProperty("rss_refresh_interval")]
        public uint? RssRefreshInterval { get; set; }

        /// <summary>
        /// Max stored articles per RSS feed.
        /// </summary>
        [JsonProperty("rss_max_articles_per_feed")]
        public int? RssMaxArticlesPerFeed { get; set; }

        /// <summary>
        /// Enable processing of RSS feeds.
        /// </summary>
        [JsonProperty("rss_processing_enabled")]
        public bool? RssProcessingEnabled { get; set; }

        /// <summary>
        /// Enable auto-downloading of torrents from the RSS feeds.
        /// </summary>
        [JsonProperty("rss_auto_downloading_enabled")]
        public bool? RssAutoDownloadingEnabled { get; set; }

        /// <summary>
        /// True if additional trackers are enabled.
        /// </summary>
        [JsonProperty("add_trackers_enabled")]
        public bool? AdditionalTrackersEnabled { get; set; }

        /// <summary>
        /// The list of addional trackers.
        /// </summary>
        [JsonProperty("add_trackers")]
        [JsonConverter(typeof(SeparatedStringToListConverter), "\n")]
        public IList<string> AdditinalTrackers { get; set; }

        /// <summary>
        /// The list of banned IP addresses.
        /// </summary>
        [JsonProperty("banned_IPs")]
        [JsonConverter(typeof(SeparatedStringToListConverter), "\n")]
        public IList<string> BannedIpAddresses { get; set; }

        /// <summary>
        /// Bittorrent protocol.
        /// </summary>
        [JsonProperty("bittorrent_protocol")]
        public BittorrentProtocol? BittorrentProtocol { get; set; }

        /* API 2.2.0 */

        /// <summary>
        /// True if a subfolder should be created when adding a torrent
        /// </summary>
        [JsonProperty("create_subfolder_enabled")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        public bool? CreateTorrentSubfolder { get; set; }

        /// <summary>
        /// True if torrents should be added in a Paused state
        /// </summary>
        [JsonProperty("start_paused_enabled")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        public bool? AddTorrentPaused { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("auto_delete_mode")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        public TorrentFileAutoDeleteMode? TorrentFileAutoDeleteMode { get; set; }

        /// <summary>
        /// True if Automatic Torrent Management is enabled by default
        /// </summary>
        [JsonProperty("auto_tmm_enabled")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public bool? AutoTMMEnabledByDefault { get; set; }

        /// <summary>
        /// True if torrent should be relocated when its category changes
        /// </summary>
        [JsonProperty("torrent_changed_tmm_enabled")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public bool? AutoTMMRetainedWhenCategoryChanges { get; set; }

        /// <summary>
        /// True if torrent should be relocated when the default save path changes
        /// </summary>
        [JsonProperty("save_path_changed_tmm_enabled")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public bool? AutoTMMRetainedWhenDefaultSavePathChanges { get; set; }


        /// <summary>
        /// True if torrent should be relocated when its category's save path changes
        /// </summary>
        [JsonProperty("category_changed_tmm_enabled")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public bool? AutoTMMRetainedWhenCategorySavePathChanges { get; set; }

        /// <summary>
        /// E-mail where notifications should originate from
        /// </summary>
        [JsonProperty("mail_notification_sender")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        public string MailNotificationSender { get; set; }

        /// <summary>
        /// True if <see cref="DownloadLimit" /> and <seealso cref="UploadLimit"/> should be applied to peers on the LAN
        /// </summary>
        [JsonProperty("limit_lan_peers")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public bool? LimitLAN { get; set; }

        /// <summary>
        /// Download rate in KiB/s for a torrent to be considered "slow"
        /// </summary>
        [JsonProperty("slow_torrent_dl_rate_threshold")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        public int? SlowTorrentDownloadRateThreshold { get; set; }

        /// <summary>
        /// Upload rate in KiB/s for a torrent to be considered "slow"
        /// </summary>
        [JsonProperty("slow_torrent_ul_rate_threshold")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        public int? SlowTorrentUploadRateThreshold { get; set; }

        /// <summary>
        /// Time in seconds a torrent should be inactive before considered "slow"
        /// </summary>
        [JsonProperty("slow_torrent_inactive_timer")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        public int? SlowTorrentInactiveTime { get; set; }

        /// <summary>
        /// True if an alternative WebUI should be used
        /// </summary>
        [JsonProperty("alternative_webui_enabled")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        public bool? AlternativeWebUIEnabled { get; set; }

        /// <summary>
        /// File path to the alternative WebUI
        /// </summary>
        [JsonProperty("alternative_webui_path")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [ApiLevel(ApiLevel.V2, MinVersion = "2.2.0")]
        public string AlternativeWebUIPath { get; set; }

        /* Other */

        /// <summary>
        /// Additional properties not handled by this library.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalData { get; set; }

        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            if (WebUIPassword != null)
            {
                AdditionalData = AdditionalData ?? new Dictionary<string, JToken>();
                AdditionalData[WebUiPasswordPropertyName] = WebUIPassword;
            }
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (AdditionalData != null 
                && AdditionalData.TryGetValue(WebUiPasswordPropertyName, out var hashToken)
                && hashToken.Type == JTokenType.String)
            {
                WebUIPasswordHash = hashToken.Value<string>();
                AdditionalData.Remove(WebUiPasswordPropertyName);
            }
        }
    }
}
