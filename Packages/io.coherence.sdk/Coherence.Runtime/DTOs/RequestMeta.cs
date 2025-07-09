// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

#pragma warning disable 0649
    internal struct RequestMeta
    {
        [Deprecated("01/07/2022", 0, 9, 0, Reason = "Replaced by `X-Coherence-Request-ID` header")]
        [JsonProperty("id")]
        public int Id;

        [JsonProperty("method")]
        public string Method;

        [JsonProperty("headers", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Headers;

        [JsonProperty("path")]
        public string Path;

        [JsonProperty("resume_id", NullValueHandling=NullValueHandling.Ignore)]
        public string ResumeId;
    }
#pragma warning restore 0649
}
