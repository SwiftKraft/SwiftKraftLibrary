// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System;
    using Cloud;
    using Coherence.Tests;
    using Newtonsoft.Json;
    using NUnit.Framework;

    /// <summary>
    /// Tests for <see cref="Value"/>.
    /// </summary>
    public sealed class StorageObjectValueTests : CoherenceTest
    {
        [Test]
        public void String_Constructor_Does_Not_Throw_If_String_Is_Null()
            => _ = new Value(default(string));

        [Test]
        public void Object_Constructor_Does_Not_Throw_If_Object_Is_Null()
            => _ = new Value(default(object));

        [Test]
        public void Value_Int_As_Int_Works()
            => Assert.That(new Value(1).As<int>(), Is.EqualTo(1));

        [Test]
        public void Value_Int_As_Out_Int_Returns_True()
            => Assert.That(new Value(1).As<int>(out _), Is.True);

        [Test]
        public void Value_FromJson_As_Int_Works()
            => Assert.That(Value.FromJson("1").As<int>(), Is.EqualTo(1));

        [Test]
        public void Value_FromJson_As_Out_Int_Returns_True()
            => Assert.That(Value.FromJson("1").As<int>(out _), Is.True);

        [Test]
        public void Value_String_As_String_Works()
            => Assert.That(new Value("Expected").As<string>(), Is.EqualTo("Expected"));

        [Test]
        public void Value_Empty_String_As_String_Returns_Empty_String()
            => Assert.That(new Value("").As<string>(), Is.Empty);

        [Test]
        public void Value_Null_Object_As_Object_Returns_Null()
            => Assert.That(new Value(default(object)).As<object>(), Is.Null);

        [Test]
        public void Value_Empty_String_As_Object_Returns_Empty_String()
            => Assert.That(new Value("").As<object>(), Is.EqualTo(""));

        [Test]
        public void Value_Empty_String_As_CustomClass_Throws()
            => Assert.That(()=> new Value("").As<CustomClass>(), Throws.TypeOf<StorageException>());

        [Test]
        public void Value_Null_String_As_Object_Returns_Null()
            => Assert.That(new Value(default(string)).As<object>(), Is.Null);

        [Test]
        public void Value_String_As_Out_String_Returns_True()
            => Assert.That(new Value("").As<string>(out _), Is.True);

        [Test]
        public void Value_CustomStruct_As_CustomStruct_Works()
            => Assert.That(new Value(new CustomStruct(1)).As<CustomStruct>(), Is.EqualTo(new CustomStruct(1)));

        [Test]
        public void Value_CustomStruct_As_Out_CustomStruct_Returns_True()
            => Assert.That(new Value(new CustomStruct(1)).As<CustomStruct>(out _), Is.True);

        [Test]
        public void Value_String_As_CustomStruct_Throws()
            => Assert.That(()=> new Value(new Value(new CustomStruct(1)).ToString()).As<CustomStruct>(), Throws.TypeOf<StorageException>());

        [Test]
        public void Value_FromJson_For_Custom_Struct_Works()
            => Assert.That(Value.FromJson(new Value(new CustomStruct(1)).ToString()).As<CustomStruct>(), Is.EqualTo(new CustomStruct(1)));

        [Test]
        public void Value_FromJson_As_Out_CustomStruct_Returns_True()
            => Assert.That(Value.FromJson(new Value(new CustomStruct(1)).ToString()).As<CustomStruct>(out _), Is.True);

        [Test]
        public void Value_CustomStruct_As_String_Throws()
            => Assert.That(()=> new Value(new CustomStruct(1)).As<string>(), Throws.TypeOf<StorageException>());

        [Test]
        public void Value_CustomStruct_As_Out_String_Returns_False()
            => Assert.That(new Value(new CustomStruct(1)).As<string>(out _), Is.False);

        [Test]
        public void Value_CustomStruct_As_Bool_Throws()
            => Assert.That(() => new Value(new CustomStruct(1)).As<bool>(), Throws.TypeOf<StorageException>());

        [Test]
        public void Value_CustomStruct_Is_Bool_Returns_False()
            => Assert.That(new Value(new CustomStruct(1)).As<bool>(out _), Is.False);

        [Serializable]
        private struct CustomStruct
        {
            [JsonProperty("Value")]
            public int Value;

            public CustomStruct(int value) => Value = value;

            public override string ToString() => $"CustomStruct({Value})";
            public override bool Equals(object obj) => obj is CustomStruct other && Value == other.Value;
            public override int GetHashCode() => Value;
        }

        [Serializable]
        private sealed class CustomClass { }
    }
}
