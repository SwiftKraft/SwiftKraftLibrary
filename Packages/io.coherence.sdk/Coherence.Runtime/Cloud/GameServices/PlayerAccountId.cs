// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;

    /// <summary>
    /// A globally unique identifier for a <see cref="PlayerAccount"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This id is generated on the coherence Cloud backend when logging in to coherence Cloud for the first time.
    /// </para>
    /// <para>
    /// The id will always remains the same, regardless of the identities are linked to it and unlinked
    /// from it after its creation.
    /// </para>
    /// </remarks>
    [Serializable]
    public readonly struct PlayerAccountId : IFormattable, IEquatable<PlayerAccountId>
    {
        /// <summary>
        /// Represents the lack of an id.
        /// </summary>
        public static readonly PlayerAccountId None = new("");

        private readonly string value;

        internal PlayerAccountId(string value) => this.value = value ?? "";

        public override string ToString() => value ?? "";
        public string ToString(string format, IFormatProvider formatProvider) => value?.ToString(formatProvider) ?? "";
        public bool Equals(PlayerAccountId other) => string.IsNullOrEmpty(value) ? string.IsNullOrEmpty(other.value) : value.Equals(other.value);
        public override bool Equals(object obj) => obj is PlayerAccountId other && Equals(other);
        public override int GetHashCode() => value?.GetHashCode() ?? 0;

        /// <summary>
        /// Converts the given <see cref="PlayerAccountId"/> into a <see langword="string"/>.
        /// <remarks>
        /// The returned <see langword="string"/> value can be persisted on disk, and later converted back into
        /// a <see cref="PlayerAccountId"/>.
        /// </remarks>>
        /// </summary>
        /// <param name="id"> The id to convert into a <see langword="string"/>. </param>
        /// <returns>
        /// The <see langword="string"/> representation of <see paramref="sessionToken"/>.
        /// </returns>
        public static string Serialize(PlayerAccountId id) => id.value ?? "";

        /// <summary>
        /// Converts the previously <see cref="Serialize">serialized</see> <see langword="string"/> representation
        /// of a <see cref="PlayerAccountId"/> back into a <see cref="PlayerAccountId"/>.
        /// </summary>
        /// <param name="serializedId"> The <see langword="string"/> to convert back into a
        /// <see cref="PlayerAccountId"/>. </param>
        /// <returns>
        /// An object of type <see cref="PlayerAccountId"/>.
        ///<para>
        /// If <see cref="serializedId"/> is null or empty, then <see cref="PlayerAccountId.None"/>.
        /// </para>
        /// </returns>
        public static PlayerAccountId Deserialize(string serializedId) => new(serializedId);

        public static implicit operator string(PlayerAccountId id) => id.value;
        public static implicit operator PlayerAccountId(string id) => new(id);
        public static bool operator ==(PlayerAccountId left, PlayerAccountId right) => left.Equals(right);
        public static bool operator !=(PlayerAccountId left, PlayerAccountId right) => !left.Equals(right);
    }
}
