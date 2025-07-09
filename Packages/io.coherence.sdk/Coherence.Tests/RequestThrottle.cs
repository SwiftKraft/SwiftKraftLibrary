// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Tests
{
    using System.Collections.Generic;

    /// <summary>
    /// Limits the number of documentation requests in flight.
    /// </summary>
    internal class RequestThrottle
    {
        private readonly int maxRequestsInFlight;
        private readonly List<DocumentationRequest> requests;
        private readonly List<DocumentationRequest> inFlight = new();

        public RequestThrottle(IEnumerable<DocumentationRequest> requests, int maxRequestsInFlight = 3)
        {
            this.maxRequestsInFlight = maxRequestsInFlight;
            this.requests = new List<DocumentationRequest>(requests);
        }

        /// <summary>
        /// Returns true if all requests are done.
        /// </summary>
        public bool CheckDone()
        {
            Update(out var done);
            return done;
        }

        private void Update(out bool done)
        {
            // If there are no requests left and no requests in flight, we're done.
            if (requests.Count == 0 && inFlight.Count == 0)
            {
                done = true;
                return;
            }

            // Update in-flight requests and remove those that are done.
            for (var i = inFlight.Count - 1; i >= 0; i--)
            {
                var req = inFlight[i];
                req.Update();
                if (req.Done)
                {
                    inFlight.RemoveAt(i);
                }
            }

            // Start new requests if there are slots available.
            while (inFlight.Count < maxRequestsInFlight && requests.Count > 0)
            {
                var request = requests[0];
                requests.RemoveAt(0);
                inFlight.Add(request);
            }

            done = false;
        }
    }
}
