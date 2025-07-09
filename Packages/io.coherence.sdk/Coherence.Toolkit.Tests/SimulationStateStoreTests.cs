// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using NUnit.Framework;
    using System;
    using Coherence.Tests;

    public class SimulationStateStoreTests : CoherenceTest
    {
        struct State
        {
            public long Frame { get; set; }

            public State(long frame)
            {
                Frame = frame;
            }
        }

        [Test]
        public void Add_ThrowsOnNonSubsequentFrames()
        {
            // Arrange
            var store = new SimulationStateStore<State>();
            store.Add(new State(35), 35);

            // Act & Assert
            Assert.That(() => store.Add(new State(37), 37), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Add_BumpsNewestFrame()
        {
            // Arrange
            var store = new SimulationStateStore<State>();

            // Act & Assert
            store.Add(new State(35), 35);
            Assert.That(store.NewestFrame, Is.EqualTo(35));
            Assert.That(store.OldestFrame, Is.EqualTo(35));

            store.Add(new State(36), 36);
            Assert.That(store.NewestFrame, Is.EqualTo(36));
            Assert.That(store.OldestFrame, Is.EqualTo(35));
        }

        [Test]
        public void Acknowledge_IgnoresEmpty()
        {
            // Arrange
            var store = new SimulationStateStore<State>();

            // Act
            store.Acknowledge(10);

            // Assert
            Assert.That(store.NewestFrame, Is.EqualTo(-1));
            Assert.That(store.OldestFrame, Is.EqualTo(-1));
            Assert.That(store.Count, Is.EqualTo(0));
        }

        [Test]
        public void Acknowledge_IgnoresStaleFrame()
        {
            // Arrange
            var store = new SimulationStateStore<State>();
            store.Add(new State(10), 10);
            store.Add(new State(11), 11);
            store.Add(new State(12), 12);

            // Act
            store.Acknowledge(9);

            // Assert
            Assert.That(store.NewestFrame, Is.EqualTo(12));
            Assert.That(store.OldestFrame, Is.EqualTo(10));
            Assert.That(store.Count, Is.EqualTo(3));
        }

        [TestCase(10, 0, 10, 1, 10)]
        [TestCase(10, 1, 10, 1, 10)]
        [TestCase(10, 5, 10, 5, 6)]
        [TestCase(10, 10, 10, 10, 1)]
        [TestCase(1, 1, 1, 1, 1)]
        public void Acknowledge_Works(int initialFrames, int toAck, int expectedNewest, int expectedOldest, int expectedCount)
        {
            // Arrange
            var store = new SimulationStateStore<State>();
            for (int i = 1; i <= initialFrames; i++)
            {
                store.Add(new State(i), i);
            }

            // Act
            store.Acknowledge(toAck);

            // Assert
            Assert.That(store.NewestFrame, Is.EqualTo(expectedNewest));
            Assert.That(store.OldestFrame, Is.EqualTo(expectedOldest));
            Assert.That(store.Count, Is.EqualTo(expectedCount));
        }

        [TestCase(10, 0, false, 0, 10, 1, 10)]
        [TestCase(10, 1, false, 0, 10, 1, 10)]
        [TestCase(10, 2, true, 1, 1, 1, 1)]
        [TestCase(10, 5, true, 4, 4, 4, 1)]
        [TestCase(10, 10, true, 9, 9, 9, 1)]
        [TestCase(10, 11, true, 10, 10, 10, 1)]
        [TestCase(10, 12, false, 0, 10, 1, 10)]
        public void TryRollback_Works(int initialFrames, int invalidPredictionFrame,
            bool expectedRollbackOk, int expectedValidFrame,
            int expectedNewest, int expectedOldest, int expectedCount)
        {
            // Arrange
            var store = new SimulationStateStore<State>();
            for (int i = 1; i <= initialFrames; i++)
            {
                store.Add(new State(i), i);
            }

            // Act
            bool rollbackOk = store.TryRollback(invalidPredictionFrame, out State validState);

            // Assert
            Assert.That(rollbackOk, Is.EqualTo(expectedRollbackOk));
            Assert.That(validState.Frame, Is.EqualTo(expectedValidFrame));
            Assert.That(store.NewestFrame, Is.EqualTo(expectedNewest));
            Assert.That(store.OldestFrame, Is.EqualTo(expectedOldest));
            Assert.That(store.Count, Is.EqualTo(expectedCount));
        }
    }
}
