// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class SimulationStateStore<TState> : IEnumerable<TState>
    {
        public int Count => store.Count;

        public long NewestFrame { get; private set; } = -1;
        public long OldestFrame { get; private set; } = -1;

        private long ackFrame = -1;

        private readonly List<TState> store = new List<TState>(64);

        /// <summary>
        ///     Removes all stored states and resets <see cref="NewestFrame" /> and <see cref="OldestFrame" />.
        /// </summary>
        public void Clear()
        {
            store.Clear();
            NewestFrame = -1;
            OldestFrame = -1;
        }

        /// <summary>
        ///     Attempts to roll back the state store returning the latest valid state.
        /// </summary>
        /// <param name="mispredictionFrame">Last mispredicted frame.</param>
        /// <param name="validState">Last known valid state.</param>
        /// <returns>False if there is no state in the store or the misprediction frame falls outside the store range.</returns>
        public bool TryRollback(long mispredictionFrame, out TState validState)
        {
            // Couldn't mispredict without having any state
            if (store.Count <= 0)
            {
                validState = default;
                return false;
            }

            bool frameIsWithinBufferRange = mispredictionFrame > OldestFrame
                                            && mispredictionFrame <= NewestFrame + 1;
            if (!frameIsWithinBufferRange)
            {
                validState = default;
                return false;
            }

            int validFrameIndex = (int)(mispredictionFrame - OldestFrame - 1);

            bool hasAnyMispredictedStates = validFrameIndex + 1 < store.Count;
            if (hasAnyMispredictedStates)
            {
                int mispredictionCount = store.Count - validFrameIndex - 1;
                store.RemoveRange(validFrameIndex + 1, mispredictionCount);
                NewestFrame = OldestFrame + validFrameIndex;
            }

            validState = store[validFrameIndex];

            // We know that this must be the latest valid frame
            Acknowledge(Math.Max(mispredictionFrame - 1, ackFrame));

            return true;
        }

        /// <summary>
        ///     Stores the latest known state. The frame of the state must be subsequent to the previous state frame.
        /// </summary>
        public void Add(in TState state, long simulationFrame)
        {
            bool isInitialStore = NewestFrame < 0;
            if (isInitialStore)
            {
                NewestFrame = simulationFrame;
                OldestFrame = NewestFrame;
            }
            else
            {
                bool isSubsequentFrame = simulationFrame == NewestFrame + 1;
                if (!isSubsequentFrame)
                {
                    throw new ArgumentOutOfRangeException(nameof(state),
                        $"Tried to store invalid frame. Expected: {NewestFrame + 1}, Was: {simulationFrame}");
                }

                NewestFrame++;
            }

            store.Add(state);
        }

        /// <summary>
        ///     Removes all states up to a given frame.
        /// </summary>
        public void Acknowledge(long frame)
        {
            bool nothingToAck = store.Count <= 0;
            if (nothingToAck)
            {
                return;
            }

            bool staleFrame = frame < OldestFrame;
            if (staleFrame)
            {
                return;
            }

            ackFrame = frame;

            if (ackFrame >= NewestFrame)
            {
                store.RemoveRange(0, store.Count - 1);
                OldestFrame = NewestFrame;
                return;
            }

            int progress = Math.Max(0, (int)(frame - OldestFrame));
            store.RemoveRange(0, progress);
            OldestFrame += progress;
        }

        public IEnumerator<TState> GetEnumerator()
        {
            return store.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
