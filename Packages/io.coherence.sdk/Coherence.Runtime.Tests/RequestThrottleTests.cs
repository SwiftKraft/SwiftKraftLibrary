// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System;
    using Coherence.Tests;
    using Common;
    using Moq;
    using NUnit.Framework;

    public class RequestThrottleTests : CoherenceTest
    {
        private Mock<IDateTimeProvider> dateTimeProviderMock;

        private RequestThrottle CreateRequestThrottle(TimeSpan requestInterval) =>
           new RequestThrottle(requestInterval, dateTimeProviderMock.Object);

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            dateTimeProviderMock = new Mock<IDateTimeProvider>();
            dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        }

        [Test]
        public void RequestCooldown_ReturnsZero_WhenNoRequestHasBeenMade()
        {
            // Arrange
            var requestThrottle = CreateRequestThrottle(TimeSpan.FromSeconds(1));

            // Act
            var cooldown = requestThrottle.RequestCooldown("basePath", "method");

            // Assert
            Assert.That(cooldown, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void RequestCooldown_ReturnsZero_WhenRequestIsOlderThanInterval()
        {
            // Arrange
            var requestThrottle = CreateRequestThrottle(TimeSpan.FromSeconds(1.0));

            var ts = new DateTime(2024, 1, 1, 12, 1, 0);
            dateTimeProviderMock.Setup(m => m.UtcNow).Returns(ts);

            // Act
            requestThrottle.HandleTooManyRequests("basePath", "method", "requestName");

            dateTimeProviderMock.Setup(m => m.UtcNow).Returns(ts.AddSeconds(2.0));
            var cooldown = requestThrottle.RequestCooldown("basePath", "method");

            // Assert
            Assert.That(cooldown, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void RequestCooldown_ReturnsCooldown_WhenRequestIsRecent()
        {
            // Arrange
            var requestThrottle = CreateRequestThrottle(TimeSpan.FromSeconds(1));

            var ts = new DateTime(2024, 1, 1, 12, 1, 0);
            dateTimeProviderMock.Setup(m => m.UtcNow).Returns(ts);

            // Act
            requestThrottle.HandleTooManyRequests("basePath", "method", "requestName");

            dateTimeProviderMock.Setup(m => m.UtcNow).Returns(ts.AddSeconds(0.5));

            var cooldown = requestThrottle.RequestCooldown("basePath", "method");

            // Assert
            Assert.That(cooldown, Is.EqualTo(TimeSpan.FromSeconds(0.5)));
        }

        [Test]
        public void HandleTooManyRequests_ReturnsFalse_WhenNoRequestHasBeenMade()
        {
            // Arrange
            var requestThrottle = CreateRequestThrottle(TimeSpan.FromSeconds(1));

            // Act
            var result = requestThrottle.HandleTooManyRequests("basePath", "method", "requestName");

            // Assert
            Assert.False(result);
        }

        [Test]
        public void HandleTooManyRequests_ReturnsTrue_WhenRequestIsRecent()
        {
            // Arrange
            var requestThrottle = CreateRequestThrottle(TimeSpan.FromSeconds(1));
            requestThrottle.HandleTooManyRequests("basePath", "method", "requestName");

            // Act
            var result = requestThrottle.HandleTooManyRequests("basePath", "method", "requestName");

            // Assert
            Assert.True(result);
        }

        [Test]
        public void HandleTooManyRequests_ReturnsFalse_WhenRequestIsNotRecent()
        {
            // Arrange
            var requestThrottle = CreateRequestThrottle(TimeSpan.FromSeconds(1.0));

            var ts = new DateTime(2024, 1, 1, 12, 1, 0);
            dateTimeProviderMock.Setup(m => m.UtcNow).Returns(ts);

            // Act
            requestThrottle.HandleTooManyRequests("basePath", "method", "requestName");

            dateTimeProviderMock.Setup(m => m.UtcNow).Returns(ts.AddSeconds(2.0));
            var result = requestThrottle.HandleTooManyRequests("basePath", "method", "requestName");

            // Assert
            Assert.False(result);
        }
    }
}
