// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System;
    using Debugging;

    public class OutBitStream : IOutBitStream
    {
        private const int AccumulatorSize = 32;
        private const uint MaxFilter = 0xffffffff;

        public uint Accumulator { get; private set; }
        public bool IsFull { get; private set; }
        public uint Position { get; private set; }
        public uint OverflowBitCount { get; private set; }

        private readonly IOctetWriter octetWriter;
        private readonly uint octetWriterInitialPosition;
        private int remainingBits = AccumulatorSize;

        [ThreadStatic] private static byte[] octetArrayCache;
        private static byte[] OctetArrayCache
        {
            get
            {
                if (octetArrayCache == null)
                {
                    octetArrayCache = new byte[4];
                }
                return octetArrayCache;
            }
        }

        public OutBitStream(IOctetWriter octetWriter)
        {
            this.octetWriter = octetWriter;
            this.octetWriterInitialPosition = octetWriter.Position;
        }

        public void WriteUint16(ushort v)
        {
            WriteBits(v, 16);
        }

        public void WriteInt16(short v)
        {
            WriteSignedBits(v, 16);
        }

        public void WriteUint32(uint v)
        {
            WriteBits(v, 32);
        }

        public void WriteUint64(ulong v)
        {
            WriteBits((uint)(v >> 32), 32);
            WriteBits((uint)(v & 0xffffffff), 32);
        }

        public void WriteUint8(byte v)
        {
            WriteBits(v, 8);
        }

        public void WriteBytesUnaligned(ReadOnlySpan<byte> bytes, int bitCount)
        {
            DbgAssert.ThatFmt(bytes.Length * 8 >= bitCount,
                "Bit count {0} is too large for {1} bytes", bitCount, bytes.Length);

            const int chunkBitSize = 8;
            var restBitCount = bitCount % chunkBitSize;
            var chunkCount = bitCount / chunkBitSize;

            for (var i = 0; i < chunkCount; ++i)
            {
                WriteUint8(bytes[i]);
            }

            if (restBitCount > 0)
            {
                var restBits = (bytes[chunkCount] >> (8 - restBitCount));
                WriteBits((byte)restBits, restBitCount);
            }
        }

        public void WriteFromStream(IInBitStream inBitStream, int bitCount)
        {
            const int ChunkBitSize = AccumulatorSize;
            int restBitCount = bitCount % ChunkBitSize;
            int chunkCount = bitCount / ChunkBitSize;

            for (int i = 0; i < chunkCount; ++i)
            {
                uint data = inBitStream.ReadRawBits(ChunkBitSize);
                WriteRawBits(data, ChunkBitSize);
            }

            if (restBitCount > 0)
            {
                uint restData = inBitStream.ReadRawBits(restBitCount);
                WriteRawBits(restData, restBitCount);
            }
        }

        public void Flush()
        {
            WriteLast();
        }

        public uint RemainingBitCount => (octetWriter.Capacity - octetWriterInitialPosition) * 8 - Position;

        private uint MaskFromCount(int count)
        {
            return count < AccumulatorSize ? ((uint)1 << count) - 1 : MaxFilter;
        }

        public void Seek(uint newPosition)
        {
            if (newPosition > Position)
            {
                throw new ArgumentOutOfRangeException("Can't seek forwards.");
            }
            else if (newPosition < Position)
            {
                WriteLast();

                uint leftOverBits = newPosition % 8;
                uint octets = newPosition / 8;
                octets = Math.Min(octets + octetWriterInitialPosition, octetWriter.Capacity - 1);
                if (leftOverBits > 0)
                {
                    remainingBits = AccumulatorSize - (int)leftOverBits;
                    uint bits = octetWriter.Octets[(int)octets];
                    uint mask = ~MaskFromCount(remainingBits);
                    Accumulator = (bits << (AccumulatorSize - 8)) & mask;
                }
                octetWriter.Seek(octets);
                Position = newPosition;

                IsFull = RemainingBitCount == 0;
                OverflowBitCount = 0;
            }
        }

        private void WriteRest(uint v, int count, int bitsToKeepFromLeft)
        {
            uint ov = v;

            ov >>= count - bitsToKeepFromLeft;
            ov &= MaskFromCount(bitsToKeepFromLeft);
            ov <<= remainingBits - bitsToKeepFromLeft;
            remainingBits -= bitsToKeepFromLeft;
            Position += (uint)bitsToKeepFromLeft;
            Accumulator |= ov;
        }

        private void WriteOctets()
        {
            ConvertToByteArray(Accumulator, OctetArrayCache);

            octetWriter.WriteOctets(OctetArrayCache);
            Accumulator = 0;
            remainingBits = AccumulatorSize;
        }

        private void WriteLast()
        {
            if (remainingBits == AccumulatorSize)
            {
                return;
            }

            int bitsWritten = AccumulatorSize - remainingBits;
            int octetCount = ((bitsWritten - 1) / 8) + 1;
            for (int i = 0; i < octetCount; i++)
            {
                if (octetWriter.RemainingOctetCount == 0)
                {
                    IsFull = true;
                    break;
                }

                byte outOctet = (byte)((Accumulator & 0xff000000) >> 24);
                Accumulator <<= 8;
                octetWriter.WriteOctet(outOctet);
            }

            Accumulator = 0;
            remainingBits = AccumulatorSize;

            IsFull = IsFull || octetWriter.RemainingOctetCount == 0;
        }

        public void WriteSignedBits(int v, int count)
        {
            int sign = v < 0 ? 1 : 0;

            if (sign != 0)
            {
                v = -v;
            }

            WriteBits((uint)sign, 1);
            WriteBits((uint)v, count - 1);
        }

        public void WriteBits(uint v, int count)
        {
            if ((ulong)1 << count <= (ulong)v)
            {
                throw new Exception($"insufficient bits ({count}) for value ({v}), max value is {((ulong)1 << count) - 1}");
            }

            if (count > AccumulatorSize)
            {
                throw new Exception($"Max {AccumulatorSize} bits to write ");
            }

            if (IsFull || count > RemainingBitCount)
            {
                if (!IsFull)
                {
                    OverflowBitCount += (uint)count - RemainingBitCount;
                }
                else
                {
                    OverflowBitCount += (uint)count;
                }

                IsFull = true;
                return;
            }

            if (count > remainingBits)
            {
                int firstWriteCount = remainingBits;
                WriteRest(v, count, firstWriteCount);
                WriteOctets();
                WriteRest(v, count - firstWriteCount, count - firstWriteCount);
            }
            else
            {
                WriteRest(v, count, count);
            }

            IsFull = IsFull || RemainingBitCount == 0;
        }

        public void WriteRawBits(uint v, int count)
        {
            WriteBits(v, count);
        }

        private static unsafe void ConvertToByteArray(uint value, byte[] bytes)
        {
            // See: decompiled BitConverter.GetByte(uint)
            fixed (byte* numPtr = bytes)
                *(int*)numPtr = (int)value;

            (bytes[0], bytes[3]) = (bytes[3], bytes[0]);
            (bytes[1], bytes[2]) = (bytes[2], bytes[1]);
        }
    }
}
