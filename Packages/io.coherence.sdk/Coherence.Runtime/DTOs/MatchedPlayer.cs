// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#pragma warning disable 649

namespace Coherence.Runtime
{
    using System;
    using System.ComponentModel;
    using Cloud;
    using Newtonsoft.Json;

    public struct MatchedPlayer
    {
        [JsonIgnore]
        public PlayerAccountId Id => id;

        [JsonProperty("user_id")]
        internal string id;

        [JsonIgnore]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use " + nameof(Id) + " instead.")]
        [Deprecated("04/2025", 1, 6, 0, Reason = "Use " + nameof(Id) + " instead.")]
        public string UserId => id;

        [JsonProperty("team")]
        public string Team;

        [JsonProperty("score")]
        public int Score;

        [JsonProperty("payload")]
        public string Payload;
    }
}
