// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using NUnit.Framework;
    using Coherence.Tests;

    public class ArchetypeMathTests : CoherenceTest
    {
        [TestCase(0, 1, 1)]
        [TestCase(0, 2, 2)]
        [TestCase(0, 3, 2)]
        [TestCase(0, 4, 3)]
        [TestCase(0, ushort.MaxValue, 16)]
        [TestCase(-1, ushort.MaxValue, 17)]
        [TestCase(0, uint.MaxValue, 32)]
        [TestCase(int.MinValue, int.MaxValue, 32)]
        public void GetBitsForIntValue_Works(long minRangeInclusive, long maxRangeInclusive, int expectedBits)
        {
            int bits = ArchetypeMath.GetBitsForIntValue(minRangeInclusive, maxRangeInclusive);
            Assert.That(bits, Is.EqualTo(expectedBits));
        }

        [TestCase(4, 0.1, 1u)]
        [TestCase(5, 0.1, 3u)]
        [TestCase(32, 0.001, 4294967u)]
        public void GetRangeByBitsAndPrecision_Works(int bits, double precision, ulong expectedRange)
        {
            ulong range = ArchetypeMath.GetTotalRangeByBitsAndPrecision(bits, precision);
            Assert.That(range, Is.EqualTo(expectedRange));
        }
    }
}
