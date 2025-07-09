// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Cloud;
    using Coherence.Tests;
    using NUnit.Framework;
    using UnityEngine;

    /// <summary>
    /// Unit tests for converting various types of objects to <see cref="StorageObject"/> and back.
    /// </summary>
    public sealed class StorageObjectConversionTests : CoherenceTest
    {
        private static StorageObjectId ObjectId => ("type", "id");

        [Test]
        public void StorageObject_To_And_From_Array_Works()
        {
            TestStorageObject(new[] { 1, 2, 3 });
            TestStorageObject(Array.Empty<int>());
            TestStorageObject(default(int[]));
        }

        [Test]
        public void StorageObject_To_And_From_List_Works()
        {
            TestStorageObject(new List<int> { 1, 2, 3 });
            TestStorageObject(new List<int>(0));
            TestStorageObject(default(List<int>));
        }

        [Test]
        public void StorageObject_To_And_From_Dictionary_Works()
        {
            TestStorageObject(new Dictionary<string, int> { { "A", 1 }, { "B", 2 }, { "C", 3 } });
            TestStorageObject(new Dictionary<string, int>(0));
            TestStorageObject(default(Dictionary<string, int>));
        }

        [Test]
        public void StorageObject_To_And_From_Bool_Works()
        {
            TestStorageObject(true);
            TestStorageObject(false);
        }

        [Test]
        public void StorageObject_To_And_From_Int_Works()
        {
            TestStorageObject(0);
            TestStorageObject(1);
            TestStorageObject(int.MaxValue);
            TestStorageObject(int.MinValue);
        }

        [Test]
        public void StorageObject_To_And_From_Uint_Works()
        {
            TestStorageObject<uint>(0);
            TestStorageObject<uint>(1);
            TestStorageObject(uint.MaxValue);
        }

        [Test]
        public void StorageObject_To_And_From_Byte_Works()
        {
            TestStorageObject<byte>(0);
            TestStorageObject<byte>(1);
            TestStorageObject(byte.MaxValue);
        }

        [Test]
        public void StorageObject_To_And_From_Char_Works()
        {
            TestStorageObject('\0');
            TestStorageObject('\n');
            TestStorageObject('\r');
            TestStorageObject('A');
            TestStorageObject('z');
            TestStorageObject('0');
        }

        [Test]
        public void StorageObject_To_And_From_Short_Works()
        {
            TestStorageObject<short>(0);
            TestStorageObject<short>(1);
            TestStorageObject(short.MinValue);
            TestStorageObject(short.MaxValue);
        }

        [Test]
        public void StorageObject_To_And_From_Ushort_Works()
        {
            TestStorageObject<ushort>(0);
            TestStorageObject<ushort>(1);
            TestStorageObject(ushort.MaxValue);
        }

        [Test]
        public void StorageObject_To_And_From_Float_Works()
        {
            TestStorageObject(0f);
            TestStorageObject(1f);
            TestStorageObject(Mathf.Epsilon);
            TestStorageObject(float.MinValue);
            TestStorageObject(float.MaxValue);
            TestStorageObject(float.NaN);
            TestStorageObject(float.PositiveInfinity);
            TestStorageObject(float.NegativeInfinity);
        }

        [Test]
        public void StorageObject_To_And_From_String_Works()
        {
            TestStorageObject("Test");
            TestStorageObject("");
            TestStorageObject(default(string));
        }

        [Test]
        public void StorageObject_To_And_From_Vector2_Works()
        {
            TestStorageObject(Vector2.zero);
            TestStorageObject(new Vector2(1, 2));
        }

        [Test]
        public void StorageObject_To_And_From_Vector3_Works()
        {
            TestStorageObject(Vector3.zero);
            TestStorageObject(new Vector3(1f, 2f, 3f));
        }

        [Test]
        public void StorageObject_To_And_From_Quaternion_Works()
        {
            TestStorageObject(Quaternion.identity);
            TestStorageObject(new Quaternion(1f, 2f, 3f, 4f));
        }

        [Test]
        public void StorageObject_To_And_From_ByteArray_Works()
        {
            TestStorageObject(new byte[] { 1, 2, 3 });
            TestStorageObject(Array.Empty<byte>()); // Test empty byte array
            TestStorageObject(default(byte[])); //Test null byte array
        }

        [Test]
        public void StorageObject_To_And_From_Long_Works()
        {
            TestStorageObject(0L);
            TestStorageObject(1L);
            TestStorageObject(long.MinValue);
            TestStorageObject(long.MaxValue);
        }

        [Test]
        public void StorageObject_To_And_From_Ulong_Works()
        {
            TestStorageObject(0UL);
            TestStorageObject(1UL);
            TestStorageObject(ulong.MaxValue);
        }

        [Test]
        public void StorageObject_To_And_From_Int64_Works()
        {
            TestStorageObject(0L);
            TestStorageObject(1L);
            TestStorageObject(Int64.MinValue);
            TestStorageObject(Int64.MaxValue);

        }

        [Test]
        public void StorageObject_To_And_From_UInt64_Works()
        {
            TestStorageObject(0UL);
            TestStorageObject(1UL);
            TestStorageObject(UInt64.MaxValue);
        }

        [Test]
        public void StorageObject_To_And_From_Color_Works()
        {
            TestStorageObject(default(Color));
            TestStorageObject(new Color(0.1f, 0.2f, 0.3f, 0.4f));
        }

        [Test]
        public void StorageObject_To_And_From_Double_Works()
        {
            TestStorageObject(0d);
            TestStorageObject(0.1d);
            TestStorageObject(1d);
            TestStorageObject(double.MaxValue);
            TestStorageObject(double.MinValue);
            TestStorageObject(double.NaN);
            TestStorageObject(double.PositiveInfinity);
            TestStorageObject(double.NegativeInfinity);
        }

        private void TestStorageObject<T>(T input)
        {
            var fromWasSuccessful = StorageObject.From(ObjectId, StorageObjectMutationType.Full, input, out var storageObject, out var fromException);
            var toWasSuccessful = StorageObject.To(storageObject, out T output, out var toException);

            Assert.That(fromWasSuccessful, Is.True, fromException?.Message);
            Assert.That(toWasSuccessful, Is.True, toException?.Message);

            if (input is IEnumerable enumerable)
            {
                Assert.That(output, Is.EquivalentTo(enumerable));
            }
            else
            {
                Assert.That(output, Is.EqualTo(input));
            }
        }
    }
}
