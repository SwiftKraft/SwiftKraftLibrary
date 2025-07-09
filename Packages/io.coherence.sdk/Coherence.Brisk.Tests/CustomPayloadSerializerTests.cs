// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Tests
{
    using System;
    using Coherence.Brook;
    using Coherence.Brook.Octet;
    using Coherence.Common;
    using Coherence.Brisk.Serializers;
    using NUnit.Framework;

    public class CustomPayloadSerializerTests
    {
        private IOutOctetStream outStream;

        [SetUp]
        public void SetUp()
        {
            outStream = new OutOctetStream(1024);
        }

        public IInOctetStream GetInStream()
        {
            return new InOctetStream(outStream.Close().ToArray());
        }

        [Test]
        public void CustomPayloadSerialize_Empty()
        {
            // Arrange
            var payload = CustomPayload.Empty;

            // Act
            outStream.WriteCustomPayload(payload);
            var deserializedPayload = GetInStream().ReadCustomPayload();

            // Assert
            Assert.IsTrue(deserializedPayload.IsEmpty);
        }

        [Test]
        public void CustomPayloadSerialize_Bytes()
        {
            // Arrange
            var len = CustomPayload.MaxCustomPayloadLen;
            var bytes = new byte[len];
            for (var i = 0; i < len; i++)
            {
                bytes[i] = (byte)(i % 256);
            }

            var payload = new CustomPayload(bytes);

            // Act
            outStream.WriteCustomPayload(payload);
            var deserializedPayload = GetInStream().ReadCustomPayload();

            // Assert
            Assert.IsFalse(deserializedPayload.IsEmpty);
            Assert.AreEqual(len, deserializedPayload.Bytes.Length);
            for (var i = 0; i < len; i++)
            {
                Assert.AreEqual(bytes[i], deserializedPayload.Bytes[i]);
            }
        }

        [Test]
        public void CustomPayloadSerialize_String()
        {
            // Arrange
            var str = "crazy𝕬💥Ⱥ⟁⚡︎⇌☯︎⛧ʕ•́ᴥ•̀ʔっ♡⧗⟁🜏𓂀✨☄️🂡𓆏🜲𐍈𓃰⁂👾✧⇨𐰴𐰀⛩⟁⊛⛓️🐉🌪️𓄂🜚𖤐𓃱🤖🌌⚛️ᚠ☢️𐂃🜸🍄🕸️⌬🚬žđšćč";

            var payload = new CustomPayload(str);

            // Act
            outStream.WriteCustomPayload(payload);
            var deserializedPayload = GetInStream().ReadCustomPayload();

            // Assert
            Assert.IsFalse(deserializedPayload.IsEmpty);
            Assert.AreEqual(str, deserializedPayload.AsString);
        }

        [Test]
        public void CustomPayloadSerialize_TooLong()
        {
            // Arrange
            var len = CustomPayload.MaxCustomPayloadLen + 1;
            var bytes = new byte[len];

            var payload = new CustomPayload(bytes);

            // Act & Assert
            Assert.Throws<Exception>(() => outStream.WriteCustomPayload(payload));
        }
    }
}
