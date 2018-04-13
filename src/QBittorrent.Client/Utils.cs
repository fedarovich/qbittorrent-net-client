using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;

namespace QBittorrent.Client
{
    internal static class Utils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool HashIsValid(string hash)
        {
            return hash.Length == 40 && hash.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ContractAnnotation("null => halt")]
        internal static void ValidateHash(string hash)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));

            if (!HashIsValid(hash))
                throw new ArgumentException("The parameter must be a hexadecimal representation of SHA-1 hash.", nameof(hash));
        }

        [ContractAnnotation("null => halt")]
        internal static string JoinHashes(IEnumerable<string> hashes)
        {
            if (hashes == null)
                throw new ArgumentNullException(nameof(hashes));

            var builder = new StringBuilder(4096);
            foreach (var hash in hashes)
            {
                if (hash == null || !HashIsValid(hash))
                    throw new ArgumentException("The values must be hexadecimal representations of SHA-1 hashes.", nameof(hashes));

                if (builder.Length > 0)
                {
                    builder.Append('|');
                }

                builder.Append(hash);
            }

            if (builder.Length == 0)
                throw new ArgumentException("The list of hashes cannot be empty.", nameof(hashes));

            return builder.ToString();
        }
    }
}
