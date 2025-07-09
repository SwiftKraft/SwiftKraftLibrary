// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.Threading;

    /// <summary>
    /// Represents a deferred operation to load items from a single storage object.
    /// </summary>
    /// <seealso cref="StorageOperationQueue"/>
    internal readonly struct DeferredStorageObjectQuery
    {
        public readonly StorageObjectId ObjectId;
        public readonly Type ObjectType;
        public readonly Key[] Filter;
        public readonly bool IsPartial;
        public readonly CancellationToken CancellationToken;
        public readonly LoadTaskCompletionHandler TaskCompletionHandler;

        public DeferredStorageObjectQuery(StorageObjectId objectId, Type objectType, Key[] filter, bool isPartial, LoadTaskCompletionHandler taskCompletionHandler, CancellationToken cancellationToken)
        {
            ObjectId = objectId;
            ObjectType = objectType;
            Filter = filter;
            IsPartial = isPartial;
            TaskCompletionHandler = taskCompletionHandler;
            CancellationToken = cancellationToken;
        }
    }
}
