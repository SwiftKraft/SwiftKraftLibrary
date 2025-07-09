// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Tend.Client
{
    using System;
    using Brook;
    using Models;
    using Log;

    public class Tend
    {
        // Only process reliable packets when connected is true. Set by the
        // layer above Tend (Brisk).
        public bool Connected;

        public event Action<DeliveryInfo> OnDeliveryInfo;

        public bool CanSend => tendOut.CanIncrementOutgoingSequence;
        public SequenceId OutgoingSequenceId => tendOut.OutgoingSequenceId;
        public SequenceId LastReceivedByRemote => tendOut.LastReceivedByRemoteSequenceId;

        private int NumberOfPacketsPending => tendOut.LastReceivedByRemoteSequenceId.Distance(tendOut.OutgoingSequenceId);

        private readonly IOutgoingLogic tendOut;
        private readonly IIncomingLogic tendIn;
        private readonly Logger logger;

        public Tend(Logger logger)
        {
            this.logger = logger.With<Tend>();

            tendOut = new OutgoingLogic(this.logger);
            tendIn = new IncomingLogic(this.logger);
        }

        public Tend(Logger logger, IOutgoingLogic tendOut, IIncomingLogic tendIn)
        {
            this.logger = logger.With<Tend>();

            this.tendOut = tendOut;
            this.tendIn = tendIn;
        }

        public bool ReadHeader(IInOctetStream stream, out TendHeader tendHeader, out bool didAck)
        {
            didAck = false;

            if (stream.RemainingOctetCount < 1)
            {
                // need at least one byte to read reliability
                logger.Warning(Warning.TendLessThan1Byte);
                tendHeader = new TendHeader();
                return false;
            }

            try
            {
                tendHeader = DeserializeHeader(stream);
            }
            catch (System.Exception ex)
            {
                // failed to deserialize header, invalid packet packet
                logger.Warning(Warning.TendInvalidPacket, ("exception", ex));
                tendHeader = new TendHeader();
                return false;
            }


            if (!tendHeader.isReliable)
            {
                return true; // do not care about order in this packet
            }

            // Do not process reliable packets unless connected.  This
            // prevents accidentally processing random data from other connections.
            if (!Connected)
            {
                return false;
            }

            if (!tendIn.ReceivedToUs(tendHeader.packetId))
            {
                return false; // out of order packet received, should be thrown away
            }

            didAck = tendOut.ReceivedByRemote(tendHeader.receivedId, tendHeader.receiveMask);

            // Trigger delivery notification callbacks
            while (tendOut.Count > 0)
            {
                DeliveryInfo deliveryInfo = tendOut.Dequeue();
                OnDeliveryInfo?.Invoke(deliveryInfo);
            }

            return true;
        }

        public TendHeader WriteHeader(IOutOctetStream stream, bool isReliable)
        {
            if (!isReliable)
            {
                TendHeader unreliableTendHeader = new TendHeader() { isReliable = false };
                SerializeHeader(stream, unreliableTendHeader);
                return unreliableTendHeader;
            }

            // Throw exception if there are 32 pending packets already
            if (!tendOut.CanIncrementOutgoingSequence)
            {
                throw new Exception($"Can't increment tend, outgoing: {OutgoingSequenceId} last received: {LastReceivedByRemote} number of pending {NumberOfPacketsPending} ");
            }

            var tendHeader = new TendHeader
            {
                isReliable = true,
                packetId = tendOut.OutgoingSequenceId,     // Tend packet Id
                receivedId = tendIn.LastReceivedToUs,      // The Id of the incoming packet that was last received from server
                receiveMask = tendIn.ReceiveMask           // Lost or dropped status for the 32 last incoming packets
            };

            SerializeHeader(stream, tendHeader);
            return tendHeader;
        }

        public void OnPacketSent(SequenceId sequenceId, bool isReliable)
        {
            if (!isReliable)
            {
                return;
            }

            // increment only if the sequence is the current one since it's
            // possible to resend an old sequence a few times.
            if (sequenceId.Equals(tendOut.OutgoingSequenceId))
            {
                _ = tendOut.IncreaseOutgoingSequenceId();
            }
        }

        public bool IsValidSeqToSend(in SequenceId sentSequenceId)
        {
            var higherThanReceived = LastReceivedByRemote.IsValidSuccessor(sentSequenceId);
            if (!higherThanReceived)
            {
                return false;
            }

            return OutgoingSequenceId.Equals(sentSequenceId);
        }

        public static void SerializeHeader(IOutOctetStream stream, TendHeader tendHeader)
        {
            // TODO: To 1 bit (coherence/unity#1941)
            stream.WriteUint8(tendHeader.isReliable ? (byte)1 : (byte)0);

            if (tendHeader.isReliable)
            {
                stream.WriteUint8(tendHeader.packetId.Value);
                stream.WriteUint8(tendHeader.receivedId.Value);
                stream.WriteUint32(tendHeader.receiveMask.Bits);
            }
        }

        public static TendHeader DeserializeHeader(IInOctetStream stream)
        {
            var header = new TendHeader();

            // TODO: To 1 bit (coherence/unity#1941)
            header.isReliable = stream.ReadUint8() != 0;

            if (header.isReliable)
            {
                header.packetId = new SequenceId(stream.ReadUint8());
                header.receivedId = new SequenceId(stream.ReadUint8());
                header.receiveMask = new ReceiveMask(stream.ReadUint32());
            }

            return header;
        }
    }
}
