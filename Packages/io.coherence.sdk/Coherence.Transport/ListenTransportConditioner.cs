// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport
{
    using Brook;
    using Brook.Octet;
    using Common;
    using Connection;
    using Log;
    using System;
    using System.Collections.Generic;
    using System.Net;

    public class ListenTransportConditioner : TransportConditioner, IListenTransport
    {
        private readonly IListenTransport transport;

        private struct OutgoingSendToData
        {
            public IOutOctetStream Data;
            public IPEndPoint Endpoint;
            public SessionID SessionID;
        }

        private readonly Queue<OutgoingSendToData> heldOutgoingSendToPackets = new Queue<OutgoingSendToData>();
        private readonly Queue<DelayedPacket<OutgoingSendToData>> delayedOutgoingSendToPackets = new Queue<DelayedPacket<OutgoingSendToData>>();

        public ListenTransportConditioner(IListenTransport transport, IDateTimeProvider dateTimeProvider, Logger logger) : base(transport, dateTimeProvider, logger)
        {
            this.transport = transport;
        }

        public void Listen(EndpointData entpointData, ConnectionSettings settings)
        {
            transport.Listen(entpointData, settings);
        }

        public void SendTo(IOutOctetStream data, IPEndPoint endpoint, SessionID sessionID)
        {
            DateTime now = dateTimeProvider.UtcNow;

            try
            {
                ProcessDelayedOutgoingPackets(now);

                if (Config.HoldOutgoingPackets)
                {
                    heldOutgoingSendToPackets.Enqueue(new OutgoingSendToData()
                    {
                        Data = data,
                        Endpoint = endpoint,
                        SessionID = sessionID,
                    });

                    return;
                }

                if (ShouldDropOutgoingPacket())
                {
                    logger.Trace("Listen Transport Conditioner Dropping sent packet");
                    data.ReturnIfPoolable();
                    return;
                }

                if (ShouldDelayOutgoingPacket())
                {
                    delayedOutgoingSendToPackets.Enqueue(new DelayedPacket<OutgoingSendToData>
                    {
                        Data = new OutgoingSendToData()
                        {
                            Data = data,
                            Endpoint = endpoint,
                            SessionID = sessionID,
                        },
                        DeliveryTime = now + TimeSpan.FromSeconds(Conditions.sendDelaySec)
                    });

                    return;
                }

                FlushDelayedOutgoingPackets();
                transport.SendTo(data, endpoint, sessionID);
            }
            finally
            {
                NotifyOnNextPacketSent();
            }
        }

        protected override void FlushDelayedOutgoingPackets()
        {
            while (delayedOutgoingSendToPackets.Count > 0)
            {
                var packet = delayedOutgoingSendToPackets.Dequeue();
                transport.SendTo(packet.Data.Data, packet.Data.Endpoint, packet.Data.SessionID);
            }
        }

        protected override void ProcessDelayedOutgoingPackets(DateTime now)
        {
            while (delayedOutgoingSendToPackets.Count > 0)
            {
                var packet = delayedOutgoingSendToPackets.Peek();
                if (!packet.ReadyForDelivery(now))
                {
                    break;
                }

                delayedOutgoingSendToPackets.Dequeue();
                transport.SendTo(packet.Data.Data, packet.Data.Endpoint, packet.Data.SessionID);
            }
        }
    }
}
