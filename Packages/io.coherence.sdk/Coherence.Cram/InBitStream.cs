// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cram
{
    using Brook;
    using System;
    using System.Numerics;

    public struct InBitStream
    {
        private readonly IInBitStream instream;

        public InBitStream(IInBitStream instream)
        {
            this.instream = instream;
        }

        public unsafe double ReadDouble()
        {
            ulong raw = instream.ReadUint64();
            return *(double*)&raw;
        }

        public float ReadFloat(in FloatMeta meta)
        {
            switch (meta.Compression)
            {
                case FloatCompression.None:
                    return ReadFullFloat();
                case FloatCompression.Truncated:
                    return ReadTruncatedFloat(meta.BitCount);
                case FloatCompression.FixedPoint:
                    return (float)ReadFixedDouble(meta.Minimum, meta.Maximum, meta.Precision);
                default:
                    throw new ArgumentOutOfRangeException(nameof(meta), meta.Compression, "Unsupported compression type");
            }
        }

        public Vector2 ReadVector2(in FloatMeta meta)
        {
            return new Vector2(
                ReadFloat(meta),
                ReadFloat(meta));
        }

        public Vector3 ReadVector3(in FloatMeta meta)
        {
            return new Vector3(
                ReadFloat(meta),
                ReadFloat(meta),
                ReadFloat(meta));
        }

        public Vector4 ReadVector4(in FloatMeta meta)
        {
            return new Vector4(
                ReadFloat(meta),
                ReadFloat(meta),
                ReadFloat(meta),
                ReadFloat(meta));
        }

        public Quaternion ReadQuaternion(int bitsPerComponent)
        {
            int xCursor = (int)instream.ReadBits(bitsPerComponent);
            int yCursor = (int)instream.ReadBits(bitsPerComponent);
            int zCursor = (int)instream.ReadBits(bitsPerComponent);
            uint wSign = instream.ReadBits(1);

            return Utils.UncompressQuaternion(bitsPerComponent, xCursor, yCursor, zCursor, wSign);
        }

        private unsafe float ReadFullFloat()
        {
            uint raw = instream.ReadUint32();
            return *(float*)&raw;
        }

        private unsafe float ReadTruncatedFloat(int bits)
        {
            FloatMeta.EnsureValidTruncatedBitCount(bits);

            uint raw = instream.ReadBits(bits);
            raw = raw << (32 - bits);
            return *(float*)&raw;
        }

        private double ReadFixedDouble(int minRange, int maxRange, double precision)
        {
            int bits = Utils.GetNumberOfBitsForFixedPointCompression(minRange, maxRange, precision, out _, out _);
            var cursor = instream.ReadBits(bits);

            return Utils.UncompressFixedPoint(cursor, minRange, maxRange, precision);
        }
    }
}
