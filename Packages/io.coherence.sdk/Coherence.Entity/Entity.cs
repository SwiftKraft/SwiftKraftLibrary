// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Entities
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A local index for coherence entities.
    /// </summary>
    public struct Entity : IComparable<Entity>, IEquatable<Entity>
    {
        /// <summary>
        /// MaxIndex max ID index.
        /// </summary>
        public const ushort MaxIndex = ushort.MaxValue;

        /// <summary>
        /// EndOfEntities used to mark the serialized end of entities.
        /// </summary>
        public const ushort EndOfEntities = MaxIndex;

        /// <summary>
        /// MaxIndices is the maximum number of usable indicies.
        /// </summary>
        public const ushort MaxIndices = MaxIndex - 2; //0 and MaxIndex are reserved

        /// <summary>
        /// MaxID is the highest useable ID.
        /// </summary>
        public const ushort MaxID = MaxIndex - 1;

        /// <summary>
        /// ClientInitialIndex .
        /// </summary>
        public const ushort ClientInitialIndex = (MaxIndex >> 1) + 1; // 32,768

        /// <summary>
        /// NumIndexBits is the number of bits reqiured to serialize Index.
        /// </summary>
        public const int NumIndexBits = 16;

        /// <summary>
        /// MaxVersions max number of times Index is reused.
        /// </summary>
        public const byte MaxVersions = 16;

        /// <summary>
        /// NumVersionBits is the number of bits required to serialize up to MaxVersion values.
        /// Should be Log2(MaxVersions).
        /// </summary>
        public const int NumVersionBits = 4;

        public static readonly bool Absolute = true;
        public static readonly bool Relative = false;

        public static readonly Entity InvalidRelative = new(0, 0, Relative);
        public static readonly Entity InvalidAbsolute = new(0, 0, Absolute);

        public static readonly EntityComparer DefaultComparer = new();
        public ushort Index { get; }

        public byte Version { get; }

        public bool IsValid => Index > 0;
        public bool IsAbsolute { get; }

        public byte NextVersion => (byte)((Version + 1) % MaxVersions);

        // If true this is the EntityManager's version of ID, otherwise it is client-base (relative/serializeEntity) ID.

        public Entity(ushort index, byte version, bool isAbsolute)
        {
            Index = index;
            Version = version;
            IsAbsolute = isAbsolute;
        }

        public bool IsClientCreated()
        {
            return Index >= ClientInitialIndex;
        }

        public void AssertAbsolute()
        {
            if (!IsAbsolute)
            {
                throw new Exception($"expected absolute entityID, got relative {Index}");
            }
        }

        public void AssertRelative()
        {
            if (IsAbsolute)
            {
                throw new Exception($"expected relative entityID, got absolute {Index}");
            }
        }

        public override string ToString()
        {
            var versionString = Version == 0 ? "" : $"({Version})";
            return $"{Index}{versionString}";
        }

        public static string TypeToString(bool isAbsolute)
        {
            return isAbsolute ? "absolute" : "relative";
        }

        public static void AssertSameType(Entity a, Entity b)
        {
            if (a.IsAbsolute)
            {
                b.AssertAbsolute();
            }
            else
            {
                b.AssertRelative();
            }
        }

        public static bool operator ==(Entity a, Entity b)
        {
            // It's a very bad thing to compare the wrong types.
            AssertSameType(a, b);

            return a.Index == b.Index && a.Version == b.Version;
        }

        public static bool operator !=(Entity a, Entity b)
        {
            // It's a very bad thing to compare the wrong types.
            AssertSameType(a, b);

            return a.Index != b.Index || a.Version != b.Version;
        }

        // This uses boxing so it has allocations, don't use it in performance critical code.
        public override bool Equals(object obj)
        {
            return obj is Entity other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                const uint Base = 2166136261;
                const uint Multiplier = 16777619;

                var hash = Base;

                hash = (hash * Multiplier) ^ (uint)Index.GetHashCode();
                hash = (hash * Multiplier) ^ (uint)Version.GetHashCode();
                hash = (hash * Multiplier) ^ (uint)IsAbsolute.GetHashCode();

                return (int)hash;
            }
        }

        public int CompareTo(Entity other)
        {
            var indexComparison = Index.CompareTo(other.Index);
            if (indexComparison != 0)
            {
                return indexComparison;
            }

            return Version.CompareTo(other.Version);
        }

        public bool Equals(Entity other)
        {
            return Index == other.Index && Version == other.Version && IsAbsolute == other.IsAbsolute;
        }
    }

    public class EntityComparer : IEqualityComparer<Entity>
    {
        public bool Equals(Entity a, Entity b)
        {
            return a == b;
        }

        public int GetHashCode(Entity obj)
        {
            return obj.GetHashCode();
        }
    }
}
