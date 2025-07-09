// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using Newtonsoft.Json;

#pragma warning disable 0649
    public struct KvPair
    {
        [JsonProperty("key")]
        public string Key;

        [JsonProperty("value")]
        public string Value;
    }
#pragma warning disable 0649
}
