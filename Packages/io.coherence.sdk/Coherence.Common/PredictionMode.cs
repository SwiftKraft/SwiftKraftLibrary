// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    /// <summary>
    /// PredictionMode is used to configure when bindings should apply or discard incoming network samples.
    /// </summary>
    public enum PredictionMode
    {
        /// <summary>
        /// Prediction is turned off. Incoming network samples are always applied.
        /// This is the default option.
        /// </summary>
        Never,

        /// <summary>
        /// Prediction is turned on. Incoming network samples are never applied.
        /// </summary>
        Always,

        /// <summary>
        /// Prediction is turned on for entities with <see cref="CoherenceSync.HasInputAuthority"/>.
        /// This option is commonly used to enable prediction for the locally controlled player.
        /// This option is only valid for prefabs that use <see cref="CoherenceInput"/>.
        /// </summary>
        InputAuthority
    }
}
