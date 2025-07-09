// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using Newtonsoft.Json;

#pragma warning disable 649
    public class MatchResponse
    {
        [JsonProperty("match_id")]
        public string MatchId;

        [JsonProperty("players")]
        public MatchedPlayer[] Players;

        [JsonProperty("error")]
        public string Error;
    }
#pragma warning restore 649
}
