// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    internal struct LobbiesFetchResponse
    {
        [JsonProperty("lobbies")]
        public List<LobbyData> Lobbies;
    }
}
