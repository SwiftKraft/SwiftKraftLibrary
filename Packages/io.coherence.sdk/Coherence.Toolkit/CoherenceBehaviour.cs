// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using UnityEngine;

    /// <summary>
    /// Base class to Unity components implemented by coherence.
    /// </summary>
    public abstract class CoherenceBehaviour : MonoBehaviour
    {
        internal delegate void ResetDelegate(CoherenceBehaviour behaviour);
        internal static event ResetDelegate OnReset;

        /// <summary>
        /// Resets values to defaults.
        /// </summary>
        protected virtual void Reset() => OnReset?.Invoke(this);
    }
}
