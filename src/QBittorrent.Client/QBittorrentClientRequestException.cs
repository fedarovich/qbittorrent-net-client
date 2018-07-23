using System;
using System.Net;
using System.Net.Http;

namespace QBittorrent.Client
{
    /// <summary>
    /// This exception is thrown if QBittorrent server answers with non-success status code.
    /// </summary>
    [Serializable]
    public class QBittorrentClientRequestException : HttpRequestException
    {
        /// <summary>
        /// Creates a new instance of <see cref="QBittorrentClientRequestException"/>.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="statusCode">HTTPS status code.</param>
        public QBittorrentClientRequestException(string message, HttpStatusCode statusCode) : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; }
    }
}
