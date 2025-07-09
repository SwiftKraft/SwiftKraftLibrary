// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a request to load <see cref="StorageItem">items</see> from a storage object stored in <see cref="CloudStorage"/>.
    /// </summary>
    /// <example>
    /// <code source="Runtime/CloudStorage/StorageObjectQueryExample.cs" language="csharp"/>
    /// </example>
    /// <seealso cref="CloudStorage.LoadAsync(StorageObjectQuery, System.Threading.CancellationToken)"/>
    internal sealed class StorageObjectQuery : IEquatable<StorageObjectId>, IEnumerable<Key>
    {
        /// <summary>
        /// Identifier of the storage object that this query targets.
        /// </summary>
        public StorageObjectId ObjectId { get; }

        /// <summary>
        /// Type of the object to load.
        /// </summary>
        public Type ObjectType { get; }

        // TODO: make this public and used it in Phase 2 of Cloud Storage implementation (#5413)
        // to enable users to only load a subset of properties from a Storage object
        // instead of the whole thing.
        internal Key[] Filter { get; }

        /// <summary>
        /// Does this query fetch the entire contents of the storage object, or only a subset of its items?
        /// </summary>
        internal bool IsPartial { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageObjectQuery"/> struct representing an operation
        /// to load the full state of a storage object from <see cref="CloudStorage"/>.
        /// </summary>
        /// <param name="objectId"> Identifier of the storage object to load. </param>
        public StorageObjectQuery(StorageObjectId objectId, Type objectType) : this(objectId, objectType, Array.Empty<Key>(), false) { }

        // TODO: make this public and used it in Phase 2 of Cloud Storage implementation (#5413)
        // to enable users to only load a subset of properties from a Storage object
        // instead of the whole thing.
        internal StorageObjectQuery(StorageObjectId objectId, Type objectType, params Key[] keys) : this(objectId, objectType, keys,true) { }

        private StorageObjectQuery(StorageObjectId objectId, Type objectType, Key[] keys, bool isPartial)
        {
            ObjectId = objectId;
            ObjectType = objectType;
            Filter = keys ?? Array.Empty<Key>();
            IsPartial = isPartial;
        }

        public bool Equals(StorageObjectId other) => ObjectId.Equals(other);
        public IEnumerator<Key> GetEnumerator() => ((IEnumerable<Key>)Filter).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Filter.GetEnumerator();
    }
}
