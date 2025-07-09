namespace Coherence.Toolkit
{
    using System.Collections.Generic;
    using Logger = Log.Logger;
    using System;

    /// <summary>
    /// Handles resolution of entity uniqueness.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="CoherenceBridge"/>, <see cref="CoherenceSync"/> and <see cref="EntitiesManager"/>.
    /// </remarks>
    public class UniquenessManager : IDisposable
    {
        private Dictionary<string, UniqueObjectReplacement> uniqueObjectReplacementDict = new();

        private Queue<string> registeredUniqueIds = new();

        private readonly Logger logger;

        internal UniquenessManager(Logger logger)
        {
            this.logger = logger.With<UniquenessManager>();
        }

        /// <summary>
        /// Registers a unique identifier to be later applied by <see cref="CoherenceSync"/>.
        /// </summary>
        /// <remarks>
        /// Internally, a queue is maintained.
        /// Multiple unique identifiers can be queued at any time.
        ///
        /// Queued identifiers are dequeued by <see cref="CoherenceSync"/>'s OnEnable event (see <see cref="UnityEngine.MonoBehaviour"/>).
        /// If the queue contains items while a <see cref="CoherenceSync"/> goes through the OnEnable event, that entity
        /// will get that unique identifier assigned, instead of it's predetermined identifier - <see cref="CoherenceSync.ManualUniqueId"/>.
        ///
        /// Empty or <see langword="null"/> identifiers are ignored, and <see cref="CoherenceSync.ManualUniqueId"/> is used instead.
        /// </remarks>
        /// <param name="uniqueIdentifier">The unique identifier to register.</param>
        /// <seealso cref="CoherenceSync.ManualUniqueId"/>
        public void RegisterUniqueId(string uniqueIdentifier)
        {
            if (string.IsNullOrEmpty(uniqueIdentifier))
            {
                return;
            }

            registeredUniqueIds.Enqueue(uniqueIdentifier);
        }

        internal string GetUniqueId()
        {
            if (registeredUniqueIds.Count > 0)
            {
                return registeredUniqueIds.Dequeue();
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets a <see cref="CoherenceSync"/> associated with a unique identifier.
        /// </summary>
        /// <param name="uuid">The unique identifier</param>
        // TODO refactor
        public UniqueObjectReplacement TryGetUniqueObject(string uuid)
        {
            uniqueObjectReplacementDict.TryGetValue(uuid, out var uniqueObject);

            return uniqueObject;
        }

        internal bool FindUniqueObjectForNewRemoteNetworkEntity(SpawnInfo info, Action onBeforeLocalObjectInit)
        {
            if (!string.IsNullOrEmpty(info.uniqueId) &&
                uniqueObjectReplacementDict.TryGetValue(info.uniqueId,
                    out UniqueObjectReplacement localSync))
            {
                localSync.localObjectInit = sync =>
                {
                    logger.Trace($"Found unique object for uuid {info.uniqueId}");

                    sync.InitializeReplacedUniqueObject(info);
                };

                return CheckUniqueObjectReplacement(info.uniqueId, onBeforeLocalObjectInit);
            }

            return false;
        }

        internal bool RegisterUniqueCoherenceSyncAndDestroyIfDuplicate(ICoherenceSync sync, string uuid)
        {
            if (sync.IsUnique)
            {
                if (string.IsNullOrEmpty(uuid))
                {
                    logger.Error(Log.Error.ToolkitUniquenessUUIDInvalid,
                        $"A unique object for {sync.name} has null/empty UUID, so it can't be registered as a unique object." +
                        $" You can register a runtime unique ID by calling the method CoherenceBridge.UniquenessManager.RegisterUniqueId");
                }

                if (uniqueObjectReplacementDict.TryGetValue(uuid, out UniqueObjectReplacement localSync))
                {
                    if (localSync.localObject == sync)
                    {
                        return false;
                    }

                    if (localSync.localObject.gameObject.activeSelf)
                    {
                        logger.Debug($"A unique object (with UUID '{uuid}') already exists, we will destroy its duplicate.", ("context", sync));

                        //Dont call destroy directly from here, as CoherenceSync OnDestroy doesn't know it was destroyed because it was a duplicate, which means it will remove itself from the dict
                        //Prefabs destroyed because they are duplicates, should not remove anything from dict
                        sync.DestroyAsDuplicate();

                        return true;
                    }
                    else
                    {
                        // If the existing unique entity is inactive, we need to destroy it and let the new one take its place
                        // because the unique entity might not exist on the RS anymore and we are storing
                        // a unique instance in a disabled state. See: https://github.com/coherence/unity/issues/7292

                        localSync.localObject.DestroyAsDuplicate();
                        RemoveEntityFromObjectReplacementDict(uuid);
                    }
                }

                var uniqueObjectReplacement = new UniqueObjectReplacement { localObject = sync };

                uniqueObjectReplacementDict[uuid] = uniqueObjectReplacement;
            }

            return false;
        }

        internal bool ReplaceRemoteDuplicatedEntity(ICoherenceSync sync, NetworkEntityState entity)
        {
            if (!string.IsNullOrEmpty(entity.CoherenceUUID) &&
                uniqueObjectReplacementDict.TryGetValue(entity.CoherenceUUID, out UniqueObjectReplacement localSync))
            {
                localSync.localObject = sync;

                CheckUniqueObjectReplacement(entity.CoherenceUUID, null);
                return true;
            }

            return false;
        }

        internal void OnUniqueObjectDestroyed(string uuid)
        {
            RemoveEntityFromObjectReplacementDict(uuid);
        }

        private bool CheckUniqueObjectReplacement(string uuid, Action onBeforeLocalObjectInit)
        {
            if (!string.IsNullOrEmpty(uuid) &&
                uniqueObjectReplacementDict.TryGetValue(uuid, out UniqueObjectReplacement localSync))
            {
                if (localSync.ReplaceReady)
                {
                    onBeforeLocalObjectInit?.Invoke();

                    localSync.localObjectInit(localSync.localObject);
                    localSync.localObjectInit = null;

                    return true;
                }
            }

            return false;
        }

        private void RemoveEntityFromObjectReplacementDict(string uuid)
        {
            if (!string.IsNullOrEmpty(uuid) &&
                uniqueObjectReplacementDict.ContainsKey(uuid))
            {
                uniqueObjectReplacementDict.Remove(uuid);
            }
        }

        void IDisposable.Dispose() => logger.Dispose();
    }
}
