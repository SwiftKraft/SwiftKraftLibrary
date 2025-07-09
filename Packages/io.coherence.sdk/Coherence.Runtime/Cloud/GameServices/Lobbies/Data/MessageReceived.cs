// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public struct MessagesReceived
    {
        [JsonProperty("lobby_id")]
        public string LobbyId;

        [JsonProperty("player_id")]
        public string PlayerSenderId;

        [JsonProperty("time")]
        public int Time;

        [JsonProperty("messages")]
        public List<string> Messages;
    }
}
