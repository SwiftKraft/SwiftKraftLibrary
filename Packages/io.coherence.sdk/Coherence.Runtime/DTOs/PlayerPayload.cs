// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#pragma warning disable 649

namespace Coherence.Runtime
{
    using System;
    using System.ComponentModel;
    using Newtonsoft.Json;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Replaced by " + nameof(MatchedPlayer))]
    [Deprecated("04/2025", 1, 6, 0, Reason = "Replaced by " + nameof(MatchedPlayer) + " for consistency and to avoid avoid naming conflicts.")]
    public struct PlayerPayload
    {
        [JsonProperty("user_id")]
        public string UserId;

        [JsonProperty("team")]
        public string Team;

        [JsonProperty("score")]
        public int Score;

        [JsonProperty("payload")]
        public string Payload;

        public static implicit operator MatchedPlayer(PlayerPayload player) => new()
        {
            id = player.UserId,
            Team = player.Team,
            Score = player.Score,
            Payload = player.Payload
        };

        public static implicit operator PlayerPayload(MatchedPlayer player) => new()
        {
            UserId = player.Id,
            Team = player.Team,
            Score = player.Score,
            Payload = player.Payload
        };
    }
}
