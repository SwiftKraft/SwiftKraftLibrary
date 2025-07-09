// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Runtime.Tests
{
    using System;
    using Cloud;
    using Coherence.Tests;
    using NUnit.Framework;
    using UnityEngine;

    /// <summary>
    /// Unit tests for <see cref="StorageObjectMutation"/>.
    /// </summary>
    public sealed class StorageObjectMutationTests : CoherenceTest
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
        private static Value OldValue=> "OldValue";
        private static Value OldValue1 => "OldValue1";
        private static Value OldValue2 => "OldValue2";
        private static Value OldValue3 => "OldValue3";
        private static Value NewValue => "OldValue";
        private static Value NewValue1 => "OldValue1";
        private static Value NewValue2 => "OldValue2";
        private static Value NewValue3 => "OldValue3";

        private StorageObjectMutation FullMutation => new(new StorageObjectId("Test", "Storage"), StorageObjectMutationType.Full);

        [Test]
        public void Count_ReturnsTheNumberOfItemsInTheStorageObject()
        {
            var mutation = FullMutation;
            Assert.That(mutation.Count, Is.Zero);

            mutation.SetItems(Item1, Item2, Item3);

            Assert.That(mutation.Count, Is.EqualTo(3));
        }

        [Test]
        public void Keys_ReturnsTheKeysOfTheStorageObject()
        {
            var mutation = FullMutation;
            mutation.SetItems(Item1, Item2, Item3);

            var keys = mutation.Keys;

            Assert.That(keys, Is.EquivalentTo(new[] { Key1, Key2, Key3 }));
        }

        [Test]
        public void Values_ReturnsTheValuesOfTheStorageObject()
        {
            var mutation = FullMutation;
            mutation.SetItems(Item1, Item2, Item3);

            var result = mutation.Values;

            Assert.That(result, Is.EquivalentTo(new[] { Value1, Value2, Value3 }));
        }

        [Test]
        public void Indexer_Get_ReturnsTheValueAssociatedWithTheSpecifiedKey()
        {
            var mutation = FullMutation;
            mutation[Key] = Value;

            var value = mutation[Key];

            Assert.That(value, Is.EqualTo(Value));
        }

        [Test]
        public void Indexer_Set_SetsTheValueAssociatedWithTheSpecifiedKey()
        {
            var mutation = FullMutation;
            mutation.Set(Key, OldValue);

            mutation[Key] = NewValue;

            Assert.That(mutation[Key], Is.EqualTo(NewValue));
        }

        [Test]
        public void Set_SetsTheValueAssociatedWithTheSpecifiedKey()
        {
            var mutation = FullMutation;
            mutation.Set(Key, OldValue);

            mutation.Set(Key, NewValue);

            Assert.That(mutation[Key], Is.EqualTo(NewValue));
        }

        [Test]
        public void Clear_RemovesAllItems()
        {
            var mutation = FullMutation;
            mutation.SetItems(Item1, Item2, Item3);

            mutation.Clear();

            Assert.That(mutation.Count, Is.EqualTo(0));
        }

        [Test]
        public void Clear_DoesNothing_IfStorageObjectIsAlreadyEmpty()
        {
            var mutation = FullMutation;
            mutation.Clear();

            Assert.That(mutation.Count, Is.EqualTo(0));
        }

        [Test]
        public void ContainsKey_ReturnsTrue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var mutation = FullMutation;
            mutation[Key] = Value;

            var result = mutation.ContainsKey(Key);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ContainsKey_ReturnsFalse_WhenTheStorageObjectDoesNotContainTheSpecifiedKey()
        {
            var mutation = FullMutation;
            var result = mutation.ContainsKey(Key);

            Assert.That(result, Is.False);
        }

        [Test]
        public void Contains_ReturnsTrue_WhenTheStorageObjectContainsTheSpecifiedItem()
        {
            var mutation = FullMutation;
            mutation.Set(Item);

            var result = mutation.Contains(Item);

            Assert.That(result, Is.True);
        }

        [Test]
        public void Contains_ReturnsFalse_WhenTheStorageObjectDoesNotContainTheSpecifiedItem()
        {
            var mutation = FullMutation;
            var result = mutation.Contains(Item);

            Assert.That(result, Is.False);
        }

        [Test]
        public void SetRange_AddsAllItems()
        {
            var mutation = FullMutation;
            mutation.SetItems(Item1, Item2, Item3);

            Assert.That(mutation.Count, Is.EqualTo(3));
            Assert.That(mutation[Key1], Is.EqualTo(Value1));
            Assert.That(mutation[Key2], Is.EqualTo(Value2));
            Assert.That(mutation[Key3], Is.EqualTo(Value3));
        }

        [Test]
        public void SetRange_OverridesExistingItemsWithTheSameKey()
        {
            var mutation = FullMutation;
            mutation.SetItems(new(Key1, OldValue1), new(Key2, OldValue2), new(Key3, OldValue3));

            mutation.SetItems(new(Key1, NewValue1), new(Key2, NewValue2), new(Key3, NewValue3));

            Assert.That(mutation.Count, Is.EqualTo(3));
            Assert.That(mutation[Key1], Is.EqualTo(NewValue1));
            Assert.That(mutation[Key2], Is.EqualTo(NewValue2));
            Assert.That(mutation[Key3], Is.EqualTo(NewValue3));
        }

        [Test]
        public void RemoveRange_Key_RemovesItemsWithSameKey()
        {
            var mutation = FullMutation;
            mutation.SetItems(Item1, Item2, Item3);

            var result = mutation.RemoveItems(Key1, "", Key3);

            Assert.That(result, Is.EqualTo(2));
            Assert.That(mutation.Count, Is.EqualTo(1));
            Assert.That(mutation.Contains(Item1), Is.False);
            Assert.That(mutation.Contains(Item2), Is.True);
            Assert.That(mutation.Contains(Item3), Is.False);
        }

        [Test]
        public void RemoveRange_Items_RemovesItemsWithSameKeyAndValue()
        {
            var mutation = FullMutation;
            mutation.SetItems(Item1, Item2, Item3);

            var result = mutation.RemoveItems(Item1, new("", Item2.Value), new(Item3.Key, ""));

            Assert.That(result, Is.EqualTo(1));
            Assert.That(mutation.Count, Is.EqualTo(2));
            Assert.That(mutation.Contains(Item1), Is.False);
            Assert.That(mutation.Contains(Item2), Is.True);
            Assert.That(mutation.Contains(Item3), Is.True);
        }

        [Test]
        public void Remove_RemovesTheItemWithTheSpecifiedKey()
        {
            var mutation = FullMutation;
            mutation[Key] = Value;

            mutation.Remove(Key);

            Assert.That(mutation.ContainsKey(Key), Is.False);
        }

        [Test]
        public void Remove_ReturnsTrue_WhenTheItemWithTheSpecifiedKeyIsRemoved()
        {
            var mutation = FullMutation;
            mutation[Key] = Value;

            var result = mutation.Remove(Key);

            Assert.That(result, Is.True);
        }

        [Test]
        public void Remove_ReturnsFalse_WhenTheItemWithTheSpecifiedKeyDoesNotExist()
        {
            var mutation = FullMutation;
            var result = mutation.Remove(Key);

            Assert.That(result, Is.False);
        }

        [Test]
        public void TryGetValue_ReturnsTrue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var mutation = FullMutation;
            mutation[Key] = Value;

            var result = mutation.TryGetValue(Key, out Value _);

            Assert.That(result, Is.True);
        }

        [Test]
        public void TryGetValue_ReturnsFalse_WhenTheStorageObjectDoesNotContainTheSpecifiedKey()
        {
            var mutation = FullMutation;
            var result = mutation.TryGetValue(Key, out Value _);

            Assert.That(result, Is.False);
        }

        [Test]
        public void TryGetValue_SetsValue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var mutation = FullMutation;
            mutation[Key] = Value;

            mutation.TryGetValue(Key, out Value value);

            Assert.That(value, Is.EqualTo(Value));
        }

        [Test]
        public void TryGetValue_SetsValueToNone_WhenTheStorageObjectDoesNotContainTheSpecifiedKey()
        {
            var mutation = FullMutation;
            mutation.TryGetValue(Key, out Value value);

            Assert.That(value, Is.EqualTo(Value.None));
        }

        [Test]
        public void TryGetValue_String_SetsValue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var mutation = FullMutation;
            mutation[Key] = "string";

            mutation.TryGetValue(Key, out string value);

            Assert.That(value, Is.EqualTo("string"));
        }

        [Test]
        public void TryGetValue_Int_SetsValue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var mutation = FullMutation;
            mutation[Key] = 1;

            mutation.TryGetValue(Key, out int value);

            Assert.That(value, Is.EqualTo(1));
        }

        [Test]
        public void TryGetValue_Float_SetsValue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var mutation = FullMutation;
            mutation[Key] = 1f;

            mutation.TryGetValue(Key, out float value);

            Assert.That(value, Is.EqualTo(1f));
        }

        [Test]
        public void TryGetValue_Double_SetsValue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var mutation = FullMutation;
            mutation[Key] = 1d;

            mutation.TryGetValue(Key, out double value);

            Assert.That(value, Is.EqualTo(1d));
        }

        [Test]
        public void TryGetValue_Enum_SetsValue_WhenTheStorageObjectContainsTheSpecifiedKey()
        {
            var mutation = FullMutation;
            mutation[Key] = KeyCode.A;

            mutation.TryGetValue(Key, out Enum value);

            Assert.That(value, Is.EqualTo(KeyCode.A));
        }
    }
}
