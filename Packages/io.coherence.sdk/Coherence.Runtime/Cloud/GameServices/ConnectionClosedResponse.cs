// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using Newtonsoft.Json;

#pragma warning disable 0649
    public struct ConnectionClosedResponse
    {
        [JsonProperty("reason")]
        public string ConnectionClosedReason;
    }
#pragma warning restore 0649
}
