using System;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace QBittorrent.Client.Extensions
{
    internal static class HttpClientExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<string> GetStringAsync(this HttpClient client, 
            Uri uri, 
            CancellationToken token)
        {
            return client.GetStringAsync(uri, false, token);
        }
        
        public static async Task<string> GetStringAsync(this HttpClient client, 
            Uri uri, 
            bool returnEmptyIfNotFound, 
            CancellationToken token)
        {
            using (var response = await client.GetAsync(uri, token).ConfigureAwait(false))
            {
                if (returnEmptyIfNotFound && response.StatusCode == HttpStatusCode.NotFound)
                    return string.Empty;
                
                response.EnsureSuccessStatusCode();
                HttpContent content = response.Content;
                if (content != null)
                {
                    return await content.ReadAsStringAsync().ConfigureAwait(false);
                }

                return string.Empty;
            }
        }
    }
}
