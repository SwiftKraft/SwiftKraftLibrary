// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Bindings;
    using Bindings.ValueBindings;
    using Entities;
    using Log;
    using ProtocolDef;
    using Utils;
    using UnityEngine;
    using Coherence.Connection;
    using Coherence.Tests;

    public class MockNetworkedEntityBuilder
    {
        private Entity entityID;
        private MockPrefab prefab;
        private ICoherenceComponentData[] comps;
        private Entity parent = Entity.InvalidRelative;
        private ClientID? clientID;
        private bool hasStateAuthority;
        private string uuid = null;
        
        public static MockNetworkedEntityBuilder CreateNetworkedEntity(Entity entityID)
        {
            MockNetworkedEntityBuilder builder = new()
            {
                entityID = entityID,
            };

            return builder;
        }

        public MockNetworkedEntityBuilder Prefab(MockPrefab prefab)
        {
            this.prefab = prefab;

            return this;
        }

        public MockNetworkedEntityBuilder Comps(ICoherenceComponentData[] comps)
        {
            this.comps = comps;

            return this;
        }

        public MockNetworkedEntityBuilder Parent(Entity parent)
        {
            this.parent = parent;

            return this;
        }

        public MockNetworkedEntityBuilder ClientID(ClientID? id)
        {
            this.clientID = id;

            return this;
        }

        public MockNetworkedEntityBuilder UUID(string uuid)
        {
            this.uuid = uuid;

            return this;
        }

        public MockNetworkedEntityBuilder SetHasStateAuthority()
        {
            this.hasStateAuthority = true;

            return this;
        }

        public ICoherenceSync Build(Mock<IClient> client = null)
        {
            var result = BuildWithResult(client);

            return result.mockSync.Object;
        }

        public MockSyncBuilder.Result BuildWithResult(Mock<IClient> client = null)
        {
            var assetID = $"MOCK ASSET : {entityID}";

            var spawnInfo = new SpawnInfo();
            spawnInfo.assetId = assetID;
            spawnInfo.connectedEntity = parent;
            spawnInfo.prefab = prefab?.sync;
            spawnInfo.clientId = clientID;
            spawnInfo.connectionType = ConnectionType.Client;
            spawnInfo.uniqueId = uuid;

            var update = IncomingEntityUpdate.New();
            update.Meta = new EntityWithMeta()
            {
                EntityId = entityID,
                HasMeta = true,
                HasStateAuthority = hasStateAuthority,
                HasInputAuthority = false,
                IsOrphan = false,
                LOD = 0,
                Operation = EntityOperation.Create,
                DestroyReason = DestroyReason.BadReason,
            };

            var result = new MockSyncBuilder()
                .EntityID(entityID)
                .SetInitialComps(comps)
                .SetUpdate(update)
                .SetSpawnInfo(spawnInfo, true)
                .SetUUID(uuid)
                .Build();

            client?.Raise(client => client.OnEntityCreated += null, entityID, update);

            return result;
        }
    }
}
