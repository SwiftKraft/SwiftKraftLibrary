// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#pragma warning disable 0649

namespace Coherence.Runtime
{
    using Newtonsoft.Json;

    internal interface IPlayerAccountOperationResponse { }

    internal struct PlayerAccountOperationNullResponse : IPlayerAccountOperationResponse { }

    internal struct GetOneTimeCodeResponse : IPlayerAccountOperationResponse
    {
        [JsonProperty("code")]
        public string OneTimeCode;
    }

    internal struct GetAccountInfoResponse : IPlayerAccountOperationResponse
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("username")]
        public string Username;

        [JsonProperty("email")]
        public string Email;

        [JsonProperty("display_name")]
        public string DisplayName;

        [JsonProperty("avatar_url")]
        public string AvatarUrl;

        [JsonProperty("identities")]
        public IdentityResponse[] Identities;

        [JsonProperty("verified")]
        public bool Verified;

        [JsonProperty("created_at")]
        public long CreatedAt;
    }

    internal struct IdentityResponse
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("type")]
        public string Type;
    }
}
