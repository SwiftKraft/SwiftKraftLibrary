// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Toolkit
{
    using System.Collections;
    using Internal;
    using UnityEngine;

    /// <summary>
    /// A class that allows executing coroutines that are not tied
    /// to the lifetime of any particular mono behaviour or scene.
    /// </summary>
    internal sealed class CoroutineRunner : CoherenceSharedBehaviour<CoroutineRunner>
    {
        /// <summary>
        /// Starts a coroutine on a single shared hidden mono behaviour instance that survives scene transitions.
        /// </summary>
        /// <param name="coroutine"> The coroutine to start. </param>
        /// <returns> A reference to the started coroutine. </returns>
        public new static Coroutine StartCoroutine(IEnumerator coroutine) => ((MonoBehaviour)SharedInstance).StartCoroutine(coroutine);
    }
}
