// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    public interface IInputBuffer
    {
        /// <inheritdoc cref="InputBuffer{T}.OnStaleInput"/>
        event StaleInputHandler OnStaleInput;

        /// <inheritdoc cref="InputBuffer{T}.Size"/>
        int Size { get; }

        /// <inheritdoc cref="InputBuffer{T}.Delay"/>
        int Delay { get; set; }

        /// <inheritdoc cref="InputBuffer{T}.LastFrame"/>
        long LastFrame { get; }

        /// <inheritdoc cref="InputBuffer{T}.LastSentFrame"/>
        long LastSentFrame { get; }

        /// <inheritdoc cref="InputBuffer{T}.LastReceivedFrame"/>
        long LastReceivedFrame { get; }

        /// <inheritdoc cref="InputBuffer{T}.LastAcknowledgedFrame"/>
        long LastAcknowledgedFrame { get; }
        
        /// <inheritdoc cref="InputBuffer{T}.LastConsumedFrame"/>
        long LastConsumedFrame { get; }

        /// <inheritdoc cref="InputBuffer{T}.MispredictionFrame"/>
        long? MispredictionFrame { get; }

        /// <inheritdoc cref="InputBuffer{T}.ShouldPause"/>
        bool ShouldPause(long currentFrame, long commonReceivedFrame);

        /// <inheritdoc cref="InputBuffer{T}.Reset"/>
        void Reset();

        /// <summary>Tries to fetch input for a given frame.</summary>
        /// <returns>True if the input was in the buffer.</returns>
        internal bool TryPeekInput(long frame, out object input);

        /// <summary>Number of inputs queued for processing.</summary>
        internal int QueueCount { get; }
    }
}
