using System;

namespace QBittorrent.Client
{
    /// <summary>
    /// This exception is thrown on attempts to use functions not supported in current API version.
    /// </summary>
    /// <seealso cref="ApiLevel"/>
    /// <seealso cref="ApiLevelAttribute"/>
    [Serializable]
    public class ApiNotSupportedException : NotSupportedException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ApiNotSupportedException"/>.
        /// </summary>
        /// <param name="requiredApiLevel">The minimal required API level.</param>
        public ApiNotSupportedException(ApiLevel requiredApiLevel) 
            : this("The API version being used does not support this function.", requiredApiLevel)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ApiNotSupportedException"/>.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="requiredApiLevel">The minimal required API level.</param>
        public ApiNotSupportedException(string message, ApiLevel requiredApiLevel)
            : base(message)
        {
            RequiredApiLevel = requiredApiLevel;
        }

        /// <summary>
        /// The minimal required API level.
        /// </summary>
        public ApiLevel RequiredApiLevel { get; }
    }
}
