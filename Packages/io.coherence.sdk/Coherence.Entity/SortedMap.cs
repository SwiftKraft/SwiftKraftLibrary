// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Entities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class SortedValueMap<K, V> : IDictionary<K, V>
    {
        private readonly Dictionary<K, V> dictionary;
        private readonly List<V> sortedValues;
        private readonly IComparer<V> comparer;
        private bool isSorted;

        public SortedValueMap(IComparer<V> comparer)
        {
            this.comparer = comparer;
            dictionary = new Dictionary<K, V>();
            sortedValues = new List<V>();
            isSorted = true;
        }

        public SortedValueMap(IComparer<V> comparer, IDictionary<K, V> data)
        {
            this.comparer = comparer;
            this.dictionary = new Dictionary<K, V>(data);
            sortedValues = new List<V>(dictionary.Count);
            isSorted = false;
        }

        public SortedValueMap(IComparer<V> comparer, int capacity)
        {
            this.comparer = comparer;
            dictionary = new Dictionary<K, V>(capacity);
            sortedValues = new List<V>(capacity);
            isSorted = true;
        }

        /// <summary>
        /// Get enumerator for unsorted KeyValuePair's
        /// </summary>
        /// <returns></returns>
        public Dictionary<K, V>.Enumerator GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        /// <summary>
        /// Get enumerator for unsorted KeyValuePair's
        /// </summary>
        /// <returns></returns>
        IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        /// <summary>
        /// Get enumerator for unsorted KeyValuePair's
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        public void Add(KeyValuePair<K, V> item)
        {
            dictionary.Add(item.Key, item.Value);
            isSorted = false;
        }

        public void Clear()
        {
            dictionary.Clear();
            sortedValues.Clear();
            isSorted = true;
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            sortedValues.Remove(item.Value);
            return dictionary.Remove(item.Key);
        }

        public int Count => dictionary.Count;
        public bool IsReadOnly => false;

        public void Add(K key, V value)
        {
            dictionary.Add(key, value);
            isSorted = false;
        }

        public bool ContainsKey(K key)
        {
            return dictionary.ContainsKey(key);
        }

        public bool Remove(K key)
        {
            if (!dictionary.TryGetValue(key, out var v))
            {
                return false;
            }

            dictionary.Remove(key);
            sortedValues.Remove(v);
            return true;
        }

        public bool TryGetValue(K key, out V value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        public V this[K key]
        {
            get => dictionary[key];
            set
            {
                dictionary[key] = value;
                isSorted = false;
            }
        }

        public ICollection<K> Keys { get => dictionary.Keys; }

        ICollection<V> IDictionary<K, V>.Values => dictionary.Values;

        public IReadOnlyList<V> SortedValues
        {
            get
            {
                if (!isSorted)
                {
                    sortedValues.Clear();
                    sortedValues.AddRange(dictionary.Values);
                    sortedValues.Sort(comparer);
                    isSorted = true;
                }

                return sortedValues;
            }
        }
    }
}
