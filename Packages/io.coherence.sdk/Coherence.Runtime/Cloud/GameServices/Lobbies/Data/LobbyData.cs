// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public struct LobbyData
    {
        public IReadOnlyList<CloudAttribute> Attributes => lobbyAttributes;
        public IReadOnlyList<LobbyPlayer> Players => players;

        [JsonProperty("id")]
        public string Id;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("region")]
        public string Region;

        [JsonProperty("tag")]
        public string Tag;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("closed")]
        public bool Closed;

        [JsonProperty("unlisted")]
        public bool Unlisted;

        [JsonProperty("private")]
        public bool IsPrivate;

        [JsonIgnore]
        public PlayerAccountId OwnerId => ownerId;
        [JsonProperty("owner_id")]
        internal string ownerId;

        [JsonProperty("sim_slug")]
        public string SimulatorSlug;

        [JsonProperty("room_id")]
        public long RoomId;

        [JsonProperty("room")]
        public RoomData? RoomData;

        [JsonProperty("players")]
        internal List<LobbyPlayer> players;

        [JsonProperty("attributes")]
        internal List<CloudAttribute> lobbyAttributes;

        public CloudAttribute? GetAttribute(string key)
        {
            if (lobbyAttributes == null)
            {
                return null;
            }

            foreach (var attribute in lobbyAttributes)
            {
                if (attribute.Key.Equals(key))
                {
                    return attribute;
                }
            }

            return null;
        }
    }
}
