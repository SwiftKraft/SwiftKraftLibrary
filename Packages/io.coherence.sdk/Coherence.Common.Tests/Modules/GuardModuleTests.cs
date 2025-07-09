namespace Coherence.Common.Tests.Modules
{
    using NUnit.Framework;
    using Coherence.Common.Pooling.Modules;
    using System.Collections.Generic;

    [TestFixture]
    public class GuardModuleTests
    {
        [Test]
        [Description("Verifies that the OnRent method throws an " +
                     "exception if we're returning an item that wasn't rented.")]
        public void OnReturn_ThrowsIfReturningNonRentedItem()
        {
            // Arrange
            var guard = new GuardModule<string>();

            // Act & Assert
            Assert.That(() => guard.OnReturn("test"), Throws.Exception);
        }

        [Test]
        [Description("Verifies that the OnRent method throws an " +
                     "exception if we're renting the same item twice.")]
        public void OnRent_ThrowsIfSameObjectRentedTwice()
        {
            // Arrange
            var guard = new GuardModule<object>();
            var testObject = new object();
            guard.OnRent(testObject);

            // Act & Assert
            Assert.That(() => guard.OnRent(testObject), Throws.Exception);
        }

        [Test]
        [Description("Verifies that the basic rent-return cycle works.")]
        public void OnRent_OnReturn_Works()
        {
            // Arrange
            var guard = new GuardModule<object>();
            var testObjects = new Stack<object>();
            for (var i = 0; i < 10; i++)
            {
                testObjects.Push(new object());
            }

            // Act
            foreach (var testObject in testObjects)
            {
                guard.OnRent(testObject);
            }

            foreach (var testObject in testObjects)
            {
                guard.OnReturn(testObject);
            }

            // Assert
            Assert.Pass();
        }
    }
}
