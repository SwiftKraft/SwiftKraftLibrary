// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport
{
    using System;

    /// <summary>
    /// Defines the transport to be used when connecting using the default transport factory.
    /// </summary>
    public enum TransportType : byte
    {
        /// <summary>
        /// UDP will be used as a primary transport, falling back to TCP if connection over UDP times out.
        /// </summary>
        UDPWithTCPFallback,

        /// <summary>
        /// Only UDP transport will be used.
        /// </summary>
        UDPOnly,

        /// <summary>
        /// Only TCP transport will be used.
        /// </summary>
        TCPOnly
    }

    public enum TransportConfiguration
    {
        /// <summary>
        /// With this option, native core will use native transports, while managed core will use managed transports.
        /// </summary>
        Default,

        /// <summary>
        /// Both cores will use managed transports.
        /// </summary>
        ManagedOnly,

        /// <summary>
        /// In case TransportType uses UDP, both cores will use new, experimental, allocation-less, managed UDPv2.
        /// </summary>
        ManagedWithExperimentalUDP
    }

    [Obsolete("Replaced by TransportType.")]
    [Deprecated("04/2024", 1, 3, 0, Reason = "Replaced by TransportType.")]
    public enum DefaultTransportMode
    {
        UDPWithTCPFallback,
        UDPOnly,
        TCPOnly,
        UDPExperimental
    }
}
