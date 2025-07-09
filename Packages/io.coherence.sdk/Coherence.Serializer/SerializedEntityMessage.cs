// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Serializer
{
    using Coherence.Entities;

    public class SerializedEntityMessage
    {
        public SerializedEntityMessage(Entity targetEntity, byte[] octets, uint bitCount)
        {
            TargetEntity = targetEntity;
            Octets = octets;
            BitCount = bitCount;
        }

        public Entity TargetEntity { get; }
        public byte[] Octets { get; }
        public uint BitCount { get; }
    }
}
