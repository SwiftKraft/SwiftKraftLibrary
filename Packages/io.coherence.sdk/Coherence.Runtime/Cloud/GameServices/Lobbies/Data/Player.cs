// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Newtonsoft.Json;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Replaced by " + nameof(LobbyPlayer))]
    [Deprecated("04/2025", 1, 6, 0, Reason = "Replaced by " + nameof(LobbyPlayer) + " to avoid confusion with PlayerAccount.")]
    public struct Player : IEquatable<Player>, IComparable<Player>
    {
        public IReadOnlyList<CloudAttribute> Attributes => playerAttributes;

        [JsonProperty("id")]
        public string UserId;

        [JsonProperty("username")]
        public string Username;

        [JsonProperty("attributes")]
        internal List<CloudAttribute> playerAttributes;

        bool IEquatable<Player>.Equals(Player other) => Equals(other);

        public bool Equals(in Player other) => UserId == other.UserId;
        public bool Equals(in LobbyPlayer other) => UserId == other.Id;
        public override bool Equals(object obj) => (obj is Player player && Equals(player)) || (obj is LobbyPlayer lobbyPlayer && Equals(lobbyPlayer));
        public int CompareTo(Player other) => string.Compare(UserId, other.UserId, StringComparison.Ordinal);
        public int CompareTo(LobbyPlayer other) => string.Compare(UserId, other.Id, StringComparison.Ordinal);
        public static bool operator ==(in Player left, in Player right) => left.Equals(right);
        public static bool operator !=(in Player left, in Player right) => !left.Equals(right);
        public static bool operator ==(in LobbyPlayer left, in Player right) => left.Equals(right);
        public static bool operator !=(in LobbyPlayer left, in Player right) => !left.Equals(right);
        public static bool operator ==(in Player left, in LobbyPlayer right) => right.Equals(left);
        public static bool operator !=(in Player left, in LobbyPlayer right) => !right.Equals(left);

        public override int GetHashCode() => UserId.GetHashCode();

        public CloudAttribute? GetAttribute(string key)
        {
            if (Attributes == null)
            {
                return null;
            }

            foreach (var attribute in Attributes)
            {
                if (attribute.Key.Equals(key))
                {
                    return attribute;
                }
            }

            return null;
        }

        public static implicit operator LobbyPlayer(Player player) => new
        (
            id: player.UserId,
            username: player.Username,
            attributes: player.playerAttributes
        );

        public static implicit operator Player(LobbyPlayer player) => new()
        {
            UserId = player.Id,
            Username = player.Username,
            playerAttributes = player.Attributes.ToList()
        };
    }
}
