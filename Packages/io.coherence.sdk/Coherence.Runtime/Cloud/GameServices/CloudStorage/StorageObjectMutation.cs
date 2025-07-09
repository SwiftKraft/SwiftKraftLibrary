// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Cloud
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Represents a request to modify items in a <see cref="StorageObject">storage object</see> stored in <see cref="CloudStorage"/>.
    /// <remarks>
    /// If <see cref="Type"/> is set to <see langword="StorageObjectMutationType.Full"/>, then the <see cref="StorageItem">items</see> that the
    /// storage object contains in <see cref="CloudStorage"/> are entirely replaced with the items in the mutation; any other pre-existing items that
    /// the storage object had in will be removed.
    /// </remarks>
    /// </summary>
    /// <example>
    /// <code source="Runtime/CloudStorage/StorageObjectMutationExample.cs" language="csharp"/>
    /// </example>
    /// <seealso cref="CloudStorage.SaveAsync(StorageObjectMutation, System.Threading.CancellationToken)"/>
    internal sealed class StorageObjectMutation : ICollection<StorageItem>
    {
        internal readonly StorageObject storageObject;

        /// <summary>
        /// Does this mutation set the entire contents of the storage object, or only affect a subset of its items?
        /// <remarks>
        /// When value is set to <see langword="StorageObjectMutationType.Full"/>, any existing items that the storage object had in
        /// <see cref="CloudStorage"/> that are not listed in the mutation will be removed from the storage object.
        /// </remarks>
        /// </summary>
        public StorageObjectMutationType Type { get; }

        /// <summary>
        /// Gets the identifier of the storage object that is modified.
        /// </summary>
        public StorageObjectId ObjectId => storageObject.ObjectId;

        /// <summary>
        /// Gets the number of items in the mutation.
        /// </summary>
        public int Count => storageObject.Count;

        /// <summary>
        /// Gets a collection containing the keys in the mutation.
        /// </summary>
        public Dictionary<Key, Value>.KeyCollection Keys => storageObject.Keys;

        /// <summary>
        /// Gets a collection containing the values in the mutation.
        /// </summary>
        public Dictionary<Key, Value>.ValueCollection Values => storageObject.Values;

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key"> The key of the item to get or set. </param>
        public Value this[Key key]
        {
            get => storageObject[key];
            set => storageObject[key] = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageObjectMutation"/> class.
        /// </summary>
        /// <param name="objectId"> Identifier of the storage object to mutate. </param>
        /// <param name="type">
        /// Type of the mutation to perform on the storage object.
        /// <para>
        /// When value is set to <see langword="StorageObjectMutationType.Full"/>, any existing items that the storage object had in
        /// <see cref="CloudStorage"/> that are not listed in the mutation will be removed from the storage object.
        /// </para>
        /// </param>
        public StorageObjectMutation(StorageObjectId objectId, StorageObjectMutationType type)
        {
            storageObject = new(objectId);
            Type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageObjectMutation"/> class.
        /// </summary>
        /// <param name="storageObject"> The storage object to mutate. </param>
        /// <param name="type">
        /// Type of the mutation to perform on the storage object.
        /// <para>
        /// When value is set to <see langword="StorageObjectMutationType.Full"/>, any existing items that the storage object had in
        /// <see cref="CloudStorage"/> that are not listed in the mutation will be removed from the storage object.
        /// </para>
        /// </param>
        public StorageObjectMutation(StorageObject storageObject, StorageObjectMutationType type = StorageObjectMutationType.Full)
        {
            this.storageObject = storageObject;
            Type = type;
        }

        /// <summary>
        /// Determines whether the mutation contains the specified key.
        /// </summary>
        /// <param name="key"> The key to locate in the mutation. </param>
        /// <returns>
        /// <see langword="true"/> if the mutation contains an item with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool ContainsKey(Key key) => storageObject.ContainsKey(key);

        /// <summary>
        /// Determines whether the mutation contains the specified item.
        /// </summary>
        /// <param name="item"> The item to locate in the mutation. </param>
        /// <returns>
        /// <see langword="true"/> if the mutation contains an item with the specified key, and the value
        /// of the item matches the specified value; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Contains(StorageItem item) => storageObject.Contains(item);

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key"> The key of the value to get. </param>
        /// <param name="value">
        /// When this method returns, contains the value associated with the specified key, if found; otherwise, <see cref="Value.None"/>.
        /// <para>
        /// This parameter is passed uninitialized.
        /// </para>
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the mutation contains an element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out Value value) => storageObject.TryGetValue(key, out value);

        /// <summary>
        /// Gets the <see langword="bool"/> value associated with the specified key.
        /// </summary>
        /// <param name="key"> The key of the value to get. </param>
        /// <param name="value">
        /// When this method returns, contains the <see langword="bool"/> value associated with the specified key, if found; otherwise, <see langword="false"/>.
        /// <para>
        /// This parameter is passed uninitialized.
        /// </para>
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the mutation contains a <see langword="bool"/> element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out bool value) => storageObject.TryGetValue(key, out value);

        /// <summary>
        /// Gets the <see langword="int"/> value associated with the specified key.
        /// </summary>
        /// <param name="key"> The key of the value to get. </param>
        /// <param name="value">
        /// When this method returns, contains the <see langword="int"/> value associated with the specified key, if found; otherwise, 0.
        /// <para>
        /// This parameter is passed uninitialized.
        /// </para>
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the mutation contains an <see langword="int"/> element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out int value) => storageObject.TryGetValue(key, out value);

        /// <summary>
        /// Gets the value of type <see typeparamref="TValue"/> associated with the specified key.
        /// </summary>
        /// <param name="key"> The key of the value to get. </param>
        /// <param name="value">
        /// When this method returns, contains the <see typeparamref="TValue"/> value associated with the specified key, if found; otherwise, the default
        /// value of <see typeparamref="TValue"/>.
        /// <para>
        /// This parameter is passed uninitialized.
        /// </para>
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the mutation contains an element of type <see typeparamref="TValue"/> with the
        /// specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue<TValue>(Key key, out TValue value) => storageObject.TryGetValue(key, out value);

        /// <summary>
        /// Gets the <see langword="string"/> value associated with the specified key.
        /// </summary>
        /// <param name="key"> The key of the value to get. </param>
        /// <param name="value">
        /// When this method returns, contains the <see langword="string"/> value associated with the specified key, if found; otherwise, <see langword="null"/>.
        /// <para>
        /// This parameter is passed uninitialized.
        /// </para>
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the mutation contains a <see langword="string"/> element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out string value) => storageObject.TryGetValue(key, out value);

        /// <summary>
        /// Gets the <see langword="float"/> value associated with the specified key.
        /// </summary>
        /// <param name="key"> The key of the value to get. </param>
        /// <param name="value">
        /// When this method returns, contains the <see langword="float"/> value associated with the specified key, if found; otherwise, 0f.
        /// <para>
        /// This parameter is passed uninitialized.
        /// </para>
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the mutation contains a <see langword="float"/> element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out float value) => storageObject.TryGetValue(key, out value);

        /// <summary>
        /// Gets the <see langword="double"/> value associated with the specified key.
        /// </summary>
        /// <param name="key"> The key of the value to get. </param>
        /// <param name="value">
        /// When this method returns, contains the <see langword="double"/> value associated with the specified key, if found; otherwise, 0d.
        /// <para>
        /// This parameter is passed uninitialized.
        /// </para>
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the mutation contains a <see langword="double"/> element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out double value) => storageObject.TryGetValue(key, out value);

        /// <summary>
        /// Gets the <see langword="short"/> value associated with the specified key.
        /// </summary>
        /// <param name="key"> The key of the value to get. </param>
        /// <param name="value">
        /// When this method returns, contains the <see langword="short"/> value associated with the specified key, if found; otherwise, 0.
        /// <para>
        /// This parameter is passed uninitialized.
        /// </para>
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the mutation contains a <see langword="short"/> element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out short value) => storageObject.TryGetValue(key, out value);

        /// <summary>
        /// Gets the <see langword="byte"/> value associated with the specified key.
        /// </summary>
        /// <param name="key"> The key of the value to get. </param>
        /// <param name="value">
        /// When this method returns, contains the <see langword="byte"/> value associated with the specified key, if found; otherwise, 0.
        /// <para>
        /// This parameter is passed uninitialized.
        /// </para>
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the mutation contains a <see langword="byte"/> element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out byte value) => storageObject.TryGetValue(key, out value);

        /// <summary>
        /// Gets the <see langword="Enum"/> value associated with the specified key.
        /// </summary>
        /// <param name="key"> The key of the value to get. </param>
        /// <param name="value">
        /// When this method returns, contains the <see langword="Enum"/> value associated with the specified key, if found; otherwise, <see langword="null"/>
        /// <para>
        /// This parameter is passed uninitialized.
        /// </para>
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the mutation contains a <see langword="Enum"/> element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out Enum value) => storageObject.TryGetValue(key, out value);

        /// <summary>
        /// Removes all items from the mutation.
        /// </summary>
        public void Clear() => storageObject.Clear();

        /// <summary>
        /// Removes the value associated with the specified key from the mutation.
        /// </summary>
        /// <param name="key"> The key of the value to remove from the mutation. </param>
        /// <returns>
        /// <see langword="true"/> if the mutation contained an element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Remove(Key key) => storageObject.Remove(key);

        /// <summary>
        /// Removes the specified item from the mutation.
        /// </summary>
        /// <param name="item"> The item to remove from the mutation. </param>
        /// <see langword="true"/> if the mutation contained an item with the specified key, and the value
        /// <returns>
        /// of the item matched the specified value; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Remove(StorageItem item) => storageObject.Remove(item);

        /// <summary>
        /// Removes items with the specified items from the mutation.
        /// </summary>
        /// <param name="keys"> The keys of items to remove from the mutation. </param>
        public int RemoveItems([DisallowNull] IEnumerable<Key> keys) => storageObject.RemoveItems(keys);

        /// <summary>
        /// Removes the specified items from the mutation.
        /// <para>
        /// Both the key and value of an item must match for it to be removed.
        /// </para>
        /// </summary>
        /// <param name="items"> The items to remove from the mutation. </param>
        public int RemoveItems([DisallowNull] IEnumerable<StorageItem> items) => storageObject.RemoveItems(items);

        /// <summary>
        /// Removes items with the specified items from the mutation.
        /// </summary>
        /// <param name="keys"> The keys of items to remove from the mutation. </param>
        public int RemoveItems([DisallowNull] params Key[] keys) => storageObject.RemoveItems(keys);

        /// <summary>
        /// Removes the specified items from the mutation.
        /// <para>
        /// Both the key and value of an item must match for it to be removed.
        /// </para>
        /// </summary>
        /// <param name="items"> The items to remove from the mutation. </param>
        public int RemoveItems([DisallowNull] params StorageItem[] items) => storageObject.RemoveItems(items);

        /// <summary>
        /// Sets the values associated with the specified keys.
        /// <para>
        /// Any existing values in the mutation associated with the specified keys will be replaced.
        /// </para>
        /// </summary>
        /// <param name="items"> The items to set. </param>
        public void SetItems([DisallowNull] IEnumerable<StorageItem> items) => storageObject.SetItems(items);

        /// <summary>
        /// Sets the values associated with the specified keys.
        /// <para>
        /// Any existing values in the mutation associated with the specified keys will be replaced.
        /// </para>
        /// </summary>
        /// <param name="items"> The items to set. </param>
        public void SetItems([DisallowNull] IEnumerable<KeyValuePair<Key, Value>> items) => storageObject.SetItems(items);

        /// <summary>
        /// Sets the values associated with the specified keys.
        /// <para>
        /// Any existing values in the mutation associated with the specified keys will be replaced.
        /// </para>
        /// </summary>
        /// <param name="items"> The items to set. </param>
        public void SetItems([DisallowNull] IEnumerable<KeyValuePair<string, string>> items) => storageObject.SetItems(items);

        /// <summary>
        /// Sets the values associated with the specified keys.
        /// <para>
        /// Any existing values in the mutation associated with the specified keys will be replaced.
        /// </para>
        /// </summary>
        /// <param name="items"> The items to set. </param>
        public void SetItems([DisallowNull] params StorageItem[] items) => storageObject.SetItems(items);

        /// <summary>
        /// Sets the value associated with the specified key.
        /// <para>
        /// Any existing values in the mutation associated with the specified key will be replaced.
        /// </para>
        /// </summary>
        /// <param name="item"> The item to set. </param>
        public void Set(StorageItem item) => storageObject.Set(item);

        /// <summary>
        /// Sets the value associated with the specified key.
        /// <para>
        /// Any existing values in the mutation associated with the specified key will be replaced.
        /// </para>
        /// </summary>
        /// <param name="key"> The key to associate the value with. </param>
        /// <param name="value"> The value to associate with they key. </param>
        public void Set(Key key, Value value) => storageObject.Set(key, value);

        bool ICollection<StorageItem>.IsReadOnly => false;
        void ICollection<StorageItem>.Add(StorageItem item) => ((ICollection<StorageItem>)storageObject).Add(item);
        void ICollection<StorageItem>.CopyTo(StorageItem[] array, int arrayIndex) => ((ICollection<StorageItem>)storageObject).CopyTo(array, arrayIndex);

        IEnumerator<StorageItem> IEnumerable<StorageItem>.GetEnumerator() => ((IEnumerable<StorageItem>)storageObject).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)storageObject).GetEnumerator();
    }
}
