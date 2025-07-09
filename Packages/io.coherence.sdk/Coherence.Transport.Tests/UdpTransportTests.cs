// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport.Tests
{
    using Brook;
    using Common;
    using Connection;
    using Log;
    using Moq;
    using NUnit.Framework;
    using Stats;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using Coherence.Tests;

    public class UdpTransportTests : CoherenceTest
    {
        [Test]
        public void Receive_DoesntThrowTimeoutExceptionInitially()
        {
            // Arrange
            var stats = new Mock<IStats>();
            var dateTimeProvider = new Mock<IDateTimeProvider>();

            UdpTransport udpTransport = new UdpTransport(stats.Object, new UnityLogger(), dateTimeProvider.Object);

            var date = DateTime.Parse("2020-01-01T15:00:00Z");
            var connectionSettings = ConnectionSettings.Default;

            dateTimeProvider
                .Setup(m => m.UtcNow)
                .Returns(date);

            udpTransport.Open(default, connectionSettings);

            ConnectionException exception = null;
            udpTransport.OnError += exc => exception = exc;

            // Act
            udpTransport.Receive(new List<(IInOctetStream, IPEndPoint)>());

            // Assert
            Assert.That(exception, Is.Null);
        }

        [Test]
        public void Receive_ThrowsTimeoutException()
        {
            // Arrange
            var stats = new Mock<IStats>();
            var dateTimeProvider = new Mock<IDateTimeProvider>();

            UdpTransport udpTransport = new UdpTransport(stats.Object, new UnityLogger(), dateTimeProvider.Object);

            var date = DateTime.Parse("2020-01-01T15:00:00Z");
            var connectionSettings = ConnectionSettings.Default;

            dateTimeProvider
                .Setup(m => m.UtcNow)
                .Returns(date);

            udpTransport.Open(default, connectionSettings);

            dateTimeProvider
                .Setup(m => m.UtcNow)
                .Returns(date + connectionSettings.DisconnectTimeout);

            ConnectionException exception = null;
            udpTransport.OnError += exc => exception = exc;

            // Act
            udpTransport.Receive(new List<(IInOctetStream, IPEndPoint)>());

            // Assert
            Assert.That(exception, Is.TypeOf<ConnectionTimeoutException>());
        }
    }
}
