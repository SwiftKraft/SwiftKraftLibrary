// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a deferred operation to mutate or delete items from a single storage object.
    /// </summary>
    /// <seealso cref="StorageOperationQueue"/>
    internal readonly struct DeferredStorageObjectMutationOrDeletion
    {
        public readonly StorageObjectId ObjectId;

        /// <summary>
        /// Object to save fully.
        /// </summary>
        [MaybeNull]
        public readonly StorageObject StorageObject;

        /// <summary>
        /// Items to mutate.
        /// </summary>
        public readonly StorageItem[] Items;

        /// <summary>
        /// Keys of items to delete, or an empty array, if the whole object should be deleted.
        /// </summary>
        public readonly Key[] Filter;

        /// <summary>
        /// If true this represents a deletion (DeleteAsync), otherwise this represents a mutation (SaveAsync).
        /// </summary>
        public readonly bool IsDelete;

        /// <summary>
        /// If true, then only some items on the storage object are affected.
        /// In case of a deletion, this means that only items matching the filter are deleted.
        /// In case of a mutation, this means that only the items in the Items array are mutated, and others are left untouched.
        /// </summary>
        public readonly bool IsPartial;
        public readonly TaskCompletionSource<bool> TaskCompletionSource;
        public readonly CancellationToken CancellationToken;

        private DeferredStorageObjectMutationOrDeletion(StorageObjectId objectId, StorageObject storageObject, StorageItem[] items, Key[] filter, bool isDelete, bool isPartial, TaskCompletionSource<bool> taskCompletionSource, CancellationToken cancellationToken)
        {
            ObjectId = objectId;
            StorageObject = storageObject;
            Items = items;
            Filter = filter;
            IsDelete = isDelete;
            IsPartial = isPartial;
            TaskCompletionSource = taskCompletionSource;
            CancellationToken = cancellationToken;
        }

        public static DeferredStorageObjectMutationOrDeletion Mutation(StorageObjectId objectId, StorageObject storageObject, StorageItem[] items, bool isPartial, TaskCompletionSource<bool> taskCompletionSource, CancellationToken cancellationToken) => new
        (
            objectId,
            storageObject,
            items,
            Array.Empty<Key>(),
            isDelete: false,
            isPartial: isPartial,
            taskCompletionSource,
            cancellationToken
        );

        public static DeferredStorageObjectMutationOrDeletion Deletion(StorageObjectId objectId, Key[] filter, bool isPartial, TaskCompletionSource<bool> taskCompletionSource, CancellationToken cancellationToken) => new
        (
            objectId,
            null,
            Array.Empty<StorageItem>(),
            filter,
            isDelete: true,
            isPartial: isPartial,
            taskCompletionSource,
            cancellationToken
        );
    }
}
