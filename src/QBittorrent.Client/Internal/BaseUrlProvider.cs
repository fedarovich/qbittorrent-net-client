using System;
using System.Linq;

namespace QBittorrent.Client.Internal
{
    internal abstract class BaseUrlProvider
    {
        private readonly Uri _baseUri;

        private protected BaseUrlProvider(Uri baseUri)
        {
            _baseUri = baseUri;
        }

        private protected Uri Create(string relativeUri) => new Uri(
            _baseUri,
            _baseUri.AbsolutePath.TrimEnd('/') + relativeUri);

        private protected Uri Create(string relativeUri, 
            params (string key, string value)[] parameters)
        {
            var builder = new UriBuilder(_baseUri)
            {
                Path = _baseUri.AbsolutePath.TrimEnd('/') + relativeUri,
                Query = string.Join("&", parameters
                    .Where(t => t.value != null)
                    .Select(t => $"{Uri.EscapeDataString(t.key)}={Uri.EscapeDataString(t.value)}"))
            };
            return builder.Uri;
        }
    }
}
