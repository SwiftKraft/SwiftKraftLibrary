// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Contains information about a player that has joined a <see cref="LobbyData">lobby</see>.
    /// </summary>
    public struct LobbyPlayer : IEquatable<LobbyPlayer>, IComparable<LobbyPlayer>
    {
        [JsonProperty("id")]
        private string id;

        [JsonProperty("username")]
        public string username;

        [JsonProperty("attributes")]
        internal List<CloudAttribute> attributes;

        public PlayerAccountId Id => id;

        [JsonIgnore]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use " + nameof(Id) + " instead.")]
        [Deprecated("04/2025", 1, 6, 0, Reason = "Use " + nameof(Id) + " instead.")]
        public string UserId => id;

        [NotNull] public string Username => username ?? "";
        [NotNull] public IReadOnlyList<CloudAttribute> Attributes => attributes ?? (IReadOnlyList<CloudAttribute>)Array.Empty<CloudAttribute>();

        bool IEquatable<LobbyPlayer>.Equals(LobbyPlayer other) => Equals(other);

        internal LobbyPlayer(string id)
        {
            this.id = id;
            this.username = "";
            attributes = new(0);
        }

        internal LobbyPlayer(string id, string username, List<CloudAttribute> attributes)
        {
            this.id = id;
            this.username = username;
            this.attributes = attributes;
        }

        public bool Equals(in LobbyPlayer other) => Id == other.Id;
        public override bool Equals(object obj) => obj is LobbyPlayer other && Equals(other);
        public int CompareTo(LobbyPlayer other) => string.Compare(Id, other.Id, StringComparison.Ordinal);
        public static bool operator ==(in LobbyPlayer left, in LobbyPlayer right) => left.Equals(right);
        public static bool operator !=(in LobbyPlayer left, in LobbyPlayer right) => !left.Equals(right);

        public override int GetHashCode() => Id.GetHashCode();

        public CloudAttribute? GetAttribute(string key)
        {
            foreach (var attribute in Attributes)
            {
                if (attribute.Key.Equals(key))
                {
                    return attribute;
                }
            }

            return null;
        }
    }
}
