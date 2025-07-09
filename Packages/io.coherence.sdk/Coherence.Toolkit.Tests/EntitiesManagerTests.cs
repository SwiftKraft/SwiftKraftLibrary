// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Log;
    using Coherence.Tests;
    using Connection;
    using Entities;
    using Moq;
    using NUnit.Framework;
    using ProtocolDef;
    using UnityEditor;
    using UnityEngine;

    public class EntitiesManagerTests : CoherenceTest
    {
        private EntitiesManager entitiesManager;
        private Mock<IClient> mockClient;
        private MockBridgeBuilder mockBridgeBuilder;
        private Mock<ICoherenceBridge> mockBridge;
        private Mock<IClientConnectionManager> mockConnectionManager;
        private CoherenceInputManager inputManager;
        private UniquenessManager uniquenessManager;
        private Mock<IDefinition> mockRoot;
        private Entity entityA = new(1, 0, Entity.Relative);
        private Entity entityB = new(2, 0, Entity.Relative);
        private Entity entityC = new(3, 0, Entity.Relative);
        private Entity entityD = new(4, 0, Entity.Relative);
        private Entity entityE = new(5, 0, Entity.Relative);
        private ClientID client0 = new(1234);

        // Using a hashset since there are several different access mechanisms and I'm
        // just going to use slow ways to process so I'm not creating parallel dicts like
        // in the actual CCM.
        private HashSet<CoherenceClientConnection> connections;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // There are no invalid asset IDs.
            Impl.AssetId = () => 0;

            mockBridgeBuilder = new MockBridgeBuilder().SetClientID(client0);

            (mockBridge, mockClient) = mockBridgeBuilder.Build();

            mockRoot = new Mock<IDefinition>(MockBehavior.Strict);

            mockConnectionManager = new Mock<IClientConnectionManager>(MockBehavior.Strict);

            connections = new();
            var connectionsForNextTest = connections;

            mockConnectionManager.Setup(manager => manager.Add(It.IsAny<CoherenceClientConnection>()))
                .Callback((CoherenceClientConnection connection) =>
                {
                    bool wasAdded = connectionsForNextTest.Add(connection);
                    Assert.That(wasAdded, Is.True, "Adding duplicate connection!");
                });
            mockConnectionManager.Setup(manager => manager.Remove(It.IsAny<Entity>()))
                .Returns((Entity id) =>
                {
                    var connection = connectionsForNextTest.FirstOrDefault(conn => conn.EntityId == id);
                    connectionsForNextTest.Remove(connection);

                    return connection != null;
                });

            inputManager = new CoherenceInputManager(mockBridge.Object);
            uniquenessManager = new UniquenessManager(logger);

            entitiesManager = new EntitiesManager(
                mockBridge.Object,
                mockConnectionManager.Object,
                inputManager,
                uniquenessManager,
                mockRoot.Object,
                logger);
        }

        [TearDown]
        public override void TearDown()
        {
            mockBridgeBuilder.Dispose();
            base.TearDown();
        }

        [Test]
        [Description("Basic test for creating the entities manager.")]
        public void Test_CreateEntitiesManager() => entitiesManager.SyncAndSend();

        [Test]
        [Description("Verify that attempting to sync an entity when the client is disconnected returns no state.")]
        public void Test_NewSyncEntityNoConnection()
        {
            // Arrange
            MockClientConnected(false);

            var (mockSync, _) = new MockSyncBuilder()
                .Build();

            // Act
            (NetworkEntityState state, ComponentUpdates? updates, uint? LOD, bool disabled) = entitiesManager.SyncNetworkEntityState(mockSync.Object);

            // Assert
            Assert.That(state, Is.Null);
            Assert.That(updates, Is.Null);
            Assert.That(LOD, Is.Null);
            Assert.That(disabled, Is.False);
        }

        [Test]
        [Description("Verify that syncing a network entity when connected returns the expected state and data.")]
        public void Test_NewSyncEntity()
        {
            // Arrange
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            var (mockSync, _) = new MockSyncBuilder()
                .SetInitialComps(comps)
                .Build();

            // Act
            (NetworkEntityState state, ComponentUpdates? updates, uint? LOD, bool disabled) = entitiesManager.SyncNetworkEntityState(mockSync.Object);

            // Assert
            Assert.That(state, Is.Not.Null);
            Assert.That(updates, Is.Null); // only not null if unsynced.
            Assert.That(LOD, Is.Null);
            Assert.That(disabled, Is.False);
        }

        [Test]
        [Description("Verifies that a network entity can be created")]
        public void Test_CreateNetworkedEntity()
        {
            // Arrange / Act / Assert
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            CreateNetworkedEntity(entityA, comps, Entity.InvalidRelative);
        }

        [Test]
        [Description("Verify that EntityCount is increased by one when a networked entity is synchronized.")]
        public void SyncNetworkEntityState_Increases_EntityCount_By_One()
        {
            MockClientConnected(true);
            Mock<ICoherenceSync> mockSync = new MockSyncBuilder().Build();
            var entityCountBefore = entitiesManager.EntityCount;

            entitiesManager.SyncNetworkEntityState(mockSync.Object);

            Assert.That(entitiesManager.EntityCount, Is.EqualTo(entityCountBefore + 1));
        }

        [Test]
        [Description("Verify that EntityCount becomes zero when the IClient.OnDisconnected event is raised, on the happy path.")]
        public void WhenClientDisconnects_AllEntitiesAreClearedIfNoExceptionsAreThrown()
        {
            MockClientConnected(true);
            Mock<ICoherenceSync> mockSync = new MockSyncBuilder().Build();
            entitiesManager.SyncNetworkEntityState(mockSync.Object);

            mockClient.OnDisconnected();

            Assert.That(entitiesManager.EntityCount, Is.Zero);
        }

        [Test]
        [Description("Verify that EntityCount becomes zero when the IClient.OnDisconnected event is raised, even if an exception is thrown during ICoherenceSync.HandleDisconnected.")]
        public void WhenClientDisconnects_AllEntitiesAreClearedIfCoherenceSyncHandleDisconnectedThrows()
        {
            MockClientConnected(true);
            Mock<ICoherenceSync> mockSync = new MockSyncBuilder()
                .SetHandleDisconnected(new Exception())
                .Build();
            // State authority is Full by default, which should cause ICoherenceSync.HandleDisconnected to get used instead of CoherenceSyncConfig.Instantiator.Destroy.
            entitiesManager.SyncNetworkEntityState(mockSync.Object);

            mockClient.OnDisconnected();

            Assert.That(entitiesManager.EntityCount, Is.Zero);
            Assert.That(logger.GetCountForErrorID(Error.ToolkitEntitiesManagerHandleDisconnected), Is.EqualTo(1));
        }

        [Test]
        [Description("Verify that UnsyncedEntityCount becomes zero when the IClient.OnDisconnected event is raised.")]
        public void WhenClientDisconnects_AllUnsyncedEntitiesAreCleared()
        {
            MockClientConnected(true);
            var queue = new Queue<UnsyncedNetworkEntity>(1);
            queue.Enqueue(new());
            entitiesManager.unsyncedNetworkEntities[""] = queue;
            entitiesManager.unsyncedNetworkEntitiesByUniqueId[""] = new();

            mockClient.OnDisconnected();

            Assert.That(entitiesManager.unsyncedNetworkEntities, Is.Empty);
            Assert.That(entitiesManager.unsyncedNetworkEntitiesByUniqueId, Is.Empty);
        }

        [Test]
        [Description("Verify that EntityCount becomes zero when the IClient.OnDisconnected event is raised, even if an exception is thrown during ICoherenceSync.Config.Instantiator.Destroy.")]
        public void WhenClientDisconnects_AllEntitiesAreClearedIfCoherenceSyncConfigInstantiatorDestroyThrows()
        {
            MockClientConnected(true);
            Mock<ICoherenceSync> mockSync = new MockSyncBuilder()
                .SetInstantiatorDestroy(new Exception())
                .Build();
            entitiesManager.SyncNetworkEntityState(mockSync.Object);
            var (entity, state) = entitiesManager.networkEntities.First();
            // Remove state authority so that CoherenceSyncConfig.Instantiator.Destroy gets used instead of ICoherenceSync.HandleDisconnected.
            entitiesManager.networkEntities[entity]
                = new(state.EntityID, AuthorityType.None, state.IsOrphaned, state.NetworkInstantiated, state.Sync, state.CoherenceUUID);

            mockClient.OnDisconnected();

            Assert.That(entitiesManager.EntityCount, Is.Zero);
            Assert.That(logger.GetCountForErrorID(Error.ToolkitEntitiesManagerHandleDisconnected), Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        [Description("Verifies that a connection entity is created correctly when no prefab defined.")]
        public async Task Test_CreateClientConnectionEntity_NoPrefab(bool isMyConnection)
        {
            // Arrange
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            // No prefab registered.
            mockConnectionManager.Setup(manager => manager.GetPrefab(It.IsAny<ClientID>(), It.IsAny<ConnectionType>(), It.IsAny<Action<ICoherenceSync>>()))
                .Callback((ClientID id, ConnectionType connType, Action<ICoherenceSync> onLoaded) => onLoaded(null)); // Does not have a client/server prefab bound.

            // Act
            var builder = MockNetworkedEntityBuilder.CreateNetworkedEntity(entityA)
                .Comps(comps)
                .ClientID(client0);

            if (isMyConnection)
            {
                builder.SetHasStateAuthority();
            }

            builder.Build(mockClient);

            // EntitiesManager is not guaranteed to register the connection immediately.
            await WaitUntilConnectionsIsNotEmpty(5d);

            // Assert - that the client entity was added to the connections but not the public entities info.
            Assert.That(entitiesManager.EntityCount, Is.EqualTo(0));
            Assert.That(entitiesManager.NetworkEntities, Is.Empty);
            Assert.That(entitiesManager.ContainsEntity(entityA), Is.True);
            Assert.That(entitiesManager.ConnectionEntityID, Is.EqualTo(isMyConnection ? entityA : Entity.InvalidRelative));

            // verify that the connection was added to the connection manager from the entities manager only once.
            mockConnectionManager.Verify(manager =>
                manager.Add(It.Is<CoherenceClientConnection>(connection => connections.Contains(connection))),
                Times.Once);
        }

        [TestCase(true)]
        [TestCase(false)]
        [Description("Verifies that a connection entity is created correctly when a prefab is defined.")]
        public async Task Test_CreateClientConnectionEntity_WithPrefab(bool isMyConnection)
        {
            // Arrange
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            // There is a prefab registered.
            mockConnectionManager.Setup(manager => manager.GetPrefab(It.IsAny<ClientID>(), It.IsAny<ConnectionType>(), It.IsAny<Action<ICoherenceSync>>()))
                .Callback((ClientID id, ConnectionType connType, Action<ICoherenceSync> onLoaded) =>
                {
                    var assetID = $"MOCK ASSET: {connType.ToString()}";

                    var (mockSync, _) = new MockSyncBuilder()
                        .SetAssetID(assetID)
                        .Build();

                    onLoaded(mockSync.Object);
                });

            // Act
            var builder = MockNetworkedEntityBuilder.CreateNetworkedEntity(entityA)
                .Comps(comps)
                .ClientID(client0);

            if (isMyConnection)
            {
                builder.SetHasStateAuthority();
            }

            builder.Build(mockClient);

            // Assert - that the client entity was added to the connections but not the public entities info.
            Assert.That(entitiesManager.EntityCount, Is.EqualTo(0));
            Assert.That(entitiesManager.NetworkEntities, Is.Empty);
            Assert.That(entitiesManager.ContainsEntity(entityA), Is.True);
            Assert.That(entitiesManager.ConnectionEntityID, Is.EqualTo(isMyConnection ? entityA : Entity.InvalidRelative));

            // EntitiesManager is not guaranteed to register the connection immediately.
            await WaitUntilConnectionsIsNotEmpty(5d);

            // verify that the connection was added to the connection manager from the entities manager only once.
            mockConnectionManager.Verify(manager =>
                manager.Add(It.Is<CoherenceClientConnection>(connection => connections.Contains(connection))),
                Times.Once);
        }

        [Test]
        [Description("Verifies that when a network entity is created that the OnNetworkEntityCreatedInvoke is called.")]
        public void Test_OnNetworkEntityCreated_Is_Invoked()
        {
            // Arrange
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            // Act
            CreateNetworkedEntity(entityA, comps, Entity.InvalidRelative);

            // Assert - that the right entity was created.
            AssertNetworkEntityCreated(entityA);
        }

        [Test]
        [Description("Verifies that multiple entities can be created.")]
        public void Test_CreateMultipleEntities()
        {
            // Arrange
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            // Act
            var sync = CreateNetworkedEntity(entityA, comps, Entity.InvalidRelative);
            var stateA = OnPrefabInstantiated(sync).state;

            sync = CreateNetworkedEntity(entityB, comps, Entity.InvalidRelative);
            var stateB = OnPrefabInstantiated(sync).state;

            // Assert
            AssertNetworkEntityCreated(entityA);
            AssertNetworkEntityCreated(entityB);

            Assert.That(entitiesManager.EntityCount, Is.EqualTo(2));
            Assert.That(entitiesManager.NetworkEntities, Has.Member(stateA));
            Assert.That(entitiesManager.NetworkEntities, Has.Member(stateB));
        }

        [Test]
        [Description("Verifies that created entites are unsynced until their state is synced.")]
        public void Test_CreatedEntitySyncedCorrectly()
        {
            // Arrange
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            // Act 1
            var sync = CreateNetworkedEntity(entityA, comps, Entity.InvalidRelative);

            // Assert 1 - the created entity is not synced
            mockBridge.Verify(bridge =>
                bridge.OnNetworkEntityCreatedInvoke(It.Is<NetworkEntityState>(state =>
                    state.Sync == null)), Times.Once);

            // Act 2 - sync it
            var info = OnPrefabInstantiated(sync);

            // Assert - correctly synced now
            Assert.That(info.state.Sync, Is.EqualTo(sync));
            Assert.False(info.disabled);
        }

        [Test]
        [Description("Verifies that the EM can get the entity state for a synced entity.")]
        public void Test_CanGetEntityStateForSyncedEntity()
        {
            // Arrange
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            // Act
            var sync = CreateNetworkedEntity(entityA, comps, Entity.InvalidRelative);
            OnPrefabInstantiated(sync);

            // Assert
            var state = entitiesManager.GetNetworkEntityStateForEntity(entityA);
            Assert.That(state, Is.Not.Null);
            Assert.That(state.Sync, Is.EqualTo(sync));
        }

        [Test]
        [Description("Verifies that the returned entity state for synced entities is not null.")]
        public void Test_GetNetworkEntityStateForEntityReturnsState()
        {
            // Arrange
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            // Act
            var sync = CreateNetworkedEntity(entityA, comps, Entity.InvalidRelative);
            OnPrefabInstantiated(sync);

            // Create a second entity to ensure we get the right one.
            CreateNetworkedEntity(entityB, comps, Entity.InvalidRelative);

            // Assert
            var state = entitiesManager.GetNetworkEntityStateForEntity(entityA);
            Assert.That(state, Is.Not.Null);
            Assert.That(state.EntityID, Is.EqualTo(entityA));
        }

        [Test]
        [Description("Verifies that the returned entity state unknown entities is null.")]
        public void Test_GetNetworkEntityStateForUnknownEntityReturnsNullState()
        {
            // Arrange
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            // Act & Assert
            var state = entitiesManager.GetNetworkEntityStateForEntity(new Entity(66, 0, Entity.Relative));
            Assert.That(state, Is.Null);
        }

        [TestCase(true)]
        [TestCase(false)]
        [Description("Verifies that GetCoherenceSyncForEntity returns the correct entity if one has been mapped.")]
        public void Test_GetCoherenceSyncForEntityReturnsCorrectly(bool instantiated)
        {
            // Arrange
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            // Act
            var sync = CreateNetworkedEntity(entityA, comps, Entity.InvalidRelative);
            if (instantiated)
            {
                OnPrefabInstantiated(sync);
            }

            // Make some extra entities
            CreateNetworkedEntity(entityB, comps, Entity.InvalidRelative);
            var syncC = CreateNetworkedEntity(entityC, comps, Entity.InvalidRelative);
            OnPrefabInstantiated(syncC);

            // Assert
            var returnedSync = entitiesManager.GetCoherenceSyncForEntity(entityA);
            if (instantiated)
            {
                Assert.That(returnedSync, Is.EqualTo(sync));
            }
            else
            {
                Assert.That(returnedSync, Is.Null);
            }
        }

        [Test]
        [Description("Verify that when an entity with a parent is created but the parent isn't " +
            "that the instantiation callback is not called back.")]
        public void Test_DelayInstantiationForMissingParent()
        {
            // Arrange
            var parent = new Entity(2, 0, Entity.Relative);
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            // Act
            var sync = CreateNetworkedEntity(entityA, comps, parent);

            // Assert
            // Instantiation is delayed because the parent is not there yet.
            mockBridge.Verify(bridge =>
                bridge.OnNetworkEntityCreatedInvoke(It.IsAny<NetworkEntityState>()), Times.Never);
        }

        [Test]
        [Description("Verify that a deferred child instantiation waiting for a parent happens after the " +
            "EM is updated and only one at a time.")]
        public void Test_DelayedInstantiationHappensAfterUpdateOnce()
        {
            // Arrange
            var child = entityA;
            var child2 = entityB;
            var parent = entityC;
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            var childSync = CreateNetworkedEntity(child, comps, parent);
            _ = CreateNetworkedEntity(child2, comps, child);

            var parentSync = CreateNetworkedEntity(parent, comps, Entity.InvalidRelative);
            _ = OnPrefabInstantiated(parentSync);

            // ensure that neither child was instantiated.
            mockBridge.Verify(bridge =>
                bridge.OnNetworkEntityCreatedInvoke(It.Is<NetworkEntityState>(state => state.EntityID == child)), Times.Never);
            mockBridge.Verify(bridge =>
                bridge.OnNetworkEntityCreatedInvoke(It.Is<NetworkEntityState>(state => state.EntityID == child2)), Times.Never);

            // Act 1 - update causes the child to be instantiated since it was waiting for parent.
            entitiesManager.Update();

            // Assert 1 - only the first child is instantiated (saves from a stack killing chain)
            AssertNetworkEntityCreated(child);

            _ = OnPrefabInstantiated(childSync);

            mockBridge.Verify(bridge =>
                bridge.OnNetworkEntityCreatedInvoke(It.Is<NetworkEntityState>(state => state.EntityID == child2)), Times.Never);

            // Act 2 - update causes the second child to be instantiated since it was waiting for the first.
            entitiesManager.Update();

            // Assert 2
            AssertNetworkEntityCreated(child2);
        }

        [TestCase(true, TestName = "Parent First")]
        [TestCase(false, TestName = "Child First")]
        [Description("Verify that networked parent and child are both created regardless of the order " +
            "of creation from the client.")]
        public void Test_ChildEntityCreatedWithParentInAnyOrder(bool createParentFirst)
        {
            // Arrange
            var child = entityA;
            var parent = new Entity(2, 0, Entity.Relative);
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            // Act
            if (createParentFirst)
            {
                // Parent created and instantiated before the child is created.
                var parentSync = CreateNetworkedEntity(parent, comps, Entity.InvalidRelative);
                OnPrefabInstantiated(parentSync);

                CreateNetworkedEntity(child, comps, parent);
            }
            else
            {
                // Child created before the parent is created and instantiated.
                // expect the child create to defer the OnNetworkEntityCreated callback
                // until the parent is created and instantiated.
                CreateNetworkedEntity(child, comps, parent);

                var parentSync = CreateNetworkedEntity(parent, comps, Entity.InvalidRelative);
                OnPrefabInstantiated(parentSync);

                // need to tick the EM since the child was deferred waiting for the parent's instantiation.
                entitiesManager.Update();
            }

            // Assert
            AssertNetworkEntityCreated(child);
        }

        // https://app.zenhub.com/workspaces/engine-group-5fb3b64dabadec002057e6f2/issues/gh/coherence/unity/5289
        // A -> B -> C where a is the parent root of a chain of references. 3! permutations manually outlined
        // for clarity.
        [TestCase((object)new[] { "A", "B", "C" }, TestName = "Create order: A B C")]
        [TestCase((object)new[] { "A", "C", "B" }, TestName = "Create order: A C B")]
        [TestCase((object)new[] { "B", "A", "C" }, TestName = "Create order: B A C")]
        [TestCase((object)new[] { "B", "C", "A" }, TestName = "Create order: B C A")]
        [TestCase((object)new[] { "C", "A", "B" }, TestName = "Create order: C A B")]
        [TestCase((object)new[] { "C", "B", "A" }, TestName = "Create order: C B A")]
        [Description("Verify that a hierarchy of entities created in any order will eventually resolve itself.")]
        public void Test_HierarchyCreatedInAnyOrder(string[] createOrder)
        {
            // Arrange
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            ICoherenceSync bSync = null;

            // Act
            foreach (var create in createOrder)
            {
                switch (create)
                {
                    case "A":
                        var aSync = CreateNetworkedEntity(entityA, comps, Entity.InvalidRelative);
                        OnPrefabInstantiated(aSync);

                        // need to tick the EM since the child was deferred waiting for the parent's instantiation.
                        entitiesManager.Update();
                        break;
                    case "B":
                        bSync = CreateNetworkedEntity(entityB, comps, entityA);
                        break;
                    case "C":
                        CreateNetworkedEntity(entityC, comps, entityB);
                        break;
                }
            }

            AssertNetworkEntityCreated(entityB);
            OnPrefabInstantiated(bSync);

            // need to tick the EM since the child was deferred waiting for the parent's instantiation.
            entitiesManager.Update();

            // Assert
            AssertNetworkEntityCreated(entityC);
        }

        [Test]
        [Description("Verifies that sync updater calls SampleBindings, InterpolateBindings, InvokeCallbacks" +
            " and SyncAndSend in the appropriate coherence loop steps.")]
        public void Test_UpdaterMethodCallsCorrectForDifferentLoops()
        {
            // Arrange

            // Create an entity that is updated only in Update
            var updateUpdater = SetupUpdaterMock(entityA, CoherenceSync.InterpolationLoop.Update);

            // Create an entity that is updated only in LateUpdate
            var lateUpdateUpdater = SetupUpdaterMock(entityB, CoherenceSync.InterpolationLoop.LateUpdate);

            // Create an entity that is updated only in FixedUpdate
            var fixedUpdateUpdater = SetupUpdaterMock(entityC, CoherenceSync.InterpolationLoop.FixedUpdate);

            // Create an entity that is updated in Update and FixedUpdate
            var updateAndFixedUpdater = SetupUpdaterMock(entityD, CoherenceSync.InterpolationLoop.UpdateAndFixedUpdate);

            // Create an entity that is updated in LateUpdate and FixedUpdate
            var lateUpdateAndFixedUpdateUpdater = SetupUpdaterMock(entityE, CoherenceSync.InterpolationLoop.LateUpdateAndFixedUpdate);

            // Act
            entitiesManager.SampleBindings(CoherenceSync.InterpolationLoop.Update);
            entitiesManager.InterpolateBindings(CoherenceSync.InterpolationLoop.Update);
            entitiesManager.InvokeCallbacks(CoherenceSync.InterpolationLoop.Update);

            entitiesManager.SampleBindings(CoherenceSync.InterpolationLoop.LateUpdate);
            entitiesManager.InterpolateBindings(CoherenceSync.InterpolationLoop.LateUpdate);
            entitiesManager.InvokeCallbacks(CoherenceSync.InterpolationLoop.LateUpdate);

            entitiesManager.SampleBindings(CoherenceSync.InterpolationLoop.FixedUpdate);
            entitiesManager.InterpolateBindings(CoherenceSync.InterpolationLoop.FixedUpdate);
            entitiesManager.InvokeCallbacks(CoherenceSync.InterpolationLoop.FixedUpdate);

            entitiesManager.SyncAndSend();

            // Assert that each updater is updated the correct number of times.
            VerifyUpdaterMock(updateUpdater, CoherenceSync.InterpolationLoop.Update, 1);
            VerifyUpdaterMock(lateUpdateUpdater, CoherenceSync.InterpolationLoop.LateUpdate, 1);
            VerifyUpdaterMock(fixedUpdateUpdater, CoherenceSync.InterpolationLoop.FixedUpdate, 1);
            VerifyUpdaterMock(updateAndFixedUpdater, CoherenceSync.InterpolationLoop.UpdateAndFixedUpdate, 2);
            VerifyUpdaterMock(lateUpdateAndFixedUpdateUpdater, CoherenceSync.InterpolationLoop.LateUpdateAndFixedUpdate, 2);
        }

        [Test]
        [Description("Verifies that an invalid entity ID is returned if the parameter is null.")]
        public void Test_EntityIDFromUnityTypeWorks_Nulls()
        {
            Assert.That(EntitiesManager.UnityObjectToEntityId((GameObject)null), Is.EqualTo(Entity.InvalidRelative));
            Assert.That(EntitiesManager.UnityObjectToEntityId((Transform)null), Is.EqualTo(Entity.InvalidRelative));
            Assert.That(EntitiesManager.UnityObjectToEntityId((ICoherenceSync)null), Is.EqualTo(Entity.InvalidRelative));
        }

        [Test]
        [Description("Verifies that an invalid entity ID is returned if the parameter doesn't have a CoherenceSync behaviour.")]
        public void Test_EntityIDFromUnityTypeWorks_NoCoherenceSync()
        {
            // A GameObject with no CoherenceSync.
            var go = new GameObject();
            Assert.That(EntitiesManager.UnityObjectToEntityId(go), Is.EqualTo(Entity.InvalidRelative));
            Assert.That(EntitiesManager.UnityObjectToEntityId(go.transform), Is.EqualTo(Entity.InvalidRelative)); ;
        }

        [Test]
        [Description("Verifies that an invalid entity ID is returned if the Unity object has a CoherenceSync but it is not " +
            "Syched on the network yet. This means there is no NetworkEntityState assigned.")]
        public void Test_EntityIDFromUnityTypeWorks_IsNotSynced()
        {
            // A GameObject with an unsynced CoherenceSync
            var go = new GameObject();
            var sync = go.AddComponent<CoherenceSync>();
            Assert.That(EntitiesManager.UnityObjectToEntityId(go), Is.EqualTo(Entity.InvalidRelative));
            Assert.That(EntitiesManager.UnityObjectToEntityId(go.transform), Is.EqualTo(Entity.InvalidRelative));
            Assert.That(EntitiesManager.UnityObjectToEntityId((ICoherenceSync)sync), Is.EqualTo(Entity.InvalidRelative));
        }


        [Test]
        [Description("Verifies that a valid entity ID is returned if the Unity object has a CoherenceSync and is " +
            "Syched on the network. This means there is a NetworkEntityState assigned.")]
        public void Test_EntityIDFromUnityTypeWorks_IsSynced()
        {
            // A GameObject with a "synced" CoherenceSync.
            // Dislike this since it makes assumptions about the content of the sync but
            // doesn't actually set it up correctly.  Can't use a mock for this since
            // can't call AddComponent<ICoherenceSync> with an instantiated mock.
            var go = new GameObject();
            var sync = go.AddComponent<CoherenceSync>();
            sync.EntityState = new NetworkEntityState(entityA, AuthorityType.Full, false, false, sync, string.Empty);
            Assert.That(EntitiesManager.UnityObjectToEntityId(go), Is.EqualTo(entityA));
            Assert.That(EntitiesManager.UnityObjectToEntityId(go.transform), Is.EqualTo(entityA));
            Assert.That(EntitiesManager.UnityObjectToEntityId((ICoherenceSync)sync), Is.EqualTo(entityA));
        }

        [Test]
        [Description("Verifies that null is returned if the parameter is an invalid entity ID.")]
        public void Test_EntityIDToUnityTypesWorks_InvalidID()
        {
            Assert.That(entitiesManager.EntityIdToGameObject(Entity.InvalidRelative), Is.Null);
            Assert.That(entitiesManager.EntityIdToTransform(Entity.InvalidRelative), Is.Null);
            Assert.That(entitiesManager.EntityIdToRectTransform(Entity.InvalidRelative), Is.Null);
            Assert.That(entitiesManager.EntityIdToCoherenceSync(Entity.InvalidRelative), Is.Null);
        }

        [Test]
        [Description("Verifies that null is returned if entity parameter is not synced.")]
        public void Test_EntityIDToUnityTypesWorks_NotSynced()
        {
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            // Unsynced entity
            var result = CreateNetworkedEntityWithResult(entityA, comps, Entity.InvalidRelative);
            Assert.That(entitiesManager.EntityIdToGameObject(entityA), Is.Null);
            Assert.That(entitiesManager.EntityIdToTransform(entityA), Is.Null);
            Assert.That(entitiesManager.EntityIdToRectTransform(entityA), Is.Null);
            Assert.That(entitiesManager.EntityIdToCoherenceSync(entityA), Is.Null);
        }

        [Test]
        [Description("Verifies that the proper non-null object is returned if entity parameter is synced.")]
        public void Test_EntityIDToUnityTypesWorks_Synced()
        {
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            // Adding another synced object to make sure we get the right one when we
            // test the synced entity.
            var otherSync = CreateNetworkedEntity(entityA, comps, Entity.InvalidRelative);
            OnPrefabInstantiated(otherSync);

            // Synced entity
            var result = CreateNetworkedEntityWithResult(entityB, comps, Entity.InvalidRelative);
            var go = new GameObject("test");
            var rectTransform = go.AddComponent<RectTransform>();
            result.mockSync.Setup(sync => sync.gameObject).Returns(go);
            result.mockSync.Setup(sync => sync.transform).Returns(go.transform);

            OnPrefabInstantiated(result.mockSync.Object);

            Assert.That(entitiesManager.EntityIdToGameObject(entityB), Is.EqualTo(go));
            Assert.That(entitiesManager.EntityIdToTransform(entityB), Is.EqualTo(go.transform));
            Assert.That(entitiesManager.EntityIdToRectTransform(entityB), Is.EqualTo(rectTransform));
            Assert.That(entitiesManager.EntityIdToCoherenceSync(entityB), Is.EqualTo(result.mockSync.Object));
        }

        [Test]
        [Description("Verifies that after a unique entity is resolved with a remote entity," +
            "that the original entity state is removed from the mapper (even before the DuplicateDestroys comes)." +
            "This is important so the original entity state isn't in the mapper without an underlying sync instance, " +
            "which would be an illegal state.")]
        public void Test_UniqueResolved_ShouldRemoveFromMapper()
        {
            var uuid = "UNIQUE_ID";

            var (local, _) = new MockSyncBuilder().SetManualUUID(uuid).SetIsUnique(true).Build();

            local.Setup(s => s.InitializeReplacedUniqueObject(It.IsAny<SpawnInfo>())).Callback(() => entitiesManager.SyncNetworkEntityState(local.Object));

            var assetID = $"MOCK ASSET : {entityA}";
            var config = (CoherenceSyncConfig)ScriptableObject.CreateInstance(typeof(CoherenceSyncConfig));
            config.Init(assetID);
            local.Setup(sync => sync.CoherenceSyncConfig).Returns(config);

            var (localState, _, _, _) = entitiesManager.SyncNetworkEntityState(local.Object);

            local.Setup(s => s.EntityState).Returns(localState);

            CreateNetworkedEntityWithResult(entityA, new ICoherenceComponentData[] { }, Entity.InvalidRelative, uuid: uuid);

            Assert.That(entitiesManager.GetNetworkEntityStateForEntity(localState.EntityID), Is.Null);
        }

        private Mock<ICoherenceSyncUpdater> SetupUpdaterMock(Entity entity, CoherenceSync.InterpolationLoop loop)
        {
            var mockComp = new MockComponentData(42);
            var comps = new ICoherenceComponentData[] { mockComp };

            var result = CreateNetworkedEntityWithResult(entity, comps, Entity.InvalidRelative);
            result.mockSync.Setup(sync => sync.InterpolationLocationConfig).Returns(loop);
            result.mockUpdater.Setup(updater => updater.SampleBindings());
            result.mockUpdater.Setup(updater => updater.InterpolateBindings());
            result.mockUpdater.Setup(updater => updater.InvokeCallbacks());
            result.mockUpdater.Setup(updater => updater.SyncAndSend());
            OnPrefabInstantiated(result.mockSync.Object);

            return result.mockUpdater;
        }

        private void VerifyUpdaterMock(Mock<ICoherenceSyncUpdater> mockUpdater, CoherenceSync.InterpolationLoop loop, int expectedTimes)
        {
            mockUpdater.Verify(updater => updater.SampleBindings(), Times.Exactly(expectedTimes));
            mockUpdater.Verify(updater => updater.InterpolateBindings(), Times.Exactly(expectedTimes));
            mockUpdater.Verify(updater => updater.InvokeCallbacks(), Times.Exactly(expectedTimes));

            mockUpdater.Verify(updater => updater.SyncAndSend(), Times.Once);
        }

        private void AssertNetworkEntityCreated(Entity entity)
        {
            mockBridge.Verify(bridge =>
                bridge.OnNetworkEntityCreatedInvoke(It.Is<NetworkEntityState>(state =>
                    state.EntityID == entity
                )), Times.Once);
        }

        private struct PostNetworkSyncState
        {
            public NetworkEntityState state;
            public ComponentUpdates? updates;
            public uint? LOD;
            public bool disabled;
        }

        // Manually calling what would be called back in OnNetworkEntityCreated so that
        // the order of callbacks can be manipulated since it's possible to make the order
        // arbirtary among several entities.  This simulates the prefab being created
        // and enabled which triggers the CoherenceSync to sync the entity state.
        private PostNetworkSyncState OnPrefabInstantiated(ICoherenceSync sync)
        {
            // Can't legitimately call SyncNetworkEntityState unless
            // the prefab has been created and the OnNetworkEntityCreated callback
            // was invoked.  So ensure this has happened before attempting
            // to sync the entity state.
            AssertNetworkEntityCreated(sync.EntityState.EntityID);

            (NetworkEntityState state, ComponentUpdates? updates, uint? LOD, bool disabled) = entitiesManager.SyncNetworkEntityState(sync);

            return new PostNetworkSyncState()
            {
                state = state,
                updates = updates,
                LOD = LOD,
                disabled = disabled,
            };
        }

        private void MockClientConnected(bool isConnected)
        {
            mockClient.Setup(client => client.IsConnected()).Returns(() => isConnected);
        }

        private ICoherenceSync CreateNetworkedEntity(Entity entityID, ICoherenceComponentData[] comps, Entity parent, ClientID? clientID = null)
        {
            var result = CreateNetworkedEntityWithResult(entityID, comps, parent, clientID);

            return result.mockSync.Object;
        }

        private MockSyncBuilder.Result CreateNetworkedEntityWithResult(Entity entityID, ICoherenceComponentData[] comps, Entity parent,
            ClientID? clientID = null, string uuid = null)
        {
            var mockPrefab = MockPrefab.New();

            var assetID = $"MOCK ASSET : {entityID}";
            var config = (CoherenceSyncConfig)ScriptableObject.CreateInstance(typeof(CoherenceSyncConfig));
            config.Init(assetID);

            mockPrefab.sync.CoherenceSyncConfig = config;

            return MockNetworkedEntityBuilder.CreateNetworkedEntity(entityID)
                .Comps(comps)
                .Prefab(mockPrefab)
                .Parent(parent)
                .ClientID(clientID)
                .UUID(uuid)
                .BuildWithResult(mockClient);
        }

        private async Task WaitUntilConnectionsIsNotEmpty(double maxSecondsToWait)
        {
            var failAfter = EditorApplication.timeSinceStartup + maxSecondsToWait;

            while (connections.Count == 0)
            {
                await Task.Yield();

                if (EditorApplication.timeSinceStartup > failAfter)
                {
                    Assert.Fail($"Time spent waiting for {nameof(connections)} to become non-empty exceeded the maximum time limit of {maxSecondsToWait} seconds.\nconnections: {string.Join(", ", connections.Select(c => c.ClientId + "/" + c.EntityId))}");
                    return;
                }
            }
        }
    }
}
