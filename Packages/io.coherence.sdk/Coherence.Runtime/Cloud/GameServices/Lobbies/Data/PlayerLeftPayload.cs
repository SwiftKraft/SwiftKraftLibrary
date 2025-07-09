// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;

    internal struct PlayerLeftPayload
    {
        [JsonProperty("lobby_id")]
        public string LobbyId;

        [JsonProperty("player_id")]
        public string PlayerId;

        [JsonProperty("reason")]
        public string Reason;
    }
}
