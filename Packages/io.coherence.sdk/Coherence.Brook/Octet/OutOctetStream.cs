// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook.Octet
{
    using System;
    using System.IO;

    public class OutOctetStream : IOutOctetStream
    {
        public ReadOnlySpan<byte> Octets => new(stream.GetBuffer(), 0, (int)Position);

        public uint Capacity => (uint)stream.Capacity;
        public uint Position => (uint)stream.Position;
        public uint RemainingOctetCount => Capacity - Position;

        private BinaryWriter writer;
        private MemoryStream stream;

        public OutOctetStream(int capacity) : this(new byte[capacity])
        {
        }

        public OutOctetStream(byte[] buffer)
        {
            Init(buffer);
        }

        private void Init(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (buffer.Length == 0)
            {
                throw new ArgumentException("Buffer length must be greater than 0", nameof(buffer));
            }

            stream = new MemoryStream(buffer, 0, buffer.Length, true, true);
            writer = new BinaryWriter(stream);
        }

        protected void ResizeAndReset(int capacity)
        {
            if (capacity > stream.Capacity)
            {
                Init(new byte[capacity]);
                return;
            }

            var buffer = stream.GetBuffer();
            stream = new MemoryStream(buffer, 0, capacity, true, true);
            writer = new BinaryWriter(stream);
        }

        protected void Reset()
        {
            stream.Position = 0;
            stream.SetLength(0);
        }

        public void WriteUint16(ushort data)
        {
            writer.Write(data);
        }

        public void WriteUint32(uint data)
        {
            writer.Write(data);
        }

        public void WriteUint64(ulong data)
        {
            writer.Write(data);
        }

        public void WriteUint8(byte data)
        {
            writer.Write(data);
        }

        public void WriteOctet(byte v)
        {
            WriteUint8(v);
        }

        public void WriteOctets(byte[] data)
        {
            writer.Write(data);
        }

        public void WriteOctets(ReadOnlySpan<byte> data)
        {
            writer.Write(data);
        }

        public void Seek(uint newPosition)
        {
            stream.Position = newPosition;
        }

        public ArraySegment<byte> Close()
        {
            var buffer = stream.GetBuffer();
            var span = new ArraySegment<byte>(buffer, 0, (int)stream.Position);

            return span;
        }
    }
}
