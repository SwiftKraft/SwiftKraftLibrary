// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using Utils;

    internal struct LobbyCreationRequest
    {
        [JsonProperty("tag")]
        public string Tag;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("unlisted")]
        public bool Unlisted;

        [JsonProperty("secret")]
        public string Secret;

        [JsonProperty("region")]
        public string Region;

        [JsonProperty("sim_slug")]
        public string SimSlug;

        [JsonProperty("lobby_attr")]
        public List<CloudAttribute> LobbyAttributes;

        [JsonProperty("player_attr")]
        public List<CloudAttribute>  PlayerAttributes;

        public static string GetRequestBody(CreateLobbyOptions options)
        {
            var requestBody = CoherenceJson.SerializeObject(new LobbyCreationRequest()
            {
                Tag = options.Tag,
                Region = options.Region,
                Name = options.Name,
                MaxPlayers = options.MaxPlayers,
                Unlisted = options.Unlisted,
                Secret = options.Secret,
                SimSlug = options.SimulatorSlug,
                LobbyAttributes = options.LobbyAttributes,
                PlayerAttributes = options.PlayerAttributes
            });

            return requestBody;
        }
    }
}
