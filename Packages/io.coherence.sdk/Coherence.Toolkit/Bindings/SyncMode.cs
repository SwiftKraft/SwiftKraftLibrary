// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Bindings
{
    /// <summary>
    /// SyncMode defines when a <see cref="ValueBinding{T}"/> is replicated.
    /// </summary>
    public enum SyncMode
    {
        /// <summary>
        /// The binding is always replicated.
        /// </summary>
        Always,

        /// <summary>
        /// The binding is only replicated when the network object is created.
        /// </summary>
        CreationOnly,
    }
}
