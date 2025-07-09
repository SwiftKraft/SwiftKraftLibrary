// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using Common;
    using Entities;
    using Log;
    using ProtocolDef;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // TODO: Merge with RSL counterpart
    // Based on the pkg/entitymanager/InBuffer
    internal class ReceiveChangeBuffer
    {
        public static readonly TimeSpan MessageTTL = TimeSpan.FromSeconds(5f);

        private readonly Dictionary<Entity, IncomingEntityUpdate> entityDataByID = new(32);

        private readonly List<ExpirableMessage> commands = new List<ExpirableMessage>(32);
        private readonly List<ExpirableMessage> inputs = new List<ExpirableMessage>(32);

        private readonly List<Entity> entitiesTaken = new List<Entity>(32);
        private readonly List<RefsInfo> refsToResolve = new List<RefsInfo>(8);

        private readonly IEntityRegistry entityRegistry;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly Logger logger;

        public ReceiveChangeBuffer(IEntityRegistry entityRegistry, Logger logger, IDateTimeProvider dateTimeProvider = null)
        {
            this.entityRegistry = entityRegistry;
            this.dateTimeProvider = dateTimeProvider ?? new SystemDateTimeProvider();
            this.logger = logger.With<ReceiveChangeBuffer>();
        }

        public void Clear()
        {
            logger.Trace("Clear");

            entityDataByID.Clear();
            commands.Clear();
            inputs.Clear();
        }

        public void AddChange(in IncomingEntityUpdate change)
        {
            Entity entity = change.Entity;

            bool changeExists = entityDataByID.TryGetValue(entity, out var existingChange);

            logger.Trace("Adding change",
                ("entity", entity),
                ("exists", changeExists),
                ("change", change));

            if (changeExists)
            {
                existingChange.Merge(change);
                entityDataByID[entity] = existingChange;
            }
            else
            {
                entityDataByID.Add(entity, change);
            }

            if (change.IsDestroy)
            {
                HandleMessagesReferencingDestroyedEntity(entity, commands);
                HandleMessagesReferencingDestroyedEntity(entity, inputs);
            }
        }

        public void AddCommand(IEntityMessage command)
        {
            logger.Trace("Adding command",
                ("entity", command.Entity),
                ("type", command.GetType()),
                ("sender", command.Sender),
                ("routing", command.Routing));

            commands.Add(new ExpirableMessage(command, dateTimeProvider.UtcNow + MessageTTL));
        }

        public void AddInput(IEntityMessage input)
        {
            logger.Trace("Adding input",
                ("entity", input.Entity),
                ("type", input.GetType()),
                ("sender", input.Sender),
                ("routing", input.Routing));

            inputs.Add(new ExpirableMessage(input, dateTimeProvider.UtcNow + MessageTTL));
        }

        public List<RefsInfo> GetRefsInfos()
        {
            refsToResolve.Clear();

            foreach (var update in entityDataByID.Values)
            {
                // Ignore partial updates
                if (IsPartialUpdate(update))
                {
                    continue;
                }

                if (update.Meta.Operation == EntityOperation.Destroy)
                {
                    continue;
                }

                // Gather reference information
                var refsInfo = new RefsInfo(update);
                refsToResolve.Add(refsInfo);
            }

            return refsToResolve;
        }

        /// <summary>
        /// Moves resolvable IncomingEntityUpdates into the given buffer.
        /// Leftover updates stay in the ChangeBuffer.
        /// </summary>
        /// <param name="buffer">List of IncomingEntityUpdate to write valid updates into.</param>
        /// <param name="resolvableEntities">Set of Entities which are resolvable (not referencing an unknown entity).</param>
        public void TakeUpdates(List<IncomingEntityUpdate> buffer, IReadOnlyCollection<Entity> resolvableEntities)
        {
            refsToResolve.Clear();
            entitiesTaken.Clear();

            foreach (var update in entityDataByID.Values)
            {
                // Ignore partial updates
                if (IsPartialUpdate(update))
                {
                    continue;
                }

                // Destroy can't come with any other change
                // and so can be handled immediately
                if (HandleDestroy(update, buffer))
                {
                    continue;
                }

                if (resolvableEntities.Contains(update.Entity))
                {
                    buffer.Add(update);
                    entitiesTaken.Add(update.Entity);
                }
            }

            foreach (var takenEntity in entitiesTaken)
            {
                entityDataByID.Remove(takenEntity);
            }

            // Sort the changes first by the operation type and then by the number of entity reference fields.
            // It is !!mandatory!! that creates are before updates in case the update references the created entity.
            // After that, same operation changes are sorted by the number of references so that the changes with
            // least references are first, so there is a smaller chance of pushing entities in the wrong order out of the core.
            // IMPORTANT: this doesn't fix the problem of SDK receiving create with
            // reference to non-existing entity because entities might have a cyclic
            // reference. The real fix for that must be on the SDK level.
            SortChanges(buffer);
        }

        private static void SortChanges(List<IncomingEntityUpdate> buff)
        {
            buff.Sort((lhs, rhs) =>
            {
                var operationDiff = GetOperationPriority(rhs.Meta.Operation) - GetOperationPriority(lhs.Meta.Operation);
                if (operationDiff != 0)
                {
                    return operationDiff;
                }

                return GetNumberOfRefs(lhs) - GetNumberOfRefs(rhs);
            });
        }

        private static int GetOperationPriority(EntityOperation operation)
        {
            return operation switch
            {
                EntityOperation.Create => 3,
                EntityOperation.Destroy => 2,
                EntityOperation.Update => 1,
                EntityOperation.Unknown => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };
        }

        private static int GetNumberOfRefs(IncomingEntityUpdate update)
        {
            var result = 0;

            var values = update.Components.Updates.Store.SortedValues;
            for (var i = 0; i < values.Count; i++)
            {
                var change = values[i];
                if (change.Data.HasRefFields())
                {
                    var entityRefs = change.Data.GetEntityRefs();
                    result += entityRefs.Count;
                }
            }

            return result;
        }

        public void TakeCommands(List<IEntityMessage> buffer, IReadOnlyCollection<Entity> resolvableEntities)
        {
            TakeMessages(buffer, commands, resolvableEntities);
        }

        public void TakeInputs(List<IEntityMessage> buffer, IReadOnlyCollection<Entity> resolvableEntities)
        {
            TakeMessages(buffer, inputs, resolvableEntities);
        }

        private void TakeMessages(List<IEntityMessage> buffer, List<ExpirableMessage> source, IReadOnlyCollection<Entity> resolvableEntities)
        {
            if (source.Count == 0)
            {
                return;
            }

            DateTime now = dateTimeProvider.UtcNow;

            for (int i = source.Count - 1; i >= 0; i--)
            {
                IEntityMessage message = source[i].Message;

                if (IsMessageResolvable(message, resolvableEntities))
                {
                    buffer.Add(message);
                    source.RemoveAt(i);
                    continue;
                }

                if (source[i].HasExpired(now))
                {
                    source.RemoveAt(i);

                    logger.Debug("Message expired",
                        ("entity", message.Entity),
                        ("type", message.GetType()),
                        ("sender", message.Sender),
                        ("routing", message.Routing));
                }
            }
        }

        private bool IsMessageResolvable(IEntityMessage message, IReadOnlyCollection<Entity> resolvableEntities)
        {
            if (!entityRegistry.EntityExists(message.Entity) && !resolvableEntities.Contains(message.Entity))
            {
                return false;
            }

            var entityRefs = message.GetEntityRefs();
            if (entityRefs == null)
            {
                return true;
            }

            foreach (var entity in message.GetEntityRefs())
            {
                if (!entity.IsValid)
                {
                    continue;
                }

                if (!entityRegistry.EntityExists(entity) && !resolvableEntities.Contains(entity))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsPartialUpdate(in IncomingEntityUpdate update)
        {
            if (update.Meta.Operation == EntityOperation.Create || update.Meta.Operation == EntityOperation.Destroy)
            {
                return false;
            }

            if (entityRegistry.EntityExists(update.Entity))
            {
                return false;
            }

            logger.Debug("Skipping partial update",
                ("entity", update.Entity),
                ("data", update.Components));

            return true;
        }

        private bool HandleDestroy(in IncomingEntityUpdate data, List<IncomingEntityUpdate> changes)
        {
            if (data.Meta.Operation != EntityOperation.Destroy)
            {
                return false;
            }

            entitiesTaken.Add(data.Entity);

            if (!entityRegistry.EntityExists(data.Entity))
            {
                logger.Debug("Destroy for non-existing entity",
                    ("entity", data.Entity),
                    ("data", data.Components));

                return true;
            }

            changes.Add(data);

            return true;
        }

        private void HandleMessagesReferencingDestroyedEntity(in Entity entity, List<ExpirableMessage> messages)
        {
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (messages[i].Message.Entity == entity)
                {
                    logger.Debug("Message removed for destroyed entity",
                        ("entity", entity),
                        ("message", messages[i]));

                    messages.RemoveAt(i);
                    continue;
                }

                messages[i].Message.NullEntityRefs(entity);
            }
        }
    }
}
