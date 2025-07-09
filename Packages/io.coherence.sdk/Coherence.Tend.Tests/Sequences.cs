// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Tend.Tests
{
    using Brook;
    using NUnit.Framework;
    using System;

    public static class Sequences
    {
        [Test]
        public static void Setting()
        {
            SequenceId s = new SequenceId(0);
            Assert.AreEqual(0, s.Value);
        }

        [Test]
        public static void NextWrap()
        {
            SequenceId current = SequenceId.Max;
            SequenceId next = current.Next();

            Assert.AreEqual(SequenceId.MaxValue, current.Value);
            Assert.AreEqual(0, next.Value);
        }

        [Test]
        public static void NormalNext()
        {
            SequenceId current = new SequenceId(12);
            SequenceId next = current.Next();

            Assert.AreEqual(12, current.Value);
            Assert.AreEqual(13, next.Value);
        }

        [Test]
        public static void DistanceWrap()
        {
            SequenceId current = SequenceId.Max;
            SequenceId next = new SequenceId(0);

            Assert.AreEqual(1, current.Distance(next));
        }

        [Test]
        public static void DistanceNormal()
        {
            SequenceId current = new SequenceId(0);
            SequenceId next = new SequenceId(10);

            Assert.AreEqual(10, current.Distance(next));
        }

        [Test]
        public static void DistancePassed()
        {
            SequenceId current = new SequenceId(10);
            SequenceId next = new SequenceId(9);

            Assert.AreEqual(SequenceId.MaxValue, current.Distance(next));
        }

        [Test]
        public static void DistancePassedAgain()
        {
            SequenceId current = new SequenceId(10);
            SequenceId next = SequenceId.Max;

            Assert.AreEqual(SequenceId.MaxValue - 10, current.Distance(next));
        }

        [Test]
        public static void DistanceSame()
        {
            SequenceId current = new SequenceId(10);
            SequenceId next = new SequenceId(10);

            Assert.AreEqual(0, current.Distance(next));
        }

        [Test]
        public static void IllegalValue()
        {
            _ = Assert.Throws<Exception>(() => new SequenceId(SequenceId.MaxValue + 11));
        }
    }
}
