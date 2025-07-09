// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Tests
{
    using NUnit.Framework;
    using Pooling;

    [TestFixture]
    public class PoolTests
    {
        [Test]
        [Description("Tests that the pool is pre-filled with the default number of objects.")]
        public void Rent_PrefillingWorks()
        {
            // Arrange
            var counter = 0;

            // Act
            Pool<string>.Builder(p => $"test{counter++}").Build();

            // Assert
            Assert.That(counter, Is.EqualTo(Pool<string>.DefaultPrefillSize));
        }

        [Test]
        [Description("Verifies that new objects are generated when the pool is empty.")]
        public void Rent_GeneratesNewObjectWhenEmpty()
        {
            // Arrange
            bool generated = false;
            var pool = Pool<string>.Builder(p =>
            {
                generated = true;
                return "test";
            }).Prefill(0)
                .Build();

            Assert.That(generated, Is.False);

            // Act
            pool.Rent();

            // Assert
            Assert.That(generated, Is.True);
        }

        [Test]
        [Description("Verifies that objects are actually reused when returned to the pool.")]
        public void Return_AddsObjectBackToPool()
        {
            // Arrange
            int counter = 0;
            var pool = Pool<string>.Builder(p => $"test{counter++}")
                .Prefill(0)
                .Build();

            var rented = pool.Rent();

            // Act
            pool.Return(rented);
            var rentedAgain = pool.Rent();

            // Assert
            Assert.That(rentedAgain, Is.EqualTo("test0"));
        }

        [Test]
        [Description("Verifies that the rent action is executed when an object is rented.")]
        public void Rent_ExecutesRentActions()
        {
            // Arrange
            var actionExecuted = false;
            var pool = Pool<string>.Builder(p => "test")
                .WithRentAction(s => actionExecuted = true)
                .Build();

            // Act
            pool.Rent();

            // Assert
            Assert.That(actionExecuted, Is.True);
        }

        [Test]
        [Description("Verifies that the return action is executed when an object is returned.")]
        public void Return_ExecutesReturnActions()
        {
            // Arrange
            var actionExecuted = false;
            var pool = Pool<string>.Builder(p => "test")
                .WithReturnAction(s => actionExecuted = true)
                .Build();
            var rented = pool.Rent();

            // Act
            pool.Return(rented);

            // Assert
            Assert.That(actionExecuted, Is.True);
        }

        [Test]
        [Description("Verifies that the object generator is required.")]
        public void Rent_ThrowsExceptionWhenObjectGeneratorIsNull()
        {
            // Arrange, Act & Assert
            Assert.That(() => Pool<string>.Builder(null).Build(), Throws.ArgumentNullException);
        }
    }
}
