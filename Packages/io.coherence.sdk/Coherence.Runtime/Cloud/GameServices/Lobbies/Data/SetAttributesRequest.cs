// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using Utils;

    internal struct SetAttributesRequest
    {
        [JsonProperty("attributes")]
        public List<CloudAttribute> Attributes;

        public static string GetRequestBody(List<CloudAttribute> attr)
        {
            var requestBody = CoherenceJson.SerializeObject(new SetAttributesRequest()
            {
                Attributes = attr
            });

            return requestBody;
        }
    }
}
