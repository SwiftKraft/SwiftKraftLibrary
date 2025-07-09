// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Key associated with a single <see cref="StorageItem"/> that can be saved in <see cref="CloudStorage"/>
    /// as part of a <see cref="StorageObject">storage object</see>.
    /// </summary>
    internal readonly struct Key : IEquatable<Key>
    {
        /// <summary>
        /// The maximum number of characters a key can contain.
        /// </summary>
        public const int MaxLength = 4096;

        /// <summary>
        /// The <see langword="string"/> representation of the key.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Key"/> struct.
        /// </summary>
        /// <param name="content"> The <see langword="string"/> representation of the key. </param>
        /// <exception cref="StorageException">
        /// Thrown if the provided <see langword="string"/> is <see langword="null"/> or contains more than
        /// <see cref="MaxLength"/> characters.
        /// </exception>
        public Key([DisallowNull] string content) => Content = content switch
        {
            #if UNITY_ASSERTIONS
            null => throw GetException("Key cannot be null."),
            { Length: > MaxLength } => throw GetException($"Key cannot be longer than {MaxLength} characters long.\nInvalid Key:\"{content}\""),
            #endif
            _ => content
        };

        private static StorageException GetException(string message) => new(StorageErrorType.InvalidKey, message);

        public bool Equals(Key other) => Content == other.Content;
        public override bool Equals(object obj) => obj is Key other && Equals(other);
        public override int GetHashCode() => (Content != null ? Content.GetHashCode() : 0);
        public override string ToString() => Content;

        public static bool operator ==(Key left, Key right) => left.Equals(right);
        public static bool operator !=(Key left, Key right) => !left.Equals(right);
        public static bool operator ==(Key left, string right) => left.Content.Equals(right);
        public static bool operator !=(Key left, string right) => !left.Content.Equals(right);
        public static implicit operator Key(string value) => new(value);
        public static implicit operator string(Key value) => value.Content;
    }
}
