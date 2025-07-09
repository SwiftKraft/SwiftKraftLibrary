// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System;

    /// <summary>
    /// A token uniquely identifying a <see cref="PlayerAccount">player account</see> that has
    /// logged in to <see cref="CoherenceCloud">coherence Cloud</see>.
    /// </summary>
    /// <remarks>
    /// The token can be stored on the user's device locally, and later used to
    /// <see cref="CoherenceCloud.LoginWithSessionToken(Coherence.Cloud.SessionToken, System.Threading.CancellationToken)">log in to coherence Cloud</see>
    /// again using the same credentials, without the user needing to provide them again.
    /// </remarks>
    public readonly struct SessionToken : IEquatable<SessionToken>
    {
        /// <summary>
        /// Represents the absence of a session token.
        /// </summary>
        public static readonly SessionToken None = new();

        private readonly string value;

        internal SessionToken(string value) => this.value = value;

        /// <summary>
        /// Converts the given <see cref="SessionToken"/> into a <see langword="string"/>.
        /// <remarks>
        /// The returned <see langword="string"/> value can be persisted on disk, and later converted back into
        /// a <see cref="SessionToken"/>.
        /// </remarks>>
        /// </summary>
        /// <param name="sessionToken"> The session token to convert into a <see langword="string"/>. </param>
        /// <returns>
        /// The <see langword="string"/> representation of <see paramref="sessionToken"/>.
        /// </returns>
        public static string Serialize(SessionToken sessionToken) => sessionToken.value ?? "";

        /// <summary>
        /// Converts the previously <see cref="Serialize">serialized</see> <see langword="string"/> representation
        /// of a <see cref="SessionToken"/> back into a <see cref="SessionToken"/>.
        /// </summary>
        /// <param name="serializedSessionToken"> The <see langword="string"/> to convert back into a
        /// <see cref="SessionToken"/>. </param>
        /// <returns>
        /// An object of type <see cref="SessionToken"/>.
        ///<para>
        /// If <see cref="serializedSessionToken"/> is null or empty, then <see cref="SessionToken.None"/>.
        /// </para>
        /// </returns>
        public static SessionToken Deserialize(string serializedSessionToken) => new(serializedSessionToken);

        public static implicit operator string(SessionToken sessionToken) => sessionToken.value ?? "";
        public static implicit operator SessionToken(string sessionToken) => new(sessionToken);
        public static bool operator ==(SessionToken x, SessionToken y) => x.Equals(y);
        public static bool operator !=(SessionToken x, SessionToken y) => !x.Equals(y);
        public override string ToString() => value ?? "";
        public override int GetHashCode() => string.IsNullOrEmpty(value) ? 0 : value.GetHashCode();
        public bool Equals(SessionToken other) => Equals(other.value);
        public override bool Equals(object obj) => obj switch
        {
            SessionToken sessionToken => Equals(sessionToken),
            string @string => Equals(@string),
            _ => false
        };
        private bool Equals(string otherValue) => string.IsNullOrEmpty(otherValue) ? string.IsNullOrEmpty(value) : string.Equals(otherValue, value);
    }
}
