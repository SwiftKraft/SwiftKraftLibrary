// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    public struct DeliveryInfo
    {
        public bool WasDelivered;
        public SequenceId PacketSequenceId;

        public override string ToString()
        {
            return $"[Delivery {PacketSequenceId} wasDelivered:{WasDelivered}]";
        }
    }
}
