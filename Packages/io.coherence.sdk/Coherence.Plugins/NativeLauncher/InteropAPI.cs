// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Plugins.NativeLauncher
{
    using System;
    using System.Runtime.InteropServices;

    internal static class InteropAPI
    {
        public enum NlError
        {
            InvalidVal = -100,
            TimedOut = -101,
            Pipe = -102,
            WouldBlock = -103,
        }

        public enum NlStream
        {
            Out = 0,
            Err,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NlStartupParams
        {
            public string executablePath;
            public IntPtr arguments; // const char* const* arguments
            public UInt32 argumentsCount;
            public IntPtr envVars; // const char* const* envVars
            public UInt32 envVarsCount;
            public byte nonBlocking;
        }

        private const string DLL_NAME = "native_utils";

        [DllImport(DLL_NAME, EntryPoint = "NLCreate")]
        public static extern IntPtr Create(NlStartupParams startupParams);

        [DllImport(DLL_NAME, EntryPoint = "NLDestroy")]
        public static extern void Destroy(IntPtr processHandle);

        [DllImport(DLL_NAME, EntryPoint = "NLStart")]
        public static extern Int32 Start(IntPtr processHandle, out Int32 pid);

        [DllImport(DLL_NAME, EntryPoint = "NLStopAndWait")]
        public static extern Int32 StopAndWait(IntPtr processHandle, Int32 timeout);

        [DllImport(DLL_NAME, EntryPoint = "NLWait")]
        public static extern Int32 Wait(IntPtr processHandle, Int32 timeout);

        [DllImport(DLL_NAME, EntryPoint = "NLReadFromStream")]
        public static extern Int32 ReadFromStream(IntPtr processHandle, NlStream stream, IntPtr buffer,
            UInt32 bufferSize);

        public static string GetErrorString(Int32 error)
        {
            return Marshal.PtrToStringAnsi(GetErrorString(error));

            [DllImport(DLL_NAME, EntryPoint = "NLGetErrorString")]
            static extern IntPtr GetErrorString(Int32 error);
        }
    }
}
