// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook.Octet
{
    using System;
    using System.IO;

    public class InOctetStream : IInOctetStream
    {
        public uint Length => (uint)stream.Length;
        public uint Position => (uint)stream.Position;
        public int RemainingOctetCount => (int)Length - (int)Position;

        private byte[] Buffer => stream.GetBuffer();

        private readonly MemoryStream stream;
        private readonly BinaryReader reader;
        private readonly bool resettable;

        public InOctetStream(byte[] data)
        {
            stream = new MemoryStream(data, 0, data.Length, true, true);
            reader = new BinaryReader(stream);
            resettable = false;
        }

        protected InOctetStream(int capacity)
        {
            stream = new MemoryStream(capacity);
            reader = new BinaryReader(stream);
            resettable = true;
        }

        protected void ResetAndWrite(ReadOnlySpan<byte> data)
        {
            if (!resettable)
            {
                throw new InvalidOperationException("Stream is not resettable");
            }

            stream.Position = 0;
            stream.Write(data);

            stream.Position = 0;
            stream.SetLength(data.Length);
        }

        public ReadOnlySpan<byte> GetBuffer()
        {
            return new ReadOnlySpan<byte>(Buffer, 0, (int)Length);
        }

        public ushort ReadUint16()
        {
            return reader.ReadUInt16();
        }

        public uint ReadUint32()
        {
            return reader.ReadUInt32();
        }

        public ulong ReadUint64()
        {
            return reader.ReadUInt64();
        }

        public byte ReadUint8()
        {
            return reader.ReadByte();
        }

        public byte ReadOctet()
        {
            return ReadUint8();
        }

        public ReadOnlySpan<byte> ReadOctets(int octetCount)
        {
            if (Position + octetCount > Length)
            {
                throw new Exception($"Reading too. Pos: {Position}, Len: {Length}, Read: {octetCount}");
            }

            var slice = Buffer.AsSpan((int)Position, octetCount);
            stream.Position += octetCount;

            return slice;
        }

        public override string ToString()
        {
            return BitConverter.ToString(Buffer);
        }
    }
}
