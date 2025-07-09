// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    /// <summary>
    /// Holds constants for the script execution order.
    /// </summary>
    public static class ScriptExecutionOrder
    {
        /// <summary>
        /// <see cref="Coherence.Toolkit.CoherenceBridge"/>
        /// </summary>
        public const int CoherenceBridge = -1000;
        /// <summary>
        /// <see cref="Coherence.Toolkit.PrefabSyncGroup"/>
        /// </summary>
        public const int SyncGroup = -955;
        /// <summary>
        /// <see cref="Coherence.Toolkit.CoherenceNode"/>
        /// </summary>
        public const int CoherenceNode = -950;
        /// <summary>
        /// <see cref="Coherence.Toolkit.CoherenceSync"/>
        /// </summary>
        public const int CoherenceSync = -900;
        /// <summary>
        /// <see cref="Coherence.Toolkit.CoherenceInput"/>
        /// </summary>
        public const int CoherenceInput = -800;
        /// <summary>
        /// <see cref="Coherence.Toolkit.CoherenceQuery"/>
        /// </summary>
        public const int CoherenceQuery = 900;
        /// <summary>
        /// <see cref="Coherence.Toolkit.OnApplicationQuitSender"/>
        /// </summary>
        public const int OnApplicationQuitSender = 1000;
    }
}
