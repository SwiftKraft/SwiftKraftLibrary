// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook.Tests
{
    using Coherence.Brook;
    using NUnit.Framework;
    using System;
    using Octet;
    using Coherence.Tests;

    public class OutBitStreams : CoherenceTest
    {
        private const int bufferSize = 128; // bytes
        private const int bitStreamSize = bufferSize * 8; // bits
        private OutOctetStream octetWriter;
        private OutBitStream bitStream;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            octetWriter = new OutOctetStream(bufferSize);
            bitStream = new OutBitStream(octetWriter);
        }

        [Test]
        public void WriteNibble()
        {
            bitStream.WriteBits(0b00, 2);
            Assert.AreEqual(0b00000000_00000000_00000000_00000000, bitStream.Accumulator);
            bitStream.WriteBits(0b1, 1);
            Assert.AreEqual(0b00100000_00000000_00000000_00000000, bitStream.Accumulator);
            bitStream.WriteBits(0b1, 1);
            Assert.AreEqual(0b00110000_00000000_00000000_00000000, bitStream.Accumulator);
        }

        [Test]
        public void WriteMoreThanOctet()
        {
            bitStream.WriteBits(0b1111, 4);
            Assert.AreEqual(0b11110000_00000000_00000000_00000000, bitStream.Accumulator);
            bitStream.WriteBits(0b11, 2);
            Assert.AreEqual(0b11111100_00000000_00000000_00000000, bitStream.Accumulator);
            bitStream.WriteBits(0b0101, 4);
            Assert.AreEqual(0b11111101_01000000_00000000_00000000, bitStream.Accumulator);
        }

        [Test]
        public void WriteMoreThan32bit()
        {
            bitStream.WriteBits(0b11111110_11111110, 16);
            Assert.AreEqual(0b11111110_11111110_00000000_00000000, bitStream.Accumulator);
            bitStream.WriteBits(0b00001011_10101101, 15);
            Assert.AreEqual(0b11111110_11111110_00010111_01011010, bitStream.Accumulator);
            bitStream.WriteBits(0b11, 2);
            byte[] octets = octetWriter.Octets.ToArray();
            Assert.AreEqual(new byte[] { 0b11111110, 0b11111110, 0b00010111, 0b01011011 }, octets);
            Assert.AreEqual(0b10000000_00000000_00000000_00000000, bitStream.Accumulator);
        }

        [TestCase(1)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(31)]
        [TestCase(32)]
        public void WriteMoreThanCapacity_ShouldIncrementOverflowCount(int bitsToWritePerIteration)
        {
            // Arrange
            var expectedOverflow = 13;
            var bitsToWrite = bitStreamSize + expectedOverflow;

            // Act
            while (bitsToWrite > 0)
            {
                var mask = ((uint)1 << Math.Min(bitsToWrite, bitsToWritePerIteration)) - 1;
                bitStream.WriteBits(uint.MaxValue & mask, Math.Min(bitsToWrite, bitsToWritePerIteration));
                bitsToWrite -= bitsToWritePerIteration;
            }

            // Assert
            Assert.AreEqual(expectedOverflow, bitStream.OverflowBitCount);
        }

        // TestThatCantWriteValueLargerThanBitsAllow tests that if we try to write data using a
        // fixed number of bits that we don't try to write a value that can't be represented by
        // that number of bits. Had an issue where the value 32 was written using 5 bits which
        // resulted in the value 0 being serialized.
        [TestCase(1, 0u, true)]
        [TestCase(1, 1u, true)]
        [TestCase(1, 2u, false)]
        [TestCase(5, 31u, true)]
        [TestCase(5, 32u, false)]
        [TestCase(5, 33u, false)]
        public void TestThatCantWriteValueLargerThanBitsAllow(int numBits, uint value, bool shouldSucceed)
        {
            // Arrange
            // Setup is called automatically

            //Act
            if (shouldSucceed)
            {
                Assert.DoesNotThrow(() => bitStream.WriteBits(value, numBits));
            }
            else
            {
                Assert.Throws<Exception>(() => bitStream.WriteBits(value, numBits));
            }
        }
    }
}
