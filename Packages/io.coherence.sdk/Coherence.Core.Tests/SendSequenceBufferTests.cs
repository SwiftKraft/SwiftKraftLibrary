// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using Brook;
    using Coherence.Tests;
    using Entities;
    using NUnit.Framework;
    using Serializer;

    public class SendSequenceBufferTests : CoherenceTest
    {
        private const int BufferSize = 128;

        private SendSequenceBuffer buffer;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            buffer = new SendSequenceBuffer(BufferSize);
        }

        [Test(Description = "Messages are not appended if the buffer is full")]
        public void AppendMessages_FullBuffer()
        {
            var mockMessage = CreateMessage();

            // Fill the buffer [0, 1, ..., N].
            for (var i = 0; i < BufferSize; ++i)
            {
                var res = buffer.AppendMessage(mockMessage);
                Assert.IsTrue(res);
            }

            Assert.IsFalse(buffer.AppendMessage(mockMessage), "Buffer should be full");
        }

        [Test(Description = "Once the oldest messages have been delivered, it is possible to append more messages")]
        public void AppendMessages_AfterDelivery()
        {
            var mockMessage = CreateMessage();

            // Fill the buffer [0, 1, ..., N].
            for (var i = 0; i < BufferSize; ++i)
            {
                var res = buffer.AppendMessage(mockMessage);
                Assert.IsTrue(res);
            }

            // A few messages in the middle are delivered [3, 4].
            var messages1 = new List<MessageID>(2);
            for (var i = 0; i < 2; ++i)
            {
                messages1.Add(new MessageID((ushort)(3+i)));
            }

            buffer.OnMessagesDelivered(messages1, wasDelivered: true);
            Assert.IsFalse(buffer.AppendMessage(mockMessage), "Should not have added more messages");

            // The oldest messages are delivered [0, 1, 2].
            var messages2 = new List<MessageID>(3);
            for (var i = 0; i < 3; ++i)
            {
                messages2.Add(new MessageID((ushort)(i)));
            }

            buffer.OnMessagesDelivered(messages2, wasDelivered: true);

            // There should be space again [-,-,-,-,-, 5, 6, ..., N], append more messages.
            for (var i = 0; i < (messages1.Count + messages2.Count); i++)
            {
                var res = buffer.AppendMessage(mockMessage);
                Assert.IsTrue(res);
            }

            Assert.IsFalse(buffer.AppendMessage(mockMessage), "Buffer should be full");
        }

        [Test(Description = "Messages should not be sent more than once")]
        public void GetMessagesToSend_AfterSend()
        {
            var mockMessage = CreateMessage();

            // Append a few messages [0, 1, 2].
            for (var i = 0; i < 3; ++i)
            {
                var res = buffer.AppendMessage(mockMessage);
                Assert.IsTrue(res);
            }

            // Verify the send list.
            var sendList = new List<(MessageID, SerializedEntityMessage)>();
            buffer.GetMessagesToSend(sendList);

            Assert.AreEqual(3, sendList.Count);
            for (var i = 0; i < sendList.Count; ++i)
            {
                Assert.AreEqual((ushort)(i), sendList[i].Item1.Value);
            }

            // Mark a few messages are sent, and verify the send list again.
            var sentMessages = new List<MessageID>(2)
            {
                sendList[0].Item1,
                sendList[1].Item1
            };
            buffer.OnMessagesSent(sentMessages);

            buffer.GetMessagesToSend(sendList);
            Assert.AreEqual(1, sendList.Count, "There should only 1 message left to be sent");
            Assert.AreEqual(2, sendList[0].Item1.Value);
        }

        [Test(Description = "Messages should be resend if delivery failed")]
        public void GetMessagesToSend_AfterSendAndFailedDelivery()
        {
            var mockMessage = CreateMessage();

            // Append a few messages [0, 1, 2].
            for (var i = 0; i < 3; ++i)
            {
                var res = buffer.AppendMessage(mockMessage);
                Assert.IsTrue(res);
            }

            // Verify the send list.
            var sendList = new List<(MessageID, SerializedEntityMessage)>();
            buffer.GetMessagesToSend(sendList);

            Assert.AreEqual(3, sendList.Count);
            for (var i = 0; i < sendList.Count; ++i)
            {
                Assert.AreEqual((ushort)(i), sendList[i].Item1.Value);
            }

            // Mark a few messages are sent, and verify the send list again.
            var sentMessages = new List<MessageID>(2)
            {
                sendList[0].Item1,
                sendList[1].Item1
            };
            buffer.OnMessagesSent(sentMessages);

            buffer.GetMessagesToSend(sendList);
            Assert.AreEqual(1, sendList.Count, "There should only 1 message left to be sent");
            Assert.AreEqual(2, sendList[0].Item1.Value);

            // Delivery of sent messages failed, re-check send list.
            buffer.OnMessagesDelivered(sentMessages, wasDelivered: false);

            buffer.GetMessagesToSend(sendList);
            Assert.AreEqual(3, sendList.Count, "There should 3 messages to be sent");
        }

        [Test(Description = "Test message ids sequence window being updated as messages are acked")]
        public void SequenceWindow()
        {
            var mockMessage = CreateMessage();

            // Fill the buffer [0, 1, ..., N].
            for (var i = 0; i < BufferSize; ++i)
            {
                var res = buffer.AppendMessage(mockMessage);
                Assert.IsTrue(res);
            }

            // Mark a few messages as delivered [0, 1, 2].
            var messages = new List<MessageID>(3);
            for (var i = 0; i < 3; ++i)
            {
                messages.Add(new MessageID((ushort)(i)));
            }

            buffer.OnMessagesDelivered(messages, wasDelivered: true);

            // Add more messages.
            while (buffer.AppendMessage(mockMessage)) { }

            var sendList = new List<(MessageID, SerializedEntityMessage)>();
            buffer.GetMessagesToSend(sendList);

            // Verify the ids sequence.
            Assert.AreEqual(BufferSize, sendList.Count);
            Assert.AreEqual(3, sendList[0].Item1.Value);
            Assert.AreEqual(130, sendList[^1].Item1.Value);
        }

        private SerializedEntityMessage CreateMessage() => new SerializedEntityMessage(
            new Entity(1, 0, false),
            new byte[16],
            32
        );
    }
}
