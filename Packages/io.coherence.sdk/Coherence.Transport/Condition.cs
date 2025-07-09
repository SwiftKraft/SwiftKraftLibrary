// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Connection
{
    using System;

    /// <summary>
    ///     Settings for connections to manage debuggging artificial network conditions.
    /// </summary>
    [Serializable]
    public struct Condition
    {
        /// <summary>
        ///     Delay, in seconds, when sending a packet.  After a packet is created it is artifically delayed
        ///     by this amount.
        /// </summary>
        public float sendDelaySec;

        /// <summary>
        ///     Rate, in % from 0.0 - 1.0, of packets dropped when sending.
        /// </summary>
        public float sendDropRate;

        /// <summary>
        ///     Delay, in seconds, when receiving a packet.  After a packet is received it is artifically delayed
        ///     by this amount before sending to the processing system.
        /// </summary>
        public float receiveDelaySec;

        /// <summary>
        ///     Rate, in % from 0.0 - 1.0, of packets dropped when receiving.
        /// </summary>
        public float receiveDropRate;

        /// <summary>
        ///     Rate, in seconds, to resend duplicates of the last send packet.
        /// </summary>
        public float sendDuplicateRateSec;

        /// <summary>
        ///    Rate, in % from 0.0 - 1.0, of packets tampered when sending.
        /// </summary>
        public float packetTamperRate;

        /// <summary>
        ///    Rate, in % from 0.0 - 1.0, of bits tampered in a packet when sending.
        /// </summary>
        public float tamperRate;

        /// <summary>
        ///    Point, in % from 0.0 - 1.0, in the packet after which the tampering starts.
        /// </summary>
        public float tamperStart;

        /// <summary>
        ///    Deviation of the tamper start point.
        /// </summary>
        public float tamperStartDeviation;
    }
}
