// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Plugins.Utils
{
    using System;
    using System.Runtime.InteropServices;

    internal class InteropBuffer : IDisposable
    {
        private GCHandle handle;

        public byte[] Buffer { get; }
        public IntPtr PinnedPtr => handle.AddrOfPinnedObject();

        public InteropBuffer(int size)
        {
            Buffer = new byte[size];
            handle = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
        }

        public InteropBuffer(byte[] buffer)
        {
            Buffer = buffer;
            handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        }

        public void Dispose()
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }
}
