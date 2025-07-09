// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Interpolation.Tests
{
    using NUnit.Framework;
    using System;
    using Coherence.Tests;

    public class InterpolationBufferTests : CoherenceTest
    {
        public SampleBuffer<int> buffer;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            this.buffer = new SampleBuffer<int>();
        }

        [TestCase(3, 2, 2, 3, 20)] // Don't go below the initial size
        [TestCase(20, 0, 1, 0, 40)] // Grow by 20 when out of capacity
        [TestCase(21, 0, 20, 0, 60)]
        [TestCase(21, 0, 0, 2, 40)] // Don't shrink down a block right away
        public void TestBufferResize(int push1Count, int pop1Count, int push2Count, int pop2Count, int expectedCapacity)
        {
            // Act
            for (int i = 0; i < push1Count; i++)
            {
                buffer.PushFront(new Sample<int>(i, false, i, null));
            }

            for (int i = 0; i < pop1Count; i++)
            {
                buffer.PopBack();
            }

            for (int i = push1Count; i < push1Count + push2Count; i++)
            {
                buffer.PushFront(new Sample<int>(i, false, i, null));
            }

            for (int i = 0; i < pop2Count; i++)
            {
                buffer.PopBack();
            }

            // Assert
            for (int i = 0; i < push1Count + push2Count - pop1Count - pop2Count; i++)
            {
                Assert.That(buffer[i].Value, Is.EqualTo(i + pop1Count + pop2Count));
            }

            Assert.That(buffer.Capacity, Is.EqualTo(expectedCapacity));
        }

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(25)]
        public void TestBufferContent(int numberOfSamples)
        {
            // Arrange

            // Act
            for (var i = 0; i < numberOfSamples; i++)
            {
                buffer.PushFront(new Sample<int>(i, false, i, null));
            }

            // Assert
            for (var i = 0; i < buffer.Count; i++)
            {
                Assert.That(buffer[i].Value, Is.EqualTo(i));
            }
        }

        [TestCase(0, 101, 0, 1)]
        [TestCase(1, 101, 0, 1)]
        [TestCase(5, 101, 2, 5)]
        [TestCase(5, 101, 5, 5)]
        [TestCase(10, 101, 2, 10)]
        [TestCase(10, 101, 10, 10)]
        [TestCase(20, 101, 2, 20)]
        public void TestStaleSamples(int initialSamples, int staleSampleValue, int staleSampleFrame, int expectedCount)
        {
            // Arrange
            for (var i = 1; i <= initialSamples; i++)
            {
                buffer.PushFront(new Sample<int>(i, false, i, null));
            }

            // Act
            buffer.PushFront(new Sample<int>(staleSampleValue, false, staleSampleFrame, null));

            // Assert
            Assert.That(buffer.Last.Value.Time, Is.EqualTo(initialSamples));
            Assert.That(buffer.Last.Value.Value, Is.EqualTo(staleSampleValue));
            Assert.That(buffer.Count, Is.EqualTo(expectedCount));
        }

        /// <summary>
        /// Add samples with timestamps {1,2,3} before calling RemoveOutdatedSamples. Assert that samples
        /// older than (or equal to) the given time have been removed, while respecting the minimum number
        /// of historic samples that the given Interpolator needs to maintain so it can blend motion properly.
        /// </summary>
        [TestCase(typeof(Interpolator), new int[] { 1, 2, 3 }, 0, new int[] { 3 })]
        [TestCase(typeof(Interpolator), new int[] { 1, 2, 3 }, 1, new int[] { 3 })]
        [TestCase(typeof(Interpolator), new int[] { 1, 2, 3 }, 2, new int[] { 3 })]
        [TestCase(typeof(Interpolator), new int[] { 1, 2, 3 }, 3, new int[] { 3 })]
        [TestCase(typeof(Interpolator), new int[] { 1, 2, 3 }, 4, new int[] { 3 })]
        [TestCase(typeof(LinearInterpolator), new int[] { 1, 2, 3 }, 0, new int[] { 1, 2, 3 })]
        [TestCase(typeof(LinearInterpolator), new int[] { 1, 2, 3 }, 1, new int[] { 1, 2, 3 })]
        [TestCase(typeof(LinearInterpolator), new int[] { 1, 2, 3 }, 2, new int[] { 2, 3 })]
        [TestCase(typeof(LinearInterpolator), new int[] { 1, 2, 3 }, 3, new int[] { 2, 3 })]
        [TestCase(typeof(LinearInterpolator), new int[] { 1, 2, 3 }, 4, new int[] { 2, 3 })]
        [TestCase(typeof(SplineInterpolator), new int[] { 1, 2, 3, 4 }, 0, new int[] { 1, 2, 3, 4 })]
        [TestCase(typeof(SplineInterpolator), new int[] { 1, 2, 3, 4 }, 1, new int[] { 1, 2, 3, 4 })]
        [TestCase(typeof(SplineInterpolator), new int[] { 1, 2, 3, 4 }, 2, new int[] { 1, 2, 3, 4 })]
        [TestCase(typeof(SplineInterpolator), new int[] { 1, 2, 3, 4 }, 3, new int[] { 2, 3, 4 })]
        [TestCase(typeof(SplineInterpolator), new int[] { 1, 2, 3, 4 }, 4, new int[] { 2, 3, 4 })]
        public void TestRemoveOutdatedSamples(Type interpolatorType, int[] initialSamples, double time, int[] expectedSamples)
        {
            // Arrange
            var interpolator = (Interpolator)Activator.CreateInstance(interpolatorType);

            foreach (var sample in initialSamples)
            {
                buffer.PushFront(new Sample<int>(sample, false, sample, null));
            }

            // Act
            buffer.RemoveOutdatedSamples(time, interpolator.NumberOfSamplesToStayBehind);

            // Assert
            Assert.That(buffer.Count, Is.EqualTo(expectedSamples.Length), "Number of samples");

            for (var i = 0; i < buffer.Count; i++)
            {
                Assert.That(buffer[i].Value, Is.EqualTo(expectedSamples[i]), "Sample");
            }
        }

        [TestCase(1f, 2f, 1.5f, true)]
        [TestCase(1f, -1f, 1.5f, false)]
        [TestCase(1f, 1f, 1.5f, false)]
        [TestCase(1f, 0.5f, 1.5f, false)]
        [TestCase(1f, 2f, 1f, false)]
        [TestCase(1f, 2f, 0.5f, false)]
        public void TestRemoveOutdatedVirtualSamples(double secondVirtualTime, double lastRealTime, double currentTime, bool shouldRemove)
        {
            // Arrange
            if (lastRealTime > 0)
            {
                buffer.PushFront(new Sample<int>(0, false, lastRealTime, null));
            }

            buffer.VirtualSamples = (new Sample<int>(0, false, secondVirtualTime - 1f, null),
                new Sample<int>(1, false, secondVirtualTime, null));

            // Act
            buffer.RemoveOutdatedSamples(currentTime, 1);

            // Assert
            Assert.That(buffer.VirtualSamples.HasValue, Is.EqualTo(!shouldRemove));
        }
    }
}
