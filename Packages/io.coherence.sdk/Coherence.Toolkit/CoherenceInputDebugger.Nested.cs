// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;
    using System.Collections.Generic;
    using Connection;

    public partial class CoherenceInputDebugger
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum Event
        {
            Error,
            ClientJoined,
            ClientLeft,
            Rollback,
            Pause,
            UnPause,
            InputSent,
            InputReceived,
        }

        class FrameSample
        {
            public long Frame;
            public long AckFrame;
            public long ReceiveFrame;
            public long AckedAt;
            public long MispredictionFrame;
            public string Hash;
            public string Time;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public long UpdatedAt;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public bool ShouldPause;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public object UpdatedState;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public Dictionary<string, string> UpdatedInputs;

            public object InitialState;
            public Dictionary<string, string> InitialInputs;
            public Dictionary<string, InputBufferState> InputBufferStates;

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
            public List<EventData> Events;

            public FrameSample(CoherenceInputManager inputManager)
            {
                Frame = inputManager.CurrentFixedSimulationFrame;
                AckFrame = inputManager.AcknowledgedFrame;
                ReceiveFrame = inputManager.CommonReceivedFrame;
                ShouldPause = inputManager.ShouldPause;
                MispredictionFrame = inputManager.MispredictionFrame.GetValueOrDefault(-1);

                Events = new List<EventData>();
                Time = DateTime.Now.ToString("HH:mm:ss.fff");
            }
        }

        struct InputBufferState
        {
            public long LastFrame;
            public long LastSentFrame;
            public long LastReceivedFrame;
            public long LastAcknowledgedFrame;
            public long? MispredictionFrame;
            public int QueueCount;
            public bool ShouldPause;
        }

        struct EventData
        {
            public string Event;
            public string Time;
            public object Data;

            public EventData(string @event, object data)
            {
                Time = DateTime.Now.ToString("HH:mm:ss.fff");
                Event = @event;
                Data = data;
            }
        }
    }
}
