// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using Logger = Log.Logger;
    using Log;
    using System.Collections;

    [AddComponentMenu("coherence/Prefab Sync Group")]
    [DefaultExecutionOrder(ScriptExecutionOrder.SyncGroup)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CoherenceSync))]
    public class PrefabSyncGroup : MonoBehaviour
    {
        // for components, we don't expose direct creation of instances - add as component instead
        private PrefabSyncGroup()
        {
        }

        [Sync, OnValueSynced(nameof(OnReceivedIds))]
        public byte[] ids;

        private List<CoherenceSync> childCoherenceSyncs = new List<CoherenceSync>();

        private int numberOfChildren;

        private CoherenceSync sync;
        private Logger logger;
        private Logger lastSyncLogger;

        // this is a fixed size because we know we're using a 32bit value as the string.
        private const int idByteLen = 4;

        // The CS logger changes depending on when it is connected, so this behaviour
        // should change with it, since they're closely tied.
        private Logger Logger()
        {
            if (!sync || sync.logger == null)
            {
                logger = Log.GetLogger<PrefabSyncGroup>();
            }
            else if (lastSyncLogger != sync.logger)
            {
                logger = sync.logger.With<PrefabSyncGroup>();
                lastSyncLogger = sync.logger;
            }

            return logger;
        }

        private void Awake()
        {
            gameObject.TryGetComponent<CoherenceSync>(out sync);
        }

        private void OnEnable()
        {
            GetChildCoherenceSyncs();

            numberOfChildren = childCoherenceSyncs.Count;

            ids = new byte[numberOfChildren * idByteLen];

            for (int i = 0; i < numberOfChildren; i++)
            {
                var id = Guid.NewGuid().GetHashCode();
                var idBytes = BitConverter.GetBytes(id);
                idBytes.CopyTo(ids, i * idByteLen);

                var sync = childCoherenceSyncs[i];
                if (sync.transform.parent != transform.parent)
                {
                    sync.enabled = false;
                    sync.UnsyncedEntityPriority = CoherenceSync.UnsyncedNetworkEntityPriority.UniqueId;
                    sync.uniquenessType = CoherenceSync.UniquenessType.NoDuplicates;
                    sync.ManualUniqueId = BitConverter.ToString(idBytes);
                }
            }

            // we delay enabling coherence sync children to ensure the updates to the ids parameter have been correctly processed
            StartCoroutine(EnableChildrenRoutine());
        }

        private IEnumerator EnableChildrenRoutine()
        {
            yield return null;

            var hasDisabled = true;

            while (hasDisabled)
            {
                hasDisabled = false;

                for (var i = 0; i < numberOfChildren; i++)
                {
                    var childSync = childCoherenceSyncs[i];
                    if (!childSync.enabled)
                    {
                        // If we have state authority, that means that the child sync can be enabled to be replicated
                        // else it means that we are on remote client and we need to wait for the child entity to be received
                        // before enabling it (or else we could steal the authority if our create of the child reaches the RS first).
                        if (this.sync.HasStateAuthority ||
                            this.sync.CoherenceBridge.EntitiesManager.unsyncedNetworkEntitiesByUniqueId.ContainsKey(childSync.ManualUniqueId))
                        {
                            childSync.enabled = true;
                        }
                        else
                        {
                            hasDisabled = true;
                        }
                    }
                }

                yield return null;
            }
        }

        private void OnReceivedIds(byte[] old, byte[] newIds)
        {
            GetChildCoherenceSyncs();

            var numIDs = newIds.Length / idByteLen;

            if (numIDs > childCoherenceSyncs.Count)
            {
                Logger().Error(Error.ToolkitPrefabSyncGroupIDs,
                    ("got", numIDs),
                    ("expected", childCoherenceSyncs.Count));

                return;
            }

            for (var i = 0; i < numIDs; i++)
            {
                var sync = childCoherenceSyncs[i];

                if (sync.transform.parent != transform.parent)
                {
                    var idBytes = new byte[idByteLen];
                    Array.Copy(newIds, i * idByteLen, idBytes, 0, idByteLen);
                    childCoherenceSyncs[i].ManualUniqueId = BitConverter.ToString(idBytes);
                }
            }
        }

        private void GetChildCoherenceSyncs()
        {
            childCoherenceSyncs.Clear();
            GetComponentsInChildren(true, childCoherenceSyncs);

            for (int i = childCoherenceSyncs.Count - 1; i >= 0; i--)
            {
                if (childCoherenceSyncs[i].transform.parent == transform.parent)
                {
                    childCoherenceSyncs.RemoveAt(i);
                    break;
                }
            }
        }

        private void OnValidate()
        {
            var parent = transform.parent;
            if (parent == null || parent.name.Equals("Canvas (Environment)") || !enabled)
            {
                return;
            }

            Logger().Error(Error.ToolkitPrefabSyncGroupValidation,
                $"{nameof(PrefabSyncGroup)} Component should be at the root of the Prefab. " +
                "If you are using this Prefab as a nested instance in other Prefab Sync Group, please disable this Component in the Prefab instance.");
        }
    }
}
