// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Tests
{
    using Brook;
    using Brook.Octet;
    using Common;
    using Connection;
    using Log;
    using Models;
    using Moq;
    using NUnit.Framework;
    using Serializers;
    using System.Collections.Generic;
    using System.Net;
    using System;
    using Tend.Client;
    using Tend.Models;
    using Transport;

    internal static class BriskTestUtils
    {
        public static Brisk GetTestBrisk(
            out BriskServicesMocks servicesMocks,
            out Mock<ITransport> transportMock)
        {
            servicesMocks = new BriskServicesMocks();

            Brisk brisk = new Brisk(Log.GetLogger<Brisk>(), servicesMocks.GetServices());

            transportMock = new Mock<ITransport>();
            transportMock.SetupGet(m => m.CanSend).Returns(true);

            SetUpTransportOpenAndClose(transportMock);

            servicesMocks.TransportMock = transportMock;

            return brisk;
        }

        public static void SetUpTransportOpenAndClose(Mock<ITransport> transportMock)
        {
            Mock<ITransport> tportMock = transportMock;

            transportMock.Setup(m =>
                    m.Open(It.IsAny<EndpointData>(), It.IsAny<ConnectionSettings>()))
                .Callback(() =>
                {
                    tportMock.SetupGet(m => m.State).Returns(TransportState.Open);
                    tportMock.Raise(m => m.OnOpen += () => { });
                });

            transportMock.Setup(m => m.Close())
                .Callback(() => { tportMock.SetupGet(m => m.State).Returns(TransportState.Closed); });
        }

        public static IInOctetStream CreateIncomingOobPacket<T>(T oob, TendHeader header = default) where T : IOobMessage
        {
            var outStream = new OutOctetStream(2048);

            Tend.SerializeHeader(outStream, header);
            new BriskHeader(Mode.OobMode).Serialize(outStream);
            BriskSerializer.SerializeOobMessage(outStream, oob, ProtocolDef.Version.CurrentVersion);

            oob.Serialize(outStream, ProtocolDef.Version.CurrentVersion);

            return new InOctetStream(outStream.Close().ToArray());
        }

        public static IInOctetStream CreateReliableIncomingOobPacket<T>(T oob, SequenceId sequenceId, SequenceId receivedId, ReceiveMask receiveMask) where T : IOobMessage
        {
            var outStream = new OutOctetStream(2048);

            Tend.SerializeHeader(outStream, new TendHeader
            {
                isReliable = true,
                packetId = sequenceId,
                receivedId = receivedId,
                receiveMask = receiveMask,
            });
            new BriskHeader(Mode.OobMode).Serialize(outStream);
            BriskSerializer.SerializeOobMessage(outStream, oob, ProtocolDef.Version.CurrentVersion);

            oob.Serialize(outStream, ProtocolDef.Version.CurrentVersion);

            return new InOctetStream(outStream.Close().ToArray());
        }

        public static IInOctetStream CreateIncomingReliablePacket(SequenceId sequenceId, SequenceId receivedId, ReceiveMask receiveMask)
        {
            var outStream = new OutOctetStream(2048);

            Tend.SerializeHeader(outStream, new TendHeader
            {
                isReliable = true,
                packetId = sequenceId,
                receivedId = receivedId,
                receiveMask = receiveMask,
            });
            new BriskHeader(Mode.NormalMode).Serialize(outStream);

            return new InOctetStream(outStream.Close().ToArray());
        }

        public static ConnectResponse ConnectBrisk(
            Brisk brisk,
            Mock<ITransport> transportMock,
            ClientID? clientID = null,
            byte? sendFrequency = null,
            ulong? simulationFrame = null,
            ConnectionSettings connectionSettings = null)
        {
            Assert.That(brisk.State, Is.EqualTo(ConnectionState.Disconnected));

            brisk.Connect(new EndpointData(), ConnectionType.Client, connectionSettings);

            ConnectResponse connectResponse = SetUpTransportConnectResponse(transportMock, clientID, sendFrequency, simulationFrame);

            brisk.Receive(new List<InPacket>());

            ConnectionState expectedState = ConnectionState.Connected;

            Assert.That(brisk.State, Is.EqualTo(expectedState));

            return connectResponse;
        }

        public static void SetUpTransportKeepAliveResponse(Mock<ITransport> transportMock)
        {
            transportMock
                .Setup(m => m.Receive(It.IsAny<List<(IInOctetStream, IPEndPoint)>>()))
                .Callback((List<(IInOctetStream, IPEndPoint)> buffer) => { buffer.Add((CreateIncomingOobPacket(new KeepAlive()), null)); });
        }

        public static void SetUpTransportEmptyResponse(Mock<ITransport> transportMock)
        {
            transportMock
                .Setup(m => m.Receive(It.IsAny<List<(IInOctetStream, IPEndPoint)>>()))
                .Callback((List<(IInOctetStream, IPEndPoint)> buffer) => { });
        }

        public static ConnectResponse SetUpTransportConnectResponse(
            Mock<ITransport> transportMock,
            ClientID? clientID = null,
            byte? sendFrequency = null,
            ulong? simulationFrame = null,
            ushort? mtu = null
        )
        {
            var connectResponse = new ConnectResponse(
                clientID.GetValueOrDefault(new ClientID(10)),
                sendFrequency.GetValueOrDefault(20),
                simulationFrame.GetValueOrDefault(500),
                mtu.GetValueOrDefault(Brisk.DefaultMTU),
                CustomPayload.Empty
            );

            transportMock
                .Setup(m => m.Receive(It.IsAny<List<(IInOctetStream, IPEndPoint)>>()))
                .Callback((List<(IInOctetStream, IPEndPoint)> buffer) =>
                {
                    TendHeader tendHeader = new TendHeader { isReliable = false };
                    buffer.Add((CreateIncomingOobPacket(connectResponse, tendHeader), null));
                });

            return connectResponse;
        }

        public static TimeSpan SendInterval(this Brisk brisk)
        {
            return TimeSpan.FromSeconds(1f / brisk.SendFrequency);
        }

        public static void SetUpElapsed(this Mock<IStopwatch> stopwatchMock, TimeSpan elapsed)
        {
            stopwatchMock.Setup(m => m.Elapsed)
                .Returns(elapsed);
        }
    }
}
