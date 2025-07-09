// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Tests
{
    using Brook;
    using Brook.Octet;
    using NUnit.Framework;
    using Serializer;
    using OutOctetStream = Brook.Octet.OutOctetStream;
    using Coherence.Tests;

    public class SerializeToolsTests : CoherenceTest
    {
        [TestCase(0, 0, 5)]
        [TestCase(1, 1, 5)]
        [TestCase(8, 8, 11)]
        [TestCase(255, 255, 11)]
        [TestCase(256, 256, 20)]
        [TestCase(16000, 16000, 20)]
        [TestCase(-1, -1, 5)]
        [TestCase(-8, -8, 11)]
        [TestCase(-255, -255, 11)]
        [TestCase(-256, -256, 20)]
        [TestCase(-16000, -16000, 20)]
        public void RleSigned_Works(short value, short exp, int bitsUsed)
        {
            // Arrange
            var packetStream = new OutOctetStream(256);
            var outBitStream = (IOutBitStream)new OutBitStream(packetStream);

            // Act
            SerializeTools.WriteRleSigned(outBitStream, value);
            outBitStream.Flush();

            var actualBitCount = outBitStream.Position;

            var octetReader = new InOctetStream(packetStream.Close().ToArray());
            var inBitStream = (IInBitStream)new InBitStream(octetReader, (int)outBitStream.Position);

            var deserialized = DeserializerTools.ReadRleSigned(inBitStream);

            // Assert
            Assert.That(actualBitCount, Is.EqualTo(bitsUsed));
            Assert.That(deserialized, Is.EqualTo(exp));
        }

        [TestCase(0, 1)]
        [TestCase(1, 4)]
        [TestCase(2, 4)]
        [TestCase(3, 4)]
        [TestCase(4, 7)]
        [TestCase(5, 7)]
        [TestCase(9, 7)]
        [TestCase(15, 7)]
        [TestCase(16, 11)]
        [TestCase(17, 11)]
        [TestCase(123, 11)]
        [TestCase(254, 11)]
        [TestCase(255, 11)]
        public void FieldSimFrameDelta_Works(byte value, int bitsUsed)
        {
            // Arrange
            var packetStream = new OutOctetStream(256);
            var outBitStream = (IOutBitStream)new OutBitStream(packetStream);
            var outProtocolBitStream = new OutProtocolBitStream(outBitStream, null);

            // Act
            SerializeTools.WriteFieldSimFrameDelta(outProtocolBitStream, value);
            outBitStream.Flush();

            var actualBitCount = outBitStream.Position;

            var octetReader = new InOctetStream(packetStream.Close().ToArray());
            var inBitStream = (IInBitStream)new InBitStream(octetReader, (int)outBitStream.Position);
            var inProtocolBitStream = new InProtocolBitStream(inBitStream);

            var deserialized = DeserializerTools.ReadFieldSimFrameDelta(inProtocolBitStream);

            // Assert
            Assert.That(actualBitCount, Is.EqualTo(bitsUsed));
            Assert.That(deserialized, Is.EqualTo(value));
        }

    }
}
