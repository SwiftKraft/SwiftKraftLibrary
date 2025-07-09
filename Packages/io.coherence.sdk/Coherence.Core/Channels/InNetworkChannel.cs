// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Channels
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using Coherence.Brook;
    using Coherence.Entities;
    using Coherence.Log;
    using Coherence.ProtocolDef;
    using Coherence.Serializer;
    using Coherence.SimulationFrame;
    using Coherence.Stats;

    /// <summary>
    /// InNetworkChannel is responsible for deserializing and buffering incoming changes.
    /// While taking care of entity references and ordering.
    /// </summary>
    internal class InNetworkChannel : IInNetworkChannel
    {
        public event Action<List<IncomingEntityUpdate>> OnEntityUpdate;
        public event Action<IEntityCommand, MessageTarget, Entity> OnCommand;
        public event Action<IEntityInput, long, Entity> OnInput;

        private readonly ISchemaSpecificComponentDeserialize deserializer;
        private readonly IComponentInfo definition;

        private readonly ReceiveChangeBuffer changeBuffer;

        private readonly Stats stats;
        private readonly Logger logger;

        // Cache for HandleEntityUpdate() and FlushBuffer() so we don't need to re-allocate the list every time
        private readonly List<IncomingEntityUpdate> updatesBuffer = new(32);

        // Cache for FlushBuffer() so we don't need to re-allocate the list every time
        private readonly List<IEntityMessage> messagesBuffer = new(32);

        public InNetworkChannel(
            ISchemaSpecificComponentDeserialize deserializer,
            IComponentInfo definition,
            IEntityRegistry entityRegistry,
            Stats stats,
            Logger logger)
        {
            this.deserializer = deserializer;
            this.definition = definition;
            this.stats = stats;
            this.logger = logger.With<InNetworkChannel>();

            this.changeBuffer = new ReceiveChangeBuffer(entityRegistry, this.logger);
        }

        public bool Deserialize(IInBitStream stream, AbsoluteSimulationFrame packetSimulationFrame, Vector3 floatingOriginDelta)
        {
            var gotEntityUpdate = false;

            while (DeserializeCommands.DeserializeCommand(stream, out var messageType))
            {
                if (messageType == MessageType.EcsWorldUpdate)
                {
                    gotEntityUpdate = true;
                }

                PerformMessage(messageType, packetSimulationFrame, stream, floatingOriginDelta);
            }

            return gotEntityUpdate;
        }

        public List<RefsInfo> GetRefsInfos()
        {
            return changeBuffer.GetRefsInfos();
        }

        public void FlushBuffer(IReadOnlyCollection<Entity> resolvableEntities)
        {
            updatesBuffer.Clear();
            changeBuffer.TakeUpdates(updatesBuffer, resolvableEntities);
            OnEntityUpdate?.Invoke(updatesBuffer);

            messagesBuffer.Clear();
            changeBuffer.TakeCommands(messagesBuffer, resolvableEntities);
            foreach (var message in messagesBuffer)
            {
                var command = (IEntityCommand)message;
                OnCommand?.Invoke(command, command.Routing, command.Entity);
            }

            messagesBuffer.Clear();
            changeBuffer.TakeInputs(messagesBuffer, resolvableEntities);
            foreach (var entityMessage in messagesBuffer)
            {
                var input = (IEntityInput)entityMessage;
                OnInput?.Invoke(input, input.Frame, input.Entity);
            }
        }

        public void Clear()
        {
            changeBuffer.Clear();
        }

        private void PerformMessage(MessageType messageType, AbsoluteSimulationFrame packetSimulationFrame, IInBitStream bitStream, Vector3 floatingOriginDelta)
        {
            switch (messageType)
            {
                case MessageType.EcsWorldUpdate:
                    HandleEntityUpdate(packetSimulationFrame, bitStream, floatingOriginDelta);
                    break;
                case MessageType.Command:
                    HandleCommands(bitStream);
                    break;
                case MessageType.Input:
                    HandleInputs(bitStream);
                    break;
                default:
                    logger.Warning(Warning.CoreInNetworkChannelUnknownMessage,
                         ("MessageCode", messageType),
                         ("SimFrame", packetSimulationFrame.Frame),
                         ("RemainingBits", bitStream.RemainingBits()));
                    break;
            }
        }

        private void HandleCommands(IInBitStream bitStream)
        {
            var commandsData = deserializer.ReadCommands(bitStream, logger);
            var numCommands = commandsData.Length;
            stats.TrackIncomingMessages(MessageType.Command, numCommands);

            for (var i = 0; i < numCommands; i++)
            {
                changeBuffer.AddCommand(commandsData[i]);
            }
        }

        private void HandleInputs(IInBitStream bitStream)
        {
            var inputData = deserializer.ReadInputs(bitStream, logger);
            var numInputs = inputData.Length;
            stats.TrackIncomingMessages(MessageType.Input, numInputs);

            for (var i = 0; i < numInputs; i++)
            {
                changeBuffer.AddInput(inputData[i]);
            }
        }

        private void HandleEntityUpdate(AbsoluteSimulationFrame packetSimulationFrame, IInBitStream bitStream,
            Vector3 floatingOriginDelta)
        {
            updatesBuffer.Clear();
            Serializer.Deserialize.ReadWorldUpdate(updatesBuffer, packetSimulationFrame, floatingOriginDelta, deserializer, bitStream,
                definition, logger);
            stats.TrackIncomingMessages(MessageType.EcsWorldUpdate, updatesBuffer.Count);

            foreach (var update in updatesBuffer)
            {
                changeBuffer.AddChange(update);
            }
        }
    }
}
