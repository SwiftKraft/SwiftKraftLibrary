// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Relay
{
    /// <summary>
    ///     Represents a relay host, that is, a client that accepts other client connections made via
    ///     external service and relays them to the local Replication Server. Those clients are represented
    ///     via <see cref="IRelayConnection" /> in the relay system.
    /// </summary>
    public interface IRelay
    {
        /// <summary>Injected by the <see cref="CoherenceRelayManager" />.</summary>
        CoherenceRelayManager RelayManager { get; set; }

        /// <summary>
        ///     Called by the <see cref="CoherenceRelayManager" /> upon successful connection to the
        ///     Replication Server.
        /// </summary>
        void Open();

        /// <summary>
        ///     Called by the <see cref="CoherenceRelayManager" /> upon getting disconnected from the
        ///     Replication Server.
        /// </summary>
        void Close();

        /// <summary>Called regularly by the <see cref="CoherenceRelayManager" /> when in the "open" state.</summary>
        void Update();
    }
}
