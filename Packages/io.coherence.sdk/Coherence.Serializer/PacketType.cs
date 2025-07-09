// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Serializer
{
    /// <summary>
    /// The raw packet type values that are encountered in the Coherence protocol stream.
    /// </summary>
    public static class PacketTypeValues
    {
        /// <summary>
        /// Update an ECS world.
        /// </summary>
        public const byte Bitstreamed = 0x01;

    }

    /// <summary>
    /// All the packet types that are present in the Coherence protocol.
    /// </summary>
    public enum PacketType
    {
        /// <summary>
        /// Bitstreamed is a bitstreamed packet.
        /// </summary>
        Bitstreamed = PacketTypeValues.Bitstreamed,
    }
}
