// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Runtime.Utils
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
#if UNITY_6000_0_OR_NEWER && !UNITY_EDITOR // Use Task in Unity Editor to support Edit Mode tests.
    using Awaiter = UnityEngine.Awaitable.Awaiter;
#else
    using Awaiter = System.Runtime.CompilerServices.TaskAwaiter<bool>;
#endif

    /// <summary>
    /// Extension methods for <see cref="TimeSpan"/>.
    /// </summary>
    internal static class TimeSpanExtensions
    {
        /// <summary>
        /// Suspend execution of the calling async method for the specified amount of time.
        /// <remarks>
        /// This method is safe to use on WebGL platforms (unlike <see cref="Task.Delay(int)"/>).
        /// </remarks>
        /// </summary>
        /// <param name="timeSpan"> The amount of time to wait. </param>
        /// <returns> An awaiter. </returns>
        /// <seealso cref="Wait"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Awaiter GetAwaiter(this TimeSpan timeSpan)
        {
#if UNITY_6000_0_OR_NEWER && !UNITY_EDITOR
            return UnityEngine.Awaitable.WaitForSecondsAsync((float)timeSpan.TotalSeconds).GetAwaiter();
#else
            if (timeSpan.TotalMilliseconds <= 0)
            {
                return Task.FromResult(true).GetAwaiter();
            }

            var taskCompletionSource = new TaskCompletionSource<bool>();
            _ = Await(timeSpan, taskCompletionSource);
            return taskCompletionSource.Task.GetAwaiter();

            static async Task Await(TimeSpan timeSpan, TaskCompletionSource<bool> taskCompletionSource)
            {
                var waitUntil = DateTime.Now + timeSpan;
                do
                {
                    await Task.Yield();
                }
                while (DateTime.Now < waitUntil);

                taskCompletionSource.SetResult(true);
            }
#endif
        }
    }
}
