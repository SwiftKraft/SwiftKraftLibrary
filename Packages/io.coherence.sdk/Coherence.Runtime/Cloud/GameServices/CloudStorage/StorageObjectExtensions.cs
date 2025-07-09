// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Contains some internal convenience extension methods for storage object-related types.
    /// </summary>
    internal static class StorageObjectExtensions
    {
        public static StorageObjectId[] Ids(this IEnumerable<StorageObjectMutation> mutations) => mutations.Select(storage => storage.ObjectId).ToArray();
        public static StorageObjectId[] Ids(this IEnumerable<StorageObjectQuery> queries) => queries.Select(storage => storage.ObjectId).ToArray();
        public static StorageObjectId[] Ids(this IEnumerable<StorageObjectDeletion> deletions) => deletions.Select(storage => storage.ObjectId).ToArray();
        public static string AllToString(this IEnumerable<StorageObjectId> ids, string delimiter) => string.Join(delimiter, ids);
    }
}
