// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Coherence.SimulationFrame;
    using Core;
    using Entities;
    using ProtocolDef;
    using System;
    using System.ComponentModel;
    using UnityEngine;
    using Logger = Log.Logger;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Impl
    {
        // Set by ImplSync.gen.cs
        public static Func<uint, string> ComponentNameFromTypeId;
        public static Func<ICoherenceSync, string, bool, AbsoluteSimulationFrame, ICoherenceComponentData[]> CreateInitialComponents;
        public static Func<CoherenceBridge.EventsToken, IEntityCommand, Logger, bool> ReceiveInternalCommand;
        public static Func<Entity, Vector3, Quaternion, Vector3, AbsoluteSimulationFrame, ICoherenceComponentData> CreateConnectedEntityUpdateInternal;
        public static Func<uint> GetConnectedEntityComponentIdInternal;
        public static Action<IClient, Entity, string, AbsoluteSimulationFrame> UpdateTag;
        public static Action<IClient, Entity> RemoveTag;

        // Set by CoherenceBridge.gen.cs
        public static Func<IClient, IncomingEntityUpdate, Logger, (bool, SpawnInfo)> GetSpawnInfo;
        public static Func<IDefinition> GetRootDefinition;
        public static Func<uint> AssetId;
        public static Func<uint, AbsoluteSimulationFrame, ICoherenceComponentData> CreateConnectionSceneUpdateInternal;
        public static Func<IDataInteropHandler> GetDataInteropHandler;

        // Set by CoherenceGlobalQuery.gen.cs
        public static Action<IClient, Entity> AddGlobalQuery;
        public static Action<IClient, Entity> RemoveGlobalQuery;

        // Set by CoherenceLiveQuery.gen.cs
        public static Action<IClient, Entity, float, Vector3, AbsoluteSimulationFrame> UpdateLiveQuery;
        public static Action<IClient, Entity> RemoveLiveQuery;

        // Set by CoherenceTagQuery.gen.cs
        public static Action<IClient, Entity, string, AbsoluteSimulationFrame> UpdateTagQuery;
        public static Action<IClient, Entity> RemoveTagQuery;
    }
}
