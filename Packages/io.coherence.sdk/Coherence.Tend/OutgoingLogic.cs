// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Tend.Client
{
    using System;
    using System.Collections.Generic;
    using Brook;
    using Models;
    using Log;

    public class OutgoingLogic : IOutgoingLogic
    {
        public int Count => receivedQueue.Count;
        // LEQ now since you can send the current outgoing sequence without incrementing it and the increment
        // doesn't happen until the packet is sent.
        public bool CanIncrementOutgoingSequence => lastReceivedByRemoteSequenceId.Distance(OutgoingSequenceId) <= ReceiveMask.Range;
        public SequenceId LastReceivedByRemoteSequenceId => lastReceivedByRemoteSequenceId;
        public SequenceId OutgoingSequenceId { get; set; } = new SequenceId(0);

        private SequenceId lastReceivedByRemoteSequenceId = SequenceId.Max;
        private readonly Queue<DeliveryInfo> receivedQueue = new Queue<DeliveryInfo>();

        private Logger logger;

        public OutgoingLogic(Logger logger)
        {
            this.logger = logger.With<OutgoingLogic>();
        }

        public bool ReceivedByRemote(SequenceId receivedByRemoteId, ReceiveMask receivedByRemoteMask)
        {
            int distance = lastReceivedByRemoteSequenceId.Distance(receivedByRemoteId);
            if (distance == 0)
            {
                return false;
            }

            if (!lastReceivedByRemoteSequenceId.IsValidSuccessor(receivedByRemoteId))
            {
                throw new UnorderedPacketException("Stale packet detected", lastReceivedByRemoteSequenceId, receivedByRemoteId);
            }

            SequenceId receivedId = new SequenceId(lastReceivedByRemoteSequenceId.Value);
            receivedId = receivedId.Next();
            MutableReceiveMask bits = new MutableReceiveMask(receivedByRemoteMask, distance);
            for (int i = 0; i < distance; ++i)
            {
                Bit wasReceived = bits.ReadNextBit();
                Append(receivedId, wasReceived.IsOn);
                receivedId = receivedId.Next();
            }
            lastReceivedByRemoteSequenceId = receivedByRemoteId;
            return true;
        }

        public SequenceId IncreaseOutgoingSequenceId()
        {
            if (!CanIncrementOutgoingSequence)
            {
                throw new Exception($"Can not increase sequence ID. outgoing: {OutgoingSequenceId} last received: {lastReceivedByRemoteSequenceId} number of pending {lastReceivedByRemoteSequenceId.Distance(OutgoingSequenceId)} ");
            }

            logger.Trace("new seq", ("last", lastReceivedByRemoteSequenceId), ("next", OutgoingSequenceId.Next()), ("distance", lastReceivedByRemoteSequenceId.Distance(OutgoingSequenceId)));

            OutgoingSequenceId = OutgoingSequenceId.Next();

            return OutgoingSequenceId;
        }

        public DeliveryInfo Dequeue()
        {
            return receivedQueue.Dequeue();
        }

        private void Append(SequenceId receivedId, bool bit)
        {
            DeliveryInfo info = new DeliveryInfo
            {
                PacketSequenceId = receivedId,
                WasDelivered = bit
            };

            receivedQueue.Enqueue(info);
        }
    }
}
