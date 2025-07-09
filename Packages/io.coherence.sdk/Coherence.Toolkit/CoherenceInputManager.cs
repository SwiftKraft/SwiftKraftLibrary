// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System.Collections.Generic;

    public delegate void PauseHandler(bool isPaused);

    public class CoherenceInputManager
    {
        /// <summary>
        ///     Raised when the <see cref="ShouldPause" /> flag changes indicating that the simulation should pause.
        /// </summary>
        public event PauseHandler OnPauseChange;

        /// <summary>
        ///     Last common received frame for all inputs in the system.
        /// </summary>
        public long CommonReceivedFrame { get; private set; }

        /// <summary>
        ///     Last common acknowledged frame for all inputs in the system.
        /// </summary>
        public long AcknowledgedFrame { get; private set; }

        /// <summary>
        ///     Last common misprediction frame for all inputs in the system.
        /// </summary>
        public long? MispredictionFrame { get; private set; }

        /// <summary>
        ///     If true, the system cannot do any more predictions. Client should pause the game and stop polling inputs to not
        ///     lose any data required for a rollback.
        /// </summary>
        public bool ShouldPause { get; private set; }

        /// <summary>
        /// If false, the manager won't process anything during a fixed frame update.
        /// </summary>
        public bool ProcessingEnabled
        {
            get => processingEnabled;
            set
            {
                if (processingEnabled == value)
                {
                    return;
                }

                processingEnabled = value;
                if (processingEnabled)
                {
                    // This needs to be bumped so clients don't pause the simulation immediately
                    // as inputs processing has just begun.
                    CommonReceivedFrame = CurrentFixedSimulationFrame - 1;
                    AcknowledgedFrame = CommonReceivedFrame;
                }
            }
        }

        /// <summary/>
        public long CurrentFixedSimulationFrame => bridge.ClientFixedSimulationFrame;

        /// <summary>Misprediction that cannot be yet handled because not all inputs for a given frame were received yet.</summary>
        private long? pendingMisprediction;
        private bool processingEnabled;
        private readonly List<ICoherenceInput> allInputs = new List<ICoherenceInput>();
        private readonly ICoherenceBridge bridge;

        public CoherenceInputManager(ICoherenceBridge bridge)
        {
            this.bridge = bridge;
            bridge.OnFixedNetworkUpdate += FixedNetworkUpdate;
            bridge.OnLateFixedNetworkUpdate += LateFixedNetworkUpdate;
            bridge.OnTimeReset += HandleTimeReset;

            CommonReceivedFrame = CurrentFixedSimulationFrame - 1;
            AcknowledgedFrame = CommonReceivedFrame;
        }

        /// <summary>
        ///     Resets the internal state.
        /// </summary>
        internal void Reset()
        {
            ShouldPause = false;
            CommonReceivedFrame = 0;
            AcknowledgedFrame = 0;
            MispredictionFrame = null;
            pendingMisprediction = null;
            allInputs.Clear();
        }

        /// <summary>
        ///     Adds input to the system.
        /// </summary>
        internal void AddInput(ICoherenceInput input)
        {
            allInputs.Add(input);
        }

        /// <summary>
        ///     Removes input from the system.
        /// </summary>
        /// <param name="input"></param>
        internal void RemoveInput(ICoherenceInput input)
        {
            allInputs.Remove(input);
        }

        /// <summary>Should be called during a standard Unity update, preferably before fixed frame update.</summary>
        internal void Update()
        {
            if (!ProcessingEnabled)
            {
                return;
            }

            UpdateLastReceivedFrame();
            CheckUnPause();
        }

        private void FixedNetworkUpdate()
        {
            if (!ProcessingEnabled)
            {
                return;
            }

            // Since this method should subscribe before any user callbacks
            // we can safely do any changes to the manager state that will
            // be visible to the user in this frame.

            UpdateMispredictionFrame();
        }

        private void LateFixedNetworkUpdate()
        {
            if (!ProcessingEnabled)
            {
                return;
            }

            // We check for pause only after all the inputs have been consumed
            // during the FixedNetworkUpdate.
            CheckPause();
        }

        private void UpdateLastReceivedFrame()
        {
            if (allInputs.Count == 1 && allInputs[0].IsProducer)
            {
                // This is a special case where the local client is the only input in the simulation.
                CommonReceivedFrame = CurrentFixedSimulationFrame;
                AcknowledgedFrame = CurrentFixedSimulationFrame;
                return;
            }

            long lowestReceivedFrame = long.MaxValue;
            long lowestAcknowledgedFrame = long.MaxValue;

            for (int i = 0; i < allInputs.Count; i++)
            {
                ICoherenceInput input = allInputs[i];

                long receiveFrame = input.LastReceivedFrame;
                if (receiveFrame >= 0 && receiveFrame < lowestReceivedFrame)
                {
                    lowestReceivedFrame = receiveFrame;
                }

                long ackFrame = input.LastAcknowledgedFrame;
                if (ackFrame >= 0 && ackFrame < lowestAcknowledgedFrame)
                {
                    lowestAcknowledgedFrame = ackFrame;
                }
            }

            if (lowestReceivedFrame > CommonReceivedFrame && lowestReceivedFrame < long.MaxValue)
            {
                CommonReceivedFrame = lowestReceivedFrame;
            }

            if (lowestAcknowledgedFrame > AcknowledgedFrame && lowestAcknowledgedFrame < long.MaxValue)
            {
                AcknowledgedFrame = lowestAcknowledgedFrame;
            }
        }

        private void UpdateMispredictionFrame()
        {
            MispredictionFrame = null;

            long lowestMispredictionFrame = long.MaxValue;

            for (int i = 0; i < allInputs.Count; i++)
            {
                ICoherenceInput input = allInputs[i];

                long? mispredictionFrame = input.MispredictionFrame;
                if (mispredictionFrame < lowestMispredictionFrame)
                {
                    lowestMispredictionFrame = mispredictionFrame.Value;
                }
            }

            bool hasNewMisprediction = lowestMispredictionFrame < long.MaxValue;

            if (pendingMisprediction.HasValue)
            {
                if (lowestMispredictionFrame < pendingMisprediction)
                {
                    pendingMisprediction = lowestMispredictionFrame;
                }

                bool needMoreInputs = CommonReceivedFrame < pendingMisprediction;
                if (needMoreInputs)
                {
                    return;
                }

                MispredictionFrame = pendingMisprediction;
                pendingMisprediction = null;
                return;
            }

            if (!hasNewMisprediction)
            {
                return;
            }

            bool hasAllInputs = CommonReceivedFrame >= lowestMispredictionFrame;
            if (hasAllInputs)
            {
                MispredictionFrame = lowestMispredictionFrame;
            }
            else
            {
                pendingMisprediction = lowestMispredictionFrame;
            }

        }

        private void CheckPause()
        {
            if (ShouldPause)
            {
                return;
            }

            for (int i = 0; i < allInputs.Count; i++)
            {
                ICoherenceInput input = allInputs[i];

                if (input.ShouldPause(CommonReceivedFrame))
                {
                    // If any input needs a pause we pause the whole system
                    ShouldPause = true;
                    OnPauseChange?.Invoke(true);
                    break;
                }
            }
        }

        private void CheckUnPause()
        {
            // Note: This must be called from the standard update as during
            // a pause the FixedNetworkUpdate might be not called at all.

            if (!ShouldPause)
            {
                return;
            }

            for (int i = 0; i < allInputs.Count; i++)
            {
                if (allInputs[i].ShouldPause(CommonReceivedFrame))
                {
                    return;
                }
            }

            ShouldPause = false;
            OnPauseChange?.Invoke(false);
        }

        private void HandleTimeReset()
        {
            CommonReceivedFrame = CurrentFixedSimulationFrame - 1;
            AcknowledgedFrame = CommonReceivedFrame;
        }
    }
}
