// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cram.Tests
{
    using Brook;
    using NUnit.Framework;
    using System;
    using Brook.Octet;
    using Quaternion = System.Numerics.Quaternion;
    using Vector3 = System.Numerics.Vector3;
    using Vector4 = System.Numerics.Vector4;
    using Coherence.Tests;

    public class StreamTests : CoherenceTest
    {
        private const double BaseEpsilon = 1E-9d;

        private static object[][] SerializeQuaternion_Works_Source => new[]
        {
            new object[] { new Vector4(-0.183f, 0.683f, -0.062f, 0.704f), 32},
            new object[] { new Vector4(0.5f, 0.5f, 0.5f, 0.5f), 10 },
            new object[] { new Vector4(1f, 2f, 3f, 4f), 10 },

            new object[] { new Vector4(1f, 0f, 0f, 0f), 32 },
            new object[] { new Vector4(1f, 0f, 0f, 0f), 2 },
            new object[] { new Vector4(-1f, 0f, 0f, 0f), 32 },
            new object[] { new Vector4(-1f, 0f, 0f, 0f), 2 },

            new object[] { new Vector4(0f, 1f, 0f, 0f), 32 },
            new object[] { new Vector4(0f, 1f, 0f, 0f), 2 },
            new object[] { new Vector4(0f, -1f, 0f, 0f), 32 },
            new object[] { new Vector4(0f, -1f, 0f, 0f), 2 },

            new object[] { new Vector4(0f, 0f, 1f, 0f), 32 },
            new object[] { new Vector4(0f, 0f, 1f, 0f), 2 },
            new object[] { new Vector4(0f, 0f, -1f, 0f), 32 },
            new object[] { new Vector4(0f, 0f, -1f, 0f), 2 },

            new object[] { new Vector4(0f, 0f, 0f, 1f), 32 },
            new object[] { new Vector4(0f, 0f, 0f, 1f), 2 },
            new object[] { new Vector4(0f, 0f, 0f, -1f), 32 },
            new object[] { new Vector4(0f, 0f, 0f, -1f), 2 },
        };

        [TestCaseSource(nameof(SerializeQuaternion_Works_Source))]
        public static void SerializeQuaternion_Works(Vector4 valueSource, int bitsPerComponent)
        {
            // Arrange
            double epsilon = 2d / Math.Pow(2, bitsPerComponent - 1);

            valueSource = Vector4.Normalize(valueSource);
            Quaternion value = new Quaternion(valueSource.X, valueSource.Y, valueSource.Z, valueSource.W);

            OutOctetStream octetStream = new OutOctetStream(128);

            OutBitStream outStreamBase = new OutBitStream(octetStream);
            Cram.OutBitStream outStream = new Cram.OutBitStream(outStreamBase);

            // Act
            outStream.WriteQuaternion(value, bitsPerComponent);
            outStreamBase.Flush();

            byte[] octets = octetStream.Octets.ToArray();
            InOctetStream inOctetReader = new InOctetStream(octets);
            InBitStream inStreamBase = new InBitStream(inOctetReader, 128 * 8);
            Cram.InBitStream inStream = new Cram.InBitStream(inStreamBase);

            Quaternion outValue = inStream.ReadQuaternion(bitsPerComponent);

            // Assert
            Assert.That(outValue.X, Is.EqualTo(value.X).Within(epsilon), "X");
            Assert.That(outValue.Y, Is.EqualTo(value.Y).Within(epsilon), "Y");
            Assert.That(outValue.Z, Is.EqualTo(value.Z).Within(epsilon), "Z");
            Assert.That(outValue.W, Is.EqualTo(value.W).Within(epsilon), "W");
        }

        public static void AlmostEqual(float a, float b, double precision)
        {
            float diff = b - a;

            Assert.That(diff, Is.InRange(-precision, precision), $"Original: {a:F5}, Got: {b:F5}");
        }

        [Test]
        public static void SerializeVector()
        {
            OutOctetStream octetStream = new OutOctetStream(128);

            OutBitStream outStream = new OutBitStream(octetStream);
            Cram.OutBitStream compressedStream = new Cram.OutBitStream(outStream);

            Vector3 v = new Vector3(600.410f, 0.001f, 1.334f);

            var floatMeta = FloatMeta.ForFixedPoint(0, 1000, 0.001d);
            compressedStream.WriteVector3(v, floatMeta);

            outStream.Flush();

            byte[] octets = octetStream.Octets.ToArray();
            InOctetStream inOctetReader = new InOctetStream(octets);

            InBitStream inStream = new InBitStream(inOctetReader, 128 * 8);

            Cram.InBitStream compressedInStream = new Cram.InBitStream(inStream);

            Vector3 ov = compressedInStream.ReadVector3(floatMeta);

            const double precision = 0.0005d;
            AlmostEqual(v.X, ov.X, precision);
            AlmostEqual(v.Y, ov.Y, precision);
            AlmostEqual(v.Z, ov.Z, precision);
        }

        private static object[][] SerializeVector4_Works_Source => new[]
        {
            new object[] { new Vector4(-0.183f, 0.683f, -0.062f, 0.704f), FloatMeta.NoCompression(), BaseEpsilon },
            new object[] { new Vector4(0.5f, 0.5f, 0.5f, 0.5f), FloatMeta.ForFixedPoint(-1, 1, 0.1d), 0.1d },
            new object[] { new Vector4(1f, -2f, 3f, -4f), FloatMeta.ForFixedPoint(-4, 4, 0.1d), 0.1d },

            new object[] { new Vector4(1f, 1f, 1f, 1f), FloatMeta.ForFixedPoint(-1, 1, 0.1d), BaseEpsilon },
            new object[] { new Vector4(1f, 1f, 1f, 1f), FloatMeta.ForFixedPoint(-1, 1, 0.1d), 0.1d },
            new object[] { new Vector4(-1f, -1f, -1f, -1f), FloatMeta.ForFixedPoint(-1, 1, 0.1d), BaseEpsilon },
            new object[] { new Vector4(-1f, -1f, -1f, -1f), FloatMeta.ForFixedPoint(-1, 1, 0.1d), 0.1d },

            new object[] { new Vector4(1f, 1f, 1f, 1f), FloatMeta.ForTruncated(32), BaseEpsilon },
            new object[] { new Vector4(1f, 1f, 1f, 1f), FloatMeta.ForTruncated(12), 0.1d },
            new object[] { new Vector4(-1f, -1f, -1f, -1f), FloatMeta.ForTruncated(32), BaseEpsilon },
            new object[] { new Vector4(-1f, -1f, -1f, -1f), FloatMeta.ForTruncated(12), 0.1d },
        };

        [TestCaseSource(nameof(SerializeVector4_Works_Source))]
        public static void SerializeVector4_Works(Vector4 value, FloatMeta floatMeta, double epsilon)
        {
            // Arrange
            OutOctetStream octetStream = new OutOctetStream(128);

            OutBitStream outStreamBase = new OutBitStream(octetStream);
            Cram.OutBitStream outStream = new Cram.OutBitStream(outStreamBase);

            // Act
            outStream.WriteVector4(value, floatMeta);
            outStreamBase.Flush();

            byte[] octets = octetStream.Octets.ToArray();
            InOctetStream inOctetReader = new InOctetStream(octets);
            InBitStream inStreamBase = new InBitStream(inOctetReader, 128 * 8);
            Cram.InBitStream inStream = new Cram.InBitStream(inStreamBase);

            Vector4 outValue = inStream.ReadVector4(floatMeta);

            // Assert
            Assert.That(outValue.X, Is.EqualTo(value.X).Within(epsilon), "X");
            Assert.That(outValue.Y, Is.EqualTo(value.Y).Within(epsilon), "Y");
            Assert.That(outValue.Z, Is.EqualTo(value.Z).Within(epsilon), "Z");
            Assert.That(outValue.W, Is.EqualTo(value.W).Within(epsilon), "W");
        }

        [TestCase(0f, 0f, 0, 1, 1d, 1)]
        [TestCase(1f, 1f, 0, 1, 1d, 1)]
        [TestCase(0.5f, 0.5f, 0, 1, 0.1d, 4)]
        [TestCase(100.77f, 100.8f, 100, 101, 0.1d, 4)]
        [TestCase(0f, 0f, 0, 1, 0.1d, 4)]
        [TestCase(1f, 1f, 0, 1, 0.1d, 4)]
        [TestCase(1f, 1f, 0, 100, 0.01d, 14)]
        [TestCase(-367.023f, -367.02f, -1000, 1000, 0.01d, 18)]
        [TestCase(-367.923f, -367.92f, -1000, 1000, 0.01d, 18)]
        [TestCase(367.023f, 367.02f, -1000, 1000, 0.01d, 18)]
        [TestCase(367.923f, 367.92f, -1000, 1000, 0.01d, 18)]
        [TestCase(0.12345678f, 0.12345678f, -1, 1, 1e-8d, 28)]
        [TestCase(-0.12345678f, -0.12345678f, -1, 1, 1e-8d, 28)]
        [TestCase((float)int.MaxValue, (float)int.MaxValue, 0, int.MaxValue, 1, 31)]
        [TestCase(0f, 0f, -(int.MaxValue / 2), int.MaxValue / 2, 1d, 31)]
        [TestCase(1000.32f, 100f, -100, 100, 0.01d, 15)]
        [TestCase(-1000.32f, -100f, -100, 100, 0.01d, 15)]
        public void SerializeFixedFloat_Works(float value, float expectedValue, int minRange, int maxRange, double precision, int expectedBitsUsed)
        {
            // Arrange
            OutOctetStream octetStream = new OutOctetStream(128);

            OutBitStream outStreamBase = new OutBitStream(octetStream);
            Cram.OutBitStream outStream = new Cram.OutBitStream(outStreamBase);

            FloatMeta floatMeta = FloatMeta.ForFixedPoint(minRange, maxRange, precision);

            // Act
            outStream.WriteFloat((float)value, floatMeta);
            outStreamBase.Flush();

            byte[] octets = octetStream.Octets.ToArray();
            InOctetStream inOctetReader = new InOctetStream(octets);
            InBitStream inStreamBase = new InBitStream(inOctetReader, 128 * 8);
            Cram.InBitStream inStream = new Cram.InBitStream(inStreamBase);

            double outValue = inStream.ReadFloat(floatMeta);

            // Assert
            Assert.That(outValue, Is.EqualTo(expectedValue).Within(BaseEpsilon));
            Assert.That(outStreamBase.Position, Is.EqualTo(expectedBitsUsed));
        }

        [TestCase(0, 2u)]
        [TestCase(10, 32u)]
        public void SerializeFixedDouble_ThrowsOnInvalidRange(int maxRange, uint decimalPlaces)
        {
            // Arrange
            OutOctetStream octetStream = new OutOctetStream(128);

            OutBitStream outStreamBase = new OutBitStream(octetStream);
            Cram.OutBitStream outStream = new Cram.OutBitStream(outStreamBase);

            FloatMeta floatMeta = new FloatMeta()
            {
                Compression = FloatCompression.FixedPoint,
                Maximum = maxRange,
                Precision = decimalPlaces
            };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                outStream.WriteFloat(0f, floatMeta);
            });
        }

        [TestCase(0d, 0d)]
        [TestCase(double.NaN, double.NaN)]
        [TestCase(double.MaxValue, double.MaxValue)]
        [TestCase(double.MinValue, double.MinValue)]
        [TestCase(double.PositiveInfinity, double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity, double.NegativeInfinity)]
        [TestCase(92038152134.231450d, 92038152134.231450d)]
        [TestCase(-92038152134.231450d, -92038152134.231450d)]
        public void SerializeDouble_Works(double value, double expectedValue)
        {
            // Arrange
            OutOctetStream octetStream = new OutOctetStream(128);

            OutBitStream outStreamBase = new OutBitStream(octetStream);
            Cram.OutBitStream outStream = new Cram.OutBitStream(outStreamBase);

            // Act
            outStream.WriteDouble(value);
            outStreamBase.Flush();

            byte[] octets = octetStream.Octets.ToArray();
            InOctetStream inOctetReader = new InOctetStream(octets);
            InBitStream inStreamBase = new InBitStream(inOctetReader, 128 * 8);
            Cram.InBitStream inStream = new Cram.InBitStream(inStreamBase);

            double outValue = inStream.ReadDouble();

            // Assert
            Assert.That(outValue, Is.EqualTo(expectedValue).Within(BaseEpsilon));
        }

        [TestCase(0f, 0f)]
        [TestCase(float.NaN, float.NaN)]
        [TestCase(float.MaxValue, float.MaxValue)]
        [TestCase(float.MinValue, float.MinValue)]
        [TestCase(float.PositiveInfinity, float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity, float.NegativeInfinity)]
        [TestCase(92038152134.231450f, 92038152134.231450f)]
        [TestCase(-92038152134.231450f, -92038152134.231450f)]
        public void SerializeFloat_Works(float value, float expectedValue)
        {
            const double epsilon = 1E-9f;

            // Arrange
            OutOctetStream octetStream = new OutOctetStream(128);

            OutBitStream outStreamBase = new OutBitStream(octetStream);
            Cram.OutBitStream outStream = new Cram.OutBitStream(outStreamBase);

            FloatMeta floatMeta = FloatMeta.NoCompression();

            // Act
            outStream.WriteFloat(value, floatMeta);
            outStreamBase.Flush();

            byte[] octets = octetStream.Octets.ToArray();
            InOctetStream inOctetReader = new InOctetStream(octets);
            InBitStream inStreamBase = new InBitStream(inOctetReader, 128 * 8);
            Cram.InBitStream inStream = new Cram.InBitStream(inStreamBase);

            double outValue = inStream.ReadFloat(floatMeta);

            // Assert
            Assert.That(outValue, Is.EqualTo(expectedValue).Within(epsilon));
        }

        [TestCase(0f, 0f, 10)]
        [TestCase(0f, 0f, 32)]
        [TestCase(0.25f, 0.25f, 10)]
        [TestCase(10f, 10f, 12)]
        [TestCase(10f, 8f, 10)]
        [TestCase(15f, 12f, 10)]
        [TestCase(-10f, -10f, 12)]
        [TestCase(float.NaN, float.NaN, 10)]
        [TestCase(float.PositiveInfinity, float.PositiveInfinity, 10)]
        [TestCase(float.NegativeInfinity, float.NegativeInfinity, 10)]
        [TestCase(float.MaxValue, float.MaxValue, 32)]
        [TestCase(float.MinValue, float.MinValue, 32)]
        public void SerializeTruncatedFloat_Works(float value, float expectedValue, int bits)
        {
            const double epsilon = 1E-9f;

            // Arrange
            OutOctetStream octetStream = new OutOctetStream(128);

            OutBitStream outStreamBase = new OutBitStream(octetStream);
            Cram.OutBitStream outStream = new Cram.OutBitStream(outStreamBase);

            FloatMeta floatMeta = FloatMeta.ForTruncated(bits);

            // Act
            outStream.WriteFloat(value, floatMeta);
            outStreamBase.Flush();

            byte[] octets = octetStream.Octets.ToArray();
            InOctetStream inOctetReader = new InOctetStream(octets);
            InBitStream inStreamBase = new InBitStream(inOctetReader, 128 * 8);
            Cram.InBitStream inStream = new Cram.InBitStream(inStreamBase);

            float outValue = inStream.ReadFloat(floatMeta);

            // Assert
            Assert.That(outValue, Is.EqualTo(expectedValue).Within(epsilon));
            Assert.That(outStreamBase.Position, Is.EqualTo(bits));
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(9)]
        [TestCase(33)]
        public void SerializeTruncatedFloat_ThrowsOnInvalidPrecisionBits(int bits)
        {
            // Arrange
            OutOctetStream octetStream = new OutOctetStream(128);

            OutBitStream outStreamBase = new OutBitStream(octetStream);
            Cram.OutBitStream outStream = new Cram.OutBitStream(outStreamBase);

            var floatMeta = new FloatMeta()
            {
                Compression = FloatCompression.Truncated,
                BitCount = bits
            };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                outStream.WriteFloat(0f, floatMeta);
            });
        }
    }
}
