// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook.Shared
{
    using System;

    public sealed class OctetQueue
    {
        private int head;
        private int tail;
        private byte[] buffer;

        public int Length { get; private set; }

        public OctetQueue(int capacity)
        {
            buffer = new byte[capacity];
        }

        public void Enqueue(byte[] buffer, int offset, int size)
        {
            if (size == 0)
            {
                return;
            }

            if ((Length + size) > this.buffer.Length)
            {
                throw new Exception("Buffer is out of capacity");
            }

            if (head < tail)
            {
                int rightLength = this.buffer.Length - tail;

                if (rightLength >= size)
                {
                    Buffer.BlockCopy(buffer, offset, this.buffer, tail, size);
                }
                else
                {
                    Buffer.BlockCopy(buffer, offset, this.buffer, tail, rightLength);
                    Buffer.BlockCopy(buffer, offset + rightLength, this.buffer, 0, size - rightLength);
                }
            }
            else
            {
                Buffer.BlockCopy(buffer, offset, this.buffer, tail, size);
            }

            tail = (tail + size) % this.buffer.Length;
            Length += size;
        }

        public int Peek(byte[] buffer, int offset, int size)
        {
            if (size > Length)
            {
                size = Length;
            }

            if (size == 0)
            {
                return 0;
            }

            if (head < tail)
            {
                Buffer.BlockCopy(this.buffer, head, buffer, offset, size);
            }
            else
            {
                int rightLength = this.buffer.Length - head;

                if (rightLength >= size)
                {
                    Buffer.BlockCopy(this.buffer, head, buffer, offset, size);
                }
                else
                {
                    Buffer.BlockCopy(this.buffer, head, buffer, offset, rightLength);
                    Buffer.BlockCopy(this.buffer, 0, buffer, offset + rightLength, size - rightLength);
                }
            }

            return size;
        }

        public void Skip(int size)
        {
            if (size > Length)
            {
                throw new Exception(string.Format("Can not skip %d", size));
            }

            head = (head + size) % buffer.Length;
            Length -= size;

            if (Length == 0)
            {
                head = 0;
                tail = 0;
            }
        }

        public override string ToString()
        {
            return string.Format("[OctetQueue: length={0} (head:{1}, tail:{2})]", Length, head, tail);
        }
    }
}
