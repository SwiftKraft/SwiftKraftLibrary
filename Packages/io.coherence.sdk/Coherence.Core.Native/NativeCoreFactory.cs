// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

#if COHERENCE_FEATURE_NATIVE_CORE
namespace Coherence.Core
{
    using Coherence.Connection;
    using Coherence.Entities;
    using Coherence.ProtocolDef;
    using System;
    using Coherence.Log;
    using System.Collections.Concurrent;
    using System.Runtime.InteropServices;
    using System.Linq;
#if UNITY
    using AOT;
#endif

    public static class NativeCoreFactory
    {
        private static UInt32 nextCookie = 1;

        private static readonly ConcurrentDictionary<UInt32, NativeCore> cookieCoreMap = new();

        private static readonly Logger logger = Log.GetLogger(typeof(NativeCoreFactory));

        public static unsafe IClient CreateClient(
            IDataInteropHandler handler,
            string combinedSchema,
            Logger logger
        )
        {
            var cookie = nextCookie++;
            var result = NativeWrapper.ConstructCoherenceCore(
                cookie,
                OnLog,
                OnConnected,
                OnDisconnected,
                OnConnectionError,
                OnEntityCreated,
                OnEntityUpated,
                OnEntityDestroyed,
                OnCommand,
                OnInput,
                OnAuthorityRequested,
                OnAuthorityRequestRejected,
                OnAuthorityChanged,
                OnAuthorityTransferred,
                OnSceneIndexChanged,
                combinedSchema);

            if (result.Context == null)
            {
                throw new InvalidOperationException($"Failed to initialize coherence core. Error code: {result.ErrorCode}");
            }

            var core = new NativeCore(result.Context, handler, logger);
            if (!cookieCoreMap.TryAdd(cookie, core))
            {
                core.Dispose();
                throw new Exception($"Failed to add new core with the cookie {cookie} to the cookie map.");
            }

            if (NativeWrapper.TestInteropStructSizes(result.Context, NativeWrapper.GetInteropStructSizes(), Marshal.SizeOf<InteropStructSizes>()) != 0)
            {
                core.Dispose();
                throw new Exception("Interop struct sizes are not the same as the native struct sizes. Check logs for more info.");
            }

            return core;
        }

        private static NativeCore GetCore(UInt32 cookie)
        {
            if (!cookieCoreMap.TryGetValue(cookie, out NativeCore core))
            {
                logger.Error(Error.CoreNativeCoreFactoryCantFindCookie, ("cookie", cookie));
                return null;
            }

            return core;
        }

        internal static void Remove(NativeCore instance)
        {
            var cookie = cookieCoreMap.FirstOrDefault(kv => kv.Value == instance);

            if (cookie.Value == null)
            {
                logger.Warning(Warning.CoreNativeCoreFactoryCantRemoveInstance);
                return;
            }

            if (!cookieCoreMap.TryRemove(cookie.Key, out _))
            {
                logger.Error(Error.CoreNativeCoreFactoryFailedToRemoveInstance, ("cookie", cookie));
            }
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.OnLog))]
#endif
        internal static void OnLog(UInt32 cookie, Int32 level, string msg)
        {
            GetCore(cookie)?.OnLogCallback(level, msg);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.OnConnectedCallback))]
#endif
        internal static void OnConnected(UInt32 cookie, InteropClientID clientID, InteropEndpointData data)
        {
            GetCore(cookie)?.OnConnectedCallback(clientID, data);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.OnDisconnectedCallback))]
#endif
        internal static void OnDisconnected(UInt32 cookie, ConnectionCloseReason reason)
        {
            GetCore(cookie)?.OnDisconnectedCallback(reason);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.OnConnectionErrorCallback))]
#endif
        internal static void OnConnectionError(UInt32 cookie, string msg)
        {
            GetCore(cookie)?.OnConnectionErrorCallback(msg);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.OnEntityCreatedCallback))]
#endif
        internal static void OnEntityCreated(UInt32 cookie, InteropEntityWithMeta meta, IntPtr data, Int32 length, InteropVector3f floatingOriginDelta)
        {
            GetCore(cookie)?.OnEntityCreatedCallback(meta, data, length, floatingOriginDelta);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.OnEntityUpdatedCallback))]
#endif
        internal static void OnEntityUpated(UInt32 cookie, InteropEntityWithMeta meta, IntPtr updatedComponents, Int32 updatedCount, UIntPtr destroyedComponents, Int32 destroyedCount, InteropVector3f floatingOriginDelta)
        {
            GetCore(cookie)?.OnEntityUpdatedCallback(meta, updatedComponents, updatedCount, destroyedComponents, destroyedCount, floatingOriginDelta);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.OnEntityDestroyCallback))]
#endif
        internal static void OnEntityDestroyed(UInt32 cookie, InteropEntity entity, DestroyReason reason)
        {
            GetCore(cookie)?.OnEntityDestroyedCallback(entity, reason);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.OnCommandCallback))]
