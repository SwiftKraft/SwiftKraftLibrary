// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    internal struct StatsRequest
    {
        [JsonProperty("tags")]
        public List<string> Tags;

        [JsonProperty("regions")]
        public List<string> Regions;

        public static string GetRequestBody(List<string> tags = null, List<string> regions = null)
        {
            var requestBody = JsonConvert.SerializeObject(new StatsRequest()
            {
                Tags = tags,
                Regions = regions
            });

            return requestBody;
        }
    }
}
