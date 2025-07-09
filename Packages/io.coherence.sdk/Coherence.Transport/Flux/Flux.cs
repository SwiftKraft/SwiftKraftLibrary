// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Flux
{
    using System;
    using System.Net;
    using Brook;
    using Brook.Octet;
    using Coherence.Connection;
    using Coherence.Log;
    using Common.Pooling;
    using Transport;

    public class Flux
    {
        public event Action<IInOctetStream, IPEndPoint> OnPacketReceived;    // Hooks into Connection.OnPacketReceived and Stats.TrackPacketReceived
        public event Action<uint> OnPacketSent;    // Hooks into Stats.TrackPacketSent

        private enum Mode
        {
            Open,
            Listen,
        }

        private readonly IPort port;
        private Mode mode;
        private IPEndPoint singleEndPoint;
        private string lastHost;
        private ushort roomId;

        private Logger logger;

        public const int roomByteCount = 2;

        private readonly object sendLock = new object();
        private readonly Pool<PooledInOctetStream> streamPool;

        public Flux(IPort port, Logger logger)
        {
            this.port = port ?? throw new ArgumentNullException(nameof(port));
            this.logger = logger.With<Flux>();

            streamPool = Pool<PooledInOctetStream>
                .Builder(pool => new PooledInOctetStream(pool))
                .Concurrent()
                .Build();
        }

        public void Open(string hostAndPort)
        {
            port.Open(ReportDataReceived, this);

            mode = Mode.Open;

            SetSingleEndPoint(hostAndPort);
        }

        public void Listen(EndpointData endpoint)
        {
            port.Listen(GetEndpoint(endpoint), ReportDataReceived, this);

            mode = Mode.Listen;
        }

        public void SendPacket(ArraySegment<byte> packet)
        {
            SendPacketTo(packet, singleEndPoint);
        }

        public void SendPacketTo(ArraySegment<byte> packet, IPEndPoint endpoint)
        {
            if (packet.Offset != 0)
            {
                throw new ArgumentException("packet offset not 0");
            }

            var packetSpan = packet.AsSpan();
            BitConverter.TryWriteBytes(packetSpan[..roomByteCount], roomId);

            lock (sendLock)
            {
                port.Send(endpoint, packet);
                // consider changing this to also pass the endpoint for better tracking.
                OnPacketSent?.Invoke((uint)packet.Count);
            }
        }

        public void SetRoomId(ushort roomId)
        {
            this.roomId = roomId;
        }

        // Used to ensure that incoming packets are only from one
        // source.  Used with Open, and generally not Listen.
        private void SetSingleEndPoint(string hostAndPort)
        {
            if (lastHost == hostAndPort)
            {
                return;
            }

            if (string.IsNullOrEmpty(hostAndPort) || hostAndPort.IndexOf(":", StringComparison.Ordinal) == -1)
            {
                throw new ArgumentException("Invalid host:port address");
            }

            int hostLength = hostAndPort.LastIndexOf(':');
            string hostname = hostAndPort.Substring(0, hostLength);
            int portNumber = Convert.ToInt32(hostAndPort.Substring(hostLength + 1));

            IPAddress[] addresses = Dns.GetHostAddresses(hostname);

            if (addresses.Length < 1)
            {
                throw new Exception($"Couldn't find the dns lookup for {hostname}");
            }

            lastHost = hostAndPort;
            singleEndPoint = new IPEndPoint(addresses[0], portNumber);
        }

        private void ReportDataReceived(byte[] data, IPEndPoint receivedFrom, object state)
        {
            if (mode == Mode.Open && !receivedFrom.Equals(singleEndPoint))
            {
                logger.Warning(Warning.FluxReceivedFromInvalidIP,
                    ("from", $"{receivedFrom}"),
                    ("expected", $"{singleEndPoint}"));
                return;
            }

            if (data.Length < roomByteCount)
            {
                logger.Warning(Warning.FluxInvalidRoomID);
                return;
            }

            var stream = streamPool.Rent();
            stream.Reset(data);

            var roomId = stream.ReadUint16();

            if (roomId != this.roomId)
            {
                logger.Warning(Warning.FluxWrongRoomID,
                    ("room", roomId),
                    ("expected", this.roomId));
                return;
            }

            OnPacketReceived?.Invoke(stream, receivedFrom);
        }

        private IPEndPoint GetEndpoint(EndpointData endpointData)
        {
            if (IPAddress.TryParse(endpointData.host, out IPAddress address))
            {
                return new IPEndPoint(address, endpointData.port);
            }

            throw new Exception($"Failed to parse host: {endpointData.host}");
        }
    }
}
