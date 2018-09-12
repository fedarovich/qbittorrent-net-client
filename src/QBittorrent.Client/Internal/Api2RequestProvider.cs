using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using QBittorrent.Client.Extensions;

namespace QBittorrent.Client.Internal
{
    internal sealed class Api2RequestProvider : BaseRequestProvider
    {
        internal Api2RequestProvider(Uri baseUri)
        {
            Url = new Api2UrlProvider(baseUri);
        }

        public override IUrlProvider Url { get; }

        public override ApiLevel ApiLevel => ApiLevel.V2;

        public override (Uri url, HttpContent request) Pause(IEnumerable<string> hashes)
        {
            return BuildForm(Url.Pause(),
                ("hashes", JoinHashes(hashes)));
        }

        public override (Uri url, HttpContent request) PauseAll()
        {
            return BuildForm(Url.PauseAll(),
                ("hashes", "all"));
        }

        public override (Uri url, HttpContent request) Resume(IEnumerable<string> hashes)
        {
            return BuildForm(Url.Resume(),
                ("hashes", JoinHashes(hashes)));
        }

        public override (Uri url, HttpContent request) ResumeAll()
        {
            return BuildForm(Url.ResumeAll(),
                ("hashes", "all"));
        }

        public override (Uri url, HttpContent request) DeleteTorrents(IEnumerable<string> hashes, bool withFiles)
        {
            return BuildForm(Url.DeleteTorrents(withFiles),
                ("hashes", JoinHashes(hashes)),
                ("deleteFiles", withFiles.ToLowerString()));
        }

        public override (Uri url, HttpContent request) Recheck(IEnumerable<string> hashes)
        {
            return BuildForm(Url.Recheck(),
                ("hashes", JoinHashes(hashes)));
        }

        public override (Uri url, HttpContent request) Reannounce(IEnumerable<string> hashes)
        {
            return BuildForm(Url.Reannounce(),
                ("hashes", JoinHashes(hashes)));
        }

        public override (Uri url, HttpContent request) AddTorrents(AddTorrentsRequest request)
        {
            var data = AddTorrentsCore(request);

            foreach (var file in request.TorrentFiles)
            {
                data.AddFile("torrents", file, "application/x-bittorrent");
            }

            var urls = string.Join("\n", request.TorrentUrls.Select(url => url.AbsoluteUri));
            data.AddValue("urls", urls);

            return (Url.AddTorrentFiles(), data);
        }
    }
}
