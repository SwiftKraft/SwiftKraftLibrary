// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;

    /// <summary>
    /// Contains the <see cref="CoherenceSync"/> instance used while resolving uniqueness.
    /// </summary>
    public class UniqueObjectReplacement
    {
        internal UniqueObjectReplacement()
        {
        }

        /// <summary>
        /// The <see cref="CoherenceSync"/> instance.
        /// </summary>
        public CoherenceSync Sync => localObject as CoherenceSync;

        /// <summary>
        /// The <see cref="ICoherenceSync"/> instance.
        /// </summary>
        public ICoherenceSync localObject;
        internal Action<ICoherenceSync> localObjectInit;
        internal bool ReplaceReady => localObject != null && localObjectInit != null;
    }
}
