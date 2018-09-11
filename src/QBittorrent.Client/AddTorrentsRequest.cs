using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QBittorrent.Client
{
    [ApiLevel(ApiLevel.V2)]
    public class AddTorrentsRequest : AddTorrentRequestBase
    {
        public AddTorrentsRequest()
            : this(Enumerable.Empty<string>(), Enumerable.Empty<Uri>())
        {
        }

        public AddTorrentsRequest(IEnumerable<string> torrentFiles)
            : this(torrentFiles, Enumerable.Empty<Uri>())
        {
        }

        public AddTorrentsRequest(IEnumerable<Uri> torrentUrls)
            : this(Enumerable.Empty<string>(), torrentUrls)
        {
        }

        public AddTorrentsRequest(IEnumerable<string> torrentFiles, IEnumerable<Uri> torrentUrls)
        {
            TorrentFiles = new List<string>(torrentFiles);
            TorrentUrls = new List<Uri>(torrentUrls);
        }

        public ICollection<Uri> TorrentUrls { get; }

        public ICollection<string> TorrentFiles { get; }
    }
}
