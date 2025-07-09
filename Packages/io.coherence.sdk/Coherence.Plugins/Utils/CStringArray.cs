// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Plugins.Utils
{
    using System;
    using System.Runtime.InteropServices;

    internal class CStringArray : IDisposable
    {
        private IntPtr array;
        private IntPtr[] items;

        public IntPtr Ptr => array;
        public int Length => items?.Length ?? 0;

        public CStringArray(string[] source) => AllocGlobalHeap(source);

        public void Dispose()
        {
            if (array != IntPtr.Zero)
            {
                foreach (var item in items)
                {
                    Marshal.FreeHGlobal(item);
                }

                Marshal.FreeHGlobal(array);

                array = IntPtr.Zero;
                items = null;
            }
        }

        private void AllocGlobalHeap(string[] source)
        {
            if (source?.Length > 0)
            {
                array = Marshal.AllocHGlobal(source.Length * IntPtr.Size);
                items = new IntPtr[source.Length];

                for (var i = 0; i < source.Length; i++)
                {
                    items[i] = Marshal.StringToHGlobalAnsi(source[i]);
                    Marshal.WriteIntPtr(array, i * IntPtr.Size, items[i]);
                }
            }
        }
    }
}
