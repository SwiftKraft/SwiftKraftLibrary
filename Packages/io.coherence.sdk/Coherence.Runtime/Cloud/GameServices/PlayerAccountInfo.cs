// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains all data associated with a <see cref="PlayerAccount"/> from coherence Cloud.
    /// </summary>
    /// <seealso cref="PlayerAccount.GetInfo"/>
    public sealed record PlayerAccountInfo
    {
        /// <summary>
        /// The globally unique identifier for the player account.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This id is generated on the coherence Cloud backend when the user first logs into the player account.
        /// </para>
        /// <para>
        /// The id will always remains the same, regardless of the identities are linked to it and unlinked
        /// from it after its creation.
        /// </para>
        /// </remarks>
        public PlayerAccountId Id { get; }

        /// <summary>
        /// Username associated with the player account, if it has been given one; otherwise, an empty string.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// The display name of the player account.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The URL of the public avatar image for the player account.
        /// </summary>
        public string AvatarUrl { get; }

        /// <summary>
        /// List of all the identities linked to the player account.
        /// </summary>
        public IReadOnlyList<Identity> Identities { get; }

        /// <summary>
        /// The date and time when the player account was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; }

        /// <summary>
        /// Indicates whether the player account is verified.
        /// </summary>
        /// <remarks>
        /// Player accounts that have been authenticated using any of the following providers
        /// are considered to be human (as opposed to a potential bot) and are marked as verified:
        /// <list type="bullet">
        /// <item> <description>
        /// <see cref="CoherenceCloud.LoginWithSteam">Steam</see>
        /// </description> </item>
        /// <item> <description>
        /// <see cref="CoherenceCloud.LoginWithEpicGames">Epic Games</see>
        /// </description> </item>
        /// <item> <description>
        /// <see cref="CoherenceCloud.LoginWithPlayStation">PlayStation Network</see>
        /// </description> </item>
        /// <item> <description>
        /// <see cref="CoherenceCloud.LoginWithXbox">Xbox</see>
        /// </description> </item>
        /// <item> <description>
        /// <see cref="CoherenceCloud.LoginWithNintendo">Nintendo</see>
        /// </description> </item>
        /// </list>
        /// </remarks>
        public bool IsVerified { get; }

        internal PlayerAccountInfo(string id, string username, string displayName, string avatarUrl, Identity[] identities, DateTimeOffset createdAt, bool isVerified)
        {
            Id = id;
            Username = username;
            DisplayName = displayName;
            AvatarUrl = avatarUrl;
            CreatedAt = createdAt;
            IsVerified = isVerified;
            Identities = identities;
        }
    }

    /// <summary>
    /// Represents a single identity linked to a player account.
    /// </summary>
    public sealed record Identity
    {
        /// <summary>
        /// The type of identity.
        /// </summary>
        public IdentityType Type { get; }

        /// <summary>
        /// An identifier associated with the identity; a username, an identity token, etc.
        /// </summary>
        public string Id { get; }

        internal Identity(IdentityType type, string id)
        {
            Id = id;
            Type = type;
        }
    }

    /// <summary>
    /// Specifies the types of identities that can be linked to a player account.
    /// </summary>
    public enum IdentityType
    {
        Unknown = 0,
        Guest,
        UsernameAndPassword,
        Steam,
        EpicGames,
        PlayStation,
        Xbox,
        Nintendo,
        Jwt,
        OneTimeCode,
        Google,
        Apple
    }
}
