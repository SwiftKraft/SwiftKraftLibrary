// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Tests
{
    using Brisk;
    using Brisk.Models;
    using Brisk.Tests;
    using Brook;
    using Common;
    using Connection;
    using Entities;
    using Log;
    using Moq;
    using NUnit.Framework;
    using ProtocolDef;
    using Stats;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using Transport;
    using Coherence.Tests;

    public class ClientCoreTests : CoherenceTest
    {
        private static readonly Logger Logger = Log.GetLogger<ClientCoreTests>();

        [Test]
        public void Timeout_DoesntTriggerIfKeepaliveWasReceivedJustBeforeUpdate()
        {
            // Arrange
            ClientCore clientCore = GetTestClientCore(out BriskServicesMocks briskServices, out Mock<ITransport> transportMock, out _);
            EndpointData endpointData = GetTestEndpoint();

            clientCore.Connect(endpointData, ConnectionSettings.Default);
            BriskTestUtils.SetUpTransportConnectResponse(transportMock);
            RunUnityUpdateForClientCore(clientCore);
            Assert.That(clientCore.ConnectionState, Is.EqualTo(ConnectionState.Connected));

            briskServices.ConnectionTimer.Setup(m => m.Elapsed)
                .Returns(ConnectionSettings.Default.DisconnectTimeout);
            BriskTestUtils.SetUpTransportKeepAliveResponse(transportMock);

            // Act
            RunUnityUpdateForClientCore(clientCore);

            // Assert
            Assert.That(clientCore.ConnectionState, Is.EqualTo(ConnectionState.Connected));
        }

        private static void RunUnityUpdateForClientCore(ClientCore clientCore)
        {
            clientCore.UpdateReceiving();
            clientCore.UpdateSending();
        }

        private static EndpointData GetTestEndpoint()
        {
            return new EndpointData()
            {
                host = "127.0.0.1",
                port = 1000,
                schemaId = "testSchemaId",
                authToken = "testAuthToken"
            };
        }

        private static ITransportFactory CreateTransportFactory(Mock<ITransport> transportMock)
        {
            Mock<ITransportFactory> transportFactoryMock = new Mock<ITransportFactory>();

            transportFactoryMock.Setup(m => m.Create(It.IsAny<ushort>(), It.IsAny<Stats>(), It.IsAny<Logger>()))
                .Returns(transportMock.Object);

            return transportFactoryMock.Object;
        }

        private static ClientCore GetTestClientCore(out BriskServicesMocks briskServices, out Mock<ITransport> transportMock, out Mock<IDefinition> definitionMock)
        {
            definitionMock = new Mock<IDefinition>();

            transportMock = new Mock<ITransport>();
            transportMock.SetupGet(m => m.CanSend).Returns(true);
            BriskTestUtils.SetUpTransportOpenAndClose(transportMock);

            briskServices = new BriskServicesMocks();

            ITransportFactory transportFactory = CreateTransportFactory(transportMock);

            var domainNameResolverMock = new Mock<IDomainNameResolver>();
            domainNameResolverMock.Setup(r => r.Resolve(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<Logger>(), It.IsAny<Action<IPAddress>>(), It.IsAny<Action>())).
                Callback((string hostname, CancellationToken cancellationToken, Logger logger, Action<IPAddress> onSuccess, Action onFailure) => onSuccess(IPAddress.Parse(hostname)));

            return new ClientCore(definitionMock.Object, Logger,
                null, briskServices.GetServices(), transportFactory, domainNameResolverMock.Object);
        }
    }
}
