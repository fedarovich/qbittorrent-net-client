using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace QBittorrent.Client.Internal
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ContractAnnotation("null => halt")]
        internal static void ValidateHashes(ref IEnumerable<string> hashes)
        {
            if (hashes == null)
                throw new ArgumentNullException(nameof(hashes));

            var list = new List<string>();
            foreach (var hash in hashes)
            {
                ValidateHash(hash);
                list.Add(hash);
            }

            hashes = list;
        }
    }
}
