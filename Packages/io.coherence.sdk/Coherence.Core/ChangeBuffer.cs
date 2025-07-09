// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using System.Collections.Generic;
    using Entities;
    using Serializer;
    using System.Linq;

    using Brook;
    using Log;
    using System;
    using System.Text;

    internal class ChangeBuffer
    {
        public readonly Dictionary<Entity, OutgoingEntityUpdate> Buffer;
        public readonly Queue<SerializedEntityMessage> commandBuffer;
        public readonly Queue<SerializedEntityMessage> inputBuffer;
        public readonly SequenceId sequenceID;

        protected Logger logger;

        public ChangeBuffer(Dictionary<Entity, OutgoingEntityUpdate> buffer,
                                        Queue<SerializedEntityMessage> commands,
                                        Queue<SerializedEntityMessage> inputs,
                                        SequenceId id,
                                        Logger logger)
        {
            this.logger = logger.With<ChangeBuffer>();

            if (buffer == null)
            {
                buffer = new Dictionary<Entity, OutgoingEntityUpdate>();
            }

            Buffer = buffer;
            commandBuffer = commands;
            inputBuffer = inputs;
            sequenceID = id;
        }

        public bool HasMessages()
        {
            return commandBuffer.Count > 0 || inputBuffer.Count > 0;
        }

        /// <summary>
        /// Add the given priority to all changes
        /// </summary>
        public void ReprioritizeChanges(int priority)
        {
            foreach (var key in Buffer.Keys.ToArray())
            {
                var state = Buffer[key];
                state.Priority += priority;
                Buffer[key] = state;
            }
        }

        public void ClearAllChangesForEntity(Entity id)
        {
            logger.Trace("ClearAllChangesForEntity", ("entity", id));

            if (Buffer.TryGetValue(id, out var update))
            {
                Buffer.Remove(id);
                update.Return();
            }
        }

        public bool HasChangesForEntity(Entity id)
        {
            return Buffer.ContainsKey(id);
        }

        public void ClearComponentChangesForEntity(Entity id, uint componentID)
        {
            logger.Trace("ClearComponentChangesForEntity", ("entity", id));

            if (Buffer.TryGetValue(id, out var update))
            {
                update.Components.Updates.Remove(componentID);
                Buffer[id] = update;
            }
        }

        public bool HasComponentChangesForEntity(Entity id, uint componentID)
        {
            if (Buffer.TryGetValue(id, out var update))
            {
                if (update.Components.Updates.Store.ContainsKey(componentID))
                {
                    return true;
                }

                return update.Components.Destroys.Contains(componentID);
            }

            return false;
        }

        /// <summary>
        /// In case the changes for the given entity contain an un-acked ordered component,
        /// then the changes are merged onto the given DeltaComponents.
        /// </summary>
        public void MergeIfOrderedComponents(Entity entity, ref DeltaComponents components, IComponentInfo componentInfo)
        {
            if (!Buffer.TryGetValue(entity, out var update))
            {
                return;
            }

            if (!update.Components.ContainsOrderedComponent(componentInfo))
            {
                return;
            }

            var orderedComponentsAcked = !update.Components.HasUnackedOrderedComponents();
            if (orderedComponentsAcked)
            {
                return;
            }

            components.EnsureInitialized();

            foreach (var destructs in update.Components.Destroys)
            {
                components.RemoveComponent(destructs);
            }

            foreach (var component in update.Components.Updates.Store)
            {
                components.UpdateComponent(component.Value.Clone());
            }

            components.OrderedUpdateTime = update.Components.OrderedUpdateTime;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Updates:");
            foreach (var (entity, data) in Buffer)
            {
                sb.Append("\t");
                sb.Append("Entity: ");
                sb.Append(entity.ToString());
                sb.Append(" => ");
                sb.AppendLine(data.ToString());
            }

            sb.AppendLine("Commands:");
            foreach (var command in commandBuffer)
            {
                sb.Append("\t");
                sb.AppendLine(command.TargetEntity.ToString());
            }

            sb.AppendLine("Inputs:");
            foreach (var input in inputBuffer)
            {
                sb.Append("\t");
                sb.AppendLine(input.TargetEntity.ToString());
            }

            return sb.ToString();
        }
    }
}
