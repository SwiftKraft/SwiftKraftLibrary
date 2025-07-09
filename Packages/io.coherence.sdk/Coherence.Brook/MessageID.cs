// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System;

    /// <summary>
    /// Represents an auto incrementing message identifier. Used for ordered messages.
    /// </summary>
    public readonly struct MessageID : IEquatable<MessageID>
    {
        public const ushort MaxRange = Int16.MaxValue + 1;
        public const ushort MaxValue = +Int16.MaxValue;

        public ushort Value { get; }

        public MessageID(UInt16 value)
        {
            AssertValid(value);
            Value = value;
        }

        public MessageID Next() => new ((ushort)((Value + 1) % MaxRange));
        public MessageID Advance(int count) => new ((ushort)((Value + count) % MaxRange));

        public int Distance(MessageID id)
        {
            int start = Value;
            int end = id.Value;

            if (end < start)
            {
                end += MaxRange;
            }

            return (end - start);
        }

        public override int GetHashCode() => Value.GetHashCode();
        public bool Equals(MessageID other) => Value == other.Value;
        public override bool Equals(object obj) => obj is MessageID other && Equals(other);

        private static void AssertValid(ushort id)
        {
            if (id > MaxValue)
            {
                throw new ArgumentException($"Invalid MessageID: {id}");
            }
        }
    }
}
