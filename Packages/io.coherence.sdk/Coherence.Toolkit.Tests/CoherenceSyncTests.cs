// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_EDITOR

namespace Coherence.Toolkit.Tests
{
    using Moq;
    using NUnit.Framework;
    using ProtocolDef;
    using System;
    using System.Reflection.Emit;
    using Bindings;
    using UnityEngine;

    using Coherence.Entities;
    using Log;
    using System.Collections.Generic;
    using Coherence.Tests;
    using Connection;
    using UnityEditor.PackageManager;

    public class CoherenceSyncTests : CoherenceTest
    {
        private class MockComponent : MonoBehaviour
        {

        }

        private static object[] commonTestCases =
        {
            new object[] {MessageTarget.AuthorityOnly, MessageTarget.AuthorityOnly, true},
            new object[] {MessageTarget.AuthorityOnly, MessageTarget.All, true},
            new object[] {MessageTarget.AuthorityOnly, MessageTarget.Other, true},
            new object[] {MessageTarget.All, MessageTarget.AuthorityOnly, false},
            new object[] {MessageTarget.All, MessageTarget.All, true},
            new object[] {MessageTarget.All, MessageTarget.Other, true},
            new object[] {MessageTarget.Other, MessageTarget.AuthorityOnly, false},
            new object[] {MessageTarget.Other, MessageTarget.All, true},
            new object[] {MessageTarget.Other, MessageTarget.Other, true},
        };

        [Test]
        [TestCaseSource(nameof(commonTestCases))]
        public void ReceiveCommand_Baked_HandlesRouting(MessageTarget commandTarget, MessageTarget routing, bool expectReceived)
        {
            // Arrange
            var mockSync = new Mock<ICoherenceSync>();

            var bakedScript = new CoherenceSyncBakedMock();
            var bridgeGo = new GameObject();
            var bridge = bridgeGo.AddComponent<CoherenceBridge>();

            mockSync.Setup(cs => cs.BakedScript).Returns(bakedScript);
            mockSync.Setup(cs => cs.EntityState).Returns(new NetworkEntityState(Entity.InvalidRelative, AuthorityType.Full, false, false, mockSync.Object, String.Empty));
            mockSync.Setup(cs => cs.CoherenceBridge).Returns(bridge);

            IEntityCommand command = Mock.Of((IEntityCommand m) => m.Routing == routing);

            CommandsHandler handler = new CommandsHandler(mockSync.Object, new List<Binding>(), new UnityLogger());
            // Act
            handler.HandleCommand(command, commandTarget);

            // Assert
            Assert.That(bakedScript.TimesCalled(nameof(CoherenceSyncBaked.ReceiveCommand)), Is.EqualTo(expectReceived ? 1 : 0));
        }

        [Test]
        [Description("CoherenceBridge instance will not be destroyed when the updater is null")]
        public void HandleConnected_HandleNetworkedDestruction_BridgeNotDestroyed_WhenUpdaterIsNull()
        {
            var go = new GameObject();
            var sync = go.AddComponent<CoherenceSync>();
            using var mockBridgeBuilder = new MockBridgeBuilder();
            Mock<ICoherenceBridge> bridgeMock = mockBridgeBuilder.Build();

            sync.CoherenceSyncConfig = ScriptableObject.CreateInstance<CoherenceSyncConfig>();
            sync.CoherenceSyncConfig.IncludeInSchema = true;
            sync.CoherenceSyncConfig.Instantiator = new Mock<INetworkObjectInstantiator>().Object;

            sync.SetBridge(bridgeMock.Object);

            // Casts to interface needed because sync.CoherenceBridge returns instance of CoherenceBridge
            Assert.That(((ICoherenceSync)sync).CoherenceBridge, Is.Not.Null);
            ((ICoherenceSync)sync).HandleNetworkedDestruction(false);
            Assert.That(((ICoherenceSync)sync).CoherenceBridge, Is.Not.Null);
        }

        [Test]
        [Description("The current bridge connection is set to null when a destruction is called for over the network")]
        public void HandleConnected_HandleNetworkedDestruction_BridgeDestroyed_WhenUpdaterIsNotNull()
        {
            var updaterMock = new Mock<ICoherenceSyncUpdater>();
            updaterMock.Setup(u => u.TaggedForNetworkedDestruction).Returns(false);

            var go = new GameObject();
            var sync = go.AddComponent<CoherenceSync>();
            sync.SetUpdater(updaterMock.Object);

            using var mockBridgeBuilder = new MockBridgeBuilder();
            Mock<ICoherenceBridge> bridgeMock = mockBridgeBuilder.Build();

            sync.CoherenceSyncConfig = ScriptableObject.CreateInstance<CoherenceSyncConfig>();
            sync.CoherenceSyncConfig.IncludeInSchema = true;
            sync.CoherenceSyncConfig.Instantiator = new Mock<INetworkObjectInstantiator>().Object;

            sync.SetBridge(bridgeMock.Object);

            // Casts to interface needed because sync.CoherenceBridge returns instance of CoherenceBridge
            Assert.That(((ICoherenceSync)sync).CoherenceBridge, Is.Not.Null);
            ((ICoherenceSync)sync).HandleNetworkedDestruction(false);
            Assert.That(((ICoherenceSync)sync).CoherenceBridge, Is.Null);
            updaterMock.VerifySet(u => u.TaggedForNetworkedDestruction = true, Times.Once());
        }