#endif
        internal static void OnCommand(UInt32 cookie, InteropEntity entity, MessageTarget target, UInt32 commandType, IntPtr data, Int32 dataSize)
        {
            GetCore(cookie)?.OnCommandCallback(entity, target, commandType, data, dataSize);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.OnInputCallback))]
#endif
        internal static void OnInput(UInt32 cookie, InteropEntity entity, Int64 frame, UInt32 inputType, IntPtr data, Int32 dataSize)
        {
            GetCore(cookie)?.OnInputCallback(entity, frame, inputType, data, dataSize);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.OnAuthorityRequestedCallback))]
#endif
        internal static void OnAuthorityRequested(UInt32 cookie, InteropAuthorityRequest request)
        {
            GetCore(cookie)?.OnAuthorityRequestedCallback(request);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.OnAuthorityRequestRejectedCallback))]
#endif
        internal static void OnAuthorityRequestRejected(UInt32 cookie, InteropAuthorityRequestRejection change)
        {
            GetCore(cookie)?.OnAuthorityRequestRejectedCallback(change);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.OnAuthorityChangedCallback))]
#endif
        internal static void OnAuthorityChanged(UInt32 cookie, InteropAuthorityChange change)
        {
            GetCore(cookie)?.OnAuthorityChangedCallback(change);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.OnAuthorityTransferredCallback))]
#endif
        internal static void OnAuthorityTransferred(UInt32 cookie, InteropEntity entity)
        {
            GetCore(cookie)?.OnAuthorityTransferredCallback(entity);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.OnSceneIndexChangedCallback))]
#endif
        internal static void OnSceneIndexChanged(UInt32 cookie, InteropSceneIndexChange change)
        {
            GetCore(cookie)?.OnSceneIndexChangedCallback(change);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeTransport.TransportFactoryConstruct))]
#endif
        internal static int OnTransportFactoryConstruct(UInt32 cookie, IntPtr outFunctions, UInt16 mtu)
        {
            return GetCore(cookie)?.NativeTransportFactory?.Construct(outFunctions, mtu) ?? -1;
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeTransport.TransportFactoryDestruct))]
#endif
        internal static void OnTransportFactoryDestruct(UInt32 cookie, UInt32 transportCookie)
        {
            GetCore(cookie)?.NativeTransportFactory?.Destruct(transportCookie);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.NetworkTimeOnTimeResetCallback))]
#endif
        internal static void OnTimeReset(UInt32 cookie)
        {
            GetCore(cookie)?.NativeNetworkTime?.OnTimeResetCallback();
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.NetworkTimeOnFixedNetworkUpdateCallback))]
#endif
        internal static void OnFixedNetworkUpdate(UInt32 cookie)
        {
            GetCore(cookie)?.NativeNetworkTime?.OnFixedNetworkUpdateCallback();
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.NetworkTimeOnLateFixedNetworkUpdateCallback))]
#endif
        internal static void OnLateFixedNetworkUpdate(UInt32 cookie)
        {
            GetCore(cookie)?.NativeNetworkTime?.OnLateFixedNetworkUpdateCallback();
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.NetworkTimeOnServerSimulationFrameReceivedCallback))]
#endif
        internal static void OnServerSimulationFrameReceived(UInt32 cookie, InteropAbsoluteSimulationFrame serverSimulationFrame, InteropAbsoluteSimulationFrame clientSimulationFrame)
        {
            GetCore(cookie)?.NativeNetworkTime?.OnServerSimulationFrameReceivedCallback(serverSimulationFrame, clientSimulationFrame);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.DebugOnPacketSentCallback))]
#endif
        internal static void OnDebugPacketSent(UInt32 cookie, IntPtr outgoingEntityUpdates, Int32 count, Int32 totalChanges, UInt32 octetCount)
        {
            GetCore(cookie)?.OnDebugPacketSentCallback(outgoingEntityUpdates, count, totalChanges, octetCount);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.DebugOnPacketReceivedCallback))]
#endif
        internal static void OnDebugPacketReceived(UInt32 cookie, Int32 octetsReceived)
        {
            GetCore(cookie)?.OnDebugPacketReceivedCallback(octetsReceived);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.DebugOnEntityAckedCallback))]
#endif
        internal static void OnDebugEntityAcked(UInt32 cookie, InteropEntity entity)
        {
            GetCore(cookie)?.OnDebugEntityAckedCallback(entity);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.DebugOnNextOutPacketDroppedCallback))]
#endif
        internal static void OnDebugOnNextOutPacketDropped(UInt32 cookie)
        {
            GetCore(cookie)?.OnDebugOnNextOutPacketDroppedCallback();
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeWrapper.DebugOnNextPacketSentCallback))]
#endif
        internal static void OnDebugNextPacketSent(UInt32 cookie)
        {
            GetCore(cookie)?.OnDebugNextPacketSentCallback();
        }
    }
}
#endif
