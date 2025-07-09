// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Toolkit
{
    /// <summary>
    /// Specifies options for what should happen to the <see cref="CoherenceSync"/> component
    /// of a networked entity when the entity is
    /// <see cref="CoherenceSyncInstantiator.Destroy">destroyed</see>.
    /// </summary>
    public enum OnDestroyBehaviour
    {
        /// <summary>
        /// The <see cref="CoherenceSync"/> component is destroyed.
        /// </summary>
        Destroy,

        /// <summary>
        /// The <see cref="CoherenceSync"/> component is disabled.
        /// </summary>
        Disable
    }
}
