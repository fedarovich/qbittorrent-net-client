using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
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
    }
}
