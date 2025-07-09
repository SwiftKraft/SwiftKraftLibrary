// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using System;
    using System.Collections.Generic;
    using Entities;
    using Moq;
    using UnityEngine;
    using static CoherenceSync;

    public sealed class MockSyncBuilder
    {
        private static readonly Entity DefaultID = new(1, 0, Entity.Relative);

        private Entity entityID = DefaultID;
        private AuthorityType authorityType = AuthorityType.Full;
        private bool isOrphaned;
        private bool isNetworkInstantiated;
        private bool isUnique;
        private string uuid = string.Empty;
        private string manualUUID = string.Empty;
        private SimulationType simulationTypeConfig = SimulationType.ClientSide;
        private bool isChildFromSyncGroup;
        private ICoherenceComponentData[] initialComps = Array.Empty<ICoherenceComponentData>();
        private CoherenceSyncBaked bakedScript;
        private InterpolationLoop interpolationLocationConfig = InterpolationLoop.Update;
        private bool hasInput;
        private string assetID = string.Empty;
        private IncomingEntityUpdate? update;
        private SpawnInfo? spawnInfo;
        private bool shouldSpawn = true;
        private UnsyncedNetworkEntityPriority unsyncedEntityPriority = UnsyncedNetworkEntityPriority.AssetId;
        private Func<bool> adopt;
        private Func<bool> requestAuthority;
        private AuthorityType requestAuthorityType;
        private string name = "MockSync";
        private Action onHandleDisconnected;
        private Exception handleDisconnectedThrows;
        private Exception instantiatorDestroyThrows;

        public MockSyncBuilder Name(string name)
        {
            this.name = name;
            return this;
        }

        public MockSyncBuilder EntityID(Entity entityID)
        {
            this.entityID = entityID;
            return this;
        }

        public MockSyncBuilder SetAuthorityType(AuthorityType authorityType)
        {
            this.authorityType = authorityType;
            return this;
        }

        public MockSyncBuilder SetSimulationTypeConfig(SimulationType simulationTypeConfig)
        {
            this.simulationTypeConfig = simulationTypeConfig;
            return this;
        }

        public MockSyncBuilder SetIsOrphaned(bool isOrphaned = true)
        {
            this.isOrphaned = isOrphaned;
            return this;
        }

        public MockSyncBuilder SetHandleDisconnected(Action onHandleDisconnected)
        {
            this.onHandleDisconnected = onHandleDisconnected;
            return this;
        }

        public MockSyncBuilder SetHandleDisconnected(Exception handleDisconnectedThrows)
        {
            this.handleDisconnectedThrows = handleDisconnectedThrows;
            return this;
        }

        public MockSyncBuilder SetIsNetworkInstantiated(bool isNetworkInstantiated = true)
        {
            this.isNetworkInstantiated = isNetworkInstantiated;
            return this;
        }

        public MockSyncBuilder SetUUID(string uuid)
        {
            this.uuid = uuid;
            return this;
        }

        public MockSyncBuilder SetIsUnique(bool isUnique = true)
        {
            this.isUnique = isUnique;
            return this;
        }

        public MockSyncBuilder SetManualUUID(string manualUUID)
        {
            this.manualUUID = manualUUID;
            return this;
        }

        public MockSyncBuilder SetIsChildFromSyncGroup(bool isChildFromSyncGroup = true)
        {
            this.isChildFromSyncGroup = isChildFromSyncGroup;
            return this;
        }

        public MockSyncBuilder SetInitialComps(ICoherenceComponentData[] initialComps)
        {
            this.initialComps = initialComps;
            return this;
        }

        public MockSyncBuilder SetBakedScript(CoherenceSyncBaked bakedScript)
        {
            this.bakedScript = bakedScript;
            return this;
        }

        public MockSyncBuilder SetInterpolationLocationConfig(InterpolationLoop interpolationLocationConfig)
        {
            this.interpolationLocationConfig = interpolationLocationConfig;
            return this;
        }

        public MockSyncBuilder SetHasInput(bool hasInput = true)
        {
            this.hasInput = hasInput;
            return this;
        }

        public MockSyncBuilder SetAssetID(string ID)
        {
            assetID = ID;
            return this;
        }

        public MockSyncBuilder SetUpdate(IncomingEntityUpdate update)
        {
            this.update = update;
            return this;
        }

        public MockSyncBuilder SetSpawnInfo(SpawnInfo spawnInfo, bool shouldSpawn)
        {
            this.spawnInfo = spawnInfo;
            this.shouldSpawn = shouldSpawn;
            return this;
        }

        public MockSyncBuilder SetUnsyncedEntityPriority(UnsyncedNetworkEntityPriority unsyncedEntityPriority)
        {
            this.unsyncedEntityPriority = unsyncedEntityPriority;
            return this;
        }

        public MockSyncBuilder SetAdoptReturns(bool result) => SetAdoptReturns(() => result);

        public MockSyncBuilder SetAdoptReturns(Func<bool> adopt)
        {
            this.adopt = adopt;
            return this;
        }

        public MockSyncBuilder SetRequestAuthorityReturns(AuthorityType authorityType, bool result)
            => SetRequestAuthorityReturns(authorityType, () => result);

        public MockSyncBuilder SetRequestAuthorityReturns(AuthorityType authorityType, Func<bool> requestAuthority)
        {
            this.requestAuthority = requestAuthority;
            this.requestAuthorityType = authorityType;
            return this;
        }

        public MockSyncBuilder SetInstantiatorDestroy(Exception instantiatorDestroyThrows)
        {
            this.instantiatorDestroyThrows = instantiatorDestroyThrows;
            return this;
        }

        public Result Build()
        {
            var mockUpdater = new Mock<ICoherenceSyncUpdater>(MockBehavior.Strict);
            _ = mockUpdater.Setup(updater => updater.SampleAllBindings());
            _ = mockUpdater.Setup(updater => updater.GetComponentUpdates(It.IsAny<List<ICoherenceComponentData>>(), It.IsAny<bool>()));

            var mockSync = new Mock<ICoherenceSync>(MockBehavior.Strict);
            var config = ScriptableObject.CreateInstance<CoherenceSyncConfig>();
            config.Init(spawnInfo?.assetId ?? assetID);

            SetupMockInstantiator();

            var networkObjectProvider = new Mock<INetworkObjectProvider>(MockBehavior.Strict);
            _ = networkObjectProvider.Setup(provider => provider.LoadAsset(It.IsAny<string>(), It.IsAny<Action<ICoherenceSync>>()))
                .Callback((string _, Action<ICoherenceSync> onLoaded) => onLoaded(spawnInfo?.prefab));
            config.Provider = networkObjectProvider.Object;
            _ = CoherenceSyncConfigRegistry.Instance.Register(config);

            var entityState = new NetworkEntityState(entityID, authorityType, isOrphaned, isNetworkInstantiated, mockSync.Object, uuid);
            _ = mockSync.Setup(sync => sync.EntityState).Returns(entityState);
            _ = mockSync.Setup(sync => sync.HasStateAuthority).Returns(authorityType is AuthorityType.State or AuthorityType.Full);
            _ = mockSync.Setup(sync => sync.IsSynchronizedWithNetwork).Returns(true);
            _ = mockSync.Setup(sync => sync.CoherenceSyncConfig).Returns(config);
            _ = mockSync.Setup(sync => sync.ManualUniqueId).Returns(manualUUID);
            _ = mockSync.Setup(sync => sync.SimulationTypeConfig).Returns(simulationTypeConfig);
            _ = mockSync.Setup(sync => sync.IsUnique).Returns(isUnique);
            _ = mockSync.Setup(sync => sync.IsChildFromSyncGroup()).Returns(isChildFromSyncGroup);
            _ = mockSync.Setup(sync => sync.BakedScript).Returns(bakedScript);
            _ = mockSync.Setup(sync => sync.InterpolationLocationConfig).Returns(interpolationLocationConfig);
            _ = mockSync.Setup(sync => sync.HasInput).Returns(hasInput);
            _ = mockSync.Setup(sync => sync.IsOrphaned).Returns(isOrphaned);
            _ = mockSync.Setup(sync => sync.Updater).Returns(mockUpdater.Object);
            _ = mockSync.Setup(sync => sync.UnsyncedEntityPriority).Returns(unsyncedEntityPriority);
            _ = mockSync.Setup(sync => sync.name).Returns(this.name);
            SetupHandleDisconnected();

            if (adopt is not null)
            {
                _ = mockSync.Setup(sync => sync.Adopt()).Returns(adopt);
            }

            if (requestAuthority is not null)
            {
                _ = mockSync.Setup(sync => sync.RequestAuthority(requestAuthorityType)).Returns(requestAuthority);
            }

            PrepareSyncInitialComps(initialComps);

            if (update != null && spawnInfo != null)
            {
                MockSpawnInfo.Instance.SetSpawnInfo(update.Value, spawnInfo.Value, shouldSpawn);
            }

            return new(mockSync, mockUpdater);

            void SetupMockInstantiator()
            {
                var mockInstantiator = new Mock<INetworkObjectInstantiator>(MockBehavior.Strict);
                _ = mockInstantiator.Setup(instantiator => instantiator.Instantiate(It.IsAny<SpawnInfo>())).Returns(mockSync.Object);
                if (instantiatorDestroyThrows is not null)
                {
                    _ = mockInstantiator.Setup(instantiator => instantiator.Destroy(It.IsAny<ICoherenceSync>())).Throws(instantiatorDestroyThrows);
                }

                config.Instantiator = mockInstantiator.Object;
            }

            void SetupHandleDisconnected()
            {
                onHandleDisconnected ??= () => { };
                _ = mockSync.Setup(client => client.HandleDisconnected()).Callback(onHandleDisconnected);

                if (handleDisconnectedThrows is not null)
                {
                    _ = mockSync.Setup(client => client.HandleDisconnected()).Throws(handleDisconnectedThrows);
                }
            }

            static void PrepareSyncInitialComps(ICoherenceComponentData[] comps) => Impl.CreateInitialComponents = (_, _, _, _) => comps;
        }

        public readonly struct Result
        {
            public readonly Mock<ICoherenceSync> mockSync;
            public readonly Mock<ICoherenceSyncUpdater> mockUpdater;

            public Result(Mock<ICoherenceSync> mockSync, Mock<ICoherenceSyncUpdater> mockUpdater)
            {
                this.mockSync = mockSync;
                this.mockUpdater = mockUpdater;
            }

            public void Deconstruct(out Mock<ICoherenceSync> mockSync, out Mock<ICoherenceSyncUpdater> mockUpdater)
            {
                mockSync = this.mockSync;
                mockUpdater = this.mockUpdater;
            }

            public static implicit operator Mock<ICoherenceSync>(Result result) => result.mockSync;
        }
    }
}
