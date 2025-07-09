// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using System;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Cloud;

    /// <summary>
    /// Response from backend to login requests of all types.
    /// </summary>
    /// <see cref="Cloud.AuthClient"/>
    [EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable 0649
    public struct LoginResponse
    {
        [JsonIgnore]
        public SessionToken SessionToken => sessionToken;

        [JsonIgnore]
        public PlayerAccountId Id => id;

        [JsonProperty("kv")]
        public List<KvPair> KvStoreState;

        [JsonProperty("lobbies")]
        public List<string> LobbyIds;

        [JsonProperty("session_token")]
        internal string sessionToken;

        [JsonProperty("id")]
        private string id;

        [JsonProperty("username")]
        internal string Username;

        [JsonProperty("email")]
        internal string Email;

        [JsonProperty("display_name")]
        internal string DisplayName;

        [JsonProperty("avatar_url")]
        internal string AvatarUrl;

        [JsonProperty("verified")]
        internal bool IsVerified;

        [JsonProperty("new_player")]
        internal bool IsNewPlayer;

        [JsonIgnore]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use " + nameof(Id) + " instead.")]
        [Deprecated("04/2025", 1, 6, 0, Reason = "Use " + nameof(Id) + " instead.")]
        public string UserId => Id;
    }
#pragma warning restore 0649
}
