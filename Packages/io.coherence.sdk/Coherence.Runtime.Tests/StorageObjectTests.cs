// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Runtime.Tests
{
    using System;
    using System.Collections.Generic;
    using Cloud;
    using Coherence.Tests;
    using NUnit.Framework;
    using UnityEngine;

    /// <summary>
    /// Unit tests for <see cref="StorageObject"/>.
    /// </summary>
    public sealed class StorageObjectTests : CoherenceTest
    {
        private static StorageItem Item => new(Key, Value);
        private static StorageItem Item1 => new(Key1, Value1);
        private static StorageItem Item2 => new(Key2, Value2);
        private static StorageItem Item3 => new(Key3, Value3);
        private static Key Key => "Key";
        private static Key Key1 => "Key1";
        private static Key Key2 => "Key2";
        private static Key Key3 => "Key3";
        private static Value Value => "Value";
        private static Value Value1 => "Value1";
        private static Value Value2 => "Value2";
        private static Value Value3 => "Value3";

        [Test]
        public void Count_ReturnsTheNumberOfItemsInTheStorageObject()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"), Item1, Item2, Item3);

            var count = storageObject.Count;

            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public void Keys_ReturnsTheKeysOfTheStorageObject()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"), Item1, Item2, Item3);

            var keys = storageObject.Keys;

            Assert.That(keys, Is.EquivalentTo(new[] { Key1, Key2, Key3 }));
        }

        [Test]
        public void Values_ReturnsTheValuesOfTheStorageObject()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"), Item1, Item2, Item3);

            var result = storageObject.Values;

            Assert.That(result, Is.EquivalentTo(new[] { Value1, Value2, Value3 }));
        }

        [Test]
        public void GetEnumerator_ReturnsTheItemsOfTheStorageObject()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"), Item1, Item2, Item3);

            IEnumerable<StorageItem> enumerable = storageObject;

            Assert.That(enumerable, Is.EquivalentTo(new[] { Item1, Item2, Item3 }));
        }

        [Test]
        public void Indexer_Get_ReturnsTheValueAssociatedWithTheSpecifiedKey()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"), Item);

            var value = storageObject[Item.Key];

            Assert.That(value, Is.EqualTo(Item.Value));
        }

        [Test]
        public void ContainsKey_ReturnsTrue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"), Item);

            var contains = storageObject.ContainsKey(Item.Key);

            Assert.That(contains, Is.True);
        }

        [Test]
        public void ContainsKey_ReturnsFalse_WhenTheStorageObjectDoesNotContainTheSpecifiedKey()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"));

            var contains = storageObject.ContainsKey(Key);

            Assert.That(contains, Is.False);
        }

        [Test]
        public void Contains_ReturnsTrue_WhenTheStorageObjectContainsTheSpecifiedItem()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"), Item);

            var contains = storageObject.Contains(Item);

            Assert.That(contains, Is.True);
        }

        [Test]
        public void Contains_ReturnsFalse_WhenTheStorageObjectDoesNotContainTheSpecifiedItem()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"), Item);

            var contains = storageObject.Contains(new(Item.Key, "Not " + Item.Value));

            Assert.That(contains, Is.False);
        }

        [Test]
        public void TryGetValue_ReturnsTrue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"), Item);

            var result = storageObject.TryGetValue(Key, out Value _);

            Assert.That(result, Is.True);
        }

        [Test]
        public void TryGetValue_ReturnsFalse_WhenTheStorageObjectDoesNotContainTheSpecifiedKey()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"));

            var result = storageObject.TryGetValue(Item.Key, out Value _);

            Assert.That(result, Is.False);
        }

        [Test]
        public void TryGetValue_SetsValue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"), Item);

            _ = storageObject.TryGetValue(Key, out Value value);

            Assert.That(value, Is.EqualTo(Value));
        }

        [Test]
        public void TryGetValue_SetsValueToNone_WhenTheStorageObjectDoesNotContainTheSpecifiedKey()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"));

            _ = storageObject.TryGetValue(Key, out Value value);

            Assert.That(value, Is.EqualTo(Value.None));
        }

        [Test]
        public void TryGetValue_String_SetsValue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"), new StorageItem(Key, "string"));

            _ = storageObject.TryGetValue(Key, out string value);

            Assert.That(value, Is.EqualTo("string"));
        }

        [Test]
        public void TryGetValue_Int_SetsValue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"), new StorageItem(Key, 1));

            _ = storageObject.TryGetValue(Key, out int value);

            Assert.That(value, Is.EqualTo(1));
        }

        [Test]
        public void TryGetValue_Float_SetsValue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"), new StorageItem(Key, 1f));

            _ = storageObject.TryGetValue(Key, out float value);

            Assert.That(value, Is.EqualTo(1f));
        }

        [Test]
        public void TryGetValue_Double_SetsValue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"), new StorageItem(Key, 1d));

            _ = storageObject.TryGetValue(Key, out double value);

            Assert.That(value, Is.EqualTo(1d));
        }

        [Test]
        public void TryGetValue_Enum_SetsValue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"), new StorageItem(Key, KeyCode.A));

            _ = storageObject.TryGetValue(Key, out KeyCode value);

            Assert.That(value, Is.EqualTo(KeyCode.A));
        }

        [Test]
        public void TryGetValue_T_SetsValue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var serializedValue = new SerializableClass("SerializableClass");
            var storageObject = new StorageObject(new StorageObjectId("Test", "Storage"), new StorageItem(Key, serializedValue));

            _ = storageObject.TryGetValue(Key, out SerializableClass deserializedValue);

            Assert.That(deserializedValue, Is.EqualTo(serializedValue));
        }

        [Serializable]
        private record SerializableClass
        {
            [SerializeField]
            public string content;

            public SerializableClass(string content) => this.content = content;
        }
    }
}
