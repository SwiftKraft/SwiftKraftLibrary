// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests
{
    using System;
    using System.Collections.Generic;
    using Coherence.Entities;
    using Coherence.Log;

    public class MockSpawnInfo : IDisposable
    {
        private static MockSpawnInfo _instance;
        public static MockSpawnInfo Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MockSpawnInfo();
                }

                return _instance;
            }
        }

        private Dictionary<IncomingEntityUpdate, (bool shouldSpawn, SpawnInfo info)> spawnInfos = new();

        private MockSpawnInfo()
        {
            if (Impl.GetSpawnInfo != this.GetSpawnInfo)
            {
                Impl.GetSpawnInfo = this.GetSpawnInfo;
            }
        }

        public void SetSpawnInfo(IncomingEntityUpdate forUpdate, SpawnInfo spawnInfo, bool shouldSpawn)
        {
            this.spawnInfos[forUpdate] = (shouldSpawn, spawnInfo);
        }

        private (bool, SpawnInfo) GetSpawnInfo(IClient client, IncomingEntityUpdate update, Logger logger)
        {
            if (spawnInfos.TryGetValue(update, out var info))
            {
                return info;
            }

            throw new Exception("No spawn info found for entity update");
        }

        public void Dispose()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
