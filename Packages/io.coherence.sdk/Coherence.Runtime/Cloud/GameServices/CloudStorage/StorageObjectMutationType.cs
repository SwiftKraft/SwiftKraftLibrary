// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    /// <summary>
    /// Specifies the types of <see cref="StorageObjectMutation">mutations</see> that can be performed on
    /// a storage object using <see cref="CloudStorage.SaveAsync(StorageObjectMutation, System.Threading.CancellationToken)"/>.
    /// </summary>
    internal enum StorageObjectMutationType
    {
        /// <summary>
        /// Represents an operation that completely replaces the entire of a storage object in <see cref="CloudStorage"/>.
        /// <remarks>
        /// Any existing items that the storage object had in <see cref="CloudStorage"/> that are not included in the
        /// <see cref="StorageObjectMutation">mutation</see> will be removed from the storage object.
        /// </remarks>
        /// </summary>
        Full = 0

        // TODO: introduce Partial in Phase 2 (#5413) to enable users to perform partial updates on storage objects.
        //Partial = 1
    }
}
