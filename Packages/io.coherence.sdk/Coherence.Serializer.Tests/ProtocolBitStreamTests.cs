// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Serializer.Tests
{
    using Brook;
    using Brook.Octet;
    using Coherence.Log.Targets;
    using Log;
    using NUnit.Framework;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Coherence.Tests;

    public class ProtocolBitStreamTests : CoherenceTest
    {
        [Test]
        [Description("Verifies that OutProtocolBitStream.WriteShortString truncates " +
                     "written string if it's too long but still writes it.")]
        public void WriteShortString_WritesSubstringOnTooLongString()
        {
            // Arrange
            var octetStream = new OutOctetStream(1000);
            var bitStream = new OutBitStream(octetStream);
            var protoStream = new OutProtocolBitStream(bitStream, logger);

            var testString = "1337" + new string('x', OutProtocolBitStream.SHORT_STRING_MAX_SIZE);

            // Act
            protoStream.WriteShortString(testString);
            bitStream.Flush();

            // Assert
            var written = octetStream.Close().ToArray();
            var inOctetStream = new InOctetStream(written);
            var inBitStream = new InBitStream(inOctetStream, written.Length * 8);
            var inProtoStream = new InProtocolBitStream(inBitStream);

            string got = inProtoStream.ReadShortString();
            string expected = testString.Substring(0, OutProtocolBitStream.SHORT_STRING_MAX_SIZE);

            Assert.That(got, Is.EqualTo(expected));
            Assert.That(logger.GetCountForErrorID(Error.StringTooLong), Is.EqualTo(1));
        }

        [Test]
        [Description("Verifies that OutProtocolBitStream.WriteShortString handles 2+ byte characters when truncating.")]
        public void WriteShortString_HandlesWideCharacters_WhenTruncating()
        {
            // Arrange
            var octetStream = new OutOctetStream(1000);
            var bitStream = new OutBitStream(octetStream);
            var protoStream = new OutProtocolBitStream(bitStream, logger);

            var testString = "";
            for (int i = 0; i < OutProtocolBitStream.SHORT_STRING_MAX_SIZE; i++)
            {
                testString += "👍";
            }

            // Act
            protoStream.WriteShortString(testString);
            bitStream.Flush();

            // Assert
            var written = octetStream.Close().ToArray();
            var inOctetStream = new InOctetStream(written);
            var inBitStream = new InBitStream(inOctetStream, written.Length * 8);
            var inProtoStream = new InProtocolBitStream(inBitStream);

            string got = inProtoStream.ReadShortString();
            Assert.That(Encoding.UTF8.GetByteCount(got), Is.EqualTo(OutProtocolBitStream.SHORT_STRING_MAX_SIZE));

            Assert.That(logger.GetCountForErrorID(Error.StringTooLong), Is.EqualTo(1));
        }
    }
}
