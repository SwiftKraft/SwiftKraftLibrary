// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using Utils;

    internal struct StartGameSessionRequest
    {
        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("unlist_lobby")]
        public bool UnlistLobby;

        [JsonProperty("close_lobby")]
        public bool CloseLobby;

        [JsonProperty("sim_payload")]
        public string SimPayload;

        public static string GetRequestBody(int maxPlayers, bool unlistLobby, bool closeLobby, string simPayload)
        {
            var requestBody = CoherenceJson.SerializeObject(new StartGameSessionRequest()
            {
                MaxPlayers = maxPlayers,
                UnlistLobby = unlistLobby,
                CloseLobby = closeLobby,
                SimPayload = simPayload,
            });

            return requestBody;
        }
    }
}
