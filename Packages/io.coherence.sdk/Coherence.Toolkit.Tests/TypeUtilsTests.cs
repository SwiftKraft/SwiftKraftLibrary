// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using System.Reflection;
    using Toolkit;
    using NUnit.Framework;

    public class TypeUtilsTests
    {
        [Test]
        public void TypeUtils_GetFieldValue_ReturnsDefaultIfObjectIsNull()
        {
            var resultInt = TypeUtils.GetFieldValue<int>(null, "armour", BindingFlags.Instance | BindingFlags.NonPublic);
            var resultString = TypeUtils.GetFieldValue<string>(null, "name", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(resultInt, Is.Zero);
            Assert.That(resultString, Is.Null);
        }

        [Test]
        public void TypeUtils_GetFieldValue_ReturnsDefaultIfFieldNotFound()
        {
            var sample = new TypeUtilsSample();
            var resultInt = TypeUtils.GetFieldValue<int>(sample, "health", BindingFlags.Instance | BindingFlags.NonPublic);
            var resultString = TypeUtils.GetFieldValue<string>(sample, "nameOfCharacter", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(resultInt, Is.Zero);
            Assert.That(resultString, Is.Null);
        }

        [Test]
        public void TypeUtils_GetFieldValue_ReturnsDefaultValueIfWrongFlag()
        {
            var armour = 99;
            var name = "A Knight";
            var sample = new TypeUtilsSample(name, armour);
            var resultInt = TypeUtils.GetFieldValue<int>(sample, "armour", BindingFlags.Instance);
            var resultString = TypeUtils.GetFieldValue<string>(sample, "name", BindingFlags.Instance);

            Assert.That(resultInt, Is.Zero);
            Assert.That(resultString, Is.Null);
        }

        [Test]
        public void TypeUtils_GetFieldValue_ReturnsCorrectValue()
        {
            var armour = 99;
            var name = "A Knight";
            var sample = new TypeUtilsSample(name, armour);
            var resultInt = TypeUtils.GetFieldValue<int>(sample, "armour", BindingFlags.Instance | BindingFlags.NonPublic);
            var resultString = TypeUtils.GetFieldValue<string>(sample, "name", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(resultInt, Is.EqualTo(armour));
            Assert.That(resultString, Is.EqualTo(name));
        }

        [Test]
        public void GetImplementingTypes_Returns_All_Implementing_Types()
        {
            var results = TypeUtils.GetImplementingTypes<TestInterface>();
            Assert.That(results, Is.EquivalentTo(new[]
            {
                typeof(PrivateDirectImplementer),
                typeof(PrivateIndirectImplementer),
                typeof(PublicDirectImplementer),
                typeof(PublicIndirectImplementer)
            }));
        }

        private interface TestInterface { }
        private class PrivateDirectImplementer : TestInterface { }
        private class PrivateIndirectImplementer : PrivateDirectImplementer { }
        public class PublicDirectImplementer : TestInterface { }
        public class PublicIndirectImplementer : PublicDirectImplementer { }
    }

    internal class TypeUtilsSample
    {
        private string name;
        private int armour;

        public TypeUtilsSample()
        {

        }

        public TypeUtilsSample(string nameValue, int armourValue)
        {
            name = nameValue;
            armour = armourValue;
        }
    }
}
