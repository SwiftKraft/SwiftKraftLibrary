// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using Brook;
    using Serializer;
    using SimulationFrame;
    using ProtocolDef;
    using Entities;
    using Common;
    using Log;
    using Transport;
    using Channels;

    public class InConnection
    {
        private const int FULL_PACKET_MARGIN = 128;

        public event Action<List<IncomingEntityUpdate>> OnEntityUpdate;
        public event Action<IEntityCommand, MessageTarget, Entity> OnCommand;
        public event Action<IEntityInput, long, Entity> OnInput;
        public event Action<AbsoluteSimulationFrame> OnServerSimulationFrameReceived;

        internal event Action<int> OnPacketReceived;

        private readonly IEntityRegistry knownEntities;
        private readonly SortedList<ChannelID, IInNetworkChannel> channels = new();

        private readonly RefsResolver refsResolver;

        private Vector3d currentFloatingOrigin;
        private int octetStreamWarnThreshold;

        private readonly Logger logger;

        // Cache for FlushChangeBuffer so we don't need to re-allocate every time
        private readonly List<RefsInfo> allRefsInfos = new();

        internal InConnection(IEntityRegistry knownEntities, Dictionary<ChannelID, IInNetworkChannel> channels, Logger logger)
        {
            this.knownEntities = knownEntities;
            this.logger = logger.With<InConnection>();
            this.refsResolver = new RefsResolver(this.logger);

            foreach (var (channelID, channel) in channels)
            {
                AddChannel(channelID, channel);
            }
        }

        private void AddChannel(ChannelID channelID, IInNetworkChannel channel)
        {
            if (channel == null)
            {
                throw new ArgumentNullException(nameof(channel), "channel must not be null");
            }

            if (!channelID.IsValid())
            {
                throw new ArgumentException($"Invalid ChannelID {channelID}, only channels {ChannelID.MinValue}-{ChannelID.MaxValue} are supported");
            }

            if (!channels.TryAdd(channelID, channel))
            {
                throw new Exception($"Failed to add channel, duplicate ChannelID {channelID}");
            }

            channel.OnEntityUpdate += update => OnEntityUpdate?.Invoke(update);
            channel.OnCommand += (command, target, entity) => OnCommand?.Invoke(command, target, entity);
            channel.OnInput += (input, simFrame, entity) => OnInput?.Invoke(input, simFrame, entity);
        }

        public void ProcessIncomingPacket(IInOctetStream octetStream)
        {
            var basicHeader = PacketHeaderReader.DeserializeBasicHeader(octetStream);
            var packetSimulationFrame = basicHeader.SimulationFrame;
            OnServerSimulationFrameReceived?.Invoke(packetSimulationFrame);

            var totalSize = octetStream.RemainingOctetCount;
            var decoded = PacketHeaderReader.ToPacketHeaderInfo(octetStream, basicHeader);
            var bitStream = decoded.Stream;

            var floatingOrigin = Deserialize.ReadFloatingOrigin(bitStream, this.logger);
            if (double.IsNaN(floatingOrigin.x) || double.IsNaN(floatingOrigin.y) || double.IsNaN(floatingOrigin.z))
            {
                logger.Warning(Warning.CoreInConnectionFloatingOriginNaN, ("received origin", floatingOrigin));
                floatingOrigin = Vector3d.zero;
            }

            var originDeltaDouble = floatingOrigin - currentFloatingOrigin;
            if (!originDeltaDouble.IsWithinRange(float.MaxValue))
            {
                logger.Warning(Warning.CoreInConnectionFloatingOriginDelta,
                    ("current origin", currentFloatingOrigin),
                    ("received origin", floatingOrigin));

                originDeltaDouble = Vector3d.zero;
            }

            var floatingOriginDelta = originDeltaDouble.ToCoreVector3();

            // SDK always uses the latest protocol version, but when we transition to CommonCore this check is needed.
            var protocolVersion = ProtocolDef.Version.CurrentVersion;
            var gotEntityUpdate = protocolVersion >= ProtocolDef.Version.VersionIncludesChannelID
                ? ReadMultipleChannels(bitStream, packetSimulationFrame, floatingOriginDelta)
                : ReadSingleChannel(bitStream, packetSimulationFrame, floatingOriginDelta, ChannelID.Default);

            octetStream.ReturnIfPoolable();

            FlushChangeBuffer();

            if (!gotEntityUpdate && totalSize >= octetStreamWarnThreshold)
            {
                logger.Warning(Warning.CoreInConnectionPacketFullOfMessages);
            }

            OnPacketReceived?.Invoke(totalSize);
        }

        private bool ReadSingleChannel(IInBitStream bitStream, AbsoluteSimulationFrame packetSimulationFrame, Vector3 floatingOriginDelta, ChannelID channelID)
        {
            if (!channels.TryGetValue(channelID, out var channel))
            {
                throw new Exception($"Unexpected channelID: {channelID} does not exist");
            }

            return channel.Deserialize(bitStream, packetSimulationFrame, floatingOriginDelta);
        }

        private bool ReadMultipleChannels(IInBitStream bitStream, AbsoluteSimulationFrame packetSimulationFrame, Vector3 floatingOriginDelta)
        {
            var gotEntityUpdate = false;

            while (Deserialize.ReadChannelID(bitStream, out var channelID))
            {
                gotEntityUpdate |= ReadSingleChannel(bitStream, packetSimulationFrame, floatingOriginDelta, channelID);
            }

            return gotEntityUpdate;
        }

        internal void Clear()
        {
            foreach (var channel in channels.Values)
            {
                channel.Clear();
            }
        }

        internal void SetFloatingOrigin(Vector3d newFloatingOrigin)
        {
            this.currentFloatingOrigin = newFloatingOrigin;
        }

        internal void SetMaximumTransmissionUnit(int mtu)
        {
            octetStreamWarnThreshold = mtu - FULL_PACKET_MARGIN;
        }

        private void FlushChangeBuffer()
        {
            allRefsInfos.Clear();
            foreach (var channel in channels.Values)
            {
                var refs = channel.GetRefsInfos();
                if (refs != null)
                {
                    allRefsInfos.AddRange(refs);
                }
            }

            refsResolver.Resolve(allRefsInfos, knownEntities);

            foreach (var channel in channels.Values)
            {
                channel.FlushBuffer(refsResolver.ResolvableEntities);
            }
        }
    }
}
