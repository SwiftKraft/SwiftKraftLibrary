// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using Coherence.Brook;
    using NUnit.Framework;
    using Octet;
    using Coherence.Tests;

    public class InOutBitStreams : CoherenceTest
    {
        const int testStreamSizeOctets = 128;
        const int testStreamSizeBits = testStreamSizeOctets * 8;
        const int testHeaderOctets = 3;
        const int testHeaderBits = testHeaderOctets * 8;

        private static IOutBitStream Setup(out OutOctetStream octetWriter, int initialOctets = 0)
        {
            octetWriter = new OutOctetStream(testStreamSizeOctets);

            for (int i = 0; i < initialOctets; i++)
            {
                octetWriter.WriteOctet(69);
            }

            return new OutBitStream(octetWriter);
        }

        private static IOutBitStream SetupDebug(out OutOctetStream octetWriter, int initialOctets = 0)
        {
            var stream = Setup(out octetWriter, initialOctets);

            return new DebugOutBitStream(stream);
        }

        private static IInBitStream SetupIn(OutOctetStream writer)
        {
            var octetReader = new InOctetStream(writer.Octets.ToArray());
            return new InBitStream(octetReader, writer.Octets.Length * 8);
        }

        private static IInBitStream SetupInDebug(OutOctetStream writer)
        {
            var stream = SetupIn(writer);
            return new DebugInBitStream(stream);
        }

        //// Octetwritet at empty at start

        [Test]
        public static void WriteAndReadSigned16bit()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer);
            short v = -23988;

            outStream.WriteInt16(v);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 16);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 16);

            IInBitStream inStream = SetupIn(writer);
            short rv = inStream.ReadInt16();
            Assert.AreEqual(-23988, rv);
        }

        [Test]
        public static void WriteBytesUnaligned()
        {
            var writeBuffer = new byte[] { 0b1111_1111, 0b0110_0110, 0b1001_1001, 0b1100_1100, 0b0101_0101, 0b1010_1010 };

            for (var bitCount = 1; bitCount <= writeBuffer.Length * 8; bitCount++)
            {
                // Arrange
                var outStream = Setup(out var writer);
                var readBuffer = new byte[writeBuffer.Length];

                // Act
                outStream.WriteBytesUnaligned(writeBuffer, bitCount);
                outStream.Flush();
                Assert.That(outStream.Position, Is.EqualTo(bitCount));

                var inStream = SetupIn(writer);
                inStream.ReadBytesUnaligned(readBuffer, bitCount);

                // Assert
                for (var i = 0; i < bitCount; i++)
                {
                    var writtenBit = (writeBuffer[i/8] >> (7 - (i%8))) & 1;
                    var readBit = (readBuffer[i/8] >> (7 - (i%8))) & 1;

                    Assert.AreEqual(writtenBit, readBit, $"Bit {i} is not equal");
                }
            }
        }

        [Test]
        public static void WriteDebugValues()
        {
            IOutBitStream outStream = SetupDebug(out OutOctetStream writer);

            byte byteVal = 123;
            ushort ushortVal = 12345;
            short shortVal = -12345;
            uint uintVal = 1234567;
            int intVal = -1234567;
            ulong ulongVal = 12345678910;

            outStream.WriteUint8(byteVal);
            outStream.WriteSignedBits(-byteVal, 8);
            outStream.WriteUint16(ushortVal);
            outStream.WriteInt16(shortVal);
            outStream.WriteUint32(uintVal);
            outStream.WriteSignedBits(intVal, 32);
            outStream.WriteUint64(ulongVal);
            outStream.WriteSignedBits(-7, 7);
            outStream.WriteBits(7, 7);
            outStream.Flush();

            IInBitStream inStream = SetupInDebug(writer);
            var testUByte = inStream.ReadUint8();
            Assert.AreEqual(byteVal, testUByte);
            var testByte = inStream.ReadSignedBits(8);
            Assert.AreEqual(-byteVal, testByte);
            var testUShort = inStream.ReadUint16();
            Assert.AreEqual(ushortVal, testUShort);
            var testShort = inStream.ReadInt16();
            Assert.AreEqual(shortVal, testShort);
            var testUintVal = inStream.ReadUint32();
            Assert.AreEqual(uintVal, testUintVal);
            var testIntVal = inStream.ReadSignedBits(32);
            Assert.AreEqual(intVal, testIntVal);
            var testULong = inStream.ReadUint64();
            Assert.AreEqual(ulongVal, testULong);
            var testSigned = inStream.ReadSignedBits(7);
            Assert.AreEqual(-7, testSigned);
            var testUnsigned = inStream.ReadBits(7);
            Assert.AreEqual(7, testUnsigned);
        }

        [Test]
        public static void WriteTwoNumbers()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer);
            const short otherValue = 1234;
            short v = -23988;
            short u = otherValue;

            outStream.WriteBits(2, 3);
            outStream.WriteInt16(v);
            outStream.WriteInt16(u);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 35);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 35);

            IInBitStream inStream = SetupIn(writer);
            uint x = inStream.ReadBits(3);
            Assert.AreEqual((uint)2, x);
            short rv = inStream.ReadInt16();
            Assert.AreEqual(-23988, rv);
            short ru = inStream.ReadInt16();
            Assert.AreEqual(otherValue, ru);
        }

        [Test]
        public static void WriteExactly32BitsInPiecesThenExactly32bitsMoreInOne()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer);
            const short otherValue = 1234;
            uint v = 69696969;
            short u = otherValue;

            outStream.WriteBits(6, 7);
            outStream.WriteBits(7, 5);
            outStream.WriteBits(12, 5);
            outStream.WriteBits(12, 5);
            outStream.WriteBits(690, 10);
            outStream.WriteUint32(v);
            outStream.WriteInt16(u);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 80);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 80);

            IInBitStream inStream = SetupIn(writer);
            Assert.AreEqual((uint)6, inStream.ReadBits(7));
            Assert.AreEqual((uint)7, inStream.ReadBits(5));
            Assert.AreEqual((uint)12, inStream.ReadBits(5));
            Assert.AreEqual((uint)12, inStream.ReadBits(5));
            Assert.AreEqual((uint)690, inStream.ReadBits(10));
            Assert.AreEqual((uint)69696969, inStream.ReadUint32());
            Assert.AreEqual(otherValue, inStream.ReadInt16());
        }

        [Test]
        public static void WriteSomeNumbersThenSeek()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer);

            outStream.WriteBits(2, 3);
            outStream.WriteBits(7, 4);
            uint position = outStream.Position;
            outStream.WriteInt16(-69);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 23);

            outStream.Seek(position);

            Assert.IsFalse(outStream.IsFull);

            Assert.AreEqual(position, outStream.Position);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position);

            Assert.AreEqual(position, outStream.Position);
            IInBitStream inStream = SetupIn(writer);
            uint x = inStream.ReadBits(3);
            Assert.AreEqual((uint)2, x);
            uint y = inStream.ReadBits(4);
            Assert.AreEqual((uint)7, y);
        }

        [Test]
        public static void WriteSomeNumbersThenSeekThenMoreNumbers()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer);

            outStream.WriteBits(2, 3);
            outStream.WriteBits(7, 4);
            uint position = outStream.Position;
            outStream.WriteInt16(-69); // Throw away

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 23);

            outStream.Seek(position);

            Assert.IsFalse(outStream.IsFull);

            Assert.AreEqual(position, outStream.Position);

            outStream.WriteInt16(45);
            outStream.WriteUint32(69696969);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position - 48);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position - 48);

            IInBitStream inStream = SetupIn(writer);
            uint x = inStream.ReadBits(3);
            Assert.AreEqual((uint)2, x);
            uint y = inStream.ReadBits(4);
            Assert.AreEqual((uint)7, y);
            short z = inStream.ReadInt16();
            Assert.AreEqual(45, z);
            uint zz = inStream.ReadUint32();
            Assert.AreEqual((uint)69696969, zz);
        }

        [Test]
        public static void WriteSomeNumbersThenSeekPartWay()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer);

            outStream.WriteBits(2, 3);
            outStream.WriteBits(7, 3);
            outStream.WriteInt16(-69);
            outStream.WriteInt16(45);
            outStream.WriteUint32(69696969);
            uint position = outStream.Position;
            outStream.WriteInt16(-17);
            outStream.WriteUint32(123345);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 118);

            outStream.Seek(position);

            Assert.IsFalse(outStream.IsFull);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position);

            IInBitStream inStream = SetupIn(writer);
            uint a = inStream.ReadBits(3);
            Assert.AreEqual((uint)2, a);
            uint b = inStream.ReadBits(3);
            Assert.AreEqual((uint)7, b);
            short c = inStream.ReadInt16();
            Assert.AreEqual(-69, c);
            short d = inStream.ReadInt16();
            Assert.AreEqual(45, d);
            uint e = inStream.ReadUint32();
            Assert.AreEqual((uint)69696969, e);
        }

        [Test]
        public static void WriteRandomNumbersSeekThenKnownNumbers()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer);
            System.Random rand = new System.Random();

            for (int i = 0; i < 50; i++)
            {
                int kind = rand.Next();
                switch (kind % 5)
                {
                    case 0: outStream.WriteBits(5, 5); break;
                    case 1: outStream.WriteInt16(-69); break;
                    case 2: outStream.WriteUint16(1234); break;
                    case 3: outStream.WriteUint8(69); break;
                    default: break;
                }
            }

            outStream.Seek(0);

            Assert.IsFalse(outStream.IsFull);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits);

            outStream.WriteBits(2, 3);
            outStream.WriteBits(7, 3);
            outStream.WriteInt16(-69);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 22);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 22);

            IInBitStream inStream = SetupIn(writer);
            uint a = inStream.ReadBits(3);
            Assert.AreEqual((uint)2, a);
            uint b = inStream.ReadBits(3);
            Assert.AreEqual((uint)7, b);
            short c = inStream.ReadInt16();
            Assert.AreEqual(-69, c);
        }

        [Test]
        public static void WriteNumbersThenRandomNumbersSeekThenKnownNumbers()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer);
            System.Random rand = new System.Random();

            outStream.WriteBits(2, 3);
            outStream.WriteBits(7, 3);
            outStream.WriteInt16(-69);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 22);

            uint position = outStream.Position;

            for (int i = 0; i < 50; i++)
            {
                int kind = rand.Next();
                switch (kind % 5)
                {
                    case 0: outStream.WriteBits(5, 5); break;
                    case 1: outStream.WriteInt16(-69); break;
                    case 2: outStream.WriteUint16(1234); break;
                    case 3: outStream.WriteUint8(69); break;
                    default: break;
                }
            }

            outStream.Seek(position);

            Assert.IsFalse(outStream.IsFull);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position);

            outStream.WriteBits(4, 3);
            outStream.WriteBits(5, 3);
            outStream.WriteInt16(45);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position - 22);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position - 22);

            IInBitStream inStream = SetupIn(writer);
            uint a = inStream.ReadBits(3);
            Assert.AreEqual((uint)2, a);
            uint b = inStream.ReadBits(3);
            Assert.AreEqual((uint)7, b);
            short c = inStream.ReadInt16();
            Assert.AreEqual(-69, c);

            uint d = inStream.ReadBits(3);
            Assert.AreEqual((uint)4, d);
            uint e = inStream.ReadBits(3);
            Assert.AreEqual((uint)5, e);
            short f = inStream.ReadInt16();
            Assert.AreEqual(45, f);
        }

        [Test]
        public static void WriteToFull()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer);
            const short otherValue = 1234;

            for (int i = 0; i < (testStreamSizeBits / 16); i++)
            {
                outStream.WriteInt16(otherValue);
            }

            Assert.AreEqual(outStream.RemainingBitCount, 0);

            Assert.IsTrue(outStream.IsFull);

            outStream.Flush();

            Assert.IsTrue(outStream.IsFull);

            Assert.AreEqual(outStream.RemainingBitCount, 0);

            Assert.IsTrue(outStream.IsFull);

            IInBitStream inStream = SetupIn(writer);
            for (int i = 0; i < (testStreamSizeBits / 16); i++)
            {
                short ru = inStream.ReadInt16();
                Assert.AreEqual(otherValue, ru);
            }
        }

        [Test]
        public static void WriteToFullAndMore()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer);
            const short otherValue = 1234;

            for (int i = 0; i < (testStreamSizeBits / 16); i++)
            {
                outStream.WriteInt16(otherValue);
            }

            Assert.AreEqual(outStream.RemainingBitCount, 0);

            Assert.IsTrue(outStream.IsFull);

            //writes nothing...
            outStream.WriteInt16(otherValue);

            Assert.IsTrue(outStream.IsFull);

            outStream.Flush();

            Assert.IsTrue(outStream.IsFull);

            Assert.AreEqual(outStream.RemainingBitCount, 0);

            Assert.IsTrue(outStream.IsFull);

            IInBitStream inStream = SetupIn(writer);
            for (int i = 0; i < (testStreamSizeBits / 16); i++)
            {
                short ru = inStream.ReadInt16();
                Assert.AreEqual(otherValue, ru);
            }
        }

        [Test]
        public static void WriteToFullAndMoreAndSeek()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer);
            const short otherValue = 1234;

            for (int i = 0; i < (testStreamSizeBits / 16); i++)
            {
                outStream.WriteInt16(otherValue);
            }

            Assert.AreEqual(outStream.RemainingBitCount, 0);

            Assert.IsTrue(outStream.IsFull);

            //writes nothing...
            outStream.WriteInt16(otherValue);

            Assert.IsTrue(outStream.IsFull);

            outStream.Flush();

            Assert.IsTrue(outStream.IsFull);

            Assert.AreEqual(outStream.RemainingBitCount, 0);

            outStream.Seek(0);

            Assert.IsFalse(outStream.IsFull);

            for (int i = 0; i < (testStreamSizeBits / 16); i++)
            {
                outStream.WriteInt16(otherValue);
            }

            Assert.AreEqual(outStream.RemainingBitCount, 0);

            Assert.IsTrue(outStream.IsFull);

            outStream.Flush();

            IInBitStream inStream = SetupIn(writer);
            for (int i = 0; i < (testStreamSizeBits / 16); i++)
            {
                short ru = inStream.ReadInt16();
                Assert.AreEqual(otherValue, ru);
            }
        }

        [Test]
        public static void IsFullTest()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer);
            const short otherValue = 1234;

            for (int i = 0; i < (testStreamSizeBits / 32) - 1; i++)
            {
                outStream.WriteUint32((uint)otherValue);
            }

            outStream.WriteInt16(otherValue);

            //Fails to write so IsFull is true
            outStream.WriteBits((uint)0b11001101100110011, 17);

            Assert.IsTrue(outStream.IsFull);

            //Should not write more, but if we do, is full should remain
            //true even if this *could* write.
            outStream.WriteBits((uint)0b1110011, 7);

            Assert.IsTrue(outStream.IsFull);
        }

        //// Octetwritet has contents at start

        [Test]
        public static void WriteAndReadSigned16bitNonEmptyOctetWriter()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer, testHeaderOctets);
            short v = -23988;

            outStream.WriteInt16(v);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 16 - testHeaderBits);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 16 - testHeaderBits);

            IInBitStream inStream = SetupIn(writer);
            var header = inStream.ReadBits(testHeaderBits);
            short rv = inStream.ReadInt16();
            Assert.AreEqual(-23988, rv);
        }

        [Test]
        public static void WriteTwoNumbersNonEmptyOctetWriter()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer, testHeaderOctets);
            const short otherValue = 1234;
            short v = -23988;
            short u = otherValue;

            outStream.WriteBits(2, 3);
            outStream.WriteInt16(v);
            outStream.WriteInt16(u);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 35 - testHeaderBits);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 35 - testHeaderBits);

            IInBitStream inStream = SetupIn(writer);
            var header = inStream.ReadBits(testHeaderBits);
            uint x = inStream.ReadBits(3);
            Assert.AreEqual((uint)2, x);
            short rv = inStream.ReadInt16();
            Assert.AreEqual(-23988, rv);
            short ru = inStream.ReadInt16();
            Assert.AreEqual(otherValue, ru);
        }

        [Test]
        public static void WriteExactly32BitsInPiecesThenExactly32bitsMoreInOneNonEmptyOctetWriter()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer, testHeaderOctets);
            const short otherValue = 1234;
            uint v = 69696969;
            short u = otherValue;

            outStream.WriteBits(6, 7);
            outStream.WriteBits(7, 5);
            outStream.WriteBits(12, 5);
            outStream.WriteBits(12, 5);
            outStream.WriteBits(690, 10);
            outStream.WriteUint32(v);
            outStream.WriteInt16(u);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 80 - testHeaderBits);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 80 - testHeaderBits);

            IInBitStream inStream = SetupIn(writer);
            var header = inStream.ReadBits(testHeaderBits);
            Assert.AreEqual((uint)6, inStream.ReadBits(7));
            Assert.AreEqual((uint)7, inStream.ReadBits(5));
            Assert.AreEqual((uint)12, inStream.ReadBits(5));
            Assert.AreEqual((uint)12, inStream.ReadBits(5));
            Assert.AreEqual((uint)690, inStream.ReadBits(10));
            Assert.AreEqual((uint)69696969, inStream.ReadUint32());
            Assert.AreEqual(otherValue, inStream.ReadInt16());
        }

        [Test]
        public static void WriteSomeNumbersThenSeekNonEmptyOctetWriter()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer, testHeaderOctets);

            outStream.WriteBits(2, 3);
            outStream.WriteBits(7, 4);
            uint position = outStream.Position;
            outStream.WriteInt16(-69);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 23 - testHeaderBits);

            outStream.Seek(position);
            Assert.AreEqual(position, outStream.Position);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position - testHeaderBits);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position - testHeaderBits);

            Assert.AreEqual(position, outStream.Position);
            IInBitStream inStream = SetupIn(writer);
            var header = inStream.ReadBits(testHeaderBits);
            uint x = inStream.ReadBits(3);
            Assert.AreEqual((uint)2, x);
            uint y = inStream.ReadBits(4);
            Assert.AreEqual((uint)7, y);
        }

        [Test]
        public static void WriteSomeNumbersThenSeekThenMoreNumbersNonEmptyOctetWriter()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer, testHeaderOctets);

            outStream.WriteBits(2, 3);
            outStream.WriteBits(7, 4);
            uint position = outStream.Position;
            outStream.WriteInt16(-69); // Throw away

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 23 - testHeaderBits);

            outStream.Seek(position);
            Assert.AreEqual(position, outStream.Position);
            outStream.WriteInt16(45);
            outStream.WriteUint32(69696969);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position - 48 - testHeaderBits);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position - 48 - testHeaderBits);

            IInBitStream inStream = SetupIn(writer);
            var header = inStream.ReadBits(testHeaderBits);
            uint x = inStream.ReadBits(3);
            Assert.AreEqual((uint)2, x);
            uint y = inStream.ReadBits(4);
            Assert.AreEqual((uint)7, y);
            short z = inStream.ReadInt16();
            Assert.AreEqual(45, z);
            uint zz = inStream.ReadUint32();
            Assert.AreEqual((uint)69696969, zz);
        }

        [Test]
        public static void WriteSomeNumbersThenSeekPartWayNonEmptyOctetWriter()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer, testHeaderOctets);

            outStream.WriteBits(2, 3);
            outStream.WriteBits(7, 3);
            outStream.WriteInt16(-69);
            outStream.WriteInt16(45);
            outStream.WriteUint32(69696969);
            uint position = outStream.Position;
            outStream.WriteInt16(-17);
            outStream.WriteUint32(123345);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 118 - testHeaderBits);

            outStream.Seek(position);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position - testHeaderBits);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position - testHeaderBits);

            IInBitStream inStream = SetupIn(writer);
            var header = inStream.ReadBits(testHeaderBits);
            uint a = inStream.ReadBits(3);
            Assert.AreEqual((uint)2, a);
            uint b = inStream.ReadBits(3);
            Assert.AreEqual((uint)7, b);
            short c = inStream.ReadInt16();
            Assert.AreEqual(-69, c);
            short d = inStream.ReadInt16();
            Assert.AreEqual(45, d);
            uint e = inStream.ReadUint32();
            Assert.AreEqual((uint)69696969, e);
        }

        [Test]
        public static void WriteRandomNumbersSeekThenKnownNumbersNonEmptyOctetWriter()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer, testHeaderOctets);
            System.Random rand = new System.Random();

            for (int i = 0; i < 50; i++)
            {
                int kind = rand.Next();
                switch (kind % 5)
                {
                    case 0: outStream.WriteBits(5, 5); break;
                    case 1: outStream.WriteInt16(-69); break;
                    case 2: outStream.WriteUint16(1234); break;
                    case 3: outStream.WriteUint8(69); break;
                    default: break;
                }
            }

            outStream.Seek(0);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - testHeaderBits);

            outStream.WriteBits(2, 3);
            outStream.WriteBits(7, 3);
            outStream.WriteInt16(-69);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 22 - testHeaderBits);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 22 - testHeaderBits);

            IInBitStream inStream = SetupIn(writer);
            var header = inStream.ReadBits(testHeaderBits);
            uint a = inStream.ReadBits(3);
            Assert.AreEqual((uint)2, a);
            uint b = inStream.ReadBits(3);
            Assert.AreEqual((uint)7, b);
            short c = inStream.ReadInt16();
            Assert.AreEqual(-69, c);
        }

        [Test]
        public static void WriteNumbersThenRandomNumbersSeekThenKnownNumbersNonEmptyOctetWriter()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer, testHeaderOctets);
            System.Random rand = new System.Random();

            outStream.WriteBits(2, 3);
            outStream.WriteBits(7, 3);
            outStream.WriteInt16(-69);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - 22 - testHeaderBits);

            uint position = outStream.Position;

            for (int i = 0; i < 50; i++)
            {
                int kind = rand.Next();
                switch (kind % 5)
                {
                    case 0: outStream.WriteBits(5, 5); break;
                    case 1: outStream.WriteInt16(-69); break;
                    case 2: outStream.WriteUint16(1234); break;
                    case 3: outStream.WriteUint8(69); break;
                    default: break;
                }
            }

            outStream.Seek(position);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position - testHeaderBits);

            outStream.WriteBits(4, 3);
            outStream.WriteBits(5, 3);
            outStream.WriteInt16(45);

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position - 22 - testHeaderBits);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, testStreamSizeBits - position - 22 - testHeaderBits);

            IInBitStream inStream = SetupIn(writer);
            var header = inStream.ReadBits(testHeaderBits);
            uint a = inStream.ReadBits(3);
            Assert.AreEqual((uint)2, a);
            uint b = inStream.ReadBits(3);
            Assert.AreEqual((uint)7, b);
            short c = inStream.ReadInt16();
            Assert.AreEqual(-69, c);

            uint d = inStream.ReadBits(3);
            Assert.AreEqual((uint)4, d);
            uint e = inStream.ReadBits(3);
            Assert.AreEqual((uint)5, e);
            short f = inStream.ReadInt16();
            Assert.AreEqual(45, f);
        }

        [Test]
        public static void WriteToFullNonEmptyOctetWriter()
        {
            const int headerSizeOctets = 2;
            const int headerSizeBits = headerSizeOctets * 8;
            IOutBitStream outStream = Setup(out OutOctetStream writer, headerSizeOctets);
            const short otherValue = 1234;

            for (int i = 0; i < ((testStreamSizeBits - headerSizeBits) / 16); i++)
            {
                outStream.WriteInt16(otherValue);
            }

            Assert.AreEqual(outStream.RemainingBitCount, 0);

            Assert.IsTrue(outStream.IsFull);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, 0);

            Assert.IsTrue(outStream.IsFull);

            IInBitStream inStream = SetupIn(writer);
            var header = inStream.ReadBits(headerSizeOctets * 8);
            for (int i = 0; i < ((testStreamSizeBits - headerSizeBits) / 16); i++)
            {
                short ru = inStream.ReadInt16();
                Assert.AreEqual(otherValue, ru);
            }
        }

        [Test]
        public static void WriteToFullAndMoreNonEmptyOctetWriter()
        {
            const int headerSizeOctets = 2;
            const int headerSizeBits = headerSizeOctets * 8;
            IOutBitStream outStream = Setup(out OutOctetStream writer, headerSizeOctets);
            const short otherValue = 1234;

            for (int i = 0; i < ((testStreamSizeBits - headerSizeBits) / 16); i++)
            {
                outStream.WriteInt16(otherValue);
            }

            Assert.AreEqual(outStream.RemainingBitCount, 0);

            Assert.IsTrue(outStream.IsFull);

            //writes nothing...
            outStream.WriteInt16(otherValue);

            outStream.Flush();

            Assert.AreEqual(outStream.RemainingBitCount, 0);

            Assert.IsTrue(outStream.IsFull);

            IInBitStream inStream = SetupIn(writer);
            var header = inStream.ReadBits(headerSizeBits);
            for (int i = 0; i < ((testStreamSizeBits - testHeaderBits) / 16); i++)
            {
                short ru = inStream.ReadInt16();
                Assert.AreEqual(otherValue, ru);
            }
        }

        [Test]
        public static void WriteToFullAndMoreAndSeekNonEmptyOctetWriter()
        {
            const int headerSizeOctets = 2;
            const int headerSizeBits = headerSizeOctets * 8;
            IOutBitStream outStream = Setup(out OutOctetStream writer, headerSizeOctets);
            const short otherValue = 1234;

            for (int i = 0; i < ((testStreamSizeBits - headerSizeBits) / 16); i++)
            {
                outStream.WriteInt16(otherValue);
            }

            Assert.AreEqual(outStream.RemainingBitCount, 0);

            Assert.IsTrue(outStream.IsFull);

            //writes nothing...
            outStream.WriteInt16(otherValue);

            Assert.IsTrue(outStream.IsFull);

            outStream.Flush();

            Assert.IsTrue(outStream.IsFull);

            Assert.AreEqual(outStream.RemainingBitCount, 0);

            outStream.Seek(0);

            Assert.IsFalse(outStream.IsFull);

            for (int i = 0; i < ((testStreamSizeBits - headerSizeBits) / 16); i++)
            {
                outStream.WriteInt16(otherValue);
            }

            Assert.AreEqual(outStream.RemainingBitCount, 0);

            Assert.IsTrue(outStream.IsFull);

            outStream.Flush();

            IInBitStream inStream = SetupIn(writer);
            var header = inStream.ReadBits(headerSizeBits);
            for (int i = 0; i < ((testStreamSizeBits - headerSizeBits) / 16); i++)
            {
                short ru = inStream.ReadInt16();
                Assert.AreEqual(otherValue, ru);
            }
        }

        [Test]
        public static void IsFullTestNonEmptyOctetWriter()
        {
            IOutBitStream outStream = Setup(out OutOctetStream writer, testHeaderOctets);
            const short otherValue = 1234;

            for (int i = 0; i < ((testStreamSizeBits - testHeaderBits) / 32); i++)
            {
                outStream.WriteUint32((uint)otherValue);
            }

            outStream.WriteInt16(otherValue);

            //Fails to write so IsFull is true
            outStream.WriteBits((uint)0b11001011011011101, 17);

            Assert.IsTrue(outStream.IsFull);

            //Should not write more, but if we do, is full should remain
            //true even if this *could* write.
            outStream.WriteBits((uint)0b11001, 5);

            Assert.IsTrue(outStream.IsFull);
        }
    }
}
