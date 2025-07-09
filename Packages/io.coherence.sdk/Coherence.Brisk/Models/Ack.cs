// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Models
{
    using Brook;

    public class Ack : IOobMessage
    {
        public static readonly Ack Instance = new Ack();

        public bool IsReliable { get; set; } = true;
        public OobMessageType Type => OobMessageType.Ack;

        public void Serialize(IOutOctetStream outStream, uint protocolVersion) { }
    }
}
