// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using System.Collections.Generic;
    using Entities;

    public struct PacketSentDebugInfo
    {
        public Dictionary<ChannelID, Dictionary<Entity, OutgoingEntityUpdate>> ChangesSentPerChannel;
        public int TotalChanges;
        public uint OctetCount;
    }
}
