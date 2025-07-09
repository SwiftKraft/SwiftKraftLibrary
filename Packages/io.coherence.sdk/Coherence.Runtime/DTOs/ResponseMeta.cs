// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

#pragma warning disable 0649
    internal struct ResponseMeta
    {
        [Deprecated("01/07/2022", 0, 9, 0, Reason = "Replaced by `RequestId`")]
        [JsonProperty("id")]
        public int Id;

        [Deprecated("01/07/2022", 0, 9, 0, Reason = "Replaced by `RequestId`")]
        [JsonProperty("logId")]
        public string LogId { set => RequestId = value; }

        [JsonProperty("requestId")]
        public string RequestId;

        [JsonProperty("code")]
        public int StatusCode;

        [JsonProperty("ts")]
        public Int64 Timestamp;

        [JsonProperty("resume_id")]
        public string ResumeId;
    }
#pragma warning restore 0649
}
