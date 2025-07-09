// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#nullable enable

namespace Coherence.Cloud
{
    using System;
    using Prefs;
    using Runtime;
    using Runtime.Utils;

    /// <summary>
    /// Represents a unique identifier for a guest player account that has logged in to coherence Cloud.
    /// </summary>
    /// <remarks>
    /// A new <see cref="GuestId"/> is generated automatically the first time when logging in as a guest to a particular project,
    /// and then cached and reused when logging in again as a guest again to the same project using the same device.
    /// </remarks>
    public readonly struct GuestId : IEquatable<GuestId>
    {
        private const int MaxLength = 32;

        /// <summary>
        /// Represents the lack of an id.
        /// </summary>
        public static readonly GuestId None = new("");

        private readonly string? id;

        private GuestId(string? id) => this.id = id;

        /// <summary>
        /// Converts the given <see cref="GuestId"/> into a <see langword="string"/>.
        /// <remarks>
        /// The returned <see langword="string"/> value can be persisted on disk, and later converted back into
        /// a <see cref="GuestId"/>.
        /// </remarks>>
        /// </summary>
        /// <param name="guestId"> The session token to convert into a <see langword="string"/>. </param>
        /// <returns>
        /// The <see langword="string"/> representation of <see paramref="guestId"/>.
        /// </returns>
        public static string Serialize(GuestId guestId) => guestId.id ?? "";

        /// <summary>
        /// Converts the previously <see cref="Serialize">serialized</see> <see langword="string"/> representation
        /// of a <see cref="GuestId"/> back into a <see cref="GuestId"/>.
        /// </summary>
        /// <param name="serializedGuestId"> The <see langword="string"/> to convert back into a
        /// <see cref="GuestId"/>. </param>
        /// <returns> An object of type <see cref="GuestId"/>. </returns>
        public static GuestId Deserialize(string serializedGuestId) => new(serializedGuestId);

        internal static GuestId GetOrCreate(string projectId, CloudUniqueId uniqueId)
        {
            var prefsKey = GetPrefsKey(projectId, uniqueId);
            if (Prefs.GetString(prefsKey, null) is { Length: > 0 } id)
            {
                return new(id);
            }

            var usernamePrefsKey = LegacyLoginData.GetUsernamePrefsKey(projectId, uniqueId);
            var passwordPrefsKey = LegacyLoginData.GetGuestPasswordPrefsKey(projectId, uniqueId);
            if (Prefs.GetString(usernamePrefsKey, null) is { Length: > 0 } username
                && Prefs.GetString(passwordPrefsKey, null) is { Length: > 0 } password)
            {
                id = FromLegacyLoginData(username, password);
            }
            else
            {
                id = Guid.NewGuid().ToString("N");
            }

            Prefs.SetString(prefsKey, id);
            return new(id);
        }

        internal static GuestId FromLegacyLoginData(string username, string password)
        {
            return new(GetPart(username) + GetPart(password));

            static string GetPart(string input)
            {
                const int partMaxLength = MaxLength / 2;
                return input.Length > partMaxLength ? input[..partMaxLength] : input;
            }
        }

        internal static void Save(string projectId, CloudUniqueId uniqueId, GuestId id) => Prefs.SetString(GetPrefsKey(projectId, uniqueId), id);

        internal static void Delete(string projectId, CloudUniqueId uniqueId) => Prefs.DeleteKey(GetPrefsKey(projectId, uniqueId));

        internal static string GetPrefsKey(string projectId, CloudUniqueId uniqueId) => PrefsKeys.CachedGuestId.Format(projectId, uniqueId);

        public override string ToString() => id ?? "";
        public static implicit operator string(GuestId id) => id.id ?? "";
        public static implicit operator GuestId(string? id) => new(id);
        public bool Equals(GuestId other) => string.IsNullOrEmpty(id) ? string.IsNullOrEmpty(other.id) : id.Equals(other.id);
        public override bool Equals(object? obj) => obj is GuestId other && Equals(other);
        public override int GetHashCode() => id?.GetHashCode() ?? 0;
    }
}
