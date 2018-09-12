using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace QBittorrent.Client.Internal
{
    internal sealed class Api1RequestProvider : BaseRequestProvider
    {
        internal Api1RequestProvider(Uri baseUri)
        {
            Url = new Api1UrlProvider(baseUri);
        }

        public override IUrlProvider Url { get; }

        public override (Uri url, HttpContent request) Pause(IEnumerable<string> hashes)
        {
            var hashList = hashes.ToList();
            if (hashList.Count == 0)
                throw new InvalidOperationException("Exactly one hash must be provided.");

            if (hashList.Count > 1)
                throw new ApiNotSupportedException("API 1.x does not support pausing several torrents at once.", ApiLevel.V2);

            return BuildForm(Url.Pause(),
                ("hash", hashList[0]));
        }

        public override (Uri url, HttpContent request) PauseAll()
        {
            return BuildForm(Url.PauseAll());
        }

        public override (Uri url, HttpContent request) Resume(IEnumerable<string> hashes)
        {
            var hashList = hashes.ToList();
            if (hashList.Count == 0)
                throw new InvalidOperationException("Exactly one hash must be provided.");

            if (hashList.Count > 1)
                throw new ApiNotSupportedException("API 1.x does not support resuming several torrents at once.", ApiLevel.V2);

            return BuildForm(Url.Resume(),
                ("hash", hashList[0]));
        }

        public override (Uri url, HttpContent request) ResumeAll()
        {
            return BuildForm(Url.ResumeAll());
        }

        public override (Uri url, HttpContent request) DeleteTorrents(IEnumerable<string> hashes, bool withFiles)
        {
            return BuildForm(Url.DeleteTorrents(withFiles),
                ("hashes", JoinHashes(hashes)));
        }

        public override (Uri url, HttpContent request) Recheck(IEnumerable<string> hashes)
        {
            var hashList = hashes.ToList();
            if (hashList.Count == 0)
                throw new InvalidOperationException("Exactly one hash must be provided.");

            if (hashList.Count > 1)
                throw new ApiNotSupportedException("API 1.x does not support rechecking several torrents at once.", ApiLevel.V2);

            return BuildForm(Url.Recheck(),
                ("hash", hashList[0]));
        }

        public override (Uri url, HttpContent request) Reannounce(IEnumerable<string> hashes) => throw new ApiNotSupportedException(ApiLevel.V2);

        public override (Uri url, HttpContent request) AddTorrents(AddTorrentsRequest request) => throw new ApiNotSupportedException(ApiLevel.V2);
    }
}
