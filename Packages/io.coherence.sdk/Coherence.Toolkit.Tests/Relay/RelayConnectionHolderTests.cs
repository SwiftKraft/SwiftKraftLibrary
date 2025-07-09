// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests.Relay
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using Brook;
    using Coherence.Tests;
    using Common;
    using Connection;
    using Log;
    using Moq;
    using NUnit.Framework;
    using Toolkit.Relay;
    using Transport;

    public class RelayConnectionHolderTests : CoherenceTest
    {
        private Mock<IRelayConnection> relayConnectionMock;
        private Mock<ITransport> transportMock;

        private int subscribedToTransportError = 0;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            relayConnectionMock = new Mock<IRelayConnection>();

            transportMock = new Mock<ITransport>();
            transportMock
                .SetupGet(t => t.State)
                .Returns(TransportState.Open);
            transportMock
                .SetupAdd(t => t.OnError += It.IsAny<Action<ConnectionException>>())
                .Callback(() => subscribedToTransportError++ );
            transportMock
                .SetupRemove(t => t.OnError -= It.IsAny<Action<ConnectionException>>())
                .Callback(() => subscribedToTransportError--);
        }

        [Test(Description = "Test that the connection holder opens a relay connection")]
        public void TestOpenRelayConnection()
        {
            var _ = new RelayConnectionHolder(new EndpointData(), relayConnectionMock.Object, logger, transportMock.Object);

            transportMock.Verify(t => t.Open(It.IsAny<EndpointData>(), It.IsAny<ConnectionSettings>()), Times.Once);
            relayConnectionMock.Verify(r => r.OnConnectionOpened(), Times.Once);

            Assert.That(subscribedToTransportError, Is.EqualTo(1));
        }

        [Test(Description = "Test that the connection holder closes a relay connection")]
        public void TestCloseRelayConnection()
        {
            var connectionHolder = new RelayConnectionHolder(new EndpointData(), relayConnectionMock.Object, logger, transportMock.Object);
            connectionHolder.Close();

            transportMock.Verify(t => t.Close(), Times.Once);
            relayConnectionMock.Verify(r => r.OnConnectionClosed(), Times.Once);

            Assert.That(subscribedToTransportError, Is.EqualTo(0));
        }

        [Test(Description = "Test that transport error is propagated to the connection holder")]
        public void TestTransportErrorDelegate()
        {
            var errorRaised = false;

            var connectionHolder = new RelayConnectionHolder(new EndpointData(), relayConnectionMock.Object, logger, transportMock.Object);
            connectionHolder.OnError += (r, e) =>
            {
                errorRaised = true;

                Assert.That(r, Is.EqualTo(relayConnectionMock.Object));
                Assert.That(e.Message, Is.EqualTo("Test"));
            };

            transportMock.Raise(t => t.OnError += null, new ConnectionException("Test"));

            Assert.That(errorRaised, Is.True);
        }

        [Test(Description = "Verifies that an error is logged when updating the connection holder if the transport is closed")]
        public void TestUpdate_WithClosedTransport()
        {
            transportMock.SetupGet(t => t.State).Returns(TransportState.Closed);

            var connectionHolder = new RelayConnectionHolder(new EndpointData(), relayConnectionMock.Object, logger, transportMock.Object);
            connectionHolder.Update();

            Assert.That(logger.GetCountForErrorID(Error.ToolkitRelayUpdateFailed), Is.EqualTo(1));
        }

        [Test(Description = "Verifies that the connection holder relays messages from the client to the replication server")]
        public void TestUpdate_PacketRelay()
        {
            relayConnectionMock
                .Setup(r => r.ReceiveMessagesFromClient(It.IsAny<List<ArraySegment<byte>>>()))
                .Callback((List<ArraySegment<byte>> buffer) =>
                {
                    buffer.Add(new ArraySegment<byte>(new byte[] { 1, 2, 3 }));
                    buffer.Add(new ArraySegment<byte>(new byte[] { 4, 5, 6 }));
                });

            var connectionHolder = new RelayConnectionHolder(new EndpointData(), relayConnectionMock.Object, logger, transportMock.Object);
            connectionHolder.Update();

            transportMock.Verify(t => t.Send(It.IsAny<IOutOctetStream>()), Times.Exactly(2));
        }

        [Test(Description = "Verifies that an error is logged when an exception is thrown during the update")]
        public void TestUpdate_ExceptionHandling()
        {
            var errorRaised = false;

            relayConnectionMock
                .Setup(r => r.ReceiveMessagesFromClient(It.IsAny<List<ArraySegment<byte>>>()))
                .Throws(new Exception("Test"));

            var connectionHolder = new RelayConnectionHolder(new EndpointData(), relayConnectionMock.Object, logger, transportMock.Object);
            connectionHolder.OnError += (r, e) =>
            {
                errorRaised = true;

                Assert.That(r, Is.EqualTo(relayConnectionMock.Object));
                Assert.That(e.Message, Is.EqualTo("Test: Update error"));
            };

            connectionHolder.Update();

            Assert.That(errorRaised, Is.True);
            Assert.That(logger.GetCountForErrorID(Error.ToolkitRelayUpdateException), Is.EqualTo(1));
        }


        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            subscribedToTransportError = 0;
            relayConnectionMock.Reset();
            transportMock.Reset();
        }
    }
}
