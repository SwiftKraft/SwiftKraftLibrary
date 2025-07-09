// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;

    internal interface IStorageOperationBatchProcessor
    {
        StorageOperation DeleteBatchAsync([DisallowNull] IEnumerable<StorageObjectDeletion> deletions, CancellationToken cancellationToken = default);
        StorageOperation SaveBatchAsync([DisallowNull] IEnumerable<StorageObjectMutation> mutations, CancellationToken cancellationToken = default);
        StorageOperation<StorageObject[]> LoadBatchAsync([DisallowNull] IEnumerable<StorageObjectQuery> queries, CancellationToken cancellationToken = default);
    }
}
