// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests.Relay
{
    using System;
    using System.Collections.Generic;
    using Coherence.Tests;
    using Connection;
    using Log;
    using Moq;
    using NUnit.Framework;
    using Toolkit.Relay;
    using Transport;

    public class CoherenceRelayManagerTests : CoherenceTest
    {
        private Mock<IRelay> relayMock;
        private Mock<IRelayConnection> relayConnectionMock;
        private Mock<ITransport> transportMock;

        private CoherenceRelayManager relayManager;

        private RelayConnectionHolder CreateRelayConnectionHolder(IRelayConnection connection) =>
           new (new EndpointData(), connection, logger, transportMock.Object);

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            relayMock = new Mock<IRelay>();
            relayMock.SetupProperty(r => r.RelayManager);

            relayConnectionMock = new Mock<IRelayConnection>();
            transportMock = new Mock<ITransport>();
            transportMock
                .SetupGet(t => t.State)
                .Returns(TransportState.Open);

            relayManager = new CoherenceRelayManager(this.CreateRelayConnectionHolder, logger);
            relayManager.SetRelay(relayMock.Object);

            Assert.That(relayMock.Object.RelayManager, Is.EqualTo(relayManager));
        }

        [TestCase(false)]
        [TestCase(true)]
        [Description("Tests that the relay manager opens the relay.")]
        public void TestOpen(bool withException)
        {
            if (withException)
            {
                relayMock.Setup(r => r.Open()).Throws(new Exception());
            }

            relayManager.Open(new EndpointData());

            relayMock.Verify(r => r.Open(), Times.Once);

            if (withException)
            {
                Assert.That(logger.GetCountForErrorID(Error.ToolkitRelayOpenException), Is.EqualTo(1));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        [Description("Tests that the relay manager closes the relay.")]
        public void TestClose(bool withException)
        {
            if (withException)
            {
                relayMock.Setup(r => r.Close()).Throws(new Exception());
            }

            relayManager.Close();
            relayMock.Verify(r => r.Close(), Times.Once);

            if (withException)
            {
                Assert.That(logger.GetCountForErrorID(Error.ToolkitRelayCloseException), Is.EqualTo(1));
            }
        }

        [Test(Description = "Tests the full relay connection life cycle: open, update and close.")]
        public void TestRelayConnection_LifeCycle()
        {
            relayManager.Open(new EndpointData());
            relayMock.Verify(r => r.Open(), Times.Once);

            relayManager.OpenRelayConnection(relayConnectionMock.Object);
            relayConnectionMock.Verify(r => r.OnConnectionOpened(), Times.Once);

            relayManager.Update();
            relayMock.Verify(r => r.Update(), Times.Once);
            relayConnectionMock.Verify(r => r.ReceiveMessagesFromClient(It.IsAny<List<ArraySegment<byte>>>()), Times.Once);

            relayManager.Close();
            relayMock.Verify(r => r.Close(), Times.Once);
            relayConnectionMock.Verify(r => r.OnConnectionClosed(), Times.Once);
        }

        [Test(Description = "Test that trying to close a not registered relay connection logs an error.")]
        public void TestRelayConnection_CloseNotRegistered()
        {
            relayManager.Open(new EndpointData());
            relayMock.Verify(r => r.Open(), Times.Once);

            relayManager.CloseAndRemoveRelayConnection(relayConnectionMock.Object);

            Assert.That(logger.GetCountForErrorID(Error.ToolkitRelayRemoveNotFound), Is.EqualTo(1));
        }
    }
}
