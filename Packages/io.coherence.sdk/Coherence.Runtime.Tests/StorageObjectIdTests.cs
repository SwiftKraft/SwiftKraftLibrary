// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Runtime.Tests
{
    using System;
    using Cloud;
    using Coherence.Tests;
    using NUnit.Framework;

    public sealed class StorageObjectIdTests : CoherenceTest
    {
        [Test]
        public void String_String_Constructor_Throws_If_Type_Is_Null()
            => Assert.That(() => new StorageObjectId(default(string), "id"), Throws.TypeOf<StorageException>());

        [Test]
        public void IFormattable_String_Constructor_Throws_If_Type_Is_Null()
            => Assert.That(() => new StorageObjectId(default(IFormattable), "id"), Throws.TypeOf<StorageException>());

        [Test]
        public void IFormattable_IFormattable_Constructor_Throws_If_Type_Is_Null()
            => Assert.That(() => new StorageObjectId(default(IFormattable), Formattable.Id), Throws.TypeOf<StorageException>());

        [Test]
        public void String_String_Constructor_Throws_If_Type_Is_Empty()
            => Assert.That(() => new StorageObjectId("", "id"), Throws.TypeOf<StorageException>());

        [Test]
        public void IFormattable_String_Constructor_Throws_If_Type_ToString_Result_Is_Empty()
            => Assert.That(() => new StorageObjectId(new EmptyFormattable(), "id"), Throws.TypeOf<StorageException>());

        [Test]
        public void IFormattable_IFormattable_Constructor_Throws_If_Type_ToString_Result_Is_Empty()
            => Assert.That(() => new StorageObjectId(new EmptyFormattable(), Formattable.Id), Throws.TypeOf<StorageException>());

        [Test]
        public void String_String_Constructor_Throws_If_Id_Is_Null()
            => Assert.That(() => new StorageObjectId("type", default(string)), Throws.TypeOf<StorageException>());

        [Test]
        public void IFormattable_String_Constructor_Throws_If_Id_Is_Null()
            => Assert.That(() => new StorageObjectId(Formattable.Type, default(string)), Throws.TypeOf<StorageException>());

        [Test]
        public void IFormattable_IFormattable_Constructor_Throws_If_Id_Is_Null()
            => Assert.That(() => new StorageObjectId(Formattable.Type, default(IFormattable)), Throws.TypeOf<StorageException>());

        [Test]
        public void String_String_Constructor_Throws_If_Id_Is_Empty()
            => Assert.That(() => new StorageObjectId("type", ""), Throws.TypeOf<StorageException>());

        [Test]
        public void IFormattable_String_Constructor_Throws_If_Id_ToString_Result_Is_Empty()
            => Assert.That(() => new StorageObjectId(Formattable.Type, new EmptyFormattable()), Throws.TypeOf<StorageException>());

        private enum Formattable { Type, Id }

        private class EmptyFormattable : IFormattable
        {
            public string ToString(string format, IFormatProvider formatProvider) => "";
        }
    }
}
