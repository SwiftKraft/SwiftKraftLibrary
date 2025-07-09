// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Models
{
    using Brook;

    public interface IOobMessage
    {
        bool IsReliable { get; }
        OobMessageType Type { get; }
        void Serialize(IOutOctetStream outStream, uint protocolVersion);
    }
}
