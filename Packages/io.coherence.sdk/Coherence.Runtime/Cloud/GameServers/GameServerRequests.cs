// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
#if UNITY
    using UnityEngine;
#endif


#pragma warning disable 649
    public struct GameServerDeployOptions
    {
        [JsonProperty("kv")]
        public Dictionary<string, string> KV;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("region")]
        public string Region;

        [JsonProperty("size")]
        public string Size;

        [JsonProperty("slug")]
        public string Slug;

        [JsonProperty("tag")]
        public string Tag;
    }

    public struct GameServerListOptions
    {
        public int Limit;
        public int MaxPlayers;
        public string Region;
        public string Size;
        public string Slug;
        public bool Suspended;
        public string Tag;

        public string QueryParams()
        {
            var queryParams = new Dictionary<string, string>
            {
                { "region", Region ?? "" },
                { "slug", Slug ?? "" },
                { "tag", Tag ?? "" },
                { "size", Size ?? ""},
                { "max_players", MaxPlayers.ToString() },
                { "suspended", Suspended.ToString().ToLower() },
                { "limit", Limit.ToString() },
            };

            return "?" + string.Join("&",
                queryParams.Select(kv => kv.Key + "=" + Uri.EscapeDataString(kv.Value)).ToArray());
        }
    }

    public struct GameServerMatchOptions
    {
        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("region")]
        public string Region;

        [JsonProperty("size")]
        public string Size;

        [JsonProperty("slug")]
        public string Slug;

        [JsonProperty("tag")]
        public string Tag;
    }

    internal struct GameServerStateOptions
    {
        [JsonProperty("suspended")]
        public bool Suspended;

        [JsonProperty("secret")]
        public string Secret;
    }

    internal struct GameServerDeleteOptions
    {
        [JsonProperty("secret")]
        public string Secret;
    }

    public struct GameServerDeployResult
    {
        [JsonProperty("id")]
        public ulong Id;

        [JsonProperty("secret")]
        public string Secret;
    }

    public struct OptionalGameServerData
    {
        [JsonProperty("gameserver")]
        public GameServerData? GameServerData;
    }

    public struct GameServerData
    {
        [JsonProperty("id")]
        public ulong Id;

        [JsonProperty("region")]
        public string Region;

        [JsonProperty("slug")]
        public string Slug;

        [JsonProperty("tag")]
        public string Tag;

        [JsonProperty("kv")]
        public Dictionary<string, string> KV;

        [JsonProperty("size")]
        public string Size;

        [JsonProperty("max_players")]
        public int MaxPlayers;

        [JsonProperty("connected_players")]
        public int ConnectedPlayers;

        [JsonProperty("suspended")]
        public bool Suspended;

        [JsonProperty("ip")]
        public string Ip;

        [JsonProperty("port")]
        public int Port;

        [JsonProperty("created_at")]
        public int CreatedAt;

        [JsonProperty("last_started_at")]
        public int LastStartedAt;
    }
}
