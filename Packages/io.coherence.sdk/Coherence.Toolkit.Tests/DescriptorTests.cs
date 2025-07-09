// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using Bindings;
    using Bindings.ValueBindings;
    using NUnit.Framework;
    using Coherence.Tests;

    public class DescriptorTests : CoherenceTest
    {
        private Descriptor descriptor1;
        private Descriptor descriptor2;
        private Descriptor descriptor1_1;
        private Descriptor descriptor2_1;

        private Descriptor nullDescriptor1 = null;
        private Descriptor nullDescriptor2 = null;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            descriptor1 = new Descriptor("Foo", typeof(FooType), typeof(BoolBinding));

            descriptor2 = new Descriptor("Bar", typeof(BarType), typeof(BoolBinding));

            descriptor1_1 = new Descriptor("Foo", typeof(FooType), typeof(BoolBinding));

            descriptor2_1 = new Descriptor("Bar", typeof(BarType), typeof(BoolBinding));
        }

        [Test]
        public void Should_ReturnTrue_When_ComparingEqualityAndBothDescriptorsAreNull()
        {
            Assert.IsTrue(nullDescriptor1 == nullDescriptor2);
        }

        [Test]
        public void Should_ReturnFalse_When_ComparingEqualityAndBothDescriptorsAreValidAndDifferent()
        {
            Assert.IsFalse(descriptor1 == descriptor2);
        }

        [Test]
        public void Should_ReturnFalse_When_ComparingEqualityAndOneDescriptorIsNull()
        {
            Assert.IsFalse(nullDescriptor1 == descriptor2);
        }

        [Test]
        public void Should_ReturnTrue_When_ComparingEqualityAndBothDescriptorsAreSameInstance()
        {
            var hashcode1 = descriptor1.GetHashCode();
            var hashcode2 = descriptor1.GetHashCode();

            Assert.IsTrue(hashcode1 == hashcode2);
        }

        [Test]
        public void Should_ReturnTrue_When_ComparingEqualityAndBothDescriptorsHaveSameHashCode()
        {
            var hashcode1 = descriptor1.GetHashCode();
            var hashcode2 = descriptor1_1.GetHashCode();

            Assert.IsTrue(hashcode1 == hashcode2);
            Assert.IsTrue(descriptor1 == descriptor1_1);
        }

        [Test]
        public void Should_ReturnFalse_When_ComparingNonEqualityAndBothDescriptorsHaveSameHashCode()
        {
            var hashcode1 = descriptor2.GetHashCode();
            var hashcode2 = descriptor2_1.GetHashCode();

            Assert.IsTrue(hashcode1 == hashcode2);
            Assert.IsFalse(descriptor2 != descriptor2_1);
        }
    }

    public class FooType
    {
    }

    public class BarType
    {
    }
}
