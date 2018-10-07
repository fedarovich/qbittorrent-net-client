using Newtonsoft.Json;

namespace QBittorrent.Client
{
    /// <summary>
    /// Describes torrent category and the corresponding save path.
    /// </summary>
    public class Category
    {
        /// <summary>
        /// Category name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Category save path.
        /// </summary>
        [JsonProperty("savePath")]
        public string SavePath { get; set; }
    }
}
