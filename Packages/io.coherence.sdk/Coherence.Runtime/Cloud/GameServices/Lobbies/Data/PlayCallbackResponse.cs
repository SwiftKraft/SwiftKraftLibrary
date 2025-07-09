// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;

    internal struct PlayCallbackResponse
    {
        [JsonProperty("lobby_id")]
        public string LobbyId;

        [JsonProperty("room")]
        public RoomData Room;
    }
}
