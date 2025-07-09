// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;

    public struct LobbySortOption
    {
        [JsonProperty("key")]
        public string Key;

        [JsonProperty("desc")]
        public bool Descending;
    }
}
