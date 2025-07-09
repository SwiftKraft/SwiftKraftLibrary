// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Serializer
{
    using Brook;
    using SimulationFrame;

    public static class PacketHeaderWriter
    {
        private static void WriteSimulationFrame(IOutOctetStream stream, AbsoluteSimulationFrame now)
        {
            stream.WriteUint64((ulong)now.Frame);
        }

        public static void WriteHeader(IOutOctetStream outStream, bool isDebugStream, AbsoluteSimulationFrame simulationFrame)
        {
            byte flags = 0;
            if (isDebugStream)
            {
                flags |= 0x08;
            }

            const byte packetCommand = PacketTypeValues.Bitstreamed;
            byte octet = (byte)(((packetCommand & 0x3) << 5) | (flags & 0x1F));
            outStream.WriteOctet(octet);

            WriteSimulationFrame(outStream, simulationFrame);
        }
    }
}
