// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brook.Tests
{
    using System;
    using Coherence.Tests;
    using NUnit.Framework;

    public class MessageIDTests : CoherenceTest
    {
        [Test(Description = "Test id value wrapping when max value is reached")]
        public void WrappingSequence()
        {
            var maxValue = MessageID.MaxValue;

            Assert.Throws<ArgumentException>(() =>
            {
                var _ = new MessageID((ushort)(maxValue + 10));
            });

            var id = new MessageID(27);
            Assert.AreEqual(28, id.Next().Value);
            Assert.AreEqual(42, id.Advance(15).Value);
            Assert.AreEqual(25, id.Distance(id.Advance(25)));

            id = new MessageID(maxValue);
            Assert.AreEqual(0, id.Next().Value);
            Assert.AreEqual(14, id.Advance(15).Value);
            Assert.AreEqual(25, id.Distance(id.Advance(25)));
        }
    }
}
