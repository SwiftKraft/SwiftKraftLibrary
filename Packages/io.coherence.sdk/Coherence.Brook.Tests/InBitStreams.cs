// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook.Tests
{
    using Coherence.Brook;
    using NUnit.Framework;
    using Octet;
    using Coherence.Tests;

    public class InBitStreams : CoherenceTest
    {
        private static IInBitStream Setup(byte[] octets)
        {
            InOctetStream octetReader = new InOctetStream(octets);
            InBitStream bitStream = new InBitStream(octetReader, octets.Length * 8);

            return bitStream;
        }

        [Test]
        public static void ReadNibble()
        {
            IInBitStream bitStream = Setup(new byte[] { 0x3c });

            uint t = bitStream.ReadBits(2);

            Assert.AreEqual((uint)0, t);

            uint t2 = bitStream.ReadBits(1);
            Assert.AreEqual((uint)1, t2);

            uint t3 = bitStream.ReadBits(1);
            Assert.AreEqual((uint)1, t3);
        }

        [Test]
        public static void ReadTooFar()
        {
            IInBitStream bitStream = Setup(new byte[] { 0xfe });

            uint t = bitStream.ReadBits(4);

            Assert.AreEqual((uint)15, t);

            _ = Assert.Throws<EndOfStreamException>(() => bitStream.ReadBits(5));
        }

        [Test]
        public static void ReadOverDWord()
        {
            IInBitStream bitStream = Setup(new byte[] { 0xca, 0xfe, 0xba, 0xdb, 0xee, 0xf0 });

            uint t = bitStream.ReadBits(24);

            Assert.AreEqual((uint)0xcafeba, t);

            uint t2 = bitStream.ReadBits(16);

            Assert.AreEqual((uint)0xdbee, t2);
        }
    }
}
