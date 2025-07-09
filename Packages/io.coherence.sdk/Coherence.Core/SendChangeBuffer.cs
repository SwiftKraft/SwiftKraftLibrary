// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using System;
    using System.Collections.Generic;
    using Entities;
    using Serializer;
    using SimulationFrame;
    using System.Linq;

    using Brook;
    using Common;
    using Log;

    internal class SendChangeBuffer : ChangeBuffer
    {
        public const int CREATE_PRIORITY = 100;
        // A create followed by a destroy is automatically handled by updating the change buffer and removing the create if not
        // ever sent.  However, a create followed later by a destroy/create of two entities with the same UUID means that the
        // order of create / destroy is important and the destroys must go first so that the RS doesn't complain about
        // an unexpected created followed by a destroy.
        // See: https://app.zenhub.com/workspaces/engine-group-5fb3b64dabadec002057e6f2/issues/coherence/unity/2708
        public const int DESTROY_PRIORITY = 1000;
        // Because changes aren't stored in any order other than the arbitrary mapping of entity ID we need to add
        // priority to changes that were not sent so that we don't starve out changes just based on their mapping key.
        public const int HELDBACK_PRIORITY = 1000;

        private HashSet<Entity> sentEntities = new HashSet<Entity>(Entity.DefaultComparer);
        private readonly CacheList<(Entity, DeltaComponents)> entitiesWithOrderedComps = new(4);

        private IComponentInfo definition;

        public SendChangeBuffer(IComponentInfo definition, Logger logger) : base(new Dictionary<Entity, OutgoingEntityUpdate>(),
                                        new Queue<SerializedEntityMessage>(),
                                        new Queue<SerializedEntityMessage>(),
                                        new SequenceId(0),
                                        logger)
        {
            this.definition = definition;
        }

        public bool HasChanges(IReadOnlyCollection<Entity> ackedEntities)
        {
            if (HasMessages())
            {
                return true;
            }

            foreach (var (entityId, entityUpdate) in Buffer)
            {
                if (entityUpdate.IsDestroy && !ackedEntities.Contains(entityId))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        public int GetPrioritizedChanges(List<EntityChange> existenceChanges, List<EntityChange> updateChanges, IReadOnlyCollection<Entity> ackedEntities)
        {
            int totalChanges = 0;

            foreach (var (entityId, entityUpdate) in Buffer)
            {
                var change = new EntityChange
                {
                    ID = entityId,
                    Update = entityUpdate,
                    // This was hard-coded in the serializer since this send change
                    // is always from a client and we can assume we're always
                    // state auth if we're even sending it.
                    Meta = new SerializedMeta()
                    {
                        Version = entityId.Version,
                        HasStateAuthority = true,
                        Operation = entityUpdate.Operation,
                        // since this is always from the client,
                        // we can set this reason which is only used if the
                        // state is destroy.
                        DestroyReason = DestroyReason.ClientDestroy,
                    },
                };

                if (entityUpdate.HasExistenceOperation)
                {
                    logger.Trace("GetPrioritizedChanges - adding existence change",
                        ("entity", entityId),
                        ("operation", entityUpdate.Operation));

                    existenceChanges.Add(change);
                }
                else
                {
                    logger.Trace("GetPrioritizedChanges - adding update change",
                        ("entity", entityId));

                    updateChanges.Add(change);
                }

                totalChanges++;
            }

            existenceChanges.Sort((a, b) => (int)(b.Update.Priority - a.Update.Priority));
            updateChanges.Sort((a, b) => (int)(b.Update.Priority - a.Update.Priority));

            return totalChanges;
        }

        public void AppendSentUpdates(ref Dictionary<Entity, OutgoingEntityUpdate> updateMap, IReadOnlyList<Entity> ids)
        {
            if (ids == null || ids.Count <= 0)
            {
                return;
            }

            updateMap ??= new Dictionary<Entity, OutgoingEntityUpdate>(ids.Count);

            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                sentEntities.Add(id);

                updateMap.Add(id, Buffer[id]);
                Buffer.Remove(id);
            }
        }

        public void CreateEntity(EntityCreateChange create)
        {
            var update = CreateEntityUpdate(create.ID);
            update.Operation = EntityOperation.Create;
            update.Priority += CREATE_PRIORITY;

            update.Components.UpdateComponents(create.Data);
            if (create.Data.ContainsOrderedComponent())
            {
                update.Components.OrderedUpdateTime = DateTime.UtcNow;
            }

            Buffer[create.ID] = update;

            logger.Trace("CreateEntity", ("entity", create.ID));
        }

        public void DestroyEntity(Entity id, IReadOnlyCollection<Entity> ackedEntities, long priority = DESTROY_PRIORITY)
        {
            logger.Trace("DestroyEntity", ("entity", id), ("sent", sentEntities.Contains(id)));

            bool canDestroy = sentEntities.Contains(id) || ackedEntities.Contains(id);

            if (canDestroy)
            {
                var update = OutgoingEntityUpdate.New();
                update.Operation = EntityOperation.Destroy;
                update.Priority += priority;
                Buffer[id] = update;

                sentEntities.Remove(id);
            }
            else
            {
                if (Buffer.TryGetValue(id, out var update))
                {
                    Buffer.Remove(id);
                    update.Return();
                }
            }
        }

        public void UpdateEntity(EntityUpdateChange change)
        {
            logger.Trace("UpdateEntity", ("entity", change.ID));

            var update = FindOrCreateEntityUpdate(change.ID);

            update.Components.UpdateComponents(change.Data);
            if (change.Data.ContainsOrderedComponent())
            {
                update.Components.OrderedUpdateTime = DateTime.UtcNow;
            }

            update.Operation = update.Operation.Merge(EntityOperation.Update);
            update.Priority += change.Priority;

            Buffer[change.ID] = update;
        }

        public void RemoveComponent(EntityRemoveChange change)
        {
            logger.Trace("RemoveComponent", ("entity", change.ID));

            var update = FindOrCreateEntityUpdate(change.ID);
            update.Operation = update.Operation.Merge(EntityOperation.Update);
            update.Priority += change.Priority;

            update.Components.RemoveComponents(change.Remove);
            foreach (var removedComp in change.Remove)
            {
                if (definition.IsSendOrderedComponent(removedComp))
                {
                    update.Components.OrderedUpdateTime = DateTime.UtcNow;
                    break;
                }
            }

            Buffer[change.ID] = update;
        }

        /// <summary>
        /// Process updates in dropped Packet/ChangeBuffer.
        /// </summary>
        /// <param name="droppedChanges"> ChangeBuffer representing the packet that was dropped</param>
        /// <param name="unackedChanges">
        /// A Queue of ChangeBuffers representing packets we've sent
        /// but not yet received an ack for
        /// </param>
        /// <param name="simulationFrame"></param>
        /// <param name="ackedEntities"></param>
        public void ResetWithLostChanges(ChangeBuffer droppedChanges,
            LinkedList<ChangeBuffer> unackedChanges,
            IReadOnlyCollection<Entity> ackedEntities)
        {
            logger.Trace("ResetWithLostChanges");

            // re-insert commands, inputs
            foreach (var command in droppedChanges.commandBuffer)
            {
                logger.Trace("Resetting dropped command", ("target", command.TargetEntity));

                commandBuffer.Enqueue(command);
            }

            foreach (var input in droppedChanges.inputBuffer)
            {
                logger.Trace("Resetting dropped input", ("target", input.TargetEntity));

                inputBuffer.Enqueue(input);
            }

            // all entity updates in sent list
            foreach (var (droppedEntityID, droppedEntityUpdate) in droppedChanges.Buffer)
            {
                if (droppedEntityUpdate.IsDestroy)
                {
                    logger.Trace("Resetting dropped destroyed entity",
                        ("entity", droppedEntityID));

                    // deleted entities
                    DestroyEntity(droppedEntityID, ackedEntities, droppedEntityUpdate.Priority);

                    droppedEntityUpdate.Return();
                    continue;
                }

                if (droppedEntityUpdate.IsCreate)
                {
                    // If the drop was a create, we need to merge in all existing changes
                    // and send that new fully complete create or we risk masking out
                    // the create and sending no components. All creates must have all components
                    // sent.

                    for (var unacked = unackedChanges.Last; unacked != null; unacked = unacked.Previous)
                    {
                        if (unacked.Value.Buffer.TryGetValue(droppedEntityID, out var unackedEntityUpdate))
                        {
                            droppedEntityUpdate.Add(unackedEntityUpdate);
                        }

                        unacked.Value.ClearAllChangesForEntity(droppedEntityID);
                    }

                    if (Buffer.TryGetValue(droppedEntityID, out var unsentEntityUpdate))
                    {
                        droppedEntityUpdate.Add(unsentEntityUpdate);

                        // Since we've overwriting the update, return the update to the pool.
                        unsentEntityUpdate.Return();
                    }

                    Buffer[droppedEntityID] = droppedEntityUpdate;

                    continue;
                }

                // Diff with sent, unacked, packets
                foreach (var unackedPacket in unackedChanges)
                {
                    // check in future packets, remove existing updates
                    if (unackedPacket.Buffer.TryGetValue(droppedEntityID, out var unackedEntityUpdate))
                    {
                        droppedEntityUpdate.Subtract(unackedEntityUpdate, definition);
                        if (droppedEntityUpdate.IsDestroy)
                        {
                            // destroyed in future packet, dont care about these changes anymore
                            logger.Trace("Dropping dropped destroyed entity",
                                ("entity", droppedEntityID));
                            break;
                        }
                    }
                }

                if (droppedEntityUpdate.IsDestroy)
                {
                    // The entire update does NOT need to be resend
                    // as the entity was destroyed since this update was sent.
                    continue;
                }

                // Merge unsent changes onto the dropped change and store that as the unsent update
                if (Buffer.TryGetValue(droppedEntityID, out var currentEntityUpdate))
                {
                    logger.Trace("Merging dropped entity changes",
                        ("entity", droppedEntityID));

                    droppedEntityUpdate.Add(currentEntityUpdate);

                    // Since we've overwriting the update, return the update to the pool.
                    currentEntityUpdate.Return();

                    Buffer[droppedEntityID] = droppedEntityUpdate;
                }
                else
                {
                    Buffer.Add(droppedEntityID, droppedEntityUpdate);
                }

                logger.Trace("Updating dropped entity changes",
                    ("entity", droppedEntityID));
            }
        }

        public void Acknowledge(ChangeBuffer ackedChanges, LinkedList<ChangeBuffer> unackedChanges)
        {
            foreach (var (ackedEntity, ackedChange) in ackedChanges.Buffer)
            {
                if (ackedChange.Components.OrderedUpdateTime == null)
                {
                    continue;
                }

                foreach (var inFlightBuffer in unackedChanges)
                {
                    if (!inFlightBuffer.Buffer.TryGetValue(ackedEntity, out var inFlightChange))
                    {
                        continue;
                    }

                    if (inFlightChange.Components.OrderedUpdateTime == ackedChange.Components.OrderedUpdateTime)
                    {
                        inFlightChange.Components.OrderedUpdateTime = null;
                        inFlightBuffer.Buffer[ackedEntity] = inFlightChange;
                    }
                }
            }
        }

        /// <summary>
        /// Adds all in-flight ordered components from the sentCache to the
        /// supplied OutgoingEntityUpdates.
        /// </summary>
        public void ApplyOrderedComponentsFromSent(SentCache sentCache, IComponentInfo componentInfo)
        {
            using var _ = entitiesWithOrderedComps;

            foreach (var kv in Buffer)
            {
                var entity = kv.Key;

                // don't add ordered components if there are no changes to send
                if (Buffer[entity].Components.Count == 0)
                {
                    continue;
                }

                sentCache.GetOrderedComponents(entity, componentInfo, out var orderedComponents);
                if (orderedComponents is { Count: > 0 })
                {
                    entitiesWithOrderedComps.Add((entity, orderedComponents.Value));
                }
            }

            foreach (var (entity, orderedComponents) in entitiesWithOrderedComps)
            {
                var update = Buffer[entity];
                orderedComponents.Merge(update.Components);

                update.Components = orderedComponents;

                Buffer[entity] = update;
            }
        }

        public Dictionary<Entity, OutgoingEntityUpdate> GetEntityMeta()
        {
            var map = new Dictionary<Entity, OutgoingEntityUpdate>();
            foreach (var (entityId, entityUpdate) in Buffer)
            {
                map.Add(entityId, entityUpdate);
            }

            return map;
        }

        public OutgoingEntityUpdate? CopyEntityUpdate(Entity entity)
        {
            if (!Buffer.TryGetValue(entity, out var entityUpdate))
            {
                return null;
            }

            return entityUpdate.Clone();
        }

        private OutgoingEntityUpdate CreateEntityUpdate(Entity id)
        {
            if (Buffer.ContainsKey(id))
            {
                throw new Exception($"Can't create entity {id}, already exists");
            }

            logger.Trace("CreateEntityUpdate", ("entity", id));

            var update = OutgoingEntityUpdate.New();

            Buffer[id] = update;

            return update;
        }

        // <summary>
        // Finds or creates an entity update.  The lazy create in for situations like
        // updating an entity that was created and has been sent and, thus, cleared
        // from the change buffer, or entities that were created via authority change.
        // </summary>
        private OutgoingEntityUpdate FindOrCreateEntityUpdate(Entity id)
        {
            if (Buffer.TryGetValue(id, out var entityUpdate))
            {
                return entityUpdate;
            }

            logger.Trace("FindOrCreateEntityUpdate -- lazy create", ("entity", id));

            return CreateEntityUpdate(id);
        }
    }
}
