// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public struct LobbyPlayerStats
    {
        [JsonProperty("online")]
        public int PlayersOnline;

        [JsonProperty("in_lobbies")]
        public int PlayersInLobbies;

        [JsonProperty("in_rooms")]
        public int PlayersInRooms;

        [JsonProperty("regions")]
        public List<LobbyPlayersFilteredStats> Regions;

        [JsonProperty("tags")]
        public List<LobbyPlayersFilteredStats> Tags;
    }
}
