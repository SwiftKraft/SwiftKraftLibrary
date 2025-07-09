// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using Utils;

    internal struct JoinLobbyRequest
    {
        [JsonProperty("secret")]
        public string Secret;

        [JsonProperty("attributes")]
        public List<CloudAttribute> PlayerAttributes;

        public static string GetRequestBody(List<CloudAttribute> playerAttr, string secret)
        {
            var requestBody = CoherenceJson.SerializeObject(new JoinLobbyRequest()
            {
                Secret = secret,
                PlayerAttributes = playerAttr
            });

            return requestBody;
        }
    }
}
