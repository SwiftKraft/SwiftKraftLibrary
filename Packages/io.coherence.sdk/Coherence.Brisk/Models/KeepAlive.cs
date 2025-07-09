// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Models
{
    using Brook;

    public class KeepAlive : IOobMessage
    {
        public bool IsReliable { get; set; }
        public OobMessageType Type => OobMessageType.KeepAlive;

        public override string ToString()
        {
            return $"{nameof(KeepAlive)}";
        }

        public void Serialize(IOutOctetStream stream, uint _)
        {
        }

        public static KeepAlive Deserialize(IInOctetStream stream, uint _)
        {
            return new KeepAlive();
        }
    }
}
