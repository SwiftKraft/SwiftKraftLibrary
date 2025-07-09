// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

namespace Coherence.Cloud
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Common.Extensions;
    using Newtonsoft.Json;
    using Utils;

    /// <summary>
    /// A read-only container of <see cref="StorageItem">storage items</see> loaded from <see cref="CloudStorage"/>.
    /// </summary>
    /// <seealso cref="CloudStorage.LoadObjectAsync"/>
    internal sealed class StorageObject : ICollection<StorageItem>
    {
        internal static readonly JsonConverter[] jsonConverters =
        {
#if UNITY
            new Toolkit.UnityVector2Converter(), new Toolkit.UnityVector3Converter(), new Toolkit.UnityQuaternionConverter(), new Toolkit.UnityColorConverter(), new Toolkit.UnityColor32Converter()
#endif
        };

        private readonly Dictionary<Key, Value> dictionary = new();

        /// <summary>
        /// Gets the identifier of the storage object.
        /// </summary>
        public StorageObjectId ObjectId { get; }

        private object @object;
        private Type objectType;

        [MaybeNull]
        public object Object => objectType is not null ? @object : dictionary.ToDictionary(item => new KeyValuePair<string, string>(item.Key.Content, item.Value.Json));

        public bool TryGetObject<TObject>([MaybeNull] out TObject result)
        {
            if (objectType is not null && typeof(TObject).IsAssignableFrom(objectType))
            {
                result = (TObject)@object;
                return true;
            }

            result = default;
            return false;
        }


        [NotNull]
        public Type ObjectType => objectType ?? typeof(Dictionary<string, string>);

        /// <summary>
        /// Gets the number of items in the storage object.
        /// </summary>
        public int Count => dictionary.Count;

        /// <summary>
        /// Gets a collection containing the keys in the storage object.
        /// </summary>
        public Dictionary<Key, Value>.KeyCollection Keys => dictionary.Keys;

        /// <summary>
        /// Gets a collection containing the values in the storage object.
        /// </summary>
        public Dictionary<Key, Value>.ValueCollection Values => dictionary.Values;

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key"> The key of the item to get or set. </param>
        public Value this[Key key]
        {
            get
            {
                if (!dictionary.TryGetValue(key, out Value value))
                {
                    throw new StorageException(StorageErrorType.KeyNotFound,
                    $"No item with the key '{key}' exists in the storage {ObjectId}.\n" +
                    $"You can use {nameof(ContainsKey)} to check whether a storage object contains key or not, before attempting to retrieve the value associated with it.\n" +
                    $"You can also use {nameof(TryGetValue)} to safely get a value if one exists, without an exception being thrown, if it doesn't.");
                }

                return value;
            }

            set => dictionary[key] = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageObject"/> class.
        /// </summary>
        /// <param name="objectId"> The identifier for the storage object. </param>
        public StorageObject(StorageObjectId objectId) => ObjectId = objectId;

        public StorageObject(StorageObjectId objectId, object @object, Type objectType)
        {
            ObjectId = objectId;
            this.@object = @object;
            this.objectType = objectType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageObject"/> class.
        /// </summary>
        /// <param name="objectId"> The identifier for the storage object. </param>
        /// <param name="items"> The items the storage object contains. </param>
        public StorageObject(StorageObjectId objectId, IEnumerable<StorageItem> items) : this(objectId)
        {
            foreach (var item in items ?? Enumerable.Empty<StorageItem>())
            {
                dictionary.Add(item.Key, item.Value);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageObject"/> class.
        /// </summary>
        /// <param name="objectId"> The identifier for the storage object. </param>
        /// <param name="items"> The items the storage object contains. </param>
        public StorageObject(StorageObjectId objectId, params StorageItem[] items) : this(objectId)
        {
            foreach (var item in items ?? Array.Empty<StorageItem>())
            {
                dictionary.Add(item.Key, item.Value);
            }
        }

        internal static bool From<TObject>(StorageObjectId objectId, StorageObjectMutationType mutationType, TObject @object, [MaybeNullWhen(false), NotNullWhen(true)] out StorageObject storageObject, [MaybeNullWhen(true), NotNullWhen(false)] out StorageException exception)
        {
            var objectType = @object?.GetType() ?? typeof(TObject);
            storageObject = new(objectId, @object, objectType);
            if (mutationType is StorageObjectMutationType.Full)
            {
                exception = null;
                return true;
            }

            if (@object is Array array)
            {
                for (int i = 0, count = array.Length; i < count; i++)
                {
                    storageObject[i.ToString()] = new(array.GetValue(i));
                }
            }
            else if (@object is IDictionary dictionary && objectType.IsGenericType && objectType.GetGenericArguments().Length is 2)
            {
                var enumerator = dictionary.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var entry = enumerator.Entry;
                    string key;
                    try
                    {
                        key = entry.Key as string ?? CoherenceJson.SerializeObject(entry.Key, jsonConverters);
                    }
                    catch (Exception ex)
                    {
                        exception = new(StorageErrorType.SerializationFailed, $"{nameof(CloudStorage)}.{nameof(CloudStorage.SaveObjectAsync)} failed to serialize dictionary key '{entry.Key}' to JSON.\n{ex}");
                        return false;
                    }

                    storageObject[key] = new(entry.Value);
                }

                if (enumerator is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            else if (@object is IList list && objectType.IsGenericType && objectType.GetGenericArguments().Length is 1)
            {
                for (int i = 0, count = list.Count; i < count; i++)
                {
                    storageObject[i.ToString()] = new(list[i]);
                }
            }

            exception = null;
            return true;
        }

        internal static bool To<TObject>(StorageObject storageObject, [MaybeNull] out TObject @object, [MaybeNullWhen(true), NotNullWhen(false)] out StorageException exception)
        {
            try
            {
                if (storageObject.TryGetObject(out TObject result))
                {
                    @object = result;
                    exception = null;
                    return true;
                }

                if (typeof(IDictionary).IsAssignableFrom(typeof(TObject)) && typeof(TObject).IsGenericType && typeof(TObject).GetGenericArguments() is { Length: 2} keyAndValueTypes)
                {
                    var keyType = keyAndValueTypes[0];
                    var valueType = keyAndValueTypes[1];
                    try
                    {
                        @object = Activator.CreateInstance<TObject>();
                    }
                    catch (Exception ex)
                    {
                        @object = default;
                        exception = new(StorageErrorType.DeserializationFailed, $"{nameof(CloudStorage)}.{nameof(CloudStorage.LoadObjectAsync)} failed to create object of type {ToString(typeof(TObject))}. Is it missing a default constructor?\n{ex}");
                        return false;
                    }

                    var dictionary = (IDictionary)@object;
                    foreach (var item in storageObject)
                    {
                        object key;
                        try
                        {
                            key = keyType == typeof(string) ? item.Key.Content : CoherenceJson.DeserializeObject(item.Key, keyType, jsonConverters);
                        }
                        catch (Exception ex)
                        {
                            exception = new(StorageErrorType.DeserializationFailed, $"{nameof(CloudStorage)}.{nameof(CloudStorage.LoadObjectAsync)} failed to deserialize dictionary key '{item.Key}' to type {ToString(keyType)}.\n{ex}");
                            return false;
                        }

                        object value;
                        try
                        {
                            value = valueType == typeof(string) ? item.Value.Json : CoherenceJson.DeserializeObject(item.Value.Json, valueType, jsonConverters);
                        }
                        catch (Exception ex)
                        {
                            exception = new(StorageErrorType.DeserializationFailed, $"{nameof(CloudStorage)}.{nameof(CloudStorage.LoadObjectAsync)} failed to deserialize dictionary value '{item.Value.Json}' to type {ToString(valueType)}.\n{ex}");
                            return false;
                        }

                        dictionary.Add(key, value);
                    }

                    exception = null;
                    return true;
                }

                if (typeof(TObject).IsArray)
                {
                    var elementType = typeof(TObject).GetElementType();
                    var array = Array.CreateInstance(elementType, storageObject.Count);
                    var index = 0;
                    foreach (var item in storageObject.OrderBy(item => item.Key.Content))
                    {
                        object elementValue;
                        try
                        {
                            elementValue = elementType == typeof(string) ? item.Value.Json : CoherenceJson.DeserializeObject(item.Value.Json, elementType, jsonConverters);
                        }
                        catch (Exception ex)
                        {
                            exception = new(StorageErrorType.DeserializationFailed, $"{nameof(CloudStorage)}.{nameof(CloudStorage.LoadObjectAsync)} failed to deserialize array element '{item.Value.Json}' to type {ToString(elementType)}.\n{ex}");
                            @object = default;
                            return false;
                        }

                        array.SetValue(elementValue, index);
                        index++;
                    }

                    @object = (TObject)(object)array;
                    exception = null;
                    return true;
                }

                if (typeof(IList).IsAssignableFrom(typeof(TObject)) && typeof(TObject).IsGenericType && typeof(TObject).GetGenericArguments() is { Length: 1 } genericArguments)
                {
                    var memberType = genericArguments[0];
                    try
                    {
                        @object = Activator.CreateInstance<TObject>();
                    }
                    catch (Exception ex)
                    {
                        exception = new(StorageErrorType.DeserializationFailed, $"{nameof(CloudStorage)}.{nameof(CloudStorage.LoadObjectAsync)} failed to create object of type {ToString(typeof(TObject))}. Is it missing a default constructor?\n{ex}");
                        @object = default;
                        return false;
                    }

                    var list = (IList)@object;
                    foreach (var item in storageObject.OrderBy(item => item.Key.Content))
                    {
                        object memberValue;
                        try
                        {
                            memberValue = memberType == typeof(string) ? item.Value.Json : CoherenceJson.DeserializeObject(item.Value.Json, memberType, jsonConverters);
                        }
                        catch (Exception ex)
                        {
                            exception = new(StorageErrorType.DeserializationFailed, $"{nameof(CloudStorage)}.{nameof(CloudStorage.LoadObjectAsync)} failed to deserialize list element '{item.Value.Json}' to type {ToString(memberType)}.\n{ex}");
                            return false;
                        }

                        list.Add(memberValue);
                    }

                    exception = null;
                    return true;
                }

                exception = new(StorageErrorType.DeserializationFailed, $"Failed to convert loaded data to type {ToString(typeof(TObject))}.");
                @object = default;
                return false;
            }
            catch (Exception ex)
            {
                exception = new(StorageErrorType.DeserializationFailed, $"Failed to convert loaded data to type {ToString(typeof(TObject))}.\n{ex}");
                @object = default;
                return false;
            }
        }

        /// <summary>
        /// Determines whether the storage object contains the specified key.
        /// </summary>
        /// <param name="key"> The key to locate in the storage object. </param>
        /// <returns>
        /// <see langword="true"/> if the storage object contains an item with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool ContainsKey(Key key) => dictionary.ContainsKey(key);

        /// <summary>
        /// Determines whether the storage object contains the specified item.
        /// </summary>
        /// <param name="item"> The item to locate in the storage object. </param>
        /// <returns>
        /// <see langword="true"/> if the storage object contains an item with the specified key, and the value
        /// of the item matches the specified value; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Contains(StorageItem item) => dictionary.TryGetValue(item.Key, out var found) && found.Equals(item.Value);

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
        /// <see langword="true"/> if the storage object contains an element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out Value value) => dictionary.TryGetValue(key, out value);

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
        /// <see langword="true"/> if the storage object contains a <see langword="bool"/> element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out bool value)
        {
            if (dictionary.TryGetValue(key, out var found) && found.As(out value))
            {
                return true;
            }

            value = default;
            return false;
        }

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
        /// <see langword="true"/> if the storage object contains an <see langword="int"/> element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out int value)
        {
            if (dictionary.TryGetValue(key, out var found) && found.As(out value))
            {
                return true;
            }

            value = default;
            return false;
        }

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
        /// <see langword="true"/> if the storage object contains an element of type <see typeparamref="TValue"/> with the
        /// specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue<TValue>(Key key, out TValue value)
        {
            if (dictionary.TryGetValue(key, out var found) && found.As(out value))
            {
                return true;
            }

            value = default;
            return false;
        }

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
        /// <see langword="true"/> if the storage object contains a <see langword="string"/> element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out string value)
        {
            if (dictionary.TryGetValue(key, out var found) && found.As(out value))
            {
                return true;
            }

            value = default;
            return false;
        }

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
        /// <see langword="true"/> if the storage object contains a <see langword="float"/> element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out float value)
        {
            if (dictionary.TryGetValue(key, out var found) && found.As(out value))
            {
                return true;
            }

            value = default;
            return false;
        }

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
        /// <see langword="true"/> if the storage object contains a <see langword="double"/> element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out double value)
        {
            if (dictionary.TryGetValue(key, out var found) && found.As(out value))
            {
                return true;
            }

            value = default;
            return false;
        }

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
        /// <see langword="true"/> if the storage object contains a <see langword="short"/> element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out short value)
        {
            if (dictionary.TryGetValue(key, out var found) && found.As(out value))
            {
                return true;
            }

            value = default;
            return false;
        }

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
        /// <see langword="true"/> if the storage object contains a <see langword="byte"/> element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out byte value)
        {
            if (dictionary.TryGetValue(key, out var found) && found.As(out value))
            {
                return true;
            }

            value = default;
            return false;
        }

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
        /// <see langword="true"/> if the storage object contains a <see langword="Enum"/> element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetValue(Key key, out Enum value)
        {
            if (dictionary.TryGetValue(key, out var found) && found.As(out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Removes all items from the storage object.
        /// </summary>
        public void Clear() => dictionary.Clear();

        /// <summary>
        /// Removes the value associated with the specified key from the storage object.
        /// </summary>
        /// <param name="key"> The key of the value to remove from the storage object. </param>
        /// <returns>
        /// <see langword="true"/> if the storage object contained an element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Remove(Key key) => dictionary.Remove(key);

        /// <summary>
        /// Removes the specified item from the storage object.
        /// </summary>
        /// <param name="item"> The item to remove from the storage object. </param>
        /// <see langword="true"/> if the storage object contained an item with the specified key, and the value
        /// <returns>
        /// of the item matched the specified value; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Remove(StorageItem item)
        {
            if (!dictionary.TryGetValue(item.Key, out var currentValue) || !item.Value.Equals(currentValue))
            {
                return false;
            }

            dictionary.Remove(item.Key);
            return true;
        }

        /// <summary>
        /// Removes items with the specified items from the storage object.
        /// </summary>
        /// <param name="keys"> The keys of items to remove from the storage object. </param>
        public int RemoveItems([DisallowNull] IEnumerable<Key> keys)
        {
            var count = 0;

            foreach (var key in keys)
            {
                count += Remove(key) ? 1 : 0;
            }

            return count;
        }

        /// <summary>
        /// Removes the specified items from the storage object.
        /// <para>
        /// Both the key and value of an item must match for it to be removed.
        /// </para>
        /// </summary>
        /// <param name="items"> The items to remove from the storage object. </param>
        public int RemoveItems([DisallowNull] IEnumerable<StorageItem> items)
        {
            var count = 0;

            foreach (var item in items)
            {
                count += Contains(item) && Remove(item.Key) ? 1 : 0;
            }

            return count;
        }

        /// <summary>
        /// Removes items with the specified items from the storage object.
        /// </summary>
        /// <param name="keys"> The keys of items to remove from the storage object. </param>
        public int RemoveItems([DisallowNull] params Key[] keys)
        {
            var count = 0;

            foreach (var key in keys)
            {
                count += Remove(key) ? 1 : 0;
            }

            return count;
        }

        /// <summary>
        /// Removes the specified items from the storage object.
        /// <para>
        /// Both the key and value of an item must match for it to be removed.
        /// </para>
        /// </summary>
        /// <param name="items"> The items to remove from the storage object. </param>
        public int RemoveItems([DisallowNull] params StorageItem[] items)
        {
            var count = 0;

            foreach (var item in items)
            {
                if (Contains(item))
                {
                    count += Remove(item.Key) ? 1 : 0;
                }
            }

            return count;
        }

        /// <summary>
        /// Sets the values associated with the specified keys.
        /// <para>
        /// Any existing values in the storage object associated with the specified keys will be replaced.
        /// </para>
        /// </summary>
        /// <param name="items"> The items to set. </param>
        public void SetItems([DisallowNull] IEnumerable<StorageItem> items)
        {
            foreach (var item in items)
            {
                this[item.Key] = item.Value;
            }
        }

        /// <summary>
        /// Sets the values associated with the specified keys.
        /// <para>
        /// Any existing values in the storage object associated with the specified keys will be replaced.
        /// </para>
        /// </summary>
        /// <param name="items"> The items to set. </param>
        public void SetItems([DisallowNull] IEnumerable<KeyValuePair<Key, Value>> items)
        {
            foreach (var item in items)
            {
                this[item.Key] = item.Value;
            }
        }

        /// <summary>
        /// Sets the values associated with the specified keys.
        /// <para>
        /// Any existing values in the storage object associated with the specified keys will be replaced.
        /// </para>
        /// </summary>
        /// <param name="items"> The items to set. </param>
        public void SetItems([DisallowNull] IEnumerable<KeyValuePair<string, string>> items)
        {
            foreach (var item in items)
            {
                this[item.Key] = item.Value;
            }
        }

        /// <summary>
        /// Sets the values associated with the specified keys.
        /// <para>
        /// Any existing values in the storage object associated with the specified keys will be replaced.
        /// </para>
        /// </summary>
        /// <param name="items"> The items to set. </param>
        public void SetItems([DisallowNull] params StorageItem[] items)
        {
            foreach (var item in items)
            {
                this[item.Key] = item.Value;
            }
        }

        /// <summary>
        /// Sets the value associated with the specified key.
        /// <para>
        /// Any existing values in the storage object associated with the specified key will be replaced.
        /// </para>
        /// </summary>
        /// <param name="item"> The item to set. </param>
        public void Set(StorageItem item) => this[item.Key] = item.Value;

        /// <summary>
        /// Sets the value associated with the specified key.
        /// <para>
        /// Any existing values in the storage object associated with the specified key will be replaced.
        /// </para>
        /// </summary>
        /// <param name="key"> The key to associate the value with. </param>
        /// <param name="value"> The value to associate with they key. </param>
        public void Set(Key key, Value value) => this[key] = value;

        IEnumerator<StorageItem> IEnumerable<StorageItem>.GetEnumerator()
        {
            foreach (var item in dictionary)
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var item in dictionary)
            {
                yield return new StorageItem(item.Key, item.Value);
            }
        }

        bool ICollection<StorageItem>.IsReadOnly => false;
        void ICollection<StorageItem>.Add(StorageItem item) => dictionary.Add(item.Key, item.Value);
        void ICollection<StorageItem>.CopyTo(StorageItem[] array, int arrayIndex) => (new List<StorageItem>(dictionary.Select(x => new StorageItem(x.Key, x.Value)))).CopyTo(array, arrayIndex);

        private static string ToString(Type type) => type.ToStringWithGenericArguments();
    }
}
