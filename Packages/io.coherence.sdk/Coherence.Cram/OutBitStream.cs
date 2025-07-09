// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cram
{
    using Brook;
    using System;
    using System.Numerics;

    public struct OutBitStream
    {
        private readonly IOutBitStream outstream;

        public OutBitStream(IOutBitStream outstream)
        {
            this.outstream = outstream;
        }

        public unsafe void WriteDouble(double value)
        {
            long converted = *(long*)&value;
            outstream.WriteUint64((ulong)converted);
        }

        public void WriteFloat(float value, in FloatMeta meta)
        {
            switch (meta.Compression)
            {
                case FloatCompression.None:
                    WriteFullFloat(value);
                    break;
                case FloatCompression.Truncated:
                    WriteTruncatedFloat(value, meta.BitCount);
                    break;
                case FloatCompression.FixedPoint:
                    WriteFixedDouble(value, meta.Minimum, meta.Maximum, meta.Precision);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(meta), meta.Compression, "Unsupported compression type");
            }
        }

        public void WriteVector2(in Vector2 v, in FloatMeta meta)
        {
            WriteFloat(v.X, meta);
            WriteFloat(v.Y, meta);
        }

        public void WriteVector3(in Vector3 v, in FloatMeta meta)
        {
            WriteFloat(v.X, meta);
            WriteFloat(v.Y, meta);
            WriteFloat(v.Z, meta);
        }

        public void WriteVector4(in Vector4 v, in FloatMeta meta)
        {
            WriteFloat(v.X, meta);
            WriteFloat(v.Y, meta);
            WriteFloat(v.Z, meta);
            WriteFloat(v.W, meta);
        }

        public void WriteQuaternion(in Quaternion q, int bitsPerComponent)
        {
            var (xCursor, yCursor, zCursor, sign) = Utils.CalculateCursorsForQuaternionCompression(q, bitsPerComponent);
            var mask = bitsPerComponent < 32 ? ((uint)1 << bitsPerComponent) - 1 : uint.MaxValue;

            outstream.WriteBits(xCursor & mask, bitsPerComponent);
            outstream.WriteBits(yCursor & mask, bitsPerComponent);
            outstream.WriteBits(zCursor & mask, bitsPerComponent);
            outstream.WriteBits(sign, 1);
        }

        private unsafe void WriteFullFloat(float value)
        {
            int converted = *(int*)&value;
            outstream.WriteUint32((uint)converted);
        }

        private void WriteTruncatedFloat(float value, int bits)
        {
            FloatMeta.EnsureValidTruncatedBitCount(bits);

            var truncated = Utils.TruncateFloat(value, bits);
            outstream.WriteBits(truncated, bits);
        }

        // TODO: Support for 64-bits
        private void WriteFixedDouble(double value, int minRange, int maxRange, double precision)
        {
            var cursor = Utils.CalculateCursorForFixedFloatCompression(value, minRange, maxRange, precision, out int bits);

            outstream.WriteBits(cursor, bits);
        }
    }
}
