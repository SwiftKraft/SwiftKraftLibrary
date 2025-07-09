// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Tests
{
    using System;
    using Brook;
    using Coherence.Tests;
    using NUnit.Framework;

    public class SequenceBufferTests : CoherenceTest
    {
        private class TestEntry
        {
            public MessageID Id;
        }

        private class TestBuffer : SequenceBuffer<TestEntry>
        {
            public new int Size => base.Size;
            public TestBuffer(int size): base(size) {}

            public new void Insert(MessageID id, TestEntry data) => base.Insert(id, data);
            public new void Remove(MessageID id) => base.Remove(id);
            public new TestEntry Find(MessageID id) => base.Find(id);
        }

        private TestBuffer buffer;

        [Test]
        public void BufferSize_PowerOfTwo()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                buffer = new TestBuffer(437);
            });

            Assert.DoesNotThrow(() =>
            {
                buffer = new TestBuffer(512);
            });
        }

        [Test]
        public void BasicOperations()
        {
            buffer = new TestBuffer(128);
            Assert.AreEqual(128, buffer.Size);

            var id1 = new MessageID(7);
            var id2 = new MessageID(7+128);

            buffer.Insert(id1, new TestEntry()
            {
                Id = id1,
            });

            Assert.AreEqual(id1, buffer.Find(id1).Id);
            Assert.AreEqual(id1, buffer.Find(id2).Id);

            buffer.Remove(id1);

            Assert.Null(buffer.Find(id1));
            Assert.Null(buffer.Find(id2));
        }
    }
}
