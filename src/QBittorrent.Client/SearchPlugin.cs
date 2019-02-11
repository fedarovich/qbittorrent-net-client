using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace QBittorrent.Client
{
    /// <summary>
    /// Represents information about a search plugin.
    /// </summary>
    public class SearchPlugin
    {
        /// <summary>
        /// Name that can be used to perform search using all plugins.
        /// </summary>
        public const string All = "all";

        /// <summary>
        /// Name that can be used to perform search using all enabled plugins.
        /// </summary>
        public const string Enabled = "enabled";

        /// <summary>
        /// Whether the plugin is enabled.
        /// </summary>
        [JsonProperty("enabled")]
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Full name of the plugin
        /// </summary>
        [JsonProperty("fullName")]
        public string FullName { get; set; }

        /// <summary>
        /// Short name of the plugin
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// List of supported categories
        /// </summary>
        [JsonProperty("supportedCategories")]
        public IReadOnlyList<string> SupportedCategories { get; set; }

        /// <summary>
        /// URL of the torrent site
        /// </summary>
        [JsonProperty("url")]
        public Uri Url { get; set; }

        /// <summary>
        /// Installed version of the plugin
        /// </summary>
        [JsonProperty("version")]
        public Version Version { get; set; }
    }
}
