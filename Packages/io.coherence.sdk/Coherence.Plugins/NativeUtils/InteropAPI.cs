// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Plugins.NativeUtils
{
    using System;
    using System.Runtime.InteropServices;

    internal static class InteropAPI
    {
        private const string DLL_NAME = "native_utils";

        [DllImport(DLL_NAME, EntryPoint = "TRFindSuspendedThreads")]
        public static extern int TRFindSuspendedThreads(Int32 pid, UInt64[] buff, UInt32 len, bool verbose, out UInt64 timeMs);

        [DllImport(DLL_NAME, EntryPoint = "TRResumeThread")]
        public static extern bool TRResumeThread(UInt64 threadID);

        [DllImport(DLL_NAME, EntryPoint = "TRSuspendThread")]
        public static extern bool TRSuspendThread(UInt64 threadID);

        [DllImport(DLL_NAME, EntryPoint = "TRGetCurrentThreadId")]
        public static extern UInt64 TRGetCurrentThreadId();
    }
}
