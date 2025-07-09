// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    internal delegate void LoadTaskCompletionHandler(StorageOperation<StorageObject[]> operation);

    /// <summary>
    /// Class responsible for deferring and batching Save, Delete and Load operations performed by CloudStorage,
    /// with the aim of minimizing the time spent waiting for responses from the server.
    /// </summary>
    internal sealed class StorageOperationQueue
    {
        private readonly IStorageOperationBatchProcessor cloudStorage;
        private readonly object threadLock = new();
        private readonly List<DeferredStorageObjectMutationOrDeletion> deferredMutationsAndDeletions = new();
        private readonly List<DeferredStorageObjectQuery> deferredQueries = new();
        private bool isProcessingMutationsAndDeletions;
        private bool isProcessingQueries;

        public bool IsEmpty
        {
            get
            {
                lock (threadLock)
                {
                    return !deferredMutationsAndDeletions.Any() && !deferredQueries.Any();
                }
            }
        }

        private bool HasDeferredMutationsOrDeletions
        {
            get
            {
                lock (threadLock)
                {
                    return deferredMutationsAndDeletions.Any();
                }
            }
        }

        private bool HasDeferredQueries
        {
            get
            {
                lock (threadLock)
                {
                    return deferredQueries.Any();
                }
            }
        }

        internal StorageOperationQueue(IStorageOperationBatchProcessor cloudStorage) => this.cloudStorage = cloudStorage;

        /// <summary>
        /// Adds a <see cref="CloudStorage.SaveBatchAsync"/> operation to the end of the queue.
        /// </summary>
        public void EnqueueSaveOperation
        (
            StorageObjectMutation[] mutations,
            TaskCompletionSource<bool> taskCompletionSource,
            CancellationToken cancellationToken
        )
        {
            lock (threadLock)
            {
                foreach (var mutation in mutations)
                {
                    deferredMutationsAndDeletions.Add(DeferredStorageObjectMutationOrDeletion.Mutation(mutation.ObjectId, mutation.storageObject, mutation.ToArray(), mutation.Type != StorageObjectMutationType.Full, taskCompletionSource, cancellationToken));
                }
            }

            SendMutationsOrDeletionsLoop();
        }

        /// <summary>
        /// Adds a <see cref="CloudStorage.DeleteBatchAsync"/> operation to the end of the queue.
        /// </summary>
        public void EnqueueDeleteOperation
        (
            StorageObjectDeletion[] deletions,
            TaskCompletionSource<bool> taskCompletionSource,
            CancellationToken cancellationToken
        )
        {
            lock (threadLock)
            {
                foreach (var deletion in deletions)
                {
                    deferredMutationsAndDeletions.Add(DeferredStorageObjectMutationOrDeletion.Deletion(deletion.ObjectId, deletion.Filter, deletion.IsPartial, taskCompletionSource, cancellationToken));
                }
            }

            SendMutationsOrDeletionsLoop();
        }

        /// <summary>
        /// Adds a <see cref="CloudStorage.LoadAsync(StorageObjectQuery[])"/> operation to the end of the queue.
        /// </summary>
        public void EnqueueLoadOperation
        (
            StorageObjectQuery[] queries,
            TaskCompletionSource<StorageObject[]> taskCompletionSource,
            CancellationToken cancellationToken
        )
        {
            LoadTaskCompletionHandler taskCompletionHandler = CompleteTask;

            lock (threadLock)
            {
                foreach (var query in queries)
                {
                    deferredQueries.Add(new(query.ObjectId, query.ObjectType, query.Filter, query.IsPartial, taskCompletionHandler, cancellationToken));
                }
            }

            SendQueriesLoop();

            void CompleteTask(StorageOperation<StorageObject[]> operation)
            {
                operation.MarkErrorAsObserved();

                if (taskCompletionSource.Task.IsCompleted)
                {
                    return;
                }

                if (operation.IsCanceled || cancellationToken.IsCancellationRequested)
                {
                    taskCompletionSource.SetCanceled();
                    return;
                }

                if (operation.HasFailed)
                {
                    taskCompletionSource.SetException(operation.GetOrCreateError());
                    return;
                }

                taskCompletionSource.SetResult(GetResultsForQueries(queries, operation.Result));

                static StorageObject[] GetResultsForQueries(IEnumerable<StorageObjectQuery> queries, StorageObject[] storageObjects)
                    => queries.Select(query => GetResultForQuery(query, storageObjects.FirstOrDefault(obj => obj.ObjectId == query.ObjectId))).Where(result => result is not null).ToArray();

                static StorageObject GetResultForQuery(StorageObjectQuery query, [MaybeNull] StorageObject storageObject)
                {
                    if (storageObject is null)
                    {
                        return null;
                    }

                    if (!query.IsPartial)
                    {
                        return storageObject;
                    }

                    return new(query.ObjectId, storageObject.Where(item => query.Filter.Contains(item.Key)).ToArray());
                }
            }
        }

        public void CancelAllQueuedOperations()
        {
            DeferredStorageObjectMutationOrDeletion[] mutationsAndDeletionsToCancel;
            DeferredStorageObjectQuery[] queriesToCancel;
            lock (threadLock)
            {
                mutationsAndDeletionsToCancel = deferredMutationsAndDeletions.ToArray();
                deferredMutationsAndDeletions.Clear();
                queriesToCancel = deferredQueries.ToArray();
                deferredQueries.Clear();
            }

            foreach (var mutationOrDeletion in mutationsAndDeletionsToCancel)
            {
                mutationOrDeletion.TaskCompletionSource.SetCanceled();
            }

            var cancelledStorageOperation = new StorageOperation<StorageObject[]>(Task.FromCanceled<StorageObject[]>(new(canceled: true)));
            foreach (var query in queriesToCancel)
            {
                query.TaskCompletionHandler(cancelledStorageOperation);
            }
        }

        /// <summary>
        /// Dequeues the next batch of deferred storage object mutations and deletions from the <paramref name="queue"/> and returns collections
        /// of <see cref="StorageObjectMutation"/> and <see cref="StorageObjectDeletion"/> objects for performing all of them as single combined operations.
        /// </summary>
        internal static (IEnumerable<StorageObjectMutation>, IEnumerable<StorageObjectDeletion>) GetNextMutationsAndDeletions(List<DeferredStorageObjectMutationOrDeletion> queue, List<TaskCompletionSource<bool>> mutationCompletionSources, List<TaskCompletionSource<bool>> deletionCompletionSources)
        {
            var nextMutations = new Dictionary<StorageObjectId, StorageObjectMutation>();
            var nextDeletions = new Dictionary<StorageObjectId, StorageObjectDeletion>();

            for (var index = 0; index < queue.Count; index++)
            {
                var nextInQueue = queue[index];
                // Discard cancelled operations
                if (nextInQueue.CancellationToken.IsCancellationRequested)
                {
                    if (!nextInQueue.TaskCompletionSource.Task.IsCompleted)
                    {
                        nextInQueue.TaskCompletionSource.SetCanceled();
                    }

                    queue.RemoveAt(index);
                    index--;
                    continue;
                }

                var objectId = nextInQueue.ObjectId;
                var isPartial = nextInQueue.IsPartial;
                var isDelete = nextInQueue.IsDelete;
                var isMutation = !isDelete;

                if (isMutation)
                {
                    // If there are already deletions targeting the same object and any of the same items,
                    // then leave this operation in the queue for later processing.
                    if (nextDeletions.TryGetValue(objectId, out var deletionTargetingSameObject) &&
                        (!deletionTargetingSameObject.IsPartial
                        || deletionTargetingSameObject.Filter.Intersect(nextInQueue.Items.Select(item => item.Key)).Any()))
                    {
                        continue;
                    }

                    // If no previous mutations exist, create a new one.
                    if (!nextMutations.TryGetValue(objectId, out var combinedMutation)
                    // A full mutation also renders all preceding mutations irrelevant.
                    || !isPartial)
                    {
                        // TODO: Update to use StorageObjectMutationType.Partial when it is introduced in Phase 2 (#5413)
                        combinedMutation = new(nextInQueue.StorageObject, isPartial ? (StorageObjectMutationType)1 : StorageObjectMutationType.Full);
                        nextMutations[objectId] = combinedMutation;
                    }
                    else
                    {
                        // Later item mutations should override earlier ones.
                        foreach (var item in nextInQueue.Items)
                        {
                            combinedMutation[item.Key] = item.Value;
                        }
                    }


                    mutationCompletionSources.Add(nextInQueue.TaskCompletionSource);
                }
                else
                {
                    // If there are already mutations targeting the same object and any of the same items,
                    // then leave this operation in the queue for later processing.
                    if (nextMutations.TryGetValue(objectId, out var mutationTargetingSameObject) &&
                        (mutationTargetingSameObject.Type == StorageObjectMutationType.Full
                        || mutationTargetingSameObject.Keys.Intersect(nextInQueue.Filter).Any()))
                    {
                        continue;
                    }

                    // If no previous deletions exist, create a new one.
                    if (!nextDeletions.TryGetValue(objectId, out var combinedDeletion))
                    {
                        nextDeletions[objectId] = isPartial ? new(objectId, nextInQueue.Filter) : new(objectId);
                    }
                    // A full deletion renders all preceding deletions irrelevant.
                    else if (!isPartial)
                    {
                        nextDeletions[objectId] = new(objectId);
                    }
                    // If there are previous partial deletions targeting the same object as this partial deletion,
                    // combine the filters from all of them together.
                    else if (combinedDeletion.IsPartial)
                    {
                        nextDeletions[objectId] = new(objectId, combinedDeletion.Filter.Concat(nextInQueue.Filter).Distinct().ToArray());
                    }

                    deletionCompletionSources.Add(nextInQueue.TaskCompletionSource);
                }

                queue.RemoveAt(index);
                index--;
            }

            return (nextMutations.Values, nextDeletions.Values);
        }

        /// <summary>
        /// Dequeues the next batch of deferred storage object load requests from the <paramref name="queue"/> and returns a collection
        /// of <see cref="StorageObjectQuery"/> objects for performing all of them as one combined
        /// <see cref="CloudStorage.LoadBatchAsync"/> operation.
        /// </summary>
        /// <returns>
        /// A collection of <see cref="StorageObjectQuery"/> objects, or an empty collection if there are no queries to perform.
        /// </returns>
        internal static IEnumerable<StorageObjectQuery> GetNextQueries(List<DeferredStorageObjectQuery> queue, List<LoadTaskCompletionHandler> taskCompletionHandlers)
        {
            // Build a dictionary of queries where all deferred queries targeting the same object id have been merged together
            var queries = new Dictionary<StorageObjectId, StorageObjectQuery>();

            foreach (var nextInQueue in queue)
            {
                if (nextInQueue.CancellationToken.IsCancellationRequested)
                {
                    continue;
                }

                taskCompletionHandlers.Add(nextInQueue.TaskCompletionHandler);
                var isPartial = nextInQueue.IsPartial;

                // If there are no previous queries targeting the same object, create a new one.
                if (!queries.TryGetValue(nextInQueue.ObjectId, out var combinedQuery))
                {
                    queries[nextInQueue.ObjectId] = isPartial ? new(nextInQueue.ObjectId, nextInQueue.ObjectType, nextInQueue.Filter) : new(nextInQueue.ObjectId, nextInQueue.ObjectType);
                }
                // A full query renders all preceding queries irrelevant.
                else if (!isPartial)
                {
                    queries[nextInQueue.ObjectId] = new(nextInQueue.ObjectId, nextInQueue.ObjectType);
                }
                // If there are previous partial queries targeting the same object as this partial query,
                // combine the filters from all of them together.
                else if (combinedQuery.IsPartial)
                {
                    queries[nextInQueue.ObjectId] = new(nextInQueue.ObjectId, nextInQueue.ObjectType, combinedQuery.Filter.Concat(nextInQueue.Filter).Distinct().ToArray());
                }
            }

            queue.Clear();
            return queries.Values;
        }

        private async void SendMutationsOrDeletionsLoop()
        {
	        lock(threadLock)
	        {
                if(isProcessingMutationsAndDeletions)
                {
                    return;
                }

		        isProcessingMutationsAndDeletions = true;
	        }

	        while (HasDeferredMutationsOrDeletions)
            {
                await Task.Yield();
                await SendNextMutationsOrDeletions();
            }

	        lock(threadLock)
	        {
		        isProcessingMutationsAndDeletions = false;
	        }
        }

        private async void SendQueriesLoop()
        {
            lock (threadLock)
            {
                if (isProcessingQueries)
                {
                    return;
                }

                isProcessingQueries = true;
            }

            while (HasDeferredQueries)
            {
                await Task.Yield();
                await SendNextQueries();
            }

            lock (threadLock)
            {
                isProcessingQueries = false;
            }
        }

        /// <summary>
        /// Gets the next batch of deferred storage object mutations and deletions from the queue and sends them to the server.
        /// </summary>
        /// <returns>
        /// A task which completes once the next batch of mutation and deletion operations have finished.
        /// <para>
        /// If the queue is empty, the task will complete immediately.
        /// </para>
        /// </returns>
        private async ValueTask SendNextMutationsOrDeletions()
        {
            var mutationCompletionSources = new List<TaskCompletionSource<bool>>();
            var deletionCompletionSources = new List<TaskCompletionSource<bool>>();
            StorageOperation deleteOperation = null;
            StorageOperation saveOperation = null;
            IEnumerable<StorageObjectMutation> nextMutations;
            IEnumerable<StorageObjectDeletion> nextDeletions;

            lock (threadLock)
            {
                if (!deferredMutationsAndDeletions.Any())
                {
                    return;
                }

                (nextMutations, nextDeletions) = GetNextMutationsAndDeletions(deferredMutationsAndDeletions, mutationCompletionSources:mutationCompletionSources, deletionCompletionSources:deletionCompletionSources);
            }

            var hasDeletions = nextDeletions.Any();
            var hasMutations = nextMutations.Any();

            if (hasDeletions)
            {
                if (!hasMutations)
                {
                    deletionCompletionSources.AddRange(mutationCompletionSources);
                    mutationCompletionSources.Clear();
                }
            }
            else if (hasMutations)
            {
                mutationCompletionSources.AddRange(deletionCompletionSources);
                deletionCompletionSources.Clear();
            }

            if (hasDeletions)
            {
                deleteOperation = cloudStorage.DeleteBatchAsync(nextDeletions);
                 _ = deleteOperation.ContinueWith(() => CompleteAll(deleteOperation, deletionCompletionSources));
            }
            else if (hasMutations)
            {
                saveOperation = cloudStorage.SaveBatchAsync(nextMutations);
                _ = saveOperation.ContinueWith(() => CompleteAll(saveOperation, mutationCompletionSources));
            }

            if (deleteOperation is not null)
            {
                await deleteOperation;
            }

            if (saveOperation is not null)
            {
                await saveOperation;
            }
        }

        /// <summary>
        /// Gets the next batch of deferred storage object queries from the queue and sends them to the server.
        /// </summary>
        /// <returns>
        /// A task which completes once the next batch of queries have finished.
        /// <para>
        /// If the queue is empty, the task will complete immediately.
        /// </para>
        /// </returns>
        private async ValueTask SendNextQueries()
        {
            var taskCompletionHandlers = new List<LoadTaskCompletionHandler>();
            IEnumerable<StorageObjectQuery> nextQueries;

            lock (threadLock)
            {
                if (!deferredQueries.Any())
                {
                    return;
                }

                nextQueries = GetNextQueries(deferredQueries, taskCompletionHandlers);
            }

            if (!nextQueries.Any())
            {
                return;
            }

            var combinedOperation = await cloudStorage.LoadBatchAsync(nextQueries);
            foreach (var taskCompletionHandler in taskCompletionHandlers)
            {
                taskCompletionHandler(combinedOperation);
            }
        }

        /// <summary>
        /// Completes all the task completion sources in the list with the result of the combined operation.
        /// </summary>
        private static void CompleteAll(StorageOperation combinedOperation, List<TaskCompletionSource<bool>> taskCompletionSources)
        {
            combinedOperation.MarkErrorAsObserved();

            if (combinedOperation.IsCanceled)
            {
                CancelAll(taskCompletionSources);
                return;
            }

            if (combinedOperation.HasFailed)
            {
                FailAll(taskCompletionSources, combinedOperation.GetOrCreateError());
                return;
            }

            SetResultForAll(taskCompletionSources, true);

            static void CancelAll<T>(List<TaskCompletionSource<T>> taskCompletionSources)
            {
                foreach (var taskCompletionSource in taskCompletionSources)
                {
                    if (!taskCompletionSource.Task.IsCompleted)
                    {
                        taskCompletionSource.SetCanceled();
                    }
                }
            }

            static void FailAll<T>(List<TaskCompletionSource<T>> taskCompletionSources, Exception exception)
            {
                foreach (var taskCompletionSource in taskCompletionSources)
                {
                    if (!taskCompletionSource.Task.IsCompleted)
                    {
                        taskCompletionSource.SetException(exception);
                    }
                }
            }
        }

        private static void SetResultForAll(List<TaskCompletionSource<bool>> taskCompletionSources, bool result)
        {
            foreach (var taskCompletionSource in taskCompletionSources)
            {
                if (!taskCompletionSource.Task.IsCompleted)
                {
                    taskCompletionSource.SetResult(result);
                }
            }
        }
    }
}
