// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using System;
    using Brook;

    internal class SequenceBuffer<T> where T : class
    {
        private const int MinBufferSize = 64;
        private const int MaxBufferSize = 4096;

        private readonly T[] messages;
        protected int Size => messages.Length;

        protected SequenceBuffer(int size)
        {
            AssertValidSize(size);

            messages = new T[size];
            for (var i = 0; i < size; i++)
            {
                messages[i] = null;
            }
        }

        protected T Find(MessageID id) => messages[Index(id)];

        protected void Insert(MessageID id, T data) => messages[Index(id)] = data;

        protected void Remove(MessageID id) => messages[Index(id)] = null;

        private int Index(MessageID id) => id.Value % Size;

        protected void ClearBuffer()
        {
            for (var i = 0; i < messages.Length; ++i)
            {
                messages[i] = null;
            }
        }

        private void AssertValidSize(int size)
        {
            if (size != NextPowerOfTwo(size))
            {
                throw new ArgumentException($"Invalid buffer size {size}. Must be power of two");
            }

            if (size < MinBufferSize || size > MaxBufferSize)
            {
                throw new ArgumentException($"Invalid buffer size {size}. Min={MinBufferSize}, Max={MaxBufferSize}");
            }
        }

        private int NextPowerOfTwo(int x) => (x > 2) ? (int)Math.Pow(2, (int)Math.Log(x - 1, 2) + 1) : 1;
    }
}
