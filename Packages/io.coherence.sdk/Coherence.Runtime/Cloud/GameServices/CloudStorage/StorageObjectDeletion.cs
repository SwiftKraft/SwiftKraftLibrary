// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a request to delete <see cref="StorageItem">items</see> from a
    /// storage object stored in <see cref="CloudStorage"/>.
    /// </summary>
    /// <example>
    /// <code source="Runtime/CloudStorage/StorageObjectDeletionExample.cs" language="csharp"/>
    /// </example>
    /// <seealso cref="CloudStorage.DeleteAsync(StorageObjectDeletion, System.Threading.CancellationToken)"/>
    internal sealed class StorageObjectDeletion : IEquatable<StorageObjectId>, IEnumerable<Key>
    {
        /// <summary>
        /// Identifier of the storage object this deletion targets.
        /// </summary>
        public StorageObjectId ObjectId { get; }

        // TODO: make this public and used it in Phase 2 of Cloud Storage implementation (#5413)
        // to enable users to only delete a subset of properties from a Storage object
        // instead of the whole thing.
        internal Key[] Filter { get; }

        /// <summary>
        /// Is the storage object deleted entirely, or only a subset of its items?
        /// </summary>
        internal bool IsPartial { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageObjectDeletion"/> struct representing an operation
        /// to completely delete a storage object from <see cref="CloudStorage"/>.
        /// </summary>
        /// <param name="objectId"> Identifier of the storage object to delete. </param>
        public StorageObjectDeletion(StorageObjectId objectId) : this(objectId, Array.Empty<Key>(), false) { }

        // TODO: make this public and used it in Phase 2 of Cloud Storage implementation (#5413)
        // to enable users to only delete a subset of properties from a Storage object
        // instead of the whole thing.
        internal StorageObjectDeletion(StorageObjectId objectId, params Key[] keys) : this(objectId, keys, true) { }

        private StorageObjectDeletion(StorageObjectId objectId, Key[] keys, bool isPartial)
        {
            ObjectId = objectId;
            Filter = keys ?? Array.Empty<Key>();
            IsPartial = isPartial;
        }

        public bool Equals(StorageObjectId other) => ObjectId.Equals(other);
        public IEnumerator<Key> GetEnumerator() => ((IEnumerable<Key>)Filter).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Filter.GetEnumerator();

        public static implicit operator StorageObjectDeletion(StorageObjectId id) => new(id);
    }
}
