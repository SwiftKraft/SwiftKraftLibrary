// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

namespace Coherence.Cloud
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    /// <summary>
    /// A unique identifier for an object stored in <see cref="CloudStorage"/>.
    /// </summary>
    [Serializable]
    public struct StorageObjectId : IEquatable<StorageObjectId>
    {
        #if UNITY
        [UnityEngine.SerializeField]
        #endif
        private string type;

        #if UNITY
        [UnityEngine.SerializeField]
        #endif
        private string id;

        /// <summary>
        /// The maximum number of characters an id can contain.
        /// </summary>
        public const int MaxLength = 4096;

        /// <summary>
        /// Type of the object.
        /// <para>
        /// For example, "Inventory", "Player" or "Settings".
        /// </para>
        /// </summary>
        [NotNull]
        public string Type => type ?? "";

        /// <summary>
        /// A unique identifier for the object, to distinguish it from other objects of the same <see cref="Type"/>.
        /// <para>
        /// For example, the <see cref="PlayerAccount.Id">id</see> of a particular <see cref="PlayerAccount">player account</see>.
        /// </para>
        /// </summary>
        [NotNull]
        public string Id => id ?? "";

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageObjectId"/> struct.
        /// </summary>
        /// <param name="type">
        /// Type of the object.
        /// <para>
        /// For example, "Inventory", "Player" or "Settings".
        /// </para>
        /// </param>
        /// <param name="id">
        /// Local identifier of the object, distinguishing it from other objects that share the same <see cref="type"/>.
        /// <para>
        /// For example, the <see cref="PlayerAccount.Id"/> for a particular player.
        /// </para>
        /// </param>
        /// <exception cref="StorageException">
        /// Thrown if the provided <see cref="Type"/> or <see cref="Id"/> are <see langword="null"/> or empty after being converted into strings,
        /// or if the total length of the <see cref="StorageObjectId"/> exceeds <see cref="MaxLength"/>.
        /// </exception>
        public StorageObjectId([DisallowNull] IFormattable type, [DisallowNull] IFormattable id) : this(type?.ToString(null, CultureInfo.InvariantCulture), id?.ToString(null, CultureInfo.InvariantCulture)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageObjectId"/> struct.
        /// </summary>
        /// <param name="type">
        /// Type of the storage object.
        /// <para>
        /// For example, "Inventory", "Player" or "Settings".
        /// </para>
        /// </param>
        /// <param name="id">
        /// Local identifier of the storage object, distinguishing it from other storage objects of the same <see cref="Type"/>.
        /// <para>
        /// For example, the <see cref="PlayerAccount.Id"/> for a particular player.
        /// </para>
        /// </param>
        /// <exception cref="StorageException">
        /// Thrown if the provided <see cref="Type"/> or <see cref="Id"/> are <see langword="null"/> or empty after being converted into strings,
        /// or if the total length of the <see cref="StorageObjectId"/> exceeds <see cref="MaxLength"/>.
        /// </exception>
        public StorageObjectId([DisallowNull] IFormattable type, [DisallowNull] string id) : this(type?.ToString(null, CultureInfo.InvariantCulture), id) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageObjectId"/> struct.
        /// </summary>
        /// <param name="type">
        /// Type of the storage object.
        /// <para>
        /// For example, "Inventory", "Player" or "Settings".
        /// </para>
        /// </param>
        /// <param name="id">
        /// Local identifier of the storage object, distinguishing it from other storage objects of the same <see cref="Type"/>.
        /// <para>
        /// For example, the <see cref="PlayerAccount.Id"/> for a particular player.
        /// </para>
        /// </param>
        /// <exception cref="StorageException">
        /// Thrown if the provided <see cref="Type"/> or <see cref="Id"/> are <see langword="null"/> or empty after being converted into strings,
        /// or if the total length of the <see cref="StorageObjectId"/> exceeds <see cref="MaxLength"/>.
        /// </exception>
        public StorageObjectId([DisallowNull] string type, [DisallowNull] IFormattable id) : this(type, id?.ToString(null, CultureInfo.InvariantCulture)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageObjectId"/> struct.
        /// </summary>
        /// <param name="type">
        /// Type of the storage object.
        /// <para>
        /// For example, "Inventory", "Player" or "Settings".
        /// </para>
        /// </param>
        /// <param name="id">
        /// Local identifier of the storage object, distinguishing it from other storage objects of the same <see cref="Type"/>.
        /// <para>
        /// For example, the <see cref="PlayerAccount.Id"/> for a particular player.
        /// </para>
        /// </param>
        /// <exception cref="StorageException">
        /// Thrown if the provided <see cref="Type"/> or <see cref="Id"/> are <see langword="null"/> or empty,
        /// or if the total length of the <see cref="StorageObjectId"/> exceeds <see cref="MaxLength"/>.
        /// </exception>
        public StorageObjectId([DisallowNull] string type, [DisallowNull] string id)
        {
            this.type = type;
            this.id = id;

            #if UNITY_ASSERTIONS
            var totalLength = type switch
            {
                null => throw new StorageException(StorageErrorType.InvalidObjectId, $"{nameof(StorageObjectId)}.{nameof(Type)} cannot be null."),
                { Length: 0 } => throw new StorageException(StorageErrorType.InvalidObjectId, $"{nameof(StorageObjectId)}.{nameof(Type)} cannot be empty."),
                _ => type.Length
            };

            totalLength += id switch
            {
                null => throw new StorageException(StorageErrorType.InvalidObjectId, $"{nameof(StorageObjectId)}.{nameof(Id)} cannot be null."),
                { Length: 0 } => throw new StorageException(StorageErrorType.InvalidObjectId, $"{nameof(StorageObjectId)}.{nameof(Id)} cannot be empty."),
                _ => id.Length
            };

            if (totalLength > MaxLength)
            {
                throw new StorageException(StorageErrorType.InvalidObjectId, $"{nameof(StorageObjectId)} cannot be longer than {MaxLength} characters long.\nInvalid id:\"{Type}/{Id}\"");
            }
            #endif
        }

        [return: NotNull]
        public override string ToString() => $"{Type}/{Id}";
        public bool Equals(StorageObjectId other) => string.Equals(Type, other.Type) && string.Equals(Id, other.Id);
        public override bool Equals(object obj) => obj is StorageObjectId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Type, Id);
        public static bool operator ==(StorageObjectId left, StorageObjectId right) => left.Equals(right);
        public static bool operator !=(StorageObjectId left, StorageObjectId right) => !left.Equals(right);

        public static implicit operator StorageObjectId((string type, string id) item) => new(item.type, item.id);
        public static implicit operator StorageObjectId((IFormattable type, string id) item) => new(item.type, item.id);
        public static implicit operator StorageObjectId((string type, IFormattable id) item) => new(item.type, item.id);
        public static implicit operator StorageObjectId((IFormattable type, IFormattable id) item) => new(item.type, item.id);
    }
}
