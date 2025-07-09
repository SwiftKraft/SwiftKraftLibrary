// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a single item in a <see cref="StorageObject">storage object</see>.
    /// </summary>
    /// <example>
    /// <code source="Runtime/CloudStorage/StorageItemExample.cs" language="csharp"/>
    /// </example>
    internal readonly struct StorageItem : IEquatable<StorageItem>
    {
        /// <summary>
        /// Gets the key of the storage item.
        /// </summary>
        public Key Key { get; }

        /// <summary>
        /// Gets the value of the storage item.
        /// </summary>
        public Value Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageItem"/> struct.
        /// </summary>
        /// <param name="key"> The key of the storage item. </param>
        /// <param name="value"> The value of the storage item. </param>
        public StorageItem(Key key, Value value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageItem"/> struct.
        /// </summary>
        /// <param name="key"> The key of the storage item. </param>
        /// <param name="value"> The value of the storage item. </param>
        public StorageItem(Key key, object value)
        {
            Key = key;
            Value = new(value);
        }

        public void Deconstruct(out Key key, out Value value)
        {
            key = Key;
            value = Value;
        }

        public bool Equals(StorageItem other) => Key.Equals(other.Key) && Value.Equals(other.Value);
        public override bool Equals(object obj) => obj is StorageItem other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Key, Value);
        public override string ToString() => $"{{ {Key}: {Value} }}";

        public static bool operator ==(StorageItem left, StorageItem right) => left.Equals(right);
        public static bool operator !=(StorageItem left, StorageItem right) => !left.Equals(right);
        public static implicit operator StorageItem(KeyValuePair<Key, Value> item) => new(item.Key, item.Value);
        public static implicit operator StorageItem((Key key, Value value) item) => new(item.key, item.value);
        public static implicit operator KeyValuePair<Key, Value>(StorageItem item) => new(item.Key, item.Value);
        public static implicit operator Value(StorageItem item) => item.Value;
    }
}
