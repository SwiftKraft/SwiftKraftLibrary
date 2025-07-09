// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using Utils;

    internal struct FindLobbyRequest
    {
        [JsonProperty("tag")]
        public string Tag;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("region")]
        public string Region;

        [JsonProperty("sim_slug")]
        public string SimSlug;

        [JsonProperty("filters")]
        public List<LobbyFilter> Filters;

        [JsonProperty("sort")]
        public List<LobbySortOption> Sort;

        [JsonProperty("lobby_attr")]
        public List<CloudAttribute> LobbyAttributes;

        [JsonProperty("player_attr")]
        public List<CloudAttribute> PlayerAttributes;

        public static string GetRequestBody(FindLobbyOptions findOptions, CreateLobbyOptions createOptions)
        {
            if (findOptions.LobbyFilters?.Count > 3)
            {
                var warningMsg = "Maximum amount of Lobby Filters are three. Additional Filters will be ignored.";
                
#if UNITY
                UnityEngine.Debug.LogWarning(warningMsg);
#else
                System.Console.WriteLine(warningMsg);
#endif
            }

            List<LobbySortOption> sortOptions = null;

            if (findOptions.Sort != null)
            {
                sortOptions = new List<LobbySortOption>();
                foreach (var kv in findOptions.Sort)
                {
                    sortOptions.Add(new LobbySortOption()
                    {
                        Key = kv.Key.ToString(),
                        Descending = kv.Value
                    });
                }
            }

            var requestBody = CoherenceJson.SerializeObject(new FindLobbyRequest()
            {
                Tag = createOptions.Tag,
                Region = createOptions.Region,
                MaxPlayers = createOptions.MaxPlayers,
                Filters = findOptions.LobbyFilters,
                Sort = sortOptions,
                SimSlug = createOptions.SimulatorSlug,
                LobbyAttributes = createOptions.LobbyAttributes,
                PlayerAttributes = createOptions.PlayerAttributes
            });

            return requestBody;
        }
    }
}
