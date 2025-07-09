// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using Common;
    using Common.Extensions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Runtime;
    using Utils;

    /// <summary>
    /// <see cref="CloudStorage"/> is a service that allows you to save and load objects (<see cref="StorageObject"/>) to and from the backend.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You must be <see cref="AuthClient">logged in</see> to coherence Cloud to use the service.
    /// <see cref="IsReady"/> will be <see langword="false"/> when you are not logged in.
    /// If you try using the service while <see cref="IsReady"/> is <see langword="false"/>, the operation will fail with <see cref="StorageErrorType.NotLoggedIn"/>.
    /// </para>
    /// <para>
    /// Note that the service has a rate limit of one request of a particular type per second. What this means is that if, for example, a save operation is made while another save operation
    /// is already in progress, then the second save operation will be queued, and will only get sent to the backend after one second has passed since the previous save operation.
    /// </para>
    /// <para>
    /// However, the service can in most cases automatically batch multiple requests into a single one, which means that in practice an operation should not have to sit in queue for more than
    /// one second, except in rare circumstances where the same <see cref="StorageObject">objects</see> are being targeted by a combination of both
    /// <see cref="SaveObjectAsync{TObject}">SaveObjectAsync</see> and <see cref="DeleteObjectAsync">DeleteObjectAsync</see> operations,
    /// which can not be batched together into a single operation.
    /// </para>
    /// <para>
    /// Each operation returns a storage operation object.
    /// </para>
    /// <para>
    /// An <see langword="async"/> method can <see langword="await"/> the result object to wait until the operation has completed.
    /// </para>
    /// <para>
    /// Similarly, a <see cref="UnityEngine.Coroutine"/> can <see langword="yield"/> the result object to wait for it to complete.
    /// </para>
    /// </remarks>
    /// <seealso cref="GameServices"/>
    /// <seealso href="https://docs.coherence.io/hosting/coherence-cloud/coherence-cloud-apis/game-services/authentication-service-player-accounts">Authentication Service</seealso>
    /// <example>
    /// <code source="Runtime/CloudStorage/CloudStorageExample.cs" language="csharp"/>
    /// </example>
    public sealed class CloudStorage : IDisposable, IStorageOperationBatchProcessor
    {
        internal const string BasePath = "/cloudstorage";
        internal const string DeletePathParams = "/delete";
        internal const string LoadRequestMethod = "POST";
        internal const string SaveRequestMethod = "PUT";
        internal const string DeleteRequestMethod = "POST";

        private readonly IRequestFactory requestFactory;
        private readonly IAuthClientInternal authClient;
        private readonly RequestThrottle throttle;
        private readonly StorageOperationQueue operationQueue;
        private State state = State.Active;
        private int requestsInProgress;

        /// <summary>
        /// Gets a value indicating if the service is ready to be used.
        /// </summary>
        /// <remarks>
        /// You must be <see cref="AuthClient">logged in</see> to coherence Cloud to use the service.
        /// <see cref="IsReady"/> will be <see langword="false"/> when you are not logged in.
        /// If you try using the service while <see cref="IsReady"/> is <see langword="false"/>, the operation will fail with <see cref="StorageErrorType.NotLoggedIn"/>.
        /// </remarks>
        /// <seealso href="https://docs.coherence.io/hosting/coherence-cloud/coherence-cloud-apis/game-services/authentication-service-player-accounts">Authentication Service</seealso>
        public bool IsReady => requestFactory.IsReady && authClient.LoggedIn;

        /// <summary>
        /// Gets a value indicating whether there are currently any save, delete or load operations in progress.
        /// </summary>
        public bool IsBusy => requestsInProgress > 0 || operationQueue is { IsEmpty: false };

        internal CloudStorage(IRequestFactory requestFactory, IAuthClientInternal authClient, RequestThrottle throttle, Func<CloudStorage, StorageOperationQueue> operationQueueFactory = null)
        {
            this.requestFactory = requestFactory;
            this.authClient = authClient;
            this.throttle = throttle;

            operationQueue = operationQueueFactory?.Invoke(this);
        }

        /// <summary>
        /// Loads an object from cloud storage.
        /// </summary>
        /// <param name="objectId"> Identifier of the object to load. </param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <typeparam name="TObject"> Type of the object to load. </typeparam>
        /// <returns>
        /// The asynchronous storage operation that will contain the loaded object.
        /// </returns>
        /// <exception cref="StorageException">
        /// If the user has not logged in to coherence Cloud, the operation will fail with
        /// <see cref="StorageErrorType.NotLoggedIn"/>.
        /// <para>
        /// If no object with the specified identifier exists in cloud storage, the operation will fail with
        /// <see cref="StorageErrorType.ObjectNotFound"/>.
        /// </para>
        /// </exception>
        /// <example>
        /// <code source="Runtime/CloudStorage/CloudStorageLoad.cs" language="csharp"/>
        /// </example>
        public StorageOperation<TObject> LoadObjectAsync<TObject>(StorageObjectId objectId, CancellationToken cancellationToken = default)
        {
            if (state is State.Disposed)
            {
                return new(new StorageException(StorageErrorType.CloudStorageHasBeenDisposed, $"{nameof(CloudStorage)}.{nameof(LoadObjectAsync)} was called after the {nameof(CloudStorage)} object has already been disposed."));
            }

            var taskCompletionSource = new TaskCompletionSource<TObject>(cancellationToken);

            StorageOperation<StorageObject[]> batchOperation;
            if (operationQueue is not null)
            {
                batchOperation = null;
                var queuedTaskCompletionSource = new TaskCompletionSource<StorageObject[]>(cancellationToken);
                operationQueue.EnqueueLoadOperation(new StorageObjectQuery[] { new(objectId, typeof(TObject)) }, queuedTaskCompletionSource, cancellationToken);
                queuedTaskCompletionSource.Task.ContinueWith(OnBatchOperationCompleted, default, TaskContinuationOptions.ExecuteSynchronously, TaskUtils.Scheduler);
                return taskCompletionSource.Task;
            }

            batchOperation = LoadBatchAsync(new StorageObjectQuery[] { new(objectId, typeof(TObject)) }, cancellationToken);
            batchOperation.task.ContinueWith(OnBatchOperationCompleted, default, TaskContinuationOptions.ExecuteSynchronously, TaskUtils.Scheduler);

            return taskCompletionSource.Task;

            void OnBatchOperationCompleted(Task<StorageObject[]> task)
            {
                batchOperation?.MarkErrorAsObserved();

                if (taskCompletionSource.Task.IsCompleted)
                {
                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    taskCompletionSource.SetCanceled();
                    return;
                }

                if (task.IsFaulted)
                {
                    taskCompletionSource.SetException(StorageException.From(task.Exception, objectId));
                    return;
                }

                if (task.IsCanceled)
                {
                    taskCompletionSource.SetCanceled();
                    return;
                }

                var storageObject = task.Result.Single();

                if (storageObject.TryGetObject(out TObject @object))
                {
                    taskCompletionSource.SetResult(@object);
                    return;
                }

                if (!StorageObject.To(storageObject, out @object, out var storageException))
                {
                    taskCompletionSource.SetException(storageException);
                    return;
                }

                taskCompletionSource.SetResult(@object);
            }
        }

        private StorageOperation<StorageObject[]> LoadBatchAsync([DisallowNull] IEnumerable<StorageObjectQuery> queries, CancellationToken cancellationToken = default)
        {
            if (!authClient.LoggedIn)
            {
                return StorageException.NotLoggedIn(queries.Ids(), $"{nameof(CloudStorage)}.{nameof(LoadObjectAsync)}");
            }

            var taskCompletionSource = new TaskCompletionSource<StorageObject[]>(cancellationToken);

            try
            {
                // TODO: Add cancellation token support to request factory and pass in cancellationToken #7374
                var requestTask = SendRequestAsync(new()
                {
                    object_ids = queries.Select(query => new PayloadStorageObjectId
                    {
                        type = query.ObjectId.Type,
                        id = query.ObjectId.Id
                    }).ToArray()
                });

                requestTask.ContinueWith(task =>
                {
                    requestsInProgress--;

                    if (taskCompletionSource.Task.IsCompleted)
                    {
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        taskCompletionSource.SetCanceled();
                        return;
                    }

                    try
                    {
                        if (task.IsFaulted)
                        {
                            var exception = StorageException.From(task.Exception, queries.Ids());
                            taskCompletionSource.SetException(exception);
                            return;
                        }

                        if (task.IsCanceled)
                        {
                            taskCompletionSource.SetCanceled();
                            return;
                        }

                        var storages = task.Result.data;
                        var resultCount = storages.Length;

                        // If all the queries failed, return an error. If only some of the queries failed, return the partial results.
                        if (resultCount is 0)
                        {
                            var idsNotFound = queries.Ids().Where(queryId => !storages.Any(objectId => objectId.type != queryId.Type || objectId.id != queryId.Id)).ToArray();
                            taskCompletionSource.SetException(StorageException.StorageObjectNotFound(idsNotFound));
                            return;
                        }

                        var results = new StorageObject[resultCount];
                        for (var i = 0; i < resultCount; i++)
                        {
                            var storage = storages[i];
                            if (queries.FirstOrDefault(q => q.ObjectId.Type == storage.type && q.ObjectId.Id == storage.id) is not { } query)
                            {
                                taskCompletionSource.SetException(new StorageException(StorageErrorType.RequestException, $"Received unexpected object {storage.type}/{storage.id} from the server, instead of the ones queried {string.Join(", ", queries.Select(q => q.ObjectId.ToString()).ToArray())}."));
                                return;
                            }

                            var objectType = query.ObjectType;
                            var @object = storage.data;
                            results[i] = new(query.ObjectId, @object, objectType);
                        }

                        taskCompletionSource.SetResult(results);
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetException(StorageException.From(ex, queries.Ids()));
                    }
                }, default, TaskContinuationOptions.ExecuteSynchronously, TaskUtils.Scheduler);
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(StorageException.From(ex, queries.Ids()));
            }

            return taskCompletionSource.Task;

            async Task<PayloadLoadRequest.Response> SendRequestAsync(PayloadLoadRequest payload)
            {
                requestsInProgress++;
                await WaitForLoadCooldown(cancellationToken);

                var responseJson = await requestFactory.SendRequestAsync
                (
                    basePath: BasePath,
                    method: LoadRequestMethod,
                    body: CoherenceJson.SerializeObject(payload, StorageObject.jsonConverters),
                    headers: null,
                    requestName: $"{nameof(CloudStorage)}.{nameof(LoadObjectAsync)}",
                    sessionToken:authClient.SessionToken
                );

                var serializerSettings = new JsonSerializerSettings { Converters = StorageObject.jsonConverters };
                var serializer = JsonSerializer.Create(serializerSettings);

                var response = new PayloadLoadRequest.Response();

                var jObject = JObject.Parse(responseJson);
                foreach (var token in jObject)
                {
                    if (token.Key is nameof(PayloadLoadRequest.Response.owner_id))
                    {
                        response.owner_id = token.Value.ToObject<string>(serializer);
                        continue;
                    }

                    if (token.Key is nameof(PayloadLoadRequest.Response.data))
                    {
                        var count = token.Value.Count();
                        var data = new PayloadStorageObject[count];
                        var index = 0;
                        foreach (var storageObject in token.Value)
                        {
                            var type = storageObject[nameof(PayloadStorageObject.type)].ToObject<string>(serializer);
                            var id = storageObject[nameof(PayloadStorageObject.id)].ToObject<string>(serializer);
                            var objectId = new StorageObjectId(type, id);
                            var objectType = queries.First(q => q.ObjectId == objectId).ObjectType;

                            try
                            {
                                data[index] = new
                                (
                                    type: type,
                                    id: id,
                                    data: storageObject[nameof(PayloadStorageObject.data)].ToObject(objectType, serializer),
                                    ownerId: storageObject[nameof(PayloadStorageObject.owner_id)]?.ToObject<string>(serializer),
                                    version: storageObject[nameof(PayloadStorageObject.version)]?.ToObject<string>(serializer)
                                );
                            }
                            catch(Exception ex)
                            {
                                throw new StorageException(StorageErrorType.DeserializationFailed, $"Failed to deserialize json as type {objectType.ToStringWithGenericArguments()}.\nJson: \"{storageObject[nameof(PayloadStorageObject.data)]}\"\nException: {ex}");
                            }

                            index++;
                        }

                        response.data = data;
                    }
                }

                return response;
            }
        }

        /// <summary>
        /// Saves an object to cloud storage.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If an object with the id already exists in cloud storage, it will be overwritten.
        /// </para>
        /// </remarks>
        /// <param name="objectId">Identifier of the object to save.</param>
        /// <param name="object">The object to save.</param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <typeparam name="TObject"> Type of the object to save. </typeparam>
        /// <returns> The status of the asynchronous save operation. </returns>
        /// <exception cref="StorageException">
        /// If the user is not logged in via <see cref="AuthClient"/>, the operation will fail with
        /// <see cref="StorageErrorType.NotLoggedIn"/>.
        /// </exception>
        /// <example>
        /// <code source="Runtime/CloudStorage/CloudStorageSave.cs" language="csharp"/>
        /// </example>
        public StorageOperation SaveObjectAsync<TObject>(StorageObjectId objectId, TObject @object, CancellationToken cancellationToken = default)
        {
            if (@object is null)
            {
                return new(new StorageException(StorageErrorType.NullArgument, $"{nameof(CloudStorage)}.{nameof(SaveObjectAsync)} was called with a null object."));
            }

            if (state is State.Disposed)
            {
                return new(new StorageException(StorageErrorType.CloudStorageHasBeenDisposed, $"{nameof(CloudStorage)}.{nameof(SaveObjectAsync)} was called after the {nameof(CloudStorage)} object has already been disposed."));
            }

            var taskCompletionSource = new TaskCompletionSource<bool>(cancellationToken);

            if (!StorageObject.From(objectId, StorageObjectMutationType.Full, @object, out var storageObject, out var storageException))
            {
                taskCompletionSource.SetException(storageException);
                return taskCompletionSource.Task;
            }

            if (operationQueue is not null)
            {
                operationQueue.EnqueueSaveOperation(new StorageObjectMutation[] { new(storageObject) }, taskCompletionSource, cancellationToken);
                return taskCompletionSource.Task;
            }

            return SaveBatchAsync(new StorageObjectMutation[] { new(storageObject) }, cancellationToken);
        }

        internal StorageOperation SaveBatchAsync([DisallowNull] IEnumerable<StorageObjectMutation> mutations, CancellationToken cancellationToken = default)
        {
            if (!authClient.LoggedIn)
            {
                return new(StorageException.NotLoggedIn(mutations.Ids(), $"{nameof(CloudStorage)}.{nameof(LoadObjectAsync)}"));
            }

            var taskCompletionSource = new TaskCompletionSource<bool>(cancellationToken);
            var result = new StorageOperation(taskCompletionSource.Task);
            try
            {
                // TODO: Add cancellation token support to request factory and pass in cancellationToken #7374
                var requestTask = SendRequestAsync(new()
                {
                    storageObjectMutations = mutations.Select(mutation => new PayloadStorageObject
                    {
                        type = mutation.ObjectId.Type,
                        id = mutation.ObjectId.Id,
                        data = mutation.storageObject.Object
                    }).ToArray()
                });

                requestTask.ContinueWith(task =>
                {
                    requestsInProgress--;

                    if (taskCompletionSource.Task.IsCompleted)
                    {
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        taskCompletionSource.SetCanceled();
                        return;
                    }

                    try
                    {
                        if (task.IsFaulted)
                        {
                            taskCompletionSource.SetException(StorageException.From(task.Exception, mutations.Ids()));
                        }
                        else if (task.IsCanceled)
                        {
                            taskCompletionSource.SetCanceled();
                        }
                        else
                        {
                            taskCompletionSource.SetResult(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetException(StorageException.From(ex, mutations.Ids()));
                    }
                }, default, TaskContinuationOptions.ExecuteSynchronously, TaskUtils.Scheduler);
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(StorageException.From(ex, mutations.Ids()));
            }

            return result;

            async Task SendRequestAsync(PayloadSaveRequest payload)
            {
                requestsInProgress++;
                await WaitForSaveCooldown(cancellationToken);
                await requestFactory.SendRequestAsync
                (
                    basePath: BasePath,
                    method: SaveRequestMethod,
                    body: CoherenceJson.SerializeObject(payload, StorageObject.jsonConverters),
                    headers: null,
                    requestName: $"{nameof(CloudStorage)}.{nameof(SaveObjectAsync)}",
                    sessionToken:authClient.SessionToken
                );
            }
        }

        /// <summary>
        /// Deletes an object from cloud storage.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If no object with the specified identifier exists,
        /// the returned <see cref="StorageOperation"/>'s <see cref="CloudOperation{TError}.IsCompletedSuccessfully"/>
        /// is set to <see langword="true"/>, but no object will be deleted.
        /// </para>
        /// </remarks>
        /// <param name="objectId"> Identifier of the object to delete. </param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <returns> The status of the asynchronous operation. </returns>
        /// <exception cref="StorageException">
        /// When not logged in, <see cref="StorageErrorType"/> is set to <see cref="StorageErrorType.NotLoggedIn"/>.
        /// </exception>
        /// <example>
        /// <code source="Runtime/CloudStorage/CloudStorageDelete.cs" language="csharp"/>
        /// </example>
        public StorageOperation DeleteObjectAsync(StorageObjectId objectId, CancellationToken cancellationToken = default) => DeleteAsync(new[] { new StorageObjectDeletion(objectId) }, cancellationToken);

        /// <summary>
        /// Deletes some or all items from one or more objects.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If no object with one or more of the specified identifier exists,
        /// the returned <see cref="StorageOperation"/>'s <see cref="CloudOperation{TError}.IsCompletedSuccessfully"/>
        /// is still set to <see langword="true"/>.
        /// </para>
        /// </remarks>
        /// <returns> The status of the asynchronous operation. </returns>
        /// <param name="deletions"> One or more objects specifying which items to delete from which objects. </param>
        /// <param name="cancellationToken">Used to cancel the operation.</param>
        /// <exception cref="StorageException">
        /// If the <paramref name="deletions"/> argument is <see langword="null"/>, the operation will fail with
        /// <see cref="StorageErrorType.NullArgument"/>.
        /// <para>
        /// If the <paramref name="deletions"/> argument is empty, the operation will fail with
        /// <see cref="StorageErrorType.EmptyArgument"/>.
        /// </para>
        /// <para>
        /// If the user has not logged in to coherence Cloud, the operation will fail with
        /// <see cref="StorageErrorType.NotLoggedIn"/>.
        /// </para>
        /// </exception>
        private StorageOperation DeleteAsync([DisallowNull] IEnumerable<StorageObjectDeletion> deletions, CancellationToken cancellationToken = default)
        {
            if (deletions is null)
            {
                return new(new StorageException(StorageErrorType.NullArgument, $"{nameof(CloudStorage)}.{nameof(DeleteAsync)} was called with a null {nameof(deletions)} argument. Please make sure to provide one or more queries when calling this method."));
            }

            var deletionsArray = deletions.ToArray();
            if (deletionsArray.Length is 0)
            {
                return new(new StorageException(StorageErrorType.EmptyArgument, $"{nameof(CloudStorage)}.{nameof(DeleteAsync)} was called with an empty {nameof(deletions)} argument. Please make sure to provide one or more queries when calling this method."));
            }

            if (state is State.Disposed)
            {
                return new(new StorageException(StorageErrorType.CloudStorageHasBeenDisposed, $"{nameof(CloudStorage)}.{nameof(DeleteAsync)} was called after the {nameof(CloudStorage)} object has already been disposed."));
            }

            // Remove partial deletions with no filter
            if (deletionsArray.Any(deletion => deletion.Filter.Length is 0 && deletion.IsPartial))
            {
                deletionsArray = deletionsArray.Where(deletion => deletion.Filter.Length > 0 || !deletion.IsPartial).ToArray();

                // If all operations were partial deletions with no filter, complete the operation immediately.
                if (deletionsArray.Length is 0)
                {
                    return new(Task.CompletedTask);
                }
            }

            var taskCompletionSource = new TaskCompletionSource<bool>(cancellationToken);

            if (operationQueue is not null)
            {
                operationQueue.EnqueueDeleteOperation(deletionsArray, taskCompletionSource, cancellationToken);
                return taskCompletionSource.Task;
            }

            return DeleteBatchAsync(deletionsArray, cancellationToken);
        }

        private StorageOperation DeleteBatchAsync([DisallowNull] IEnumerable<StorageObjectDeletion> deletions, CancellationToken cancellationToken = default)
        {
            if (!authClient.LoggedIn)
            {
                return new(StorageException.NotLoggedIn(deletions.Ids(), $"{nameof(CloudStorage)}.{nameof(DeleteAsync)}"));
            }

            var taskCompletionSource = new TaskCompletionSource<bool>(cancellationToken);
            var result = new StorageOperation(taskCompletionSource.Task);

            try
            {
                // TODO: Add cancellation token support to request factory and pass in cancellationToken #7374
                var requestTask = SendRequestAsync(new()
                {
                    storageObjectIds = deletions.Select(deletion => new PayloadStorageObjectId
                    {
                        type = deletion.ObjectId.Type,
                        id = deletion.ObjectId.Id
                    }).ToArray()
                });

                requestTask.ContinueWith(task =>
                {
                    requestsInProgress--;

                    if (taskCompletionSource.Task.IsCompleted)
                    {
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        taskCompletionSource.SetCanceled();
                        return;
                    }

                    try
                    {
                        if (task.IsFaulted)
                        {
                            taskCompletionSource.SetException(StorageException.From(task.Exception, deletions.Ids()));
                        }
                        else if (task.IsCanceled)
                        {
                            taskCompletionSource.SetCanceled();
                        }
                        else
                        {
                            taskCompletionSource.SetResult(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetException(StorageException.From(ex, deletions.Ids()));
                    }
                }, default, TaskContinuationOptions.ExecuteSynchronously, TaskUtils.Scheduler);

                if (requestTask.Status is TaskStatus.Created)
                {
                    requestTask.Start();
                }
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(StorageException.From(ex, deletions.Ids()));
            }

            return result;

            async Task SendRequestAsync(PayloadDeleteRequest payload)
            {
                requestsInProgress++;
                await WaitForDeleteCooldown(cancellationToken);

                await requestFactory.SendRequestAsync
                (
                    basePath: BasePath,
                    pathParams: DeletePathParams,
                    method: DeleteRequestMethod,
                    body: CoherenceJson.SerializeObject(payload, StorageObject.jsonConverters),
                    headers: null,
                    requestName: $"{nameof(CloudStorage)}.{nameof(DeleteObjectAsync)}",
                    sessionToken:authClient.SessionToken
                );
            }
        }

        private async Task WaitForLoadCooldown(CancellationToken cancellationToken = default) => await throttle.WaitForCooldown(BasePath, LoadRequestMethod, cancellationToken);
        private async Task WaitForDeleteCooldown(CancellationToken cancellationToken = default) => await throttle.WaitForCooldown(BasePath, DeleteRequestMethod, cancellationToken);
        private async Task WaitForSaveCooldown(CancellationToken cancellationToken = default) => await throttle.WaitForCooldown(BasePath, SaveRequestMethod, cancellationToken);

        StorageOperation<StorageObject[]> IStorageOperationBatchProcessor.LoadBatchAsync([DisallowNull] IEnumerable<StorageObjectQuery> queries, CancellationToken cancellationToken)
            => LoadBatchAsync(queries, cancellationToken);

        StorageOperation IStorageOperationBatchProcessor.SaveBatchAsync([DisallowNull] IEnumerable<StorageObjectMutation> mutations, CancellationToken cancellationToken)
            => SaveBatchAsync(mutations, cancellationToken);

        StorageOperation IStorageOperationBatchProcessor.DeleteBatchAsync([DisallowNull] IEnumerable<StorageObjectDeletion> deletions, CancellationToken cancellationToken)
            => DeleteBatchAsync(deletions, cancellationToken);

        /// <summary>
        /// Payload to send to coherence Cloud when requesting objects or items to be loaded.
        /// </summary>
        internal struct PayloadLoadRequest
        {
            [JsonProperty("object_ids")]
            public PayloadStorageObjectId[] object_ids;

            internal struct Response
            {
                [JsonProperty("data")]
                public PayloadStorageObject[] data;

                [JsonProperty("owner_id")]
                public string owner_id;
            }
        }

        [Preserve]
        internal struct PayloadStorageObject
        {
            [JsonProperty("type")]
            public string type;

            [JsonProperty("id")]
            public string id;

            [JsonProperty("data")]
            public object data;

            [JsonProperty("owner_id")]
            public string owner_id;

            [JsonProperty("version")]
            public string version;

            [Preserve]
            public PayloadStorageObject(string type, string id, object data, string ownerId = null, string version = null)
            {
                this.type = type;
                this.id = id;
                this.data = data;
                this.owner_id = ownerId;
                this.version = version;
            }
        }

        [Preserve]
        internal struct PayloadStorageObjectId
        {
            [JsonProperty("type")]
            public string type;

            [JsonProperty("id")]
            public string id;
        }

        /// <summary>
        /// Payload to send to coherence Cloud when requesting objects or items to be saved.
        /// </summary>
        internal struct PayloadSaveRequest
        {
            [JsonProperty("objects")]
            public PayloadStorageObject[] storageObjectMutations;
        }

        /// <summary>
        /// Payload to send to coherence Cloud when requesting objects or items to be deleted.
        /// </summary>
        internal struct PayloadDeleteRequest
        {
            [JsonProperty("object_ids")]
            public PayloadStorageObjectId[] storageObjectIds;
        }

        void IDisposable.Dispose() => _ = DisposeAsync(false);

        internal async ValueTask DisposeAsync(bool waitForOngoingOperationsToFinish)
        {
            if (state is State.Disposed)
            {
                return;
            }

            state = State.Disposing;

            if (waitForOngoingOperationsToFinish)
            {
                while (IsBusy)
                {
                    await Task.Yield();
                }
            }
            else
            {
                operationQueue.CancelAllQueuedOperations();
            }

            state = State.Disposed;
        }

        private enum State
        {
            /// <summary>
            /// The service is in a usable state and is not disposing or disposed.
            /// </summary>
            Active,

            /// <summary>
            /// DisposeAsync(true) has been called, but has not finished yet.
            /// The service still remains in a usable state until DisposeAsync(true) completes.
            /// </summary>
            Disposing,

            /// <summary>
            /// The service has been disposed and can no longer be used.
            /// </summary>
            Disposed
        }

        /// <summary>
        /// This method should never be called. It only exists to help with ahead-of-time (AOT) compilation.
        /// </summary>
        [Preserve]
        private static void AOTFix() => _ = new List<PayloadStorageObject>(); // Json.NET needs to be able to create PayloadStorageObject lists internally
    }
}
