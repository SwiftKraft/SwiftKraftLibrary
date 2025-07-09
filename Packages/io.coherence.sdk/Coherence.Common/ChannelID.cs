// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using System;

    public readonly struct ChannelID : IEquatable<ChannelID>, IComparable<ChannelID>
    {
        public static readonly ChannelID Default = (ChannelID)0; // Reliable unordered
        public static readonly ChannelID Ordered = (ChannelID)1; // Reliable ordered (commands only)

        public static readonly ChannelID EndOfChannels = (ChannelID)15; // Marker for no more channels data in a packet.

        public static readonly ChannelID MinValue = (ChannelID)0;
        public static readonly ChannelID MaxValue = (ChannelID)((byte)EndOfChannels - 1);

        private readonly byte value;

        public ChannelID(byte value) => this.value = value;

        public static explicit operator ChannelID(byte value) => new(value);
        public static explicit operator byte(ChannelID channelID) => channelID.value;

        public bool IsValid() => this >= MinValue && this <= MaxValue;

        public static bool operator ==(ChannelID left, ChannelID right) => left.value == right.value;
        public static bool operator !=(ChannelID left, ChannelID right) => !(left.value == right.value);

        public static bool operator <(ChannelID left, ChannelID right) => left.value < right.value;
        public static bool operator >(ChannelID left, ChannelID right) => left.value > right.value;
        public static bool operator <=(ChannelID left, ChannelID right) => left.value <= right.value;
        public static bool operator >=(ChannelID left, ChannelID right) => left.value >= right.value;

        public bool Equals(ChannelID other) => value == other.value;
        public override bool Equals(object obj) => obj is ChannelID other && Equals(other);

        public override int GetHashCode() => value;

        public override string ToString() => value.ToString();

        public int CompareTo(ChannelID other) => value.CompareTo(other.value);
    }
}
