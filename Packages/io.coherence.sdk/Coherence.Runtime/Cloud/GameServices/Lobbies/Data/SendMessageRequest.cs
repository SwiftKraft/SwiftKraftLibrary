// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using Utils;

    internal struct SendMessageRequest
    {
        [JsonProperty("messages")]
        public List<string> Messages;

        [JsonProperty("targets")]
        public List<string> Targets;

        public static string GetRequestBody(List<string> messages, List<LobbyPlayer> players)
        {
            List<string> targets = null;

            if (players != null)
            {
                targets = new List<string>();

                foreach (var player in players)
                {
                    targets.Add(player.Id);
                }
            }

            var requestBody = CoherenceJson.SerializeObject(new SendMessageRequest()
            {
                Messages = messages,
                Targets = targets
            });

            return requestBody;
        }
    }
}
