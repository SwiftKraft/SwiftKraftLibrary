// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook
{
    using System;
    using Debugging;
    using Log;

    public class InBitStream : IInBitStream
    {
        private readonly IOctetReader octetReader;
        private int remainingBits;
        private uint data;
        private int position;
        private readonly int bitSize;

        private static readonly Logger Logger = Log.GetLogger<InBitStream>();

        public int Position => position;

        public InBitStream(IOctetReader octetReader, int bitSize)
        {
            this.octetReader = octetReader;
            this.bitSize = bitSize;
        }

        public void ReadBytesUnaligned(Span<byte> buffer, int bitCount)
        {
            DbgAssert.ThatFmt(buffer.Length * 8 >= bitCount,
                "Bit count {0} is too large for buffer of size {1}B", bitCount, buffer.Length);

            const int chunkBitSize = 8;
            var restBitCount = bitCount % chunkBitSize;
            var chunkCount = bitCount / chunkBitSize;

            for (var i = 0; i < chunkCount; i++)
            {
                buffer[i] = (byte)ReadBits(chunkBitSize);
            }

            if (restBitCount > 0)
            {
                buffer[chunkCount] = (byte)(ReadBits(restBitCount) << (chunkBitSize - restBitCount));
            }
        }

        public ushort ReadUint16()
        {
            return (ushort)ReadBits(16);
        }

        public int ReadSignedBits(int count)
        {
            var sign = ReadBits(1);
            var v = (int)ReadBits(count - 1);

            if (sign != 0)
            {
                v = -v;
            }

            return v;
        }

        public int RemainingBits()
        {
            return bitSize - position;
        }

        public bool IsEof => position == bitSize;

        public short ReadInt16()
        {
            return (short)ReadSignedBits(16);
        }

        public uint ReadUint32()
        {
            return ReadBits(32);
        }

        public ulong ReadUint64()
        {
            ulong upper = ReadRawBits(32);
            var result = upper << 32;
            ulong lower = ReadRawBits(32);

            result |= lower;

            return result;
        }

        public byte ReadUint8()
        {
            return (byte)ReadBits(8);
        }

        private uint MaskFromCount(int count)
        {
            return count == 32 ? 0xffffffff : ((uint)1 << count) - 1;
        }

        private uint ReadOnce(int bitsToRead)
        {
            if (bitsToRead == 0)
            {
                return 0;
            }

            if (bitsToRead > remainingBits)
            {
                throw new EndOfStreamException(bitsToRead, remainingBits);
            }

            var mask = MaskFromCount(bitsToRead);
            var shiftPos = remainingBits - bitsToRead;

            if (position + bitsToRead > bitSize)
            {
                Logger.Warning(Warning.InBitStreamEOS, ("position", position), ("bitsToRead", bitsToRead), ("bitSize", bitSize));

                var s = $"Position:{position} bitsToRead:{bitsToRead} bitSize:{bitSize}";
                throw new EndOfStreamException(s);
            }

            position += bitsToRead;

            uint bits = 0;

            if (shiftPos < 32)
            {
                bits = (data >> shiftPos) & mask;
            }

            // logger.Info("READ mask {0:X} shift:{1} bits:{2:X} data:{3:X} {4:X}", mask, shiftPos, bits, data, (data >> shiftPos));
            remainingBits -= bitsToRead;
            return bits;
        }

        private void Fill()
        {
            var octetsToRead = 4;

            if (octetsToRead > octetReader.RemainingOctetCount)
            {
                octetsToRead = octetReader.RemainingOctetCount;
            }

            uint newData = 0;
            for (var i = 0; i < octetsToRead; ++i)
            {
                newData <<= 8;
                var octet = octetReader.ReadOctet();
                newData |= octet;
            }

            data = newData;
            remainingBits = octetsToRead * 8;
            // logger.Info("Data is now {0:X} octetsToRead:{1} Remaining:{2}", data, octetsToRead, remainingBits);
        }

        public uint ReadBits(int count)
        {
            if (count > 32)
            {
                throw new Exception("Max 32 bits to read");
            }

            if (count > remainingBits)
            {
                var secondCount = count - remainingBits;
                var v = ReadOnce(remainingBits);
                Fill();

                v <<= secondCount;
                v |= ReadOnce(secondCount);
                return v;
            }
            else
            {
                var v = ReadOnce(count);
                return v;
            }
        }

        public uint ReadRawBits(int count)
        {
            return ReadBits(count);
        }
    }
}
