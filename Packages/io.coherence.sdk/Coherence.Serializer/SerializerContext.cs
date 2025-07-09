// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Serializer
{
    using System;
    using Entities;
    using Log;

    public class SerializerContext<TStream>
    {
        public TStream BitStream { get; private set; }
        public bool UseDebugStreams { get; private set; }
        public Logger Logger { get; private set;  }
        public uint ProtocolVersion { get; private set; }

        public string Section { get; private set; }
        public Entity EntityId { get; private set; }
        public uint ComponentId { get; private set;  }

        /// <summary>
        /// This is a number of free bits remaining in a packet after the header was serialized.
        /// In other words, this is the maximum number of bits a single entity can be serialized into,
        /// so it fully takes the space of a single packet.
        /// </summary>
        public uint BitsRemainingInEmptyPacket { get; private set; }

        public SerializerContext(TStream stream, bool useDebugStreams, Logger logger, uint protocolVersion = ProtocolDef.Version.CurrentVersion)
        {
            BitStream = stream;
            UseDebugStreams = useDebugStreams;
            Logger = logger;
            ProtocolVersion = protocolVersion;
            Section = "";
            EntityId = Entity.InvalidRelative;
        }

        public void StartSection(string section)
        {
            Section = section;
            EntityId = Entity.InvalidRelative;
            ComponentId = UInt32.MaxValue;
        }

        public void EndSection()
        {
            Section = "";
            EntityId = Entity.InvalidRelative;
            ComponentId = UInt32.MaxValue;
        }

        public void SetEntity(Entity id)
        {
            EntityId = id;
            ComponentId = UInt32.MaxValue;
        }

        public void SetComponent(uint id)
        {
            ComponentId = id;
        }

        public void SetBitsRemainingInEmptyPacket(uint bitsRemainingInEmptyPacket)
        {
            this.BitsRemainingInEmptyPacket = bitsRemainingInEmptyPacket;
        }
    }
}
