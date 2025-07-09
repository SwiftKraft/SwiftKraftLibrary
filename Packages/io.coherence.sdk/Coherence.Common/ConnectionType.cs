// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Connection
{
    public enum ConnectionType : byte
    {
        Client = 0x00,
        Simulator = 0x01,
        Replicator = 0x02,
        Persistence = 0x03,
    };
}