        [Test]
        [Description("Syncing network entity state is not called when there is no bridge")]
        public void HandleConnected_BridgeIsNull_SyncNetworkEntityState_NotCalled()
        {
            var updaterMock = new Mock<ICoherenceSyncUpdater>();
            updaterMock.Setup(u => u.TaggedForNetworkedDestruction).Returns(false);

            var entitiesManagerMock = new Mock<EntitiesManager>();
            entitiesManagerMock.Setup(m => m.SyncNetworkEntityState(It.IsAny<ICoherenceSync>()))
                .Throws(new ArgumentNullException());

            var go = new GameObject();
            var sync = go.AddComponent<CoherenceSync>();
            sync.SetUpdater(updaterMock.Object);

            using var mockBridgeBuilder = new MockBridgeBuilder();
            Mock<ICoherenceBridge> bridgeMock = mockBridgeBuilder.Build();
            bridgeMock.Setup(b => b.EntitiesManager)
                .Returns(entitiesManagerMock.Object);

            Assert.That(sync.CoherenceBridge, Is.Null);

            sync.EntityState = new NetworkEntityState(new Entity(0,0,false), AuthorityType.Input, false, true, sync, "uuid");
            sync.CoherenceSyncConfig = ScriptableObject.CreateInstance<CoherenceSyncConfig>();
            sync.CoherenceSyncConfig.IncludeInSchema = true;
            sync.CoherenceSyncConfig.Instantiator = new Mock<INetworkObjectInstantiator>().Object;

            bridgeMock.Raise(b => b.OnConnectedInternal += null, bridgeMock.Object);
            entitiesManagerMock.Verify(m => m.SyncNetworkEntityState(It.IsAny<ICoherenceSync>()), Times.Never);
        }

        [Test]
        [Description("Syncing the entity state happens on a new connection with a valid bridge")]
        public void HandleConnected_BridgeIsNotNull_SyncNetworkEntityState_Called()
        {
            var updaterMock = new Mock<ICoherenceSyncUpdater>();
            updaterMock.Setup(u => u.TaggedForNetworkedDestruction).Returns(false);

            var go = new GameObject();
            var sync = go.AddComponent<CoherenceSync>();
            sync.SetUpdater(updaterMock.Object);
            var componentUpdates = new ComponentUpdates();

            var entitiesManagerMock = new Mock<EntitiesManager>();
            entitiesManagerMock.Setup(m => m.SyncNetworkEntityState(It.IsAny<ICoherenceSync>()))
                .Returns((null, componentUpdates, 0, false));

            using var mockBridgeBuilder = new MockBridgeBuilder();
            Mock<ICoherenceBridge> bridgeMock = mockBridgeBuilder.Build();
            bridgeMock.Setup(b => b.EntitiesManager)
                .Returns(entitiesManagerMock.Object);

            var clientID = new ClientID(1);
            var client = new Mock<IClient>();
            client.Setup(c => c.IsConnected()).Returns(true);
            client.Setup(c => c.ClientID).Returns(clientID);

            entitiesManagerMock.Object.SetClient(client.Object);
            bridgeMock.Setup(b => b.EntitiesManager).Returns(entitiesManagerMock.Object);

            sync.SetBridge(bridgeMock.Object);
            sync.ConnectBridge(bridgeMock.Object);
            Assert.That(((ICoherenceSync)sync).CoherenceBridge, Is.Not.Null);

            sync.EntityState = new NetworkEntityState(new Entity(0,0,false), AuthorityType.Input, false, true, sync, "uuid");
            sync.CoherenceSyncConfig = ScriptableObject.CreateInstance<CoherenceSyncConfig>();
            sync.CoherenceSyncConfig.IncludeInSchema = true;
            sync.CoherenceSyncConfig.Instantiator = new Mock<INetworkObjectInstantiator>().Object;
            bridgeMock.Raise(b => b.OnConnectedInternal += null, bridgeMock.Object);
            entitiesManagerMock.Verify(m => m.SyncNetworkEntityState(It.IsAny<ICoherenceSync>()), Times.Once);
        }
    }
}

#endif // UNITY_EDITOR
