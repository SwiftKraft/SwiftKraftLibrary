// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using Brook;
    using Brook.Octet;
    using Common;
    using Connection;
    using Models;
    using Moq;
    using NUnit.Framework;
    using Serializers;
    using Tend.Client;
    using Tend.Models;
    using Coherence.Tests;

    public class BriskTests : CoherenceTest
    {
        [Test]
        public void Connect_ResendsConnectionRequest()
        {
            // Arrange
            var brisk = BriskTestUtils.GetTestBrisk(out var servicesMocks, out var transportMock);
            brisk.Connect(new EndpointData(), ConnectionType.Client);

            servicesMocks.SendTimer.Reset();
            servicesMocks.SendTimer.SetUpElapsed(brisk.SendInterval());

            var sentPackets = new List<IOutOctetStream>();

            transportMock.Setup(m => m.Send(It.IsAny<IOutOctetStream>()))
                .Callback((IOutOctetStream packet) => sentPackets.Add(packet));

            var updateCount = ReceiveMask.Range * 2;

            // Act
            for (var i = 0; i < updateCount; i++)
            {
                brisk.Update();
            }

            // Assert
            Assert.That(sentPackets.Count, Is.EqualTo(updateCount));

            for (var i = 0; i < updateCount; i++)
            {
                AssertOobPacket<ConnectRequest>(sentPackets[i]);
            }
        }

        [Test]
        public void Connect_NoHandshakeUntilOpened()
        {
            // Arrange
            var brisk = BriskTestUtils.GetTestBrisk(out _, out var transportMock);

            // Connected at least once
            brisk.Connect(new EndpointData(), ConnectionType.Client);
            brisk.Disconnect(ConnectionCloseReason.GracefulClose, false);

            // We want second connect to stop at the transport open phase
            transportMock.Setup(m =>
                    m.Open(It.IsAny<EndpointData>(), It.IsAny<ConnectionSettings>()))
                .Callback(() => { });

            brisk.Connect(new EndpointData(), ConnectionType.Client);

            transportMock.Invocations.Clear();

            // Act
            brisk.Update();

            // Assert
            transportMock.Verify(m => m.Send(It.IsAny<IOutOctetStream>()), Times.Never);
        }

        [Test]
        public void Connect_ChangesState()
        {
            // Arrange
            var brisk = BriskTestUtils.GetTestBrisk(out _, out var transportMock);
            var endpoint = new EndpointData
            {
                host = "test",
            };

            // Act
            brisk.Connect(endpoint, ConnectionType.Client);

            // Assert
            transportMock.Verify(t => t.Open(endpoint, It.IsAny<ConnectionSettings>()));
            Assert.That(brisk.State, Is.EqualTo(ConnectionState.Connecting));
        }

        [TestCase(ConnectionType.Client)]
        [TestCase(ConnectionType.Simulator)]
        public void Connect_SendsHandshakeAfterTransportOpens(ConnectionType connectionType)
        {
            // Arrange
            var brisk = BriskTestUtils.GetTestBrisk(out _, out var transportMock);

            IOutOctetStream sentHandshake = null;
            transportMock
                .Setup(m => m.Send(It.IsAny<IOutOctetStream>()))
                .Callback((IOutOctetStream stream) => sentHandshake = stream);

            var endpoint = new EndpointData
            {
                authToken = "testToken",
                roomSecret = "testSecret",
                uniqueRoomId = 100,
                schemaId = "testSchemaId",
            };

            // Act
            brisk.Connect(endpoint, connectionType);

            // Assert
            transportMock.Verify(m => m.Send(It.IsAny<IOutOctetStream>()), Times.Once);

            AssertOobPacket<ConnectRequest>(sentHandshake, req =>
            {
                Assert.That(req.Info.AuthToken, Is.EqualTo(endpoint.authToken));
                Assert.That(req.Info.RoomSecret, Is.EqualTo(endpoint.roomSecret));
                Assert.That(req.Info.RoomUid, Is.EqualTo(endpoint.uniqueRoomId));
                Assert.That(req.Info.SchemaId, Is.EqualTo(endpoint.schemaId));
                Assert.That(req.Info.IsSimulator, Is.EqualTo(connectionType == ConnectionType.Simulator));
            });
        }

        [TestCase(ConnectionCloseReason.GracefulClose)]
        [TestCase(ConnectionCloseReason.InvalidChallenge)]
        public void DisconnectRequest_EmitsError(ConnectionCloseReason disconnectReason)
        {
            // Arrange
            var brisk = BriskTestUtils.GetTestBrisk(out _, out var transportMock);

            ConnectionException connectionException = null;
            brisk.OnError += exception => connectionException = exception;

            ConnectionCloseReason? closeReason = null;
            brisk.OnDisconnect += reason => closeReason = reason;

            var disconnect = new DisconnectRequest(disconnectReason, CustomPayload.Empty);

            transportMock.Setup(m => m.Receive(It.IsAny<List<(IInOctetStream, IPEndPoint)>>()))
                .Callback((List<(IInOctetStream, IPEndPoint)> buffer) =>
                {
                    buffer.Add((BriskTestUtils.CreateIncomingOobPacket(disconnect), null));
                });

            brisk.Connect(new EndpointData(), ConnectionType.Client);

            // Act
            brisk.Receive(new List<InPacket>());

            // Assert
            Assert.That(brisk.State,
                Is.EqualTo(ConnectionState.Connecting)); // Brisk waits for higher layer to call 'Disconnect'
            Assert.That(closeReason, Is.Null, $"Did not expect {nameof(brisk.OnDisconnect)}");

            var deniedException = connectionException as ConnectionDeniedException;
            Assert.That(deniedException, Is.Not.Null);
            Assert.That(deniedException.CloseReason, Is.EqualTo(disconnectReason));
        }

        [TestCase(ConnectionCloseReason.GracefulClose)]
        [TestCase(ConnectionCloseReason.InvalidChallenge)]
        public void Disconnect_WorksWhenNotConnected(ConnectionCloseReason disconnectReason)
        {
            // Arrange
            var brisk = BriskTestUtils.GetTestBrisk(out _, out _);

            ConnectionException connectionException = null;
            brisk.OnError += exception => connectionException = exception;

            ConnectionCloseReason? closeReason = null;
            brisk.OnDisconnect += reason => closeReason = reason;

            brisk.Connect(new EndpointData(), ConnectionType.Client);

            // Act
            brisk.Disconnect(disconnectReason, true);

            // Assert
            Assert.That(brisk.State, Is.EqualTo(ConnectionState.Disconnected));
            Assert.That(closeReason, Is.Null, $"Did not expect {nameof(brisk.OnDisconnect)}");
            Assert.That(connectionException, Is.Null, $"Did not expect {nameof(brisk.OnError)}");
        }

        [TestCase(ConnectionCloseReason.GracefulClose)]
        [TestCase(ConnectionCloseReason.InvalidChallenge)]
        public void Disconnect_WorksWhenConnected(ConnectionCloseReason disconnectReason)
        {
            // Arrange
            var brisk = BriskTestUtils.GetTestBrisk(out _, out var transportMock);

            ConnectionException connectionException = null;
            brisk.OnError += exception => connectionException = exception;

            ConnectionCloseReason? closeReason = null;
            brisk.OnDisconnect += reason => closeReason = reason;

            BriskTestUtils.ConnectBrisk(brisk, transportMock);

            // Act
            brisk.Disconnect(disconnectReason, true);

            // Assert
            Assert.That(brisk.State, Is.EqualTo(ConnectionState.Disconnected));
            Assert.That(closeReason, Is.EqualTo(disconnectReason));
            Assert.That(connectionException, Is.Null, $"Did not expect {nameof(brisk.OnError)}");
        }

        [Test]
        public void Disconnect_HandlesTransportExceptions()
        {
            // Arrange
            var brisk = BriskTestUtils.GetTestBrisk(out _, out var transportMock);

            ConnectionException connectionException = null;
            brisk.OnError += exception => connectionException = exception;

            BriskTestUtils.ConnectBrisk(brisk, transportMock);

            transportMock.Setup(m => m.Send(It.IsAny<IOutOctetStream>()))
                .Throws(new Exception("Transport error"));

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                brisk.Disconnect(ConnectionCloseReason.GracefulClose, false);
            });

            Assert.That(brisk.State, Is.EqualTo(ConnectionState.Disconnected));
            Assert.That(connectionException, Is.Null);
        }

        [TestCase(1)]
        [TestCase(20)]
        [TestCase(60)]
        public void ResendsLastReliablePacketWhenTendIsBlocked(byte sendFrequency)
        {
            // Arrange
            var brisk = BriskTestUtils.GetTestBrisk(out var servicesMocks, out var transportMock);

            var sentPackets = new List<IOutOctetStream>();

            transportMock.Setup(m => m.Send(It.IsAny<IOutOctetStream>()))
                .Callback((IOutOctetStream packet) => sentPackets.Add(packet));

            BriskTestUtils.ConnectBrisk(brisk, transportMock, sendFrequency: sendFrequency);

            servicesMocks.SendTimer.SetUpElapsed(brisk.SendInterval());

            OutPacket lastSentPacket = default;

            var sent = 0;

            // Reach tend window end
            while (brisk.CanSend)
            {
                lastSentPacket = brisk.CreatePacket(true);
                brisk.Send(lastSentPacket);

                servicesMocks.SendTimer.SetUpElapsed(brisk.SendInterval());

                if (sent++ > 1000) // Just to prevent infinite loop in case of a bug
                {
                    throw new AssertionException("Expected to reach tend window end");
                }
            }

            var lastSentData = lastSentPacket.Stream.Octets.ToArray();

            // Simulate stream reuse
            lastSentPacket.Stream.Seek(0);
            lastSentPacket.Stream.WriteOctets(new byte[] { 1, 3, 3, 7, 6, 9 });

            sentPackets.Clear();

            // Act & Assert
            servicesMocks.SendTimer.SetUpElapsed(brisk.SendInterval() - TimeSpan.FromMilliseconds(1f));

            brisk.Update();

            Assert.That(sentPackets.Count, Is.EqualTo(0));

            servicesMocks.SendTimer.SetUpElapsed(brisk.SendInterval());

            brisk.Update();

            Assert.That(sentPackets.Count, Is.EqualTo(1));
            Assert.That(sentPackets[0].Octets.SequenceEqual(lastSentData));

            servicesMocks.SendTimer.SetUpElapsed(brisk.SendInterval());

            brisk.Update();

            Assert.That(sentPackets.Count, Is.EqualTo(2));
            Assert.That(sentPackets[1].Octets.SequenceEqual(lastSentData));
        }

        [TestCase(1)]
        [TestCase(20)]
        [TestCase(60)]
        public void CanSend_HonoursSendFrequency(byte sendFrequency)
        {
            // Arrange
            var brisk = BriskTestUtils.GetTestBrisk(out var servicesMocks, out var transportMock);

            var sentPackets = new List<IOutOctetStream>();

            transportMock.Setup(m => m.Send(It.IsAny<IOutOctetStream>()))
                .Callback((IOutOctetStream packet) => sentPackets.Add(packet));

            BriskTestUtils.ConnectBrisk(brisk, transportMock, sendFrequency: sendFrequency);

            servicesMocks.SendTimer.SetUpElapsed(brisk.SendInterval());

            // Act & Assert
            Assert.That(brisk.CanSend, Is.True);

            brisk.Send(brisk.CreatePacket(true));

            Assert.That(brisk.CanSend, Is.False);

            servicesMocks.SendTimer.SetUpElapsed(brisk.SendInterval() - TimeSpan.FromTicks(1));

            Assert.That(brisk.CanSend, Is.False);

            servicesMocks.SendTimer.SetUpElapsed(brisk.SendInterval());

            Assert.That(brisk.CanSend, Is.True);
        }

        [Test]
        public void ConnectResponse_EstablishesConnection()
        {
            // Arrange
            var brisk = BriskTestUtils.GetTestBrisk(out _, out var transportMock);

            ConnectResponse receivedResponse = null;
            brisk.OnConnect += response => receivedResponse = response;

            // Act
            var connectResponse = BriskTestUtils.ConnectBrisk(brisk, transportMock);

            // Assert
            Assert.That(brisk.State, Is.EqualTo(ConnectionState.Connected));
            Assert.That(receivedResponse, Is.Not.Null);
            AssertConnectResponse(brisk, receivedResponse, connectResponse);
        }

        [Test]
        public void Connect_WorksAfterDisconnect()
        {
            // Arrange
            var brisk = BriskTestUtils.GetTestBrisk(out _, out var transportMock);

            ConnectResponse receivedResponse = null;
            brisk.OnConnect += response => receivedResponse = response;

            ConnectionException connectionException = null;
            brisk.OnError += exception => connectionException = exception;

            ConnectionCloseReason? closeReason = null;
            brisk.OnDisconnect += reason => closeReason = reason;

            // Act
            BriskTestUtils.ConnectBrisk(brisk, transportMock);

            brisk.Disconnect(ConnectionCloseReason.GracefulClose, true);
            Assert.That(brisk.State, Is.EqualTo(ConnectionState.Disconnected));

            var connectResponse = BriskTestUtils.ConnectBrisk(brisk, transportMock, new ClientID(20),
                30, 1000);

            // Assert
            Assert.That(brisk.State, Is.EqualTo(ConnectionState.Connected));
            Assert.That(connectionException, Is.Null, $"Did not expect {nameof(brisk.OnError)}");
            AssertConnectResponse(brisk, receivedResponse, connectResponse);
        }

        [Test]
        public void OnDeliverInfo_Works()
        {
            // Arrange
            var brisk = BriskTestUtils.GetTestBrisk(out _, out var transportMock);

            BriskTestUtils.ConnectBrisk(brisk, transportMock);

            var deliveries = new List<DeliveryInfo>();
            brisk.OnDeliveryInfo += info => deliveries.Add(info);

            // Act
            brisk.Send(brisk.CreatePacket(true));
            brisk.Send(brisk.CreatePacket(true));
            brisk.Send(brisk.CreatePacket(true));

            var packet1 =
                BriskTestUtils.CreateIncomingReliablePacket(new SequenceId(0), new SequenceId(0), new ReceiveMask(0b1));
            var packet2 =
                BriskTestUtils.CreateIncomingReliablePacket(new SequenceId(1), new SequenceId(2),
                    new ReceiveMask(0b101));
            transportMock.Setup(m => m.Receive(It.IsAny<List<(IInOctetStream, IPEndPoint)>>()))
                .Callback((List<(IInOctetStream, IPEndPoint)> buffer) =>
                {
                    buffer.Add((packet1, null));
                    buffer.Add((packet2, null));
                });

            brisk.Receive(new List<InPacket>());

            // Assert
            Assert.That(deliveries.Count, Is.EqualTo(3));

            Assert.That(deliveries[0].WasDelivered, Is.True);
            Assert.That(deliveries[0].PacketSequenceId.Value, Is.EqualTo(0));

            Assert.That(deliveries[1].WasDelivered, Is.False);
            Assert.That(deliveries[1].PacketSequenceId.Value, Is.EqualTo(1));

            Assert.That(deliveries[2].WasDelivered, Is.True);
            Assert.That(deliveries[2].PacketSequenceId.Value, Is.EqualTo(2));
        }

        [Test]
        public void BriskHeaderSize_Matches()
        {
            // Init test data
            var briskHeader = new BriskHeader(Mode.NormalMode);
            var stream = new OutOctetStream(2048);

            // Serialize header
            briskHeader.Serialize(stream);
            var octets = stream.Close();

            // Assert the packet size is the expected header
            Assert.AreEqual(octets.Count, sizeof(byte));
        }

        [Test]
        public void DuplicateNormalPacket_NotProcessed()
        {
            // Arrange
            var brisk = BriskTestUtils.GetTestBrisk(out _, out var transportMock);

            BriskTestUtils.ConnectBrisk(brisk, transportMock);

            // Act
            var packet1 = BriskTestUtils.CreateIncomingReliablePacket(new SequenceId(0),
                new SequenceId(SequenceId.Max.Value), new ReceiveMask());
            var packet2 = BriskTestUtils.CreateIncomingReliablePacket(new SequenceId(1),
                new SequenceId(SequenceId.Max.Value), new ReceiveMask());
            var packet3 = BriskTestUtils.CreateIncomingReliablePacket(new SequenceId(1),
                new SequenceId(SequenceId.Max.Value), new ReceiveMask());
            transportMock.Setup(m => m.Receive(It.IsAny<List<(IInOctetStream, IPEndPoint)>>()))
                .Callback((List<(IInOctetStream, IPEndPoint)> buffer) =>
                {
                    buffer.Add((packet1, null));
                    buffer.Add((packet2, null));
                    buffer.Add((packet3, null)); // Duplicate should be dropped
                });

            var receivedPackets = new List<InPacket>();
            brisk.Receive(receivedPackets);

            // Assert
            Assert.That(receivedPackets.Count, Is.EqualTo(2), "Expected to receive two packets");
        }

        [Test]
        public void DuplicateOOBPacket_NotProcessed()
        {
            // Arrange
            const byte frequency1 = 1;
            const byte frequency2 = 2;
            const byte frequency3 = 3;
            var brisk = BriskTestUtils.GetTestBrisk(out var _, out var transportMock);

            BriskTestUtils.ConnectBrisk(brisk, transportMock);

            var packet1 = BriskTestUtils.CreateReliableIncomingOobPacket(new ChangeSendFrequencyRequest(frequency1),
                new SequenceId(0), new SequenceId(SequenceId.Max.Value), new ReceiveMask());
            var packet2 = BriskTestUtils.CreateReliableIncomingOobPacket(new ChangeSendFrequencyRequest(frequency2),
                new SequenceId(1), new SequenceId(SequenceId.Max.Value), new ReceiveMask());
            var packet3 = BriskTestUtils.CreateReliableIncomingOobPacket(new ChangeSendFrequencyRequest(frequency3),
                new SequenceId(1), new SequenceId(SequenceId.Max.Value), new ReceiveMask());
            transportMock.Setup(m => m.Receive(It.IsAny<List<(IInOctetStream, IPEndPoint)>>()))
                .Callback((List<(IInOctetStream, IPEndPoint)> buffer) =>
                {
                    buffer.Add((packet1, null));
                    buffer.Add((packet2, null));
                    buffer.Add((packet3, null)); // Duplicate should be dropped
                });

            brisk.Receive(new List<InPacket>());

            // Act & Assert
            Assert.That(brisk.SendFrequency, Is.EqualTo(frequency2),
                "Expected the first two frequency requests to be processed");
        }

        [Test]
        public void Reliable_Packets_NotProcessed_If_Not_Connected()
        {
            // Arrange
            var brisk = BriskTestUtils.GetTestBrisk(out _, out var transportMock);

            // Send connect but not fully connected yet.
            brisk.Connect(new EndpointData(), ConnectionType.Client, null);

            // Act - receive some reliable packet types.
            var packet1 = BriskTestUtils.CreateIncomingReliablePacket(new SequenceId(0),
                new SequenceId(SequenceId.Max.Value), new ReceiveMask());
            var packet2 = BriskTestUtils.CreateReliableIncomingOobPacket(new ChangeSendFrequencyRequest(60),
                new SequenceId(1), new SequenceId(SequenceId.Max.Value), new ReceiveMask());
            transportMock.Setup(m => m.Receive(It.IsAny<List<(IInOctetStream, IPEndPoint)>>()))
                .Callback((List<(IInOctetStream, IPEndPoint)> buffer) =>
                {
                    buffer.Add((packet1, null));
                    buffer.Add((packet2, null));
                });

            var receivedPackets = new List<InPacket>();
            brisk.Receive(receivedPackets);

            // Assert - no reliable packets get through because we're not connected yet.
            Assert.That(receivedPackets.Count, Is.EqualTo(0), "Expected to receive no packets");
        }

        private static void AssertConnectResponse(Brisk brisk, ConnectResponse receivedResponse,
            ConnectResponse originalResponse)
        {
            Assert.That(receivedResponse.ClientID, Is.EqualTo(originalResponse.ClientID).And.EqualTo(brisk.ClientID));
            Assert.That(receivedResponse.SendFrequency,
                Is.EqualTo(originalResponse.SendFrequency).And.EqualTo(brisk.SendFrequency));
            Assert.That(receivedResponse.SimulationFrame, Is.EqualTo(originalResponse.SimulationFrame));
        }

        private static T AssertOobPacket<T>(IOutOctetStream packet, Action<T> assert = null) where T : IOobMessage
        {
            Assert.That(packet, Is.Not.Null);

            var inStream = new InOctetStream(packet.Close().ToArray());

            Tend.DeserializeHeader(inStream);
            BriskHeader.Deserialize(inStream);
            var oobMessage = BriskSerializer.DeserializeOobMessage(inStream, ProtocolDef.Version.CurrentVersion);

            Assert.That(oobMessage, Is.TypeOf<T>());
            assert?.Invoke((T)oobMessage);

            return (T)oobMessage;
        }
    }
}
