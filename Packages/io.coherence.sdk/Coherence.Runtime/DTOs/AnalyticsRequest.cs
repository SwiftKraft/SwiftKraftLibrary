// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using Newtonsoft.Json;

#pragma warning disable 649
    public struct AnalyticsRequest
    {
        [JsonProperty("timestamp_ms")]
        public long TimestampMs;
        [JsonProperty("analytics_id")]
        public string AnalyticsId;

        [JsonProperty("event_name")]
        public string EventName;

        [JsonProperty("sdk_ver")]
        public string SDKVersion;

        [JsonProperty("rs_ver")]
        public string EngineVersion;

        [JsonProperty("sim_slug")]
        public string SimSlug;

        [JsonProperty("schema_id")]
        public string SchemaId;

        public override string ToString()
        {
            return $"{nameof(TimestampMs)}: {TimestampMs}, {nameof(AnalyticsId)}: {AnalyticsId}, {nameof(EventName)}: {EventName}";
        }
    }
#pragma warning restore 649
}
