// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Replaced by "+nameof(PlayerAccount) + ".")]
    [Deprecated("04/2025", 1, 6, 0, Reason = "Replaced by "+nameof(PlayerAccount) + ".")]
    public record User
    {
        public static readonly User None = new(CloudUniqueId.None, "", "", SessionToken.None);

        public string UserId { get; }

        public CloudUniqueId Guid { get; }

        public string Username { get; }

        public SessionToken SessionToken { get; }

        private User(CloudUniqueId guid, string userId, string username, SessionToken sessionToken)
        {
            Guid = guid;
            UserId = userId ?? "";
            Username = username ?? "";
            SessionToken = sessionToken;
        }

        public override string ToString()
        {
            if (Guid == CloudUniqueId.None)
            {
                return "User: None";
            }

            if (string.IsNullOrEmpty(UserId))
            {
                return $"Guest: {Guid}";
            }

            return $"User: {Username} ({UserId} / {Guid})";
        }

        public static implicit operator CloudUniqueId(User user) => user.Guid;
        public static implicit operator PlayerAccount(User user) => user is null ? null : new(default, new(user.UserId), user.Username, services: null);
        public static implicit operator User(PlayerAccount playerAccount) => playerAccount is null ? null : new(playerAccount.CloudUniqueId, playerAccount.Id, playerAccount.Username, playerAccount.SessionToken);
    }
}
