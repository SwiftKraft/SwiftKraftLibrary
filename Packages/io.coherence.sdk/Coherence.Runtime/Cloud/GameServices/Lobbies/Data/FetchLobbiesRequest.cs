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

    internal struct FetchLobbiesRequest
    {
        [JsonProperty("filters")]
        public List<LobbyFilter>  LobbyFilters;

        [JsonProperty("limit")]
        public int Limit;

        [JsonProperty("public_only")]
        public bool PublicOnly;

        [JsonProperty("sort")]
        public List<LobbySortOption> Sort;

        public static string GetRequestBody(FindLobbyOptions options)
        {
            if (options == null)
            {
                return CoherenceJson.SerializeObject(new FetchLobbiesRequest()
                {
                    Limit = 10,
                    PublicOnly = true
                });
            }

            if (options.LobbyFilters?.Count > 3)
            {
                var warningMsg = "Maximum amount of Lobby Filters are three. Additional Filters will be ignored.";

#if UNITY
                UnityEngine.Debug.LogWarning(warningMsg);
#else
                System.Console.WriteLine(warningMsg);
#endif
            }

            List<LobbySortOption> sortOptions = null;

            if (options.Sort != null)
            {
                sortOptions = new List<LobbySortOption>();
                foreach (var kv in options.Sort)
                {
                    sortOptions.Add(new LobbySortOption()
                    {
                        Key = kv.Key.ToString(),
                        Descending = kv.Value
                    });
                }
            }

            var requestBody = CoherenceJson.SerializeObject(new FetchLobbiesRequest()
            {
                Limit = options.Limit,
                LobbyFilters = options.LobbyFilters,
                Sort = sortOptions,
                PublicOnly = true
            });

            return requestBody;
        }
    }
}
