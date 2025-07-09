// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Tests.Modules
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using Pooling.Modules;

    [TestFixture]
    public class ActionsModuleTests
    {
        [Test]
        [Description("Verifies that the rent actions are executed when an item is rented.")]
        public void OnRent_ExecutesRentActions()
        {
            // Arrange
            var iterations = 10;

            var actionsModule = new ActionsModule<string>();
            var testStrings = new List<string>();
            for (var i = 0; i < iterations; i++)
            {
                testStrings.Add($"test{i}");
            }

            actionsModule.WithRentAction(s => testStrings.Remove(s));

            int counter = 0;
            actionsModule.WithRentAction(s => counter++);

            // Act
            for (var i = testStrings.Count - 1; i >= 0; i--)
            {
                var testString = testStrings[i];
                actionsModule.OnRent(testString);
            }

            // Assert
            Assert.IsEmpty(testStrings);
            Assert.AreEqual(iterations, counter);
        }

        [Test]
        [Description("Verifies that the return actions are executed when an item is returned.")]
        public void OnReturn_ExecutesReturnActions()
        {
            // Arrange
            var iterations = 10;

            var actionsModule = new ActionsModule<string>();
            var testStrings = new List<string>();
            for (var i = 0; i < iterations; i++)
            {
                testStrings.Add($"test{i}");
            }

            actionsModule.WithReturnAction(s => testStrings.Remove(s));

            int counter = 0;
            actionsModule.WithReturnAction(s => counter++);

            // Act
            for (var i = testStrings.Count - 1; i >= 0; i--)
            {
                var testString = testStrings[i];
                actionsModule.OnReturn(testString);
            }

            // Assert
            Assert.IsEmpty(testStrings);
            Assert.AreEqual(iterations, counter);
        }
    }
}
