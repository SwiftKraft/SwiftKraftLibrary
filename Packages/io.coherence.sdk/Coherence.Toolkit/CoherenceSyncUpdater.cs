// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Bindings;
    using Entities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Log;
    using Logger = Log.Logger;
    using Coherence.Toolkit.Bindings.TransformBindings;
    using Coherence.SimulationFrame;

    internal class CoherenceSyncUpdater : ICoherenceSyncUpdater
    {
        public Logger logger { get; set; }

        public bool TaggedForNetworkedDestruction { get; set; }

        public bool ChangedConnection => changedConnection;
        public Entity NewConnection => newConnection;

        private bool hasTriggeredMissingBridgeWarning;
        private string lastSerializedCoherenceUUID = string.Empty;
        private string lastSerializedTag = string.Empty;

        readonly private ICoherenceSync coherenceSync;
        readonly private IClient client;

        private const float minTimeBetweenAdoptionRequests = 0.5f;

        readonly private List<Binding> valueBindings = new List<Binding>();
        readonly private Dictionary<string, List<Binding>> valueBindingsByComponent = new Dictionary<string, List<Binding>>();
        private PositionBinding positionBinding;

        private bool initialSampleDone = false;
        private bool initialSyncDone = false;
        private bool changedConnection;
        private Entity newConnection;
        private bool didChangeParent = false;

        readonly private List<ICoherenceComponentData> queuedUpdates = new List<ICoherenceComponentData>();

        public CoherenceSyncUpdater(ICoherenceSync coherenceSync, IClient client)
        {
            logger = Log.GetLogger<CoherenceSyncUpdater>(coherenceSync);

            this.coherenceSync = coherenceSync;
            this.client = client;
            UpdateValueBindings();
        }

        public void InterpolateBindings()
        {
            if (!EnsureEntityInitializedAndReady() || client == null)
            {
                return;
            }

            if (!coherenceSync.EntityState.HasStateAuthority)
            {
                PerformInterpolationOnAllBindings();
            }
        }

        public void InvokeCallbacks()
        {
            if (!EnsureEntityInitializedAndReady())
            {
                return;
            }

            if (client == null)
            {
                return;
            }

            if (coherenceSync.EntityState.HasStateAuthority)
            {
                return;
            }

            InvokeValueSyncCallbacksOnAllBindings();

            if (didChangeParent)
            {
                coherenceSync.RaiseOnConnectedEntityChanged();

                didChangeParent = false;
            }
        }

        public void SampleBindings()
        {
            if (!EnsureEntityInitializedAndReady() || client == null)
            {
                return;
            }

            if (coherenceSync.EntityState.HasStateAuthority)
            {
                // This might look wrong, but we need to sample (and thus also "send") the connected
                // entity in the same loop as the position (and rotation and scale) bindings.
                // This is important in case the bindings are sampled in the FixedUpdate loop,
                // which can be invoked multiple times per frame, thus making multiple samples
                // in the different coordinate system (under a new parent), before we can
                // sample the new parent (and transform the samples coordinate system).
                coherenceSync.SendConnectedEntity();

                SampleAllBindings();
            }
        }

        public void SyncAndSend()
        {
            if (!EnsureEntityInitializedAndReady())
            {
                return;
            }

            if (coherenceSync.EntityState.HasStateAuthority)
            {
                SendTag();

                // Ensure all bindings are flushed during the first update,
                // to take any changes happened during this frame since creation of the entity.
                if (!initialSampleDone)
                {
                    SampleAllBindings();
                }

                var forceSerialize = !initialSyncDone;
                SendComponentUpdates(forceSerialize);
            }
            else
            {
                coherenceSync.ValidateConnectedEntity();
            }

            if (coherenceSync.EntityState.HasInputAuthority)
            {
                if (coherenceSync.Input != null && !coherenceSync.Input.UseFixedSimulationFrames)
                {
                    coherenceSync.BakedScript.SendInputState();
                }
            }

            ProcessOrphanedBehavior();
            ProcessInitialSync();
        }

        public void GetComponentUpdates(List<ICoherenceComponentData> updates, bool forceSerialize = false)
        {
            var currentTime = coherenceSync.CoherenceBridge?.NetworkTime?.TimeAsDouble ?? 0f;

            var simulationFrame = coherenceSync.CoherenceBridge?.NetworkTime?.ClientSimulationFrame ?? 0;

            // A dirty dirty hack. Bindings are now actually sampled at the end of a FixedUpdate
            // in case the InterpolationLocationConfig is set to FixedUpdate. But, since UnityEngine.Time.timeAsDouble
            // actually increases between FixedUpdate and Update, when we try to serialize a binding and interpolate
            // the sampled values, the NetworkTime.ClientSimulationFrame will most of the time be ahead of the latest
            // sample time. So we will constantly have to overshoot/extrapolate. This line decreases the simFrame by one
            // so we always interpolate between existing samples, and will unfortunately lead to a bigger latency.
            // This line should be removed when all of these are implemented:
            //  - Each binding having its own simFrame instead of sharing one: https://github.com/coherence/unity/issues/4332
            //  - Bindings which are sampled in FixedUpdate should be serialized with a simFrame from the last FixedUpdate: https://github.com/coherence/engine/issues/2070
            if (coherenceSync.InterpolationLocationConfig == CoherenceSync.InterpolationLoop.FixedUpdate)
            {
                simulationFrame -= 1;
            }

            bool invalidBindings = false;

            foreach (var kvp in valueBindingsByComponent)
            {
                var group = kvp.Value;
                var groupComponent = group[0];

                if (!groupComponent.IsValid)
                {
                    var rootObjectName = groupComponent.coherenceSync?.name;
                    logger.Error(Error.ToolkitSyncUpdateInvalidBindingGroup,
                        $"Invalid binding on \"{rootObjectName}\" for component group \"{kvp.Key}\".\nDid you delete a component or child object of a synced gameObject?");
                    invalidBindings = true;
                    continue;
                }

                ICoherenceComponentData update = null;
                uint stopMask = 0;

                foreach (var binding in group)
                {
                    if (!forceSerialize)
                    {
                        if (!binding.IsReadyToSample(currentTime))
                        {
                            continue;
                        }
                    }

                    binding.IsDirty(simulationFrame, out var dirty, out var justStopped);
                    if (justStopped)
                    {
                        stopMask |= binding.FieldMask;
                    }

                    // When the binding is just stopped,
                    // we send the last sample along with the
                    // stop state even if it's not dirty.
                    if (!dirty && !justStopped)
                    {
                        continue;
                    }

                    update ??= groupComponent.CreateComponentData();
                    update = SerializeBinding(binding, update, simulationFrame);
                    update.FieldsMask |= binding.FieldMask;
                }

                if (update != null && update.FieldsMask != 0)
                {
                    update.StoppedMask = stopMask;

                    updates.Add(update);
                }
            }

            if (coherenceSync.EntityState != null && coherenceSync.EntityState.CoherenceUUID != lastSerializedCoherenceUUID)
            {
                var update = Impl.GetRootDefinition().GenerateCoherenceUUIDData(coherenceSync.EntityState.CoherenceUUID, simulationFrame);

                updates.Add(update);

                lastSerializedCoherenceUUID = coherenceSync.EntityState.CoherenceUUID;
            }

            if (invalidBindings)
            {
                UpdateValueBindings();
            }
        }

        public void PerformInterpolationOnAllBindings()
        {
            coherenceSync.ApplyNodeBindings();

            if (changedConnection)
            {
                if (coherenceSync.ConnectedEntityChanged(newConnection, out var didChangeParent))
                {
                    changedConnection = false;

                    this.didChangeParent |= didChangeParent;
                }
            }

            foreach (var binding in valueBindings)
            {
                if (binding.IsValid && !binding.IsCurrentlyPredicted())
                {
                    binding.Interpolate(coherenceSync.CoherenceBridge.NetworkTimeAsDouble);
                }
            }
        }

        public void SampleAllBindings()
        {
            initialSampleDone = true;

            foreach (var binding in valueBindings)
            {
                if (binding.IsValid)
                {
                    binding.SampleValue();
                }
            }
        }

        private void InvokeValueSyncCallbacksOnAllBindings()
        {
            foreach (var binding in valueBindings)
            {
                binding.InvokeValueSyncCallback();
            }
        }

        public void ClearAllSampleTimes()
        {
            foreach (var binding in valueBindings)
            {
                binding.ClearSampleTime();
            }
        }

        public void OnConnectedEntityChanged()
        {
            foreach (var binding in valueBindings)
            {
                binding.OnConnectedEntityChanged();
            }
        }

        /// <summary>
        ///     Sends changes on all bindings manually instead of waiting for the update.
        ///     External use only when the CoherenceSync behaviour is disabled.
        ///     The sending of the packet containing these changes is still dependant on the packet send rate.
        ///     Do no call before the entity has been registered with the client, which will happen after the
        ///     First update after the client is connected and the CoherenceSync behaviour is enabled.
        ///     If called before the entity is registered with the client updates will be lost.
        /// </summary>
        public void ManuallySendAllChanges(bool sampleValuesBeforeSending)
        {
            if (coherenceSync.EntityState?.HasStateAuthority ?? true)
            {
                ClearAllSampleTimes();

                if (sampleValuesBeforeSending)
                {
                    SampleAllBindings();
                }

                SendComponentUpdates();
            }
        }

        public void ApplyComponentDestroys(HashSet<uint> destroyedComponents)
        {
            foreach (var componentId in destroyedComponents)
            {
                if (componentId == Impl.GetConnectedEntityComponentIdInternal())
                {
                    // Destroying the ConnectedEntity component will cause the GameObject to detach from its parent.
                    if (coherenceSync.ConnectedEntity != null)
                    {
                        changedConnection = true;
                        newConnection = Entity.InvalidRelative;
                    }
                }
                else
                {
                    logger.Warning(Warning.ToolkitSyncUpdateDestroyNotSupported,
                        $"Destroy component is not supported: {coherenceSync.name} ComponentType: {componentId}");
                }
            }
        }

        public void ApplyComponentUpdates(ComponentUpdates componentUpdates)
        {
            for (int i = 0; i < componentUpdates.Store.SortedValues.Count; i++)
            {
                ApplySingleUpdate(componentUpdates.Store.SortedValues[i], componentUpdates.FloatingOriginDelta.ToUnityVector3());
            }

            // If we get any updates then this is a replicated entity and we can set the
            // initial sample as sampled since that's what we've received.
            initialSampleDone = true;
        }

        private void ApplySingleUpdate(ComponentChange change, Vector3 floatingOriginDelta)
        {
            var newComponentData = change.Data;
            var componentTypeId = newComponentData.GetComponentType();
            var componentName = Impl.ComponentNameFromTypeId(componentTypeId);
            var clientFrame = coherenceSync.CoherenceBridge.NetworkTime?.ClientSimulationFrame ?? default;

            logger.Trace($"Comp: {componentTypeId} Frame: {clientFrame}");

            if (ApplyInternalUpdate(componentName, newComponentData))
            {
                return;
            }

            if (!valueBindingsByComponent.TryGetValue(componentName, out var group))
            {
                logger.Warning(Warning.ToolkitSyncUpdateMissingComponent,
                    $"We can't find any binding for component '{componentName}' when receiving a component update.");
                return;
            }

            foreach (var binding in group)
            {
                var hasChanges = (newComponentData.FieldsMask & binding.FieldMask) != 0;
                if (hasChanges)
                {
                    binding.ReceiveComponentData(newComponentData, clientFrame, floatingOriginDelta);
                }
            }
        }

        private bool ApplyInternalUpdate(string componentName, ICoherenceComponentData newComponentData)
        {
            switch (componentName)
            {
                case "UniqueID":
                    coherenceSync.EntityState.CoherenceUUID = lastSerializedCoherenceUUID = Impl.GetRootDefinition().ExtractCoherenceUUID(newComponentData);
                    return true;
                case "ConnectedEntity":
                    newConnection = Impl.GetRootDefinition().ExtractConnectedEntityID(newComponentData);
                    changedConnection = true;
                    return true;
                case "Tag":
                    coherenceSync.CoherenceTag = lastSerializedTag = Impl.GetRootDefinition().ExtractCoherenceTag(newComponentData);
                    return true;
                case "Scene":
                    throw new Exception("Scene updates should be filtered out by the replication server.");
                case "Persistence":
                case "Connection":
                case "Global":
                case "GlobalQuery":
                case "WorldPositionQuery":
                case "TagQuery":
                case "WorldPositionComponent":
                case "ArchetypeComponent":
                case "ConnectionScene":
                case "PreserveChildren":
                    return true;
            }

            return false;
        }

        private bool EnsureEntityInitializedAndReady()
        {
            if (TaggedForNetworkedDestruction)
            {
                return false;
            }

            if (coherenceSync.CoherenceBridge == null)
            {
                if (!hasTriggeredMissingBridgeWarning)
                {
                    logger.Warning(Warning.ToolkitSyncUpdateMissingBridge,
                        $"No CoherenceBridge instance was found, '{coherenceSync.name}' will not be able to sync.");
                    hasTriggeredMissingBridgeWarning = true;
                }

                return false;
            }

            return Application.isPlaying && coherenceSync.EntityState != null && coherenceSync.CoherenceBridge.IsConnected;
        }

        private void ProcessOrphanedBehavior()
        {
            if (coherenceSync.EntityState == null || !coherenceSync.EntityState.IsOrphaned)
            {
                return;
            }

            switch (coherenceSync.OrphanedBehaviorConfig)
            {
                case CoherenceSync.OrphanedBehavior.DoNothing:
                    break;
                case CoherenceSync.OrphanedBehavior.AutoAdopt:
                    if (Time.time - coherenceSync.EntityState.LastTimeRequestedOrphanAdoption > minTimeBetweenAdoptionRequests)
                    {
                        coherenceSync.CoherenceBridge.AuthorityManager.Adopt(coherenceSync.EntityState);
                    }
                    break;
            }
        }

        private void ProcessInitialSync()
        {
            if (initialSyncDone)
            {
                return;
            }

            initialSyncDone = true;

            if (valueBindings.Exists(b => b.SyncMode == SyncMode.CreationOnly))
            {
                UpdateValueBindings();
            }
        }

        private void UpdateValueBindings()
        {
            valueBindings.Clear();
            valueBindingsByComponent.Clear();

            foreach (var b in coherenceSync.Bindings)
            {
                if (b is null || !b.IsValid || b.IsMethod)
                {
                    continue;
                }

                if (initialSyncDone && b.SyncMode == SyncMode.CreationOnly)
                {
                    continue;
                }

                valueBindings.Add(b);
            }

            foreach (var group in valueBindings.GroupBy(b => b.CoherenceComponentName))
            {
                foreach (var binding in group)
                {
                    if (!valueBindingsByComponent.ContainsKey(binding.CoherenceComponentName))
                    {
                        valueBindingsByComponent.Add(binding.CoherenceComponentName, new List<Binding>());
                    }

                    valueBindingsByComponent[binding.CoherenceComponentName].Add(binding);
                }
            }

            positionBinding = (PositionBinding)coherenceSync.Bindings.FirstOrDefault(b => b is PositionBinding);
        }

        private ICoherenceComponentData SerializeBinding(Binding binding, ICoherenceComponentData inst, AbsoluteSimulationFrame simulationFrame)
        {
            if (!binding.unityComponent)
            {
                logger.Warning(Warning.ToolkitSyncUpdateBindingNull,
                    "Component is null for " + binding.Name);
                return default;
            }

            try
            {
                return binding.WriteComponentData(inst, simulationFrame);
            }
            catch (Exception e)
            {
                logger.Error(Error.ToolkitSyncUpdateException,
                    $"We can't serialize the binding '{binding.Name}'",
                    ("context", binding.unityComponent),
                    ("error", e.ToString()));
                return default;
            }
        }

        public void SendTag()
        {
            if (coherenceSync.CoherenceTag == lastSerializedTag)
            {
                return;
            }

            if (string.IsNullOrEmpty(coherenceSync.CoherenceTag))
            {
                Impl.RemoveTag(client, coherenceSync.EntityState.EntityID);
            }
            else
            {
                Impl.UpdateTag(client, coherenceSync.EntityState.EntityID, coherenceSync.CoherenceTag,
                    coherenceSync.CoherenceBridge.NetworkTime.ClientSimulationFrame);
            }

            lastSerializedTag = coherenceSync.CoherenceTag;
        }

        private void SendComponentUpdates(bool forceSerialize = false)
        {
            if (!client.CanSendUpdates(coherenceSync.EntityState.EntityID))
            {
                // Don't process this change until the client allows it.
                // Likely in the middle of an authority update.
                return;
            }

            queuedUpdates.Clear();

            GetComponentUpdates(queuedUpdates, forceSerialize);
            if (queuedUpdates.Count > 0)
            {
                client.UpdateComponents(coherenceSync.EntityState.EntityID, queuedUpdates.ToArray());
            }
        }

        public bool TryFlushPosition(bool sampleValueBeforeSending)
        {
            if (positionBinding == null)
            {
                return false;
            }

            positionBinding.MarkAsReadyToSend();

            if (sampleValueBeforeSending)
            {
                positionBinding.SampleValue();
            }

            SendComponentUpdates();

            return true;
        }
    }
}
