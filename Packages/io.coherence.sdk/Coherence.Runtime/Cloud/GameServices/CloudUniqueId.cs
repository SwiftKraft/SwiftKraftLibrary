// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

namespace Coherence.Cloud
{
    using System;

    /// <summary>
    /// A locally unique identifier associated with a player account.
    /// <remarks>
    /// <para>
    /// The id can be provided by the user via CoherenceBridge's Editor when 'Player Account' is set to 'Login As Guest',
    /// or via the <see cref="LoginAsGuestOptions"/> when calling
    /// <see cref="CoherenceCloud.LoginAsGuest(LoginAsGuestOptions, System.Threading.CancellationToken)"/>.
    /// If no Cloud Unique Id is provided by the user, then one is generated automatically, and cached locally on the device.
    /// </para>
    /// <para>
    /// Cloud Unique Ids can be used to create and log into multiple different guest player accounts on the same device.
    /// This might be useful for local multiplayer games, allowing each player to log into their own guest player account.
    /// </para>
    /// </remarks>
    /// </summary>
    [Serializable]
    public struct CloudUniqueId : IFormattable, IEquatable<CloudUniqueId>
    {
        /// <summary>
        /// Represents the lack of a Unique Cloud Id.
        /// </summary>
        /// <remarks>
        /// In some contexts this means that a Unique Cloud Id will be automatically generated.
        /// </remarks>
        public static readonly CloudUniqueId None = new("");

#if UNITY
        [UnityEngine.SerializeField]
#endif
        internal string value;

        internal CloudUniqueId(string value) => this.value = value ?? "";

        public override string ToString() => value;
        public string ToString(string format, IFormatProvider formatProvider) => value?.ToString(formatProvider) ?? "";
        public bool Equals(CloudUniqueId other) => string.IsNullOrEmpty(value) ? string.IsNullOrEmpty(other.value) : value.Equals(other.value);
        public override bool Equals(object obj) => obj is CloudUniqueId other && Equals(other);
        public override int GetHashCode() => value?.GetHashCode() ?? 0;

        /// <summary>
        /// Converts the given <see cref="CloudUniqueId"/> into a <see langword="string"/>.
        /// <remarks>
        /// The returned <see langword="string"/> value can be persisted on disk, and later converted back into
        /// a <see cref="CloudUniqueId"/>.
        /// </remarks>>
        /// </summary>
        /// <param name="userId"> The playerAccount id to convert into a <see langword="string"/>. </param>
        /// <returns>
        /// The <see langword="string"/> representation of <see paramref="sessionToken"/>.
        /// </returns>
        public static string Serialize(CloudUniqueId userId) => userId.value ?? "";

        /// <summary>
        /// Converts the previously <see cref="Serialize">serialized</see> <see langword="string"/> representation
        /// of a <see cref="CloudUniqueId"/> back into a <see cref="CloudUniqueId"/>.
        /// </summary>
        /// <param name="serializedUserId"> The <see langword="string"/> to convert back into a
        /// <see cref="CloudUniqueId"/>. </param>
        /// <returns>
        /// An object of type <see cref="CloudUniqueId"/>.
        ///<para>
        /// If <see cref="serializedUserId"/> is null or empty, then <see cref="CloudUniqueId.None"/>.
        /// </para>
        /// </returns>
        public static CloudUniqueId Deserialize(string serializedUserId) => new(serializedUserId);

        public static implicit operator string(CloudUniqueId id) => id.value;
        public static implicit operator CloudUniqueId(string id) => new(id);
        public static bool operator ==(CloudUniqueId left, CloudUniqueId right) => left.Equals(right);
        public static bool operator !=(CloudUniqueId left, CloudUniqueId right) => !left.Equals(right);
    }
}
