using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace QBittorrent.Client
{
    public class RssAutoDownloadingRule
    {
        /// <summary>
        /// Gets or sets the value indicating whether the rule is enabled.
        /// </summary>
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// The substring that the torrent name must contain.
        /// </summary>
        /// <seealso cref="MustNotContain"/>
        /// <seealso cref="UseRegex"/>
        [JsonProperty("mustContain")]
        public string MustContain { get; set; }

        /// <summary>
        /// The substring that the torrent name must not contain.
        /// </summary>
        /// <seealso cref="MustContain"/>
        /// <seealso cref="UseRegex"/>
        [JsonProperty("mustNotContain")]
        public string MustNotContain { get; set; }

        /// <summary>
        /// Set to <see langword="true" /> in order to use regular expressions in 
        /// for <see cref="MustContain"/> and <see cref="MustNotContain"/> properties.
        /// </summary>
        [JsonProperty("useRegex")]
        public bool UseRegex { get; set; }

        /// <summary>
        /// Episode filter definition.
        /// </summary>
        [JsonProperty("episodeFilter")]
        public string EpisodeFilter { get; set; }

        /// <summary>
        /// Enable smart episode filter.
        /// </summary>
        [JsonProperty("smartFilter")]
        public bool SmartFilter { get; set; }

        /// <summary>
        /// The list of episode IDs already matched by smart filter.
        /// </summary>
        [JsonProperty("previouslyMatchedEpisodes")]
        public IReadOnlyList<string> PreviouslyMatchedEpisodes { get; set; }

        /// <summary>
        /// The feed URLs the rule applied to.
        /// </summary>
        [JsonProperty("affectedFeeds")]
        public IReadOnlyList<Uri> AffectedFeeds { get; set; }

        /// <summary>
        /// Ignore subsequent rule matches.
        /// </summary>
        [JsonProperty("ignoreDays")]
        public int IgnoreDays { get; set; }

        /// <summary>
        /// The rule last match time.
        /// </summary>
        [JsonProperty("lastMatch")]
        // dd MMM yyyy HH:mm:ss + en-US culture
        public DateTime LastMatch { get; set; }

        /// <summary>
        /// Add matched torrent in paused mode.
        /// </summary>
        [JsonProperty("addPaused")]
        public bool AddPaused { get; set; }

        /// <summary>
        /// Category to assign to the torrent.
        /// </summary>
        [JsonProperty("assignedCategory")]
        public string AssignedCategory { get; set; }

        /// <summary>
        /// The directory to save the torrent to.
        /// </summary>
        [JsonProperty("savePath")]
        public string SavePath { get; set; }
    }
}
