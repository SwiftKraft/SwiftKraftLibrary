// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    public enum DebugSerializeType
    {
        SignedBits,
        UnsignedBits,
        Int64,
        Uint64,
    }

    public static class DebugStreamTypes
    {
        public static readonly int TypeBitCount = 3;
        public static readonly int BitCountBitCount = 7;

        public static int DebugBitsSize(int numWrites)
        {
            return numWrites * (TypeBitCount + BitCountBitCount);
        }

        public static string TypeToString(DebugSerializeType type)
        {
            return type.ToString();
        }
    }
}
