// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System;

    /// <summary>
    /// Represents an auto incrementing sequence identifier. Usually a value between 0 and 127 (7 bits). After 127 it wraps around to 0.
    /// </summary>
    public struct SequenceId : IEquatable<SequenceId>
    {
        public const byte MaxRange = 128;
        public const byte MaxValue = 127;

        public static SequenceId Max = new(MaxValue);

        /// <summary>
        /// Constructing a SequenceId
        /// </summary>
        /// <exception cref="Exception">Thrown when the provided id is not between 0 and 127.</exception>
        public SequenceId(byte id)
        {
            if (!IsValid(id))
            {
                throw new Exception("Illegal SequenceID:" + id);
            }

            Value = id;
        }

        public byte Value { get; }

        /// <summary>
        /// Returns the next SequenceId. Note that the value wraps around 127.
        /// </summary>
        public SequenceId Next()
        {
            var nextValue = (byte)((Value + 1) % MaxRange);

            return new SequenceId(nextValue);
        }

        private static bool IsValid(byte id)
        {
            return id < MaxRange;
        }

        /// <summary>
        /// Returns the closest distance between the otherId and this SequenceId.
        /// </summary>
        public int Distance(SequenceId otherId)
        {
            int nextValue = otherId.Value;
            int idValue = Value;

            if (nextValue < idValue)
            {
                nextValue += MaxRange;
            }

            var diff = nextValue - idValue;

            return diff;
        }

        /// <summary>
        /// Checks if the nextId comes after this SequenceId.
        /// </summary>
        public bool IsValidSuccessor(SequenceId nextId)
        {
            var distance = Distance(nextId);

            // TODO: Change to ReceiveMask.Range after merging Brook and Tend
            return distance != 0 && distance <= 32;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format($"[SequenceId {Value}]");
        }

        public bool Equals(SequenceId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is SequenceId other && Equals(other);
        }
    }
}
