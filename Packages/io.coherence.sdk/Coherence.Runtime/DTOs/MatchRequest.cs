// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using Newtonsoft.Json;

#pragma warning disable 649
    internal struct MatchRequest
    {
        [JsonProperty("region")]
        public string Region;

        [JsonProperty("team")]
        public string Team;

        [JsonProperty("score")]
        public int Score;

        [JsonProperty("payload")]
        public string Payload;

        [JsonProperty("friends")]
        public string[] Friends;

        [JsonProperty("tags")]
        public string[] Tags;
    }
#pragma warning restore 649
}
