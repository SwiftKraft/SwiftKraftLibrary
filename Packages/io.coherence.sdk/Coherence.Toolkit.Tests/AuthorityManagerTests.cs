namespace Coherence.Toolkit.Tests
{
    using Connection;
    using Entities;
    using Moq;
    using NUnit.Framework;
    using UnityEngine;
    using Coherence.Tests;

    public class AuthorityManagerTests : CoherenceTest
    {
        private CoherenceBridge networkManager;
        private NetworkEntityState entity;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            var go = new GameObject();
            networkManager = go.AddComponent<CoherenceBridge>();

            var sync = go.AddComponent<CoherenceSync>();
            entity = new NetworkEntityState(Entity.InvalidRelative, AuthorityType.Full, false, false, sync, string.Empty);
        }

        [Test]
        public void Should_NotSendAuthRequest_When_WhenRequestingNoneAuthority()
        {
            var client = new Mock<IClient>();

            bool calledSendAuthorityRequest = false;

            client.Setup(c => c.SendAuthorityRequest(It.IsAny<Entity>(), It.IsAny<AuthorityType>()))
                .Callback<Entity, AuthorityType>((s, a) =>
                {
                    calledSendAuthorityRequest = true;
                });

            var clientMock = client.Object;

            AuthorityManager authManager = new AuthorityManager(clientMock, networkManager);

            var result = authManager.RequestAuthority(entity, AuthorityType.None);

            Assert.IsFalse(result);
            Assert.IsFalse(calledSendAuthorityRequest);
        }

        [Test]
        public void Should_NotSendAuthority_When_HasStateAuthority()
        {
            var client = new Mock<IClient>();
            bool calledSendAuthorityTransfer = false;

            client.Setup(c => c.SendAuthorityTransfer(It.IsAny<Entity>(), It.IsAny<ClientID>(), It.IsAny<bool>(), It.IsAny<AuthorityType>()))
                .Callback<Entity, ClientID, bool, AuthorityType>((s, cId, b, authType) =>
                {
                    calledSendAuthorityTransfer = true;
                });
            client.SetupGet(c => c.ClientID).Returns((Coherence.Connection.ClientID)10); // force clientid

            var clientMock = client.Object;

            AuthorityManager authManager = new AuthorityManager(clientMock, networkManager);
            var mockedCoherenceSync = new Mock<ICoherenceSync>();
            entity.Sync = mockedCoherenceSync.Object;

            var result = authManager.TransferAuthority(entity, clientMock.ClientID);

            Assert.IsFalse(result);
            Assert.IsFalse(calledSendAuthorityTransfer);
        }

        [Test]
        public void Should_NotSendAuthRequest_When_WhenAuthorityTransferTypeIsNotTransferableAndSimTypeIsNotServerSide()
        {
            var client = new Mock<IClient>();

            bool calledSendAuthorityRequest = false;

            client.Setup(c => c.SendAuthorityRequest(It.IsAny<Entity>(), It.IsAny<AuthorityType>()))
                .Callback<Entity, AuthorityType>((s, a) =>
                {
                    calledSendAuthorityRequest = true;
                });

            var clientMock = client.Object;

            AuthorityManager authManager = new AuthorityManager(clientMock, networkManager);

            var mockedCoherenceSync = new Mock<ICoherenceSync>();
            mockedCoherenceSync.Setup(cs => cs.SimulationTypeConfig).Returns(CoherenceSync.SimulationType.ClientSide);
            mockedCoherenceSync.Setup(cs => cs.AuthorityTransferTypeConfig).Returns(CoherenceSync.AuthorityTransferType.NotTransferable);
            entity.Sync = mockedCoherenceSync.Object;

            var result = authManager.RequestAuthority(entity, AuthorityType.None);

            Assert.IsFalse(result);
            Assert.IsFalse(calledSendAuthorityRequest);
        }

        [Test]
        public void Should_NotSendAuthRequest_When_WhenAuthorityTypeEqualsRequestedAuthority()
        {
            var client = new Mock<IClient>();

            bool calledSendAuthorityRequest = false;

            client.Setup(c => c.SendAuthorityRequest(It.IsAny<Entity>(), It.IsAny<AuthorityType>()))
                .Callback<Entity, AuthorityType>((s, a) =>
                {
                    calledSendAuthorityRequest = true;
                });

            var clientMock = client.Object;

            AuthorityManager authManager = new AuthorityManager(clientMock, networkManager);

            var result = authManager.RequestAuthority(entity, AuthorityType.Full);

            Assert.IsFalse(result);
            Assert.IsFalse(calledSendAuthorityRequest);
        }

        [Test]
        public void Should_NotSendAuthRequest_When_WhenEntityIsOrphaned()
        {
            var client = new Mock<IClient>();

            bool calledSendAuthorityRequest = false;

            client.Setup(c => c.SendAuthorityRequest(It.IsAny<Entity>(), It.IsAny<AuthorityType>()))
                .Callback<Entity, AuthorityType>((s, a) =>
                {
                    calledSendAuthorityRequest = true;
                });

            var clientMock = client.Object;

            AuthorityManager authManager = new AuthorityManager(clientMock, networkManager);

            entity.IsOrphaned = true;
            var result = authManager.RequestAuthority(entity, AuthorityType.None);
            entity.IsOrphaned = false;

            Assert.IsFalse(result);
            Assert.IsFalse(calledSendAuthorityRequest);
        }

        [Test]
        public void Should_SendAuthRequest_When_ConditionsAreCorrect()
        {
            var client = new Mock<IClient>();

            bool calledSendAuthorityRequest = false;

            client.Setup(c => c.SendAuthorityRequest(It.IsAny<Entity>(), It.IsAny<AuthorityType>()))
                .Callback<Entity, AuthorityType>((s, a) =>
                {
                    calledSendAuthorityRequest = true;
                });

            var clientMock = client.Object;

            AuthorityManager authManager = new AuthorityManager(clientMock, networkManager);

            entity.AuthorityType.UpdateValue(AuthorityType.None);

            var result = authManager.RequestAuthority(entity, AuthorityType.Full);

            Assert.IsTrue(result);
            Assert.IsTrue(calledSendAuthorityRequest);
        }

        [Test]
        public void Should_NotSendTransferRequest_When_AuthTypeIsNotTransferable()
        {
            var client = new Mock<IClient>();

            bool calledSendAuthorityTransfer = false;

            client.Setup(c => c.SendAuthorityTransfer(It.IsAny<Entity>(), It.IsAny<ClientID>(), It.IsAny<bool>(), It.IsAny<AuthorityType>()))
                .Callback<Entity, ClientID, bool, AuthorityType>((s, cId, b, authType) =>
                {
                    calledSendAuthorityTransfer = true;
                });

            var clientMock = client.Object;

            AuthorityManager authManager = new AuthorityManager(clientMock, networkManager);
            var mockedCoherenceSync = new Mock<ICoherenceSync>();
            mockedCoherenceSync.Setup(cs => cs.AuthorityTransferTypeConfig).Returns(CoherenceSync.AuthorityTransferType.NotTransferable);
            entity.Sync = mockedCoherenceSync.Object;

            var result = authManager.TransferAuthority(entity, ClientID.Server);

            Assert.IsFalse(result);
            Assert.IsFalse(calledSendAuthorityTransfer);
        }

        [Test]
        public void Should_NotSendTransferRequest_When_IsClientConnection()
        {
            var client = new Mock<IClient>();

            bool calledSendAuthorityTransfer = false;

            client.Setup(c => c.SendAuthorityTransfer(It.IsAny<Entity>(), It.IsAny<ClientID>(), It.IsAny<bool>(), It.IsAny<AuthorityType>()))
                .Callback<Entity, ClientID, bool, AuthorityType>((s, cId, b, authType) =>
                {
                    calledSendAuthorityTransfer = true;
                });

            var clientMock = client.Object;

            AuthorityManager authManager = new AuthorityManager(clientMock, networkManager);
            var mockedCoherenceSync = new Mock<ICoherenceSync>();
            mockedCoherenceSync.Setup(cs => cs.AuthorityTransferTypeConfig).Returns(CoherenceSync.AuthorityTransferType.Stealing);

            entity.Sync = mockedCoherenceSync.Object;
            entity.ClientConnection = new CoherenceClientConnection(null, Entity.InvalidRelative, ClientID.Server, ConnectionType.Client, true);

            var result = authManager.TransferAuthority(entity, ClientID.Server);

            entity.ClientConnection = null;

            Assert.IsFalse(result);
            Assert.IsFalse(calledSendAuthorityTransfer);
        }

        [Test]
        public void Should_NotSendTransferRequest_When_AuthorityIsInsufficient()
        {
            var client = new Mock<IClient>();

            bool calledSendAuthorityTransfer = false;

            client.Setup(c => c.SendAuthorityTransfer(It.IsAny<Entity>(), It.IsAny<ClientID>(), It.IsAny<bool>(), It.IsAny<AuthorityType>()))
                .Callback<Entity, ClientID, bool, AuthorityType>((s, cId, b, authType) =>
                {
                    calledSendAuthorityTransfer = true;
                });

            var clientMock = client.Object;

            AuthorityManager authManager = new AuthorityManager(clientMock, networkManager);
            var mockedCoherenceSync = new Mock<ICoherenceSync>();
            mockedCoherenceSync.Setup(cs => cs.AuthorityTransferTypeConfig).Returns(CoherenceSync.AuthorityTransferType.Stealing);

            entity.Sync = mockedCoherenceSync.Object;

            var result = authManager.TransferAuthority(entity, ClientID.Server, AuthorityType.None);

            Assert.IsFalse(result);
            Assert.IsFalse(calledSendAuthorityTransfer);
        }

        [Test]
        public void Should_NotSendTransferRequest_When_EntityIsOrphaned()
        {
            var client = new Mock<IClient>();

            bool calledSendAuthorityTransfer = false;

            client.Setup(c => c.SendAuthorityTransfer(It.IsAny<Entity>(), It.IsAny<ClientID>(), It.IsAny<bool>(), It.IsAny<AuthorityType>()))
                .Callback<Entity, ClientID, bool, AuthorityType>((s, cId, b, authType) =>
                {
                    calledSendAuthorityTransfer = true;
                });

            var clientMock = client.Object;

            AuthorityManager authManager = new AuthorityManager(clientMock, networkManager);

            entity.IsOrphaned = true;

            var result = authManager.TransferAuthority(entity, ClientID.Server);

            entity.IsOrphaned = false;

            Assert.IsFalse(result);
            Assert.IsFalse(calledSendAuthorityTransfer);
        }

        [Test]
        public void Should_SendTransferRequest_When_ConditionsAreCorrect()
        {
            var client = new Mock<IClient>();

            client.Setup(c => c.SendAuthorityTransfer(It.IsAny<Entity>(), It.IsAny<ClientID>(), It.IsAny<bool>(), It.IsAny<AuthorityType>()))
                .Returns(true);
            client.SetupGet(c => c.ClientID).Returns((Coherence.Connection.ClientID)10); // force clientid

            var clientMock = client.Object;

            AuthorityManager authManager = new AuthorityManager(clientMock, networkManager);

            entity.AuthorityType.UpdateValue(AuthorityType.Input);

            var result = authManager.TransferAuthority(entity, ClientID.Server, AuthorityType.Input);

            Assert.IsTrue(result);
        }

        [Test]
        public void Should_NotAbandonAuthority_When_EntityIsNotPersistent()
        {
            var client = new Mock<IClient>();
            var clientMock = client.Object;

            AuthorityManager authManager = new AuthorityManager(clientMock, networkManager);

            entity.AuthorityType.UpdateValue(AuthorityType.Input);

            var result = authManager.AbandonAuthority(entity);

            Assert.IsFalse(result);
        }

        [Test]
        public void Should_NotAbandonAuthority_When_AuthTypeIsOnlyInput()
        {
            var client = new Mock<IClient>();
            var clientMock = client.Object;

            AuthorityManager authManager = new AuthorityManager(clientMock, networkManager);

            entity.AuthorityType.UpdateValue(AuthorityType.Input);

            var result = authManager.AbandonAuthority(entity);

            Assert.IsFalse(result);
        }

        [Test]
        public void Should_NotAbandonAuthority_When_CantTransferAuthority()
        {
            var client = new Mock<IClient>();
            var clientMock = client.Object;

            AuthorityManager authManager = new AuthorityManager(clientMock, networkManager);

            entity.IsOrphaned = true;

            var result = authManager.AbandonAuthority(entity);

            entity.IsOrphaned = false;

            Assert.IsFalse(result);
        }

        [Test]
        public void Should_AbandonAuthority_When_ConditionsAreCorrect()
        {
            var client = new Mock<IClient>();

            client.Setup(c => c.SendAuthorityTransfer(It.IsAny<Entity>(), It.IsAny<ClientID>(), It.IsAny<bool>(), It.IsAny<AuthorityType>()))
                .Returns(true);
            client.SetupGet(c => c.ClientID).Returns((Coherence.Connection.ClientID)10); // force clientid

            var clientMock = client.Object;

            var mockUpdater = new Mock<ICoherenceSyncUpdater>();

            AuthorityManager authManager = new AuthorityManager(clientMock, networkManager);
            var mockedCoherenceSync = new Mock<ICoherenceSync>();
            mockedCoherenceSync.Setup(cs => cs.LifetimeTypeConfig).Returns(CoherenceSync.LifetimeType.Persistent);
            mockedCoherenceSync.Setup(cs => cs.AuthorityTransferTypeConfig).Returns(CoherenceSync.AuthorityTransferType.Stealing);
            mockedCoherenceSync.Setup(cs => cs.Updater).Returns(mockUpdater.Object);

            entity.Sync = mockedCoherenceSync.Object;

            entity.IsOrphaned = false;
            entity.AuthorityType.UpdateValue(AuthorityType.Full);

            var result = authManager.AbandonAuthority(entity);

            Assert.IsTrue(result);
            Assert.IsTrue(entity.IsOrphaned);

            entity.IsOrphaned = false;
        }
    }
}
