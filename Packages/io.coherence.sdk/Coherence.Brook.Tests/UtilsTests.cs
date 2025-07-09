namespace Coherence.Brook.Tests
{
    using NUnit.Framework;
    using System;
    using Coherence.Tests;

    public class UtilsTests : CoherenceTest
    {
        [TestCase(0b10101010_11101010_10101010_10101010, 10,
            (uint)0b1010101011)]
        [TestCase(0b10101010_10101010_10101010_10101010, 12,
                  (uint)0b00000000_00000000_00001010_10101010)]
        [TestCase(0b10101010_10101010_10101010_10101010, 30,
            (uint)0b00101010_10101010_10101010_10101010)]
        [TestCase(0b10101010_10101010_10101010_10101011, 31,
            (uint)0b01010101_01010101_01010101_01010101)]
        [TestCase(0b10101010_10101010_10101010_10101011, 32,
                  0b10101010_10101010_10101010_10101011)]
        public void TruncateFloat_Works(uint input, int bits, uint expectedOutput)
        {
            // Checks that floats are correctly truncated by keeping most significant bits

            // Arrange
            float inputF;
            unsafe
            {
                inputF = *(float*)&input;
            }

            // Act
            var output = Utils.TruncateFloat(inputF, bits);

            // Assert
            Assert.That(output, Is.EqualTo(expectedOutput));
        }

        [TestCase(0)]
        [TestCase(9)]
        [TestCase(33)]
        public void TruncateFloat_WhenOutOfRange_ShouldThrow(int bits)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Utils.TruncateFloat(0, bits));
        }

        [TestCase(0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010, 13,
                  0b10101010_10101000_00000000_00000000_00000000_00000000_00000000_00000000)]
        [TestCase(0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010, 62,
                  0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101000)]
        [TestCase(0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101011, 63,
                  0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101010)]
        [TestCase(0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101011, 64,
                  0b10101010_10101010_10101010_10101010_10101010_10101010_10101010_10101011)]
        public void TruncateDouble_Works(ulong input, int bits, ulong expectedOutput)
        {
            // Checks that doubles are correctly truncated by keeping most significant bits

            // Arrange
            double inputD;
            unsafe
            {
                inputD = *(double*)&input;
            }

            // Act
            var output = Utils.TruncateDouble(inputD, bits);

            // Assert
            Assert.That(output, Is.EqualTo(expectedOutput));
        }

        [TestCase(0)]
        [TestCase(12)]
        [TestCase(65)]
        public void TruncateDouble_WhenOutOfRange_ShouldThrow(int bits)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Utils.TruncateDouble(0, bits));
        }

        [TestCase(0b00000000u, 0)]
        [TestCase(0b00000001u, 1)]
        [TestCase(0b10001001u, 8)]
        [TestCase(0b00000001_10001001u, 9)]
        [TestCase(0b10001001_10001001u, 16)]
        [TestCase(0b00000001_10001001_10001001u, 17)]
        [TestCase(0b10001001_10001001_10001001u, 24)]
        [TestCase(0b00000001_10001001_10001001_10001001u, 25)]
        [TestCase(0b10001001_10001001_10001001_10001001u, 32)]
        public void CountMSBitPosition_Int_Works(uint testValue, int expectedPosition)
        {
            // Checks that it correctly find the index of first most significant bit with value 1 for type uint

            // Act
            int position = Utils.CountMSBitPosition(testValue);

            // Assert
            Assert.That(position, Is.EqualTo(expectedPosition));
        }

        [TestCase(0b00000000u, 0)]
        [TestCase(0b00000001u, 1)]
        [TestCase(0b10001001u, 8)]
        [TestCase(0b00000001_10001001u, 9)]
        [TestCase(0b10001001_10001001u, 16)]
        [TestCase(0b00000001_10001001_10001001u, 17)]
        [TestCase(0b10001001_10001001_10001001u, 24)]
        [TestCase(0b00000001_10001001_10001001_10001001u, 25)]
        [TestCase(0b10001001_10001001_10001001_10001001u, 32)]
        [TestCase(0b00000001_10001001_10001001_10001001_10001001u, 33)]
        [TestCase(0b10001001_10001001_10001001_10001001_10001001u, 40)]
        [TestCase(0b00000001_10001001_10001001_10001001_10001001_10001001u, 41)]
        [TestCase(0b10001001_10001001_10001001_10001001_10001001_10001001u, 48)]
        [TestCase(0b00000001_10001001_10001001_10001001_10001001_10001001_10001001u, 49)]
        [TestCase(0b10001001_10001001_10001001_10001001_10001001_10001001_10001001u, 56)]
        [TestCase(0b00000001_10001001_10001001_10001001_10001001_10001001_10001001_10001001u, 57)]
        [TestCase(0b10001001_10001001_10001001_10001001_10001001_10001001_10001001_10001001u, 64)]
        public void CountMSBitPosition_Long_Works(ulong testValue, int expectedPosition)
        {
            // Checks that it correctly find the index of first most significant bit with value 1 for type ulong

            // Act
            int position = Utils.CountMSBitPosition(testValue);

            // Assert
            Assert.That(position, Is.EqualTo(expectedPosition));
        }

        [TestCase(0, 1, 0.1f, 1, 10u)]
        [TestCase(0, 100, 0.1f, 100, 1000u)]
        [TestCase(0, 2, 0.1f, 2, 20u)]
        [TestCase(0, uint.MaxValue, 1.0f, uint.MaxValue, uint.MaxValue)]
        public void GetMaxCursorForFixedPointCompression_Works(long minRange, long maxRange, float precision, long expectedRange, uint expectedMaxCursor)
        {
            // Checks that it correctly calculates maxCursor depending on the compression parameters

            // Act
            var maxCursor = Utils.GetMaxCursorForFixedPointCompression(minRange, maxRange, precision, out long range);

            // Assert
            Assert.That(maxCursor, Is.EqualTo(expectedMaxCursor));
            Assert.That(range, Is.EqualTo(expectedRange));
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(0, (long)uint.MaxValue + 1)]
        public void GetMaxCursorForFixedPointCompression_WhenRangeWrong_ShouldThrow(long minRange, long maxRange)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Utils.GetMaxCursorForFixedPointCompression(minRange, maxRange, 1, out _));
        }

        [TestCase(1, 0)]
        [TestCase(((long)(uint.MaxValue / 10) + 1), 0.1)]
        [TestCase(1, double.Epsilon)]
        public void GetMaxCursorForFixedPointCompression_WhenPrecisionWrong_ShouldThrow(long range, double precision)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Utils.GetMaxCursorForFixedPointCompression(0, range, precision, out _));
        }

        [TestCase(0, 1, 0.1, -0.5, 0u)]
        [TestCase(0, 1, 0.1, 0, 0u)]
        [TestCase(0, 1, 0.1, 0.01, 0u)]
        [TestCase(0, 1, 0.1, 0.049999999, 0u)]
        [TestCase(0, 1, 0.1, 0.05, 1u)]
        [TestCase(0, 1, 0.1, 0.949999999, 9u)]
        [TestCase(0, 1, 0.1, 0.95, 10u)]
        [TestCase(0, 1, 0.1, 1, 10u)]
        [TestCase(0, 1, 0.1, 1.5, 10u)]
        public void CalculateCursorForFixedFloatCompression_Works(long minRange, long maxRange, double precision, double value, uint expectedCursor)
        {
            // Checks that it correctly calculates cursor depending on the compression parameters

            // Act
            var cursor = Utils.CalculateCursorForFixedFloatCompression(value, minRange, maxRange, precision, out _);

            // Assert
            Assert.That(cursor, Is.EqualTo(expectedCursor));
        }

        [TestCase(0, 1, 0.1, 0u, 0)]
        [TestCase(0, 1, 0.1, 1u, 0.1)]
        [TestCase(0, 1, 0.1, 9u, 0.9)]
        [TestCase(0, 1, 0.1, 10u, 1)]
        public void UncompressFixedPoint_Works(long minRange, long maxRange, double precision, uint cursor, double expectedValue)
        {
            // Checks that it correctly uncompresses given cursor for the given compression parameters

            // Act
            var value = Utils.UncompressFixedPoint(cursor, minRange, maxRange, precision);

            // Assert
            Assert.That(value, Is.EqualTo(expectedValue));
        }
    }
}
