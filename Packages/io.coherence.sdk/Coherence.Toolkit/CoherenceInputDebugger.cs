// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Log;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    public partial class CoherenceInputDebugger
    {
        public const string DEBUG_CONDITIONAL = "COHERENCE_INPUT_DEBUG";

        /// <summary>Number of last frames to keep in the debugger memory. If null, all frames are kept.</summary>
        public int? FramesToKeep { get; set; }

        /// <summary>
        ///     Called by the <see cref="Dump" /> method, right after serialization. Contains all the debug data in a JSON
        ///     format.
        /// </summary>
        public Action<string> OnDump;

        private readonly CoherenceInputManager inputManager;

        private FrameSample currentSample;
        private long lastAcknowledgedFrame = -1;
        private readonly Logger logger = Log.GetLogger<CoherenceInputDebugger>();

        private readonly SortedDictionary<long, FrameSample> allSamples = new SortedDictionary<long, FrameSample>();
        private readonly Dictionary<ICoherenceInput, string> idByInput = new Dictionary<ICoherenceInput, string>();

        public CoherenceInputDebugger(CoherenceInputManager inputManager, Action<string> onDataDump = null)
        {
            this.inputManager = inputManager;
            OnDump = onDataDump ?? SaveToFile;
        }

        /// <summary>
        ///     Registers new input in the debugger. The following data is recorded for registered inputs:
        ///     <para>* Received inputs</para>
        ///     <para>* Sent inputs</para>
        ///     <para>* Input buffer state for each frame</para>
        /// </summary>
        [Conditional(DEBUG_CONDITIONAL)]
        public void AddInput(CoherenceInput input, string id)
        {
            input.Debugger = this;
            idByInput.Add(input, id);
        }

        /// <summary>Removes a input from the debugger.</summary>
        [Conditional(DEBUG_CONDITIONAL)]
        public void RemoveInput(CoherenceInput input)
        {
            idByInput.Remove(input);
            input.Debugger = null;
        }

        /// <summary>Stores the last sample if the current frame has changed.</summary>
        [Conditional(DEBUG_CONDITIONAL)]
        public void PushSample()
        {
            long currentFrame = inputManager.CurrentFixedSimulationFrame;
            if (currentFrame == currentSample?.Frame)
            {
                return;
            }

            currentSample = new FrameSample(inputManager);
            allSamples.Add(currentSample.Frame, currentSample);

            if (allSamples.Count > FramesToKeep)
            {
                long oldestFrame = allSamples.Keys.First();
                allSamples.Remove(oldestFrame);
            }

            if (lastAcknowledgedFrame >= currentFrame)
            {
                currentSample.AckedAt = currentFrame;
            }
        }

        /// <summary>Stores input buffer states for all registered inputs.</summary>
        [Conditional(DEBUG_CONDITIONAL)]
        public void SetInputBufferStates()
        {
            PushSample();

            long currentFrame = inputManager.CurrentFixedSimulationFrame;
            long commonReceiveFrame = inputManager.CommonReceivedFrame;

            currentSample.InputBufferStates = new Dictionary<string, InputBufferState>();

            foreach (KeyValuePair<ICoherenceInput, string> kv in idByInput)
            {
                IInputBuffer buffer = kv.Key.Buffer;
                currentSample.InputBufferStates[kv.Value] = new InputBufferState
                {
                    LastFrame = buffer.LastFrame,
                    LastSentFrame = buffer.LastSentFrame,
                    LastReceivedFrame = buffer.LastReceivedFrame,
                    LastAcknowledgedFrame = buffer.LastAcknowledgedFrame,
                    MispredictionFrame = buffer.MispredictionFrame,
                    QueueCount = buffer.QueueCount,
                    ShouldPause = buffer.ShouldPause(currentFrame, commonReceiveFrame)
                };
            }
        }

        /// <summary>Stores an input related event in this frame's sample.</summary>
        [Conditional(DEBUG_CONDITIONAL)]
        public void AddEvent(Event inputEvent, object eventData)
        {
            PushSample();

            currentSample.Events.Add(new EventData(inputEvent.ToString(), eventData));
        }

        /// <inheritdoc cref="AddEvent(Event,object)"/>
        [Conditional(DEBUG_CONDITIONAL)]
        public void AddEvent(string inputEvent, object eventData)
        {
            PushSample();

            currentSample.Events.Add(new EventData(inputEvent, eventData));
        }

        /// <summary>Adds or updates a state in the sample from a given frame.</summary>
        [Conditional(DEBUG_CONDITIONAL)]
        public void AddState(long frame, object state)
        {
            PushSample();

            if (!allSamples.TryGetValue(frame, out FrameSample sample))
            {
                return;
            }

            if (sample.InitialState == null)
            {
                sample.InitialState = state;
            }
            else
            {
                sample.UpdatedAt = inputManager.CurrentFixedSimulationFrame;
                sample.UpdatedState = state;
            }
        }

        /// <summary>Adds or updates an input to the sample from a given frame.</summary>
        [Conditional(DEBUG_CONDITIONAL)]
        public void AddInputs(long frame, IEnumerable<DebugInput> inputs, bool simulationEnabled)
        {
            if (!simulationEnabled)
            {
                return;
            }

            PushSample();

            if (!allSamples.TryGetValue(frame, out FrameSample sample))
            {
                return;
            }

            Dictionary<string, string> inputsById = inputs.ToDictionary(c => c.Id,
                c => c.Input.Buffer.TryPeekInput(frame, out object input) ? input.ToString() : string.Empty);

            if (sample.InitialInputs == null)
            {
                sample.InitialInputs = inputsById;
            }
            else
            {
                sample.UpdatedInputs = inputsById;
            }

            if (sample.UpdatedState is IHashable updatedState)
            {
                sample.Hash = updatedState.ComputeHash().ToString();
            }
            else if (sample.InitialState is IHashable initialState)
            {
                sample.Hash = initialState.ComputeHash().ToString();
            }
        }

        /// <summary>Marks inputs from a given frame as acknowledged.</summary>
        [Conditional(DEBUG_CONDITIONAL)]
        public void AcknowledgeFrame(long frame)
        {
            PushSample();

            long currentFrame = inputManager.CurrentFixedSimulationFrame;

            if (lastAcknowledgedFrame < 0)
            {
                lastAcknowledgedFrame = currentFrame - 1;
            }

            for (long ackFrame = lastAcknowledgedFrame + 1; ackFrame <= frame; ackFrame++)
            {
                if (allSamples.TryGetValue(ackFrame, out FrameSample sample))
                {
                    sample.AckedAt = currentFrame;
                }
            }

            lastAcknowledgedFrame = frame;
        }

        /// <summary>Adds an input receive event to the sample.</summary>
        [Conditional(DEBUG_CONDITIONAL)]
        public void HandleInputReceived(CoherenceInput coherenceInput, long frame, object input)
        {
            if (idByInput.TryGetValue(coherenceInput, out string id))
            {
                AddEvent(Event.InputReceived, new
                {
                    Id = id,
                    RecvFrame = frame,
                    Input = input.ToString()
                });
            }
        }

        /// <summary>Adds an input send event to the sample.</summary>
        [Conditional(DEBUG_CONDITIONAL)]
        public void HandleInputSent(CoherenceInput coherenceInput, long frame, object input)
        {
            if (idByInput.TryGetValue(coherenceInput, out string id))
            {
                AddEvent(Event.InputSent, new
                {
                    Id = id,
                    SentFrame = frame,
                    Input = input.ToString()
                });
            }
        }

        /// <summary>
        ///     Serializes the debug data to a JSON and dumps it using the <see cref="OnDump" /> event. By default the dump is
        ///     saved to a file that starts with 'inputDbg_', in a executable directory.
        /// </summary>
        [Conditional(DEBUG_CONDITIONAL)]
        public void Dump()
        {
            PushSample();

            try
            {
                string data = Utils.CoherenceJson.SerializeObject(allSamples, Formatting.Indented);
                OnDump?.Invoke(data);
            }
            catch (JsonSerializationException serializationException)
            {
                logger.Error(Error.ToolkitInputDebuggerError,
                    "Failed to serialize the debug data",
                    ("Exception", serializationException));
            }
            catch (Exception exception)
            {
                logger.Error(Error.ToolkitInputDebuggerError,
                    "Failed to dump the debug data",
                    ("Exception", exception));
            }
        }

        private void SaveToFile(string data)
        {
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 10);
            string fileName = $"inputDbg_{suffix}.json";
            File.WriteAllText(fileName, data);
        }
    }

    public struct DebugInput
    {
        public CoherenceInput Input;
        public string Id;
    }
}
