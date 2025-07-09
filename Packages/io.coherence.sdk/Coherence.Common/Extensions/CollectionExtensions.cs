// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Extensions
{
    using System.Collections.Generic;

    internal static class CollectionExtensions
    {
        internal static void AddKeys<TKey, TVal>(this ICollection<TKey> collection, Dictionary<TKey, TVal> dictionary)
        {
            foreach(var kv in dictionary)
            {
                collection.Add(kv.Key);
            }
        }
    }
}
