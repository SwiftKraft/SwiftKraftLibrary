// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport.Tests
{

    using Moq;
    using NUnit.Framework;
    using System;
    using System.Net;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    using Log;
    using Stats;
    using Coherence.Brook;
    using Coherence.Brook.Octet;
    using Coherence.Common;
    using Coherence.Common.Tests;
    using Coherence.Connection;
    using Coherence.Transport;
    using Coherence.Transport.Web;
    using Coherence.Tests;

    public interface IWebInterop
    {
        public int InitializeConnection(Action onOpen, Action<byte[]> onPacket, Action<JsError> onError);
        public void Connect(int id,
            string host,
            int roomId,
            string token,
            string uniqueRoomId,
            string worldId,
            string region,
            string schemaId);
        public void Disconnect(int id);
        public void Send(int id, byte[] data, int size);
    }

    public class WebTransportTests : CoherenceTest
    {
        private Mock<IWebInterop> mockWebInterop;
        private WebTransport webTransport;

        private Action OnOpen;
        private Action<byte[]> OnPacket;
        private Action<JsError> OnError;

        const int ID = 42;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            mockWebInterop = new Mock<IWebInterop>();
            mockWebInterop.Setup(m => m.InitializeConnection(It.IsAny<Action>(), It.IsAny<Action<byte[]>>(), It.IsAny<Action<JsError>>()))
                .Callback((Action onOpen, Action<byte[]> onPacket, Action<JsError> onError) =>
                {
                    OnOpen = onOpen;
                    OnPacket = onPacket;
                    OnError = onError;
                })
                .Returns(ID);
            mockWebInterop.Setup(m => m.Connect(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>())
            )
                .Callback(() => { OnOpen(); }); // Need to call on open when the connection is opened.
            mockWebInterop.Setup(m => m.Send(It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<int>()))
                .Callback(() => { });
            mockWebInterop.Setup(m => m.Disconnect(It.IsAny<int>()))
                .Callback(() => { });

            var stats = new Mock<IStats>();

            webTransport = new WebTransport(
                mockWebInterop.Object.InitializeConnection,
                mockWebInterop.Object.Connect,
                mockWebInterop.Object.Disconnect,
                mockWebInterop.Object.Send,
                stats.Object,
                logger);

            Assert.That(logger.GetCountForErrorID(Error.WebTransportNotSupported), Is.EqualTo(1));
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            // Verify the interop was initialized only once.
            mockWebInterop.Verify(m => m.InitializeConnection(It.IsAny<Action>(), It.IsAny<Action<byte[]>>(), It.IsAny<Action<JsError>>()), Times.Once);
        }

        [Test]
        [Description("Verifies that the web transport can connect and the parameters are as expected.")]
        public void Connect()
        {
            // Arrange
            var endPoint = new EndpointData();
            endPoint.host = "localhost";

            var settings = new ConnectionSettings();

            // Act
            webTransport.Open(endPoint, settings);

            // Assert
            mockWebInterop.Verify(m => m.Connect(
                It.Is<int>(id => id == ID),
                It.Is<string>(host => host == endPoint.GetHostAndPort()),
                It.Is<int>(roomID => roomID == endPoint.roomId),
                It.Is<string>(token => token == endPoint.authToken),
                It.Is<string>(uniqueRoomId => uniqueRoomId == endPoint.uniqueRoomId.ToString()),
                It.Is<string>(worldId => worldId == endPoint.WorldIdString()),
                It.Is<string>(region => region == endPoint.region),
                It.Is<string>(schemaId => schemaId == endPoint.schemaId)
            ), Times.Once);
        }

        [Test]
        [Description("Verifies that the web transport can send data.")]
        public void Send()
        {
            // Arrange
            var endPoint = new EndpointData();
            endPoint.host = "localhost";

            var settings = new ConnectionSettings();

            webTransport.Open(endPoint, settings);

            const uint testData = 123456789;

            var stream = new OutOctetStream(2048);
            stream.WriteUint32(testData);

            Func<byte[], bool> isDataCorrect = (bytes) =>
            {
                var inStream = new InOctetStream(bytes);
                var inData = inStream.ReadUint32();
                return inData == testData;
            };

            // Act
            webTransport.Send(stream);

            // Assert
            mockWebInterop.Verify(m => m.Send(
                It.Is<int>(id => id == ID),
                It.Is<byte[]>(data => isDataCorrect(data)),
                It.Is<int>(size => size == stream.Position)
            ), Times.Once);
        }

        [Test]
        [Description("Verifies that the web transport can receive data.")]
        public void Receive()
        {
            // Arrange
            var endPoint = new EndpointData();
            endPoint.host = "localhost";

            var settings = new ConnectionSettings();

            webTransport.Open(endPoint, settings);

            const uint testData = 123456789;

            var stream = new OutOctetStream(2048);
            stream.WriteUint32(testData);

            // This sends the packet to the transport.
            OnPacket(stream.Close().ToArray());

            List<(IInOctetStream, IPEndPoint)> buffer = new List<(IInOctetStream, IPEndPoint)>();

            // Act
            webTransport.Receive(buffer);

            // Assert
            Assert.That(buffer.Count, Is.EqualTo(1));

            var (inStream, ipEndpoint) = buffer[0];

            Assert.That(inStream.ReadUint32(), Is.EqualTo(testData));
        }

        [Test]
        [Description("Verifies that the web transport can be disconnected.")]
        public void Disconnect()
        {
            // Arrange
            var endPoint = new EndpointData();
            endPoint.host = "localhost";

            var settings = new ConnectionSettings();

            webTransport.Open(endPoint, settings);

            // Act
            webTransport.Close();

            // Assert
            mockWebInterop.Verify(m => m.Disconnect(It.Is<int>(id => id == ID)), Times.Once);
        }
    }
}
