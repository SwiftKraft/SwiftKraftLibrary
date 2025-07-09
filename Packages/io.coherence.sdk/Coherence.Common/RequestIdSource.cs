// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Connection
{
    using System;
    using System.Threading;

    public class RequestIdSource
    {
        private int requestCounter;
        private readonly string idBase;

        public (string, string connectionId) IdBaseLogParam => ("connId", idBase);

        public RequestIdSource()
        {
            idBase = Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        public string Next()
        {
            int counter = GetNextRequestCounter();
            return $"{idBase}:{counter}";
        }

        public string Next(out int counter)
        {
            counter = GetNextRequestCounter();
            return $"{idBase}:{counter}";
        }

        private int GetNextRequestCounter()
        {
            int counter = Interlocked.Increment(ref requestCounter);
            for (; counter == 0; counter = Interlocked.Increment(ref requestCounter))
            {
                // Loop until counter is non-zero
            }

            return counter;
        }
    }
}
