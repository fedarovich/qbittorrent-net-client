using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QBittorrent.Client.Tests
{
    public class Env
    {
        [JsonProperty("api-version")]
        [JsonConverter(typeof(VersionConverter))]
        public Version ApiVersion { get; set; }
        
        [JsonProperty("legacy-api-version")]
        public int LegacyApiVersion { get; set; }
        
        [JsonProperty("legacy-min-api-version")]
        public int LegacyMinApiVersion { get; set; }
        
        [JsonProperty("qb-version")]
        [JsonConverter(typeof(VersionConverter))]
        public Version QBittorrentVersion { get; set; }
    }
}