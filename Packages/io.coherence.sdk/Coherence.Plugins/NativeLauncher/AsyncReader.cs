// Copyright (c) coherence ApS.
// See the license file in the package root for more information.


namespace Coherence.Plugins.NativeLauncher
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;
    using Utils;

    // AsyncReader and LineSplitter.cs are based on
    // https://github.com/dotnet/runtime/blob/598d5f729a0d114a5909487e618eb842c6b45d58/src/libraries/System.Diagnostics.Process/src/System/Diagnostics/AsyncStreamReader.cs
    internal class AsyncReader
    {
        private const int BufferSize = 1024;

        private readonly IntPtr processHandle;
        private readonly CancellationTokenSource cts = new();

        private readonly LineSplitter lineSplitter = new(BufferSize);
        private readonly Action<string> userCallback;

        private bool reading;
        private bool cancelled;

        public AsyncReader(IntPtr handle, Action<string> callback)
        {
            processHandle = handle;
            userCallback = callback;
        }

        public void StartReading()
        {
            if (!reading)
            {
                reading = true;
                _ = ReadProcessOutput(cts.Token);
            }
        }

        private async Task ReadProcessOutput(CancellationToken token)
        {
            var encoding = Encoding.UTF8;
            var byteBuffer = new byte[BufferSize];
            var charBuffer = new char[encoding.GetMaxCharCount(BufferSize)];

            while (!token.IsCancellationRequested)
            {
                using var iopBuffer = new InteropBuffer(byteBuffer);

                var res = InteropAPI.ReadFromStream(
                    processHandle,
                    InteropAPI.NlStream.Out,
                    iopBuffer.PinnedPtr,
                    (uint)iopBuffer.Buffer.Length
                );

                if (res <= 0 && res != (int)InteropAPI.NlError.WouldBlock)
                {
                    break;
                }

                if (res > 0)
                {
                    var bytesRead = res;
                    var charCount = encoding.GetChars(iopBuffer.Buffer, 0, bytesRead, charBuffer, 0);
                    var newLines = lineSplitter.Append(charBuffer, charCount);
                    FlushMessages(newLines);
                }

                await Task.Yield();
            }

            var endMessages = new List<string>(2);
            var remaining = lineSplitter.Flush();
            if (remaining.Length > 0)
            {
                endMessages.Add(remaining);
            }

            endMessages.Add(null); // Signal EOF.

            FlushMessages(endMessages);
        }

        public void StopReading()
        {
            cancelled = true;
            cts.Cancel();
        }

        private void FlushMessages(IReadOnlyList<string> messages)
        {
            try
            {
                foreach (var message in messages)
                {
                    if (!cancelled)
                    {
                        userCallback(message);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
