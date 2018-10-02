using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace QBittorrent.Client
{
    public class RssFeed : RssItem
    {
        [JsonProperty("uid")]
        public Guid Uid { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("lastBuildDate")]
        // What is the format?
        public string LastBuildDate { get; set; }

        [JsonProperty("isLoading")]
        public bool? IsLoading { get; set; }

        [JsonProperty("hasError")]
        public bool? HasError { get; set; }

        [JsonProperty("articles")]
        public IList<RssArticle> Articles { get; set; }
    }
}
