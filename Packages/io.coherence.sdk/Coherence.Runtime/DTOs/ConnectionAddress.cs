// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using Newtonsoft.Json;

#pragma warning disable 649
    public struct ConnectionAddress
    {
        [JsonProperty("ip")]
        public string Ip;

        [JsonProperty("region")]
        public string Region;

        [JsonProperty("port")]
        public int Port;

        [JsonProperty("sig_port")]
        public int WebPort;

        [JsonProperty("sig_url")]
        public string SigURL;

        public override string ToString()
        {
            return $"{nameof(Region)}: {Region}, {nameof(Ip)}: {Ip}, {nameof(Port)}: {Port}, {nameof(WebPort)}: {WebPort}, {nameof(SigURL)}: {SigURL}";
        }
    }
#pragma warning restore 649
}
