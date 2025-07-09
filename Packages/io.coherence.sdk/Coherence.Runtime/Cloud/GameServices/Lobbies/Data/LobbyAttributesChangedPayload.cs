// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    internal struct LobbyAttributesChangedPayload
    {
        [JsonProperty("lobby_id")]
        public string LobbyId;

        [JsonProperty("attributes")]
        public List<CloudAttribute> AttributesChanged;
    }
}
