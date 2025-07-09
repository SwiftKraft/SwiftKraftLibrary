// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;

    public struct LobbyPlayersFilteredStats
    {
        [JsonProperty("in_lobbies")]
        public int PlayersInLobbies;

        [JsonProperty("in_rooms")]
        public int PlayersInRooms;

        [JsonProperty("filter")]
        public string Filter;
    }
}
