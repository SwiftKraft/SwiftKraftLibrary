// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport.Tests
{
    using System;
    using Common.Pooling;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    public class PooledStreamTests
    {
        [Test]
        [Description("Verifies that the stream throws an exception when trying to write more octets than the capacity.")]
        public void PooledOutOctetStream_IsNotExpandable()
        {
            // Arrange
            var streamPool = new Mock<IPool<PooledOutOctetStream>>().Object;
            var stream = new PooledOutOctetStream(streamPool, 1);
            stream.WriteOctet(1);

            // Act & Assert
            Assert.That(() => stream.WriteOctet(1),
                Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        [Description("Verifies that the stream can be reset and written to again.")]
        public void PooledOutOctetStream_CanBeReset()
        {
            // Arrange
            var streamPool = new Mock<IPool<PooledOutOctetStream>>().Object;
            var stream = new PooledOutOctetStream(streamPool, 1);

            // Act
            stream.WriteOctet(1);
            Assert.That(stream.Position, Is.EqualTo(1));
            stream.Return();

            // Assert
            Assert.That(stream.Position, Is.EqualTo(0));
            Assert.That(() => stream.WriteOctet(1), Throws.Nothing);
        }

        [Test]
        [Description("Verifies that the stream can be resized to bigger capacity.")]
        public void PooledOutOctetStream_CanBeResizedToBigger()
        {
            // Arrange
            var streamPool = new Mock<IPool<PooledOutOctetStream>>().Object;
            var stream = new PooledOutOctetStream(streamPool, 1);

            // Act
            stream.ResizeAndReset(2);

            // Assert
            Assert.That(stream.Position, Is.EqualTo(0));
            Assert.That(() =>
            {
                stream.WriteOctet(1);
                stream.WriteOctet(2);
            }, Throws.Nothing);
        }

        [Test]
        [Description("Verifies that the stream can be resized to smaller capacity.")]
        public void PooledOutOctetStream_CanBeResizedToSmaller()
        {
            // Arrange
            var streamPool = new Mock<IPool<PooledOutOctetStream>>().Object;
            var stream = new PooledOutOctetStream(streamPool, 2);
            stream.WriteOctet(1);
            stream.WriteOctet(2);

            // Act
            stream.ResizeAndReset(1);

            // Assert
            Assert.That(stream.Position, Is.EqualTo(0));
            Assert.That(() =>
            {
                stream.WriteOctet(1);
                stream.WriteOctet(2);
            }, Throws.TypeOf<NotSupportedException>());
        }
    }
}
