// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common
{
    public struct Ping
    {
        /// <summary>
        ///     The number of milliseconds it takes for a message to travel from the client to the Replication Server.
        /// </summary>
        public int AverageLatencyMs;

        /// <summary>
        ///     The number of milliseconds it takes for a message to travel from the client to the Replication Server and back to the client.
        /// </summary>
        public int AverageRoundTripMs => AverageLatencyMs * 2;

        /// <summary>
        ///     If true, the ping is considered stable (according to the <see cref="ConnectionSettings.PingSettings" />).
        /// </summary>
        public bool IsStable;

        /// <summary>
        ///     The non-averaged latency reported with the latest packet from the server.
        /// </summary>
        public int LatestLatencyMs;

        public static implicit operator int(Ping ping)
        {
            return ping.AverageRoundTripMs;
        }

        public override string ToString()
        {
            return $"{nameof(AverageRoundTripMs)}: {AverageRoundTripMs}, {nameof(IsStable)}: {IsStable}";
        }
    }
}
