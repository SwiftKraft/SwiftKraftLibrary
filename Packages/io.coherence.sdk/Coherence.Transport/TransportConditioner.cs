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

    public class TransportConditioner : ITransport
    {
        public class Configuration
        {
            public bool HoldOutgoingPackets { get; set; }
            public bool CanSend { get; set; } = true;
            public bool DropNextOutPacket { get; set; }
            public Action OnNextOutPacketDropped { get; set; }
            public Action OnNextPacketSentOneShot { get; set; }
            public Condition Conditions { get; set; }
            public IRandom Random { get; set; } = new SystemRandom();
        }

        protected struct DelayedPacket<T>
        {
            public T Data;
            public DateTime DeliveryTime;

            public bool ReadyForDelivery(DateTime time) => DeliveryTime <= time;
        }

        public event Action OnOpen
        {
            add => transport.OnOpen += value;
            remove => transport.OnOpen -= value;
        }

        public event Action<ConnectionException> OnError
        {
            add => transport.OnError += value;
            remove => transport.OnError -= value;
        }

        public TransportState State => transport.State;
        public bool IsReliable => transport.IsReliable;

        public Configuration Config { get; protected set; } = new Configuration();
        public bool CanSend => Config.CanSend;
        public int HeaderSize => transport.HeaderSize;
        public string Description => transport.Description + " (Conditioned)";

        protected readonly IDateTimeProvider dateTimeProvider;
        protected readonly Logger logger;

        protected Condition Conditions => Config.Conditions;
        protected IRandom Random => Config.Random;

        private readonly ITransport transport;
        private readonly Queue<IOutOctetStream> heldOutgoingPackets = new Queue<IOutOctetStream>();
        private readonly Queue<DelayedPacket<IOutOctetStream>> delayedOutgoingPackets = new Queue<DelayedPacket<IOutOctetStream>>();
        private readonly Queue<DelayedPacket<(IInOctetStream stream, IPEndPoint from)>> delayedIncomingPackets = new Queue<DelayedPacket<(IInOctetStream, IPEndPoint)>>();

        private DateTime lastDuplicateSendTime;
        private IOutOctetStream dataToSendDuplicate;

        public TransportConditioner(ITransport transport, IDateTimeProvider dateTimeProvider, Logger logger)
        {
            this.transport = transport;
            this.dateTimeProvider = dateTimeProvider;
            this.logger = logger.With<TransportConditioner>();
        }

        public void Open(EndpointData endpoint, ConnectionSettings settings)
        {
            transport.Open(endpoint, settings);
        }

        public void Close()
        {
            transport.Close();
        }

        public void PrepareDisconnect()
        {
            transport.PrepareDisconnect();
        }

        public void SetConfiguration(Configuration configuration)
        {
            this.Config = configuration;
        }

        public void Send(IOutOctetStream data)
        {
            DateTime now = dateTimeProvider.UtcNow;

            try
            {
                ProcessDelayedOutgoingPackets(now);

                if (Config.HoldOutgoingPackets)
                {
                    heldOutgoingPackets.Enqueue(data);
                    return;
                }

                if (ShouldDropOutgoingPacket())
                {
                    logger.Trace("Transport Conditioner Dropping sent packet");
                    return;
                }

                if (ShouldTamper())
                {
                    Tamper(data);
                }

                if (ShouldDelayOutgoingPacket())
                {
                    delayedOutgoingPackets.Enqueue(new DelayedPacket<IOutOctetStream>
                    {
                        Data = data,
                        DeliveryTime = now + TimeSpan.FromSeconds(Conditions.sendDelaySec)
                    });

                    return;
                }

                FlushDelayedOutgoingPackets();

                // Setup the sending of duplicate packets at some rate if enabled.
                if (Config.Conditions.sendDuplicateRateSec > 0f)
                {
                    lastDuplicateSendTime = now;
                    // Copy the data since these are pooled and can change.
                    var streamEnd = data.Position;
                    dataToSendDuplicate = new OutOctetStream(data.Octets.ToArray());
                    dataToSendDuplicate.Seek(streamEnd); // The position is not copied.
                }

                transport.Send(data);
            }
            finally
            {
                NotifyOnNextPacketSent();
            }
        }

        public void Receive(List<(IInOctetStream, IPEndPoint)> buffer)
        {
            DateTime now = dateTimeProvider.UtcNow;
            ProcessDelayedOutgoingPackets(now);
            ProcessDelayedIncomingPackets(buffer, now);

            int initialBufferSize = buffer.Count;

            transport.Receive(buffer);

            for (int i = initialBufferSize; i < buffer.Count; i++)
            {
                if (ShouldDropIncomingPacket())
                {
                    logger.Trace("Transport Conditioner Dropping received packet");

                    buffer.RemoveAt(i);
                    i--;
                    continue;
                }

                if (ShouldDelayIncomingPacket())
                {
                    delayedIncomingPackets.Enqueue(new DelayedPacket<(IInOctetStream, IPEndPoint)>()
                    {
                        Data = buffer[i],
                        DeliveryTime = now + TimeSpan.FromSeconds(Conditions.receiveDelaySec)
                    });

                    buffer.RemoveAt(i);
                    i--;
                }
            }

            // Because there is no tick, this is using the receive (which is expected to run
            // each frame) to tick the resend of duplicate data if that is enabled.
            if (dataToSendDuplicate != null &&
                Config.Conditions.sendDuplicateRateSec > 0f &&
                CanSend)
            {
                var diff = now - lastDuplicateSendTime;
                if (diff.TotalSeconds >= Config.Conditions.sendDuplicateRateSec)
                {
                    transport.Send(dataToSendDuplicate);
                    lastDuplicateSendTime = now; //This will have time drift, so it's not a perfect rate.
                }
            }
        }

        /// <summary>
        ///     Releases all packets held due to the <see cref="HoldOutgoingPackets" /> flag. Released packets are not subject to a
        ///     drop or delay conditions.
        /// </summary>
        public void ReleaseAllHeldOutgoingPackets()
        {
            while (heldOutgoingPackets.Count > 0)
            {
                var packet = heldOutgoingPackets.Dequeue();
                transport.Send(packet);
            }
        }

        protected bool ShouldDropOutgoingPacket()
        {
            if (Config.DropNextOutPacket)
            {
                Config.DropNextOutPacket = false;
                Config.OnNextOutPacketDropped?.Invoke();

                return true;
            }

            return Conditions.sendDropRate > 0 && Random.NextDouble() <= Conditions.sendDropRate;
        }

        protected bool ShouldTamper()
        {
            return Conditions.packetTamperRate > 0 && Random.NextDouble() <= Conditions.packetTamperRate;
        }

        protected bool ShouldDelayOutgoingPacket()
        {
            return Conditions.sendDelaySec > 0f;
        }

        protected void NotifyOnNextPacketSent()
        {
            var action = Config.OnNextPacketSentOneShot;
            // This way subscribers can resubscribe during a callback
            Config.OnNextPacketSentOneShot = null;

            action?.Invoke();
        }

        protected virtual void FlushDelayedOutgoingPackets()
        {
            while (delayedOutgoingPackets.Count > 0)
            {
                var packet = delayedOutgoingPackets.Dequeue();
                transport.Send(packet.Data);
            }
        }

        protected virtual void ProcessDelayedOutgoingPackets(DateTime now)
        {
            while (delayedOutgoingPackets.Count > 0)
            {
                var packet = delayedOutgoingPackets.Peek();
                if (!packet.ReadyForDelivery(now))
                {
                    break;
                }

                delayedOutgoingPackets.Dequeue();
                transport.Send(packet.Data);
            }
        }

        private void ProcessDelayedIncomingPackets(List<(IInOctetStream, IPEndPoint)> buffer, DateTime now)
        {
            while (delayedIncomingPackets.Count > 0)
            {
                var packet = delayedIncomingPackets.Peek();
                if (!packet.ReadyForDelivery(now))
                {
                    break;
                }

                delayedIncomingPackets.Dequeue();
                buffer.Add(packet.Data);
            }
        }

        private bool ShouldDelayIncomingPacket()
        {
            return Conditions.receiveDelaySec > 0;
        }

        private bool ShouldDropIncomingPacket()
        {
            return Conditions.receiveDropRate > 0 && Random.NextDouble() <= Conditions.receiveDropRate;
        }

        private void Tamper(IOutOctetStream packet)
        {
            var inOctetStream = new InOctetStream(packet.Close().ToArray());

            var tamperStartPoint = Math.Clamp(Random.NextNormalDistribution(Conditions.tamperStart, Conditions.tamperStartDeviation), 0, 1);

            var packetLen = packet.Position;

            packet.Seek(0);

            while (inOctetStream.RemainingOctetCount > 0)
            {
                var octet = inOctetStream.ReadOctet();

                if (packet.Position / (float)packetLen >= tamperStartPoint)
                {
                    for (var i = 0; i < 8; i++)
                    {
                        if (Conditions.tamperRate > 0 && Random.NextDouble() <= Conditions.tamperRate)
                        {
                            octet ^= (byte)(1 << i);
                        }
                    }
                }

                packet.WriteOctet(octet);
            }
        }
    }
}
