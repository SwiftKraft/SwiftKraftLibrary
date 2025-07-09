// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Pooling
{
    using System;

    internal class Buffer<T>
    {
        public T[] Data { get; private set; }
        public int Length { get; private set; }
        public int Capacity => Data.Length;

        public Buffer(int capacity)
        {
            Data = new T[capacity];
        }

        public Buffer(ReadOnlySpan<T> data)
        {
            Data = data.ToArray();
            Length = data.Length;
        }

        public ReadOnlySpan<T> AsSpan() => new(Data, 0, Length);

        public void Accomodate(ReadOnlySpan<T> data)
        {
            var wontFit = data.Length > Data.Length;
            if (wontFit)
            {
                Data = data.ToArray();
                Length = data.Length;
                return;
            }

            data.CopyTo(Data);
            Length = data.Length;
        }
    }
}
