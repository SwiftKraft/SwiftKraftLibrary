// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Utils
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
#if UNITY_6000_0_OR_NEWER && !UNITY_EDITOR  // Use Task in Unity Editor to support Edit Mode tests.
    using UnityEngine;
#endif

    /// <summary>
    /// Utility class providing methods for suspending the execution of the calling async method for a specified amount of time.
    /// <remarks>
    /// All methods in this class are safe to use on WebGL platforms (unlike <see cref="Task.Delay(int)"/>).
    /// </remarks>
    /// </summary>
    internal static class Wait
    {
        /// <summary>
        /// Suspend execution of the calling async method for the specified amount of time.
        /// <remarks>
        /// This method is safe to use on WebGL platforms (unlike <see cref="Task.Delay(int)"/>).
        /// </remarks>
        /// </summary>
        /// <param name="timeSpan"> The amount of time to wait. </param>
        /// <returns> An awaiter. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static
#if UNITY_6000_0_OR_NEWER && !UNITY_EDITOR
            Awaitable
#else
            async Task
#endif
            For(TimeSpan timeSpan)
        {
#if UNITY_6000_0_OR_NEWER && !UNITY_EDITOR
            return Awaitable.WaitForSecondsAsync((float)timeSpan.TotalSeconds);
#else
            var waitUntil = DateTime.Now + timeSpan;
            do
            {
                await Task.Yield();
            }
            while (DateTime.Now < waitUntil);
#endif
        }

        /// <summary>
        /// Suspend execution of the calling async method for the specified amount of time.
        /// <remarks>
        /// This method is safe to use on WebGL platforms (unlike <see cref="Task.Delay(int)"/>).
        /// </remarks>
        /// </summary>
        /// <param name="timeSpan"> The amount of time to wait. </param>
        /// <param name="cancellationToken">
        /// Used to cancel the operation.
        /// </param>
        /// <returns> An awaiter. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static
#if UNITY_6000_0_OR_NEWER && !UNITY_EDITOR
        Awaitable
#else
        async Task
#endif
        For(TimeSpan timeSpan, CancellationToken cancellationToken)
        {
#if UNITY_6000_0_OR_NEWER && !UNITY_EDITOR
            return Awaitable.WaitForSecondsAsync((float)timeSpan.TotalSeconds, cancellationToken);
#else
            var waitUntil = DateTime.Now + timeSpan;
            do
            {
                await Task.Yield();
            }
            while (DateTime.Now < waitUntil);

            cancellationToken.ThrowIfCancellationRequested();
#endif
        }
    }
}
