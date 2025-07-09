// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if COHERENCE_FEATURE_NATIVE_CORE
namespace Coherence.Core
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Brisk;
    using Coherence.SimulationFrame;
    using Common;
    using Connection;
    using Entities;
    using Log;
    using ProtocolDef;
    using Stats;
    using Transport;

    public enum LogLevel : Int32
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Critical = 5,
        Off = 6,
    }

    internal unsafe class NativeCore : IClient, INativeCoreComponentUpdater, INativeCoreCommandSender, INativeCoreInputSender
    {
        public event Action<ClientID> OnConnected;
        public event Action<ConnectionCloseReason> OnDisconnected;
        public event Action<ConnectionException> OnConnectionError;
        public event Action<EndpointData> OnConnectedEndpoint;

        public event Action<Entity, IncomingEntityUpdate> OnEntityCreated;
        public event Action<Entity, IncomingEntityUpdate> OnEntityUpdated;
        public event Action<Entity, DestroyReason> OnEntityDestroyed;

        public event Action<IEntityCommand, MessageTarget, Entity> OnCommand;
        public event Action<IEntityInput, long, Entity> OnInput;

        public event Action<AuthorityRequest> OnAuthorityRequested;
        public event Action<AuthorityRequestRejection> OnAuthorityRequestRejected;
        public event Action<AuthorityChange> OnAuthorityChange;
        public event Action<Entity> OnAuthorityTransferred;

        public event Action<SceneIndexChanged> OnSceneIndexChanged;

        public event Action<Entity> DebugOnEntityAcked;
        public event Action<int> DebugOnPacketReceived;

        private event Action<PacketSentDebugInfo> debugOnPacketSent;
        public event Action<PacketSentDebugInfo> DebugOnPacketSent
        {
            add
            {
                if (debugOnPacketSent == null && context != null)
                {
                    NativeWrapper.DebugSetOnPacketSent(context, NativeCoreFactory.OnDebugPacketSent);
                }

                debugOnPacketSent += value;
            }
            remove
            {
                debugOnPacketSent -= value;

                if (debugOnPacketSent == null && context != null)
                {
                    NativeWrapper.DebugSetOnPacketSent(context, null);
                }
            }
        }

        internal NativeNetworkTime NativeNetworkTime;
        internal NativeTransportFactory NativeTransportFactory;

        private CoherenceContext* context;
        private IDataInteropHandler componentInteropHandler;
        private Logger logger;

        private Action onDebugOnNextOutPacketDroppedCallback;
        private Action onDebugNextPacketSentCallback;

        public NativeCore(CoherenceContext* context, IDataInteropHandler handler, Logger logger)
        {
            this.context = context;
            this.componentInteropHandler = handler;
            this.logger = logger.With<NativeCore>();

            Stats = new Stats();

            NativeNetworkTime = new NativeNetworkTime(this);
            NativeTransportFactory = new NativeTransportFactory(Stats, logger);

            NativeWrapper.DebugSetOnPacketReceived(context, NativeCoreFactory.OnDebugPacketReceived);
            NativeWrapper.DebugSetOnEntityAcked(context, NativeCoreFactory.OnDebugEntityAcked);

            NativeWrapper.SetNetworkTimeOnTimeResetCallback(context, NativeCoreFactory.OnTimeReset);
            NativeWrapper.SetNetworkTimeOnFixedNetworkUpdateCallback(context, NativeCoreFactory.OnFixedNetworkUpdate);
            NativeWrapper.SetNetworkTimeOnLateFixedNetworkUpdateCallback(context, NativeCoreFactory.OnLateFixedNetworkUpdate);
            NativeWrapper.SetNetworkTimeOnServerSimulationFrameReceivedCallback(context, NativeCoreFactory.OnServerSimulationFrameReceived);
        }

        public ClientID ClientID => context != null ? NativeWrapper.GetClientID(context).Into() : default;

        public INetworkTime NetworkTime => this.NativeNetworkTime;

        public ConnectionType ConnectionType => context != null ? NativeWrapper.GetConnectionType(context) : default;

        public string Hostname
        {
            get
            {
                if (context == null)
                {
                    return "";
                }

                var val = NativeWrapper.GetHostName(context);
                return Marshal.PtrToStringUTF8(val);
            }
        }

        public Stats Stats { private set; get; }

        public ConnectionState ConnectionState => context != null ? NativeWrapper.GetConnectionState(context) : default;

        public Ping Ping => context != null ? NativeWrapper.GetPing(context).Into() : default;

        public EndpointData LastEndpointData => context != null ? NativeWrapper.GetLastEndpointData(context).Into() : default;

        public ConnectionSettings ConnectionSettings => context != null ? NativeWrapper.GetConnectionSettings(context).Into() : default;

        public uint InitialScene
        {
            get => context != null ? NativeWrapper.GetInitialScene(context) : default;
            set
            {
                if (context == null)
                {
                    return;
                }

                NativeWrapper.SetInitialScene(context, value);
            }
        }

        public byte SendFrequency => context != null ? NativeWrapper.GetSendFrequency(context) : default;

        public void Connect(EndpointData data,
            ConnectionSettings connectionSettings,
            ConnectionType connectionType = ConnectionType.Client)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.Connect(context, new InteropEndpointData(data), new InteropConnectionSettings(connectionSettings), connectionType);
        }

        public bool IsConnected() => context != null && NativeWrapper.IsConnected(context);
        public bool IsDisconnected() => context != null && NativeWrapper.IsDisconnected(context);
        public bool IsConnecting() => context != null && NativeWrapper.IsConnecting(context);

        public void UpdateReceiving()
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.Receive(context);
        }

        public void UpdateSending()
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.Send(context);
        }

        public void Disconnect()
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.Disconnect(context);
        }

        public void Reconnect()
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.Reconnect(context);
        }

        public Entity CreateEntity(ICoherenceComponentData[] data, bool orphan)
        {
            if (context == null)
            {
                return default;
            }

            var entity = NativeWrapper.CreateEntity(context, orphan);

            UpdateComponents(entity.Into(), data);

            return entity.Into();
        }

        public bool CanSendUpdates(Entity id) => context != null && NativeWrapper.CanSendUpdates(context, new InteropEntity(id));

        public void UpdateComponents(Entity id, ICoherenceComponentData[] data)
        {
            if (context == null)
            {
                return;
            }

            for (var i = 0; i < data.Length; i++)
            {
                componentInteropHandler.UpdateComponent(this, new InteropEntity(id), data[i]);
            }
        }

        void INativeCoreComponentUpdater.UpdateComponent<T>(InteropEntity entity, UInt32 componentId, T component,
            Int32 dataSize, UInt32 fieldMask, UInt32 stoppedMask, Int64[] frames)
        {
            if (context == null)
            {
                return;
            }

            fixed (Int64* fp = frames)
            {
                NativeWrapper.UpdateComponent(context, entity, new ComponentDataContainer
                {
                    ComponentID = componentId,
                    FieldMask = fieldMask,
                    StoppedMask = stoppedMask,
                    Data = new IntPtr(&component),
                    DataSize = dataSize,
                    SimFrames = (InteropAbsoluteSimulationFrame*)fp,
                    SimFrameCount = frames?.Length ?? 0
                });
            }
        }

        public void RemoveComponents(Entity id, uint[] data)
        {
            if (context == null)
            {
                return;
            }

            foreach (var comp in data)
            {
                NativeWrapper.RemoveComponent(context, new InteropEntity(id), comp);
            }
        }

        public void DestroyEntity(Entity id)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.DestroyEntity(context, new InteropEntity(id));
        }

        public bool EntityExists(Entity id) => context != null && NativeWrapper.EntityExists(context, new InteropEntity(id));

        public bool HasAuthorityOverEntity(Entity entity, AuthorityType authorityType)
            => context != null && NativeWrapper.HasAuthorityOverEntity(context, new InteropEntity(entity), authorityType);

        public bool IsEntityInAuthTransfer(Entity id) => context != null && NativeWrapper.IsEntityInAuthTransfer(context, new InteropEntity(id));

        public void SendCommand(IEntityCommand message, MessageTarget target, Entity id, ChannelID channelID)
        {
            if (context == null)
            {
                return;
            }

            if (channelID != ChannelID.Default)
            {
                throw new NotImplementedException("Native core currently only supports the default channel.");
            }

            if (!componentInteropHandler.SendCommand(this, new InteropEntity(id), target, message))
            {
                throw new Exception("Native core failed to send the command. See error logs for more info.");
            }
        }

        bool INativeCoreCommandSender.SendCommand<T>(InteropEntity id, MessageTarget target, UInt32 commandType, T message, Int32 dataSize)
        {
            if (context == null)
            {
                return false;
            }

            return NativeWrapper.SendCommand(context, commandType, new EntityMessageContainer
            {
                Entity = id,
                Data = new IntPtr(&message),
                DataSize = dataSize
            }, target);
        }

        public void SendInput(IEntityInput message, long frame, Entity id)
        {
            if (context == null)
            {
                return;
            }

            var inputData = new InputData
            {
                Entity = id,
                Input = message,
                Frame = frame
            };

            componentInteropHandler.SendInput(this, new InteropEntity(id), frame, inputData);
        }

        void INativeCoreInputSender.SendInput<T>(InteropEntity id, Int64 frame, UInt32 inputType, T message, Int32 dataSize)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.SendInput(context, inputType, new EntityMessageContainer
            {
                Entity = id,
                Data = new IntPtr(&message),
                DataSize = dataSize
            }, frame);
        }

        public void SendAuthorityRequest(Entity id, AuthorityType authorityMode)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.SendAuthorityRequest(context, new InteropEntity(id), authorityMode);
        }

        public void SendAdoptOrphanRequest(Entity id)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.SendAdoptOrphanRequest(context, new InteropEntity(id));
        }

        public bool SendAuthorityTransfer(Entity id, ClientID newAuthority, bool authorized, AuthorityType authorityType)
        {
            if (context == null)
            {
                return false;
            }

            return NativeWrapper.SendAuthorityTransfer(context, new InteropEntity(id), new InteropClientID(newAuthority), authorityType, authorized);
        }

        public Vector3d GetFloatingOrigin()
        {
            if (context == null)
            {
                return default;
            }

            return NativeWrapper.GetFloatingOrigin(context).Into();
        }

        public void SetFloatingOrigin(Vector3d newFloatingOrigin)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.SetFloatingOrigin(context, new InteropVector3d(newFloatingOrigin));
        }

        public void SetTransportType(TransportType transportType, TransportConfiguration transportConfiguration)
        {
            if (transportConfiguration == TransportConfiguration.ManagedOnly ||
                transportConfiguration == TransportConfiguration.ManagedWithExperimentalUDP)
            {
                SetTransportFactory(new DefaultTransportFactory(transportType, transportConfiguration));
                return;
            }

            NativeWrapper.SetTransportType(context, transportType);
        }

        public void SetTransportFactory(ITransportFactory transportFactory)
        {
            if (context == null)
            {
                return;
            }

            this.NativeTransportFactory.Factory = transportFactory;

            NativeWrapper.SetTransportFactory(context, NativeCoreFactory.OnTransportFactoryConstruct, NativeCoreFactory.OnTransportFactoryDestruct);
        }

        public void SetLogLevel(LogLevel level)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.SetLogLevel(context, (int)level);
        }

        public string DebugGetTransportDescription() => throw new NotImplementedException();

        public void DebugHoldAllPackets(bool hold)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.DebugHoldAllPackets(context, hold);
        }

        public void DebugReleaseAllHeldPackets()
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.DebugReleaseAllHeldPackets(context);
        }

        public void DebugSetNetworkCondition(Condition condition)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.DebugSetNetworkCondition(context, new InteropNetworkConditions(condition));
        }

        public void DebugStopSerializingUpdates(bool stop)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.DebugStopSerializingUpdates(context, stop);
        }

        public void DebugDropNextOutPacket(Action callback)
        {
            if (context == null)
            {
                return;
            }

            onDebugOnNextOutPacketDroppedCallback = callback;

            NativeWrapper.DebugDropNextOutPacket(context, NativeCoreFactory.OnDebugOnNextOutPacketDropped);
        }

        public void DebugOnNextPacketSentOneShot(Action callback)
        {
            if (context == null)
            {
                return;
            }

            onDebugNextPacketSentCallback = callback;

            NativeWrapper.DebugOnNextPacketSentOneShot(context, NativeCoreFactory.OnDebugNextPacketSent);
        }

        #region NetworkTime interface
        public double GetNetworkTimeFixedTimeStep() => context != null ? NativeWrapper.GetNetworkTimeFixedTimeStep(context) : default;

        public void SetNetworkTimeFixedTimeStep(double fixedTimeStep)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.SetNetworkTimeFixedTimeStep(context, fixedTimeStep);
        }

        public double GetNetworkTimeMaximumDeltaTime() => context != null ? NativeWrapper.GetNetworkTimeMaximumDeltaTime(context) : default;

        public void SetNetworkTimeMaximumDeltaTime(double maximumDeltaTime)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.SetNetworkTimeMaximumDeltaTime(context, maximumDeltaTime);
        }

        public bool GetNetworkTimeMultiClientMode() => context != null && NativeWrapper.GetNetworkTimeMultiClientMode(context);

        public void SetNetworkTimeMultiClientMode(bool multiClientMode)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.SetNetworkTimeMultiClientMode(context, multiClientMode);
        }

        public bool GetNetworkTimeAccountForPing() => context != null && NativeWrapper.GetNetworkTimeAccountForPing(context);

        public void SetNetworkTimeAccountForPing(bool accountForPing)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.SetNetworkTimeAccountForPing(context, accountForPing);
        }

        public bool GetNetworkTimeSmoothTimeScaleChange() => context != null && NativeWrapper.GetNetworkTimeSmoothTimeScaleChange(context);

        public void SetNetworkTimeSmoothTimeScaleChange(bool smoothTimeScaleChange)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.SetNetworkTimeSmoothTimeScaleChange(context, smoothTimeScaleChange);
        }

        public bool GetNetworkTimePause() => context != null && NativeWrapper.GetNetworkTimePause(context);

        public void SetNetworkTimePause(bool pause)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.SetNetworkTimePause(context, pause);
        }

        public double GetNetworkTime() => context != null ? NativeWrapper.GetNetworkTime(context) : default;
        public double GetSessionTime() => context != null ? NativeWrapper.GetSessionTime(context) : default;

        public bool IsNetworkTimeSynced() => context != null && NativeWrapper.IsNetworkTimeSynced(context);
        public AbsoluteSimulationFrame GetNetworkTimeClientSimulationFrame()
            => context != null ? NativeWrapper.GetNetworkTimeClientSimulationFrame(context).Into() : default;
        public AbsoluteSimulationFrame GetNetworkTimeClientFixedSimulationFrame()
            => context != null ? NativeWrapper.GetNetworkTimeClientFixedSimulationFrame(context).Into() : default;
        public AbsoluteSimulationFrame GetNetworkTimeServerSimulationFrame()
            => context != null ? NativeWrapper.GetNetworkTimeServerSimulationFrame(context).Into() : default;
        public AbsoluteSimulationFrame GetNetworkTimeConnectionSimulationFrame()
            => context != null ? NativeWrapper.GetNetworkTimeConnectionSimulationFrame(context).Into() : default;

        public double GetNetworkTimeScale() => context != null ? NativeWrapper.GetNetworkTimeScale(context) : default;
        public double GetTargetNetworkTimeScale() => context != null ? NativeWrapper.GetTargetNetworkTimeScale(context) : default;

        public void StepNetworkTime(double currentTime)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.StepNetworkTime(context, currentTime);
        }

        public void ResetNetworkTime(InteropAbsoluteSimulationFrame newClientAndServerSimFrame, bool notify)
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.ResetNetworkTime(context, newClientAndServerSimFrame, notify);
        }
        #endregion

        public void Dispose()
        {
            if (context == null)
            {
                return;
            }

            NativeWrapper.ShutdownCoherenceCore(context);

            context = null;

            NativeCoreFactory.Remove(this);
        }

        internal void OnLogCallback(int level, string msg)
        {
            switch ((LogLevel)level)
            {
                case LogLevel.Trace:
                    logger.Trace(msg);
                    break;
                case LogLevel.Debug:
                    logger.Debug(msg);
                    break;
                case LogLevel.Info:
                    logger.Info(msg);
                    break;
                case LogLevel.Warn:
                    logger.Warning(Warning.CoreNativeCoreFactoryOnWarningCallback, ("msg", msg));
                    break;
                case LogLevel.Error:
                    logger.Error(Error.CoreNativeCoreFactoryOnErrorCallback, ("msg", msg));
                    break;
                case LogLevel.Critical:
                    logger.Error(Error.CoreNativeCoreFactoryOnErrorCallback, ("msg", msg));
                    break;
            }
        }

        internal void OnConnectedCallback(InteropClientID clientID, InteropEndpointData data)
        {
            OnConnected?.Invoke(clientID.Into());
            OnConnectedEndpoint?.Invoke(data.Into());
        }

        internal void OnDisconnectedCallback(ConnectionCloseReason reason)
        {
            OnDisconnected?.Invoke(reason);
        }

        internal void OnConnectionErrorCallback(string reason)
        {
            OnConnectionError?.Invoke(new ConnectionException(reason));
        }

        internal void OnEntityCreatedCallback(InteropEntityWithMeta meta, IntPtr data, Int32 dataLength, InteropVector3f floatingOriginDelta)
        {
            var incomingUpdate = IncomingEntityUpdate.New();
            incomingUpdate.Components.Updates.FloatingOriginDelta = floatingOriginDelta.Into();
            incomingUpdate.Meta = meta.Into();

            var managedComponents = new Span<ComponentDataContainer>(data.ToPointer(), dataLength);
            for (var i = 0; i < managedComponents.Length; i++)
            {
                var component = componentInteropHandler.GetComponent(
                    managedComponents[i].ComponentID,
                    managedComponents[i].Data,
                    managedComponents[i].DataSize,
                    managedComponents[i].SimFrames,
                    managedComponents[i].SimFrameCount);

                component.FieldsMask = managedComponents[i].FieldMask;
                component.StoppedMask = managedComponents[i].StoppedMask;

                incomingUpdate.Components.UpdateComponent(ComponentChange.New(component));
            }

            OnEntityCreated.Invoke(meta.EntityId.Into(), incomingUpdate);
        }

        internal void OnEntityUpdatedCallback(InteropEntityWithMeta meta, IntPtr updatedComponents, Int32 updatedCount, UIntPtr destroyedComponents, Int32 destroyedCount, InteropVector3f floatingOriginDelta)
        {
            var incomingUpdate = IncomingEntityUpdate.New();
            incomingUpdate.Components.Updates.FloatingOriginDelta = floatingOriginDelta.Into();
            incomingUpdate.Meta = meta.Into();

            var managedComponents = new Span<ComponentDataContainer>(updatedComponents.ToPointer(), updatedCount);
            for (var i = 0; i < managedComponents.Length; i++)
            {
                var component = componentInteropHandler.GetComponent(
                    managedComponents[i].ComponentID,
                    managedComponents[i].Data,
                    managedComponents[i].DataSize,
                    managedComponents[i].SimFrames,
                    managedComponents[i].SimFrameCount);

                component.FieldsMask = managedComponents[i].FieldMask;
                component.StoppedMask = managedComponents[i].StoppedMask;

                incomingUpdate.Components.UpdateComponent(ComponentChange.New(component));
            }

            var managedDestroys = new Span<UInt32>(destroyedComponents.ToPointer(), destroyedCount);
            for (var i = 0; i < destroyedCount; i++)
            {
                incomingUpdate.Components.RemoveComponent(managedDestroys[i]);
            }

            OnEntityUpdated.Invoke(meta.EntityId.Into(), incomingUpdate);
        }

        internal void OnEntityDestroyedCallback(InteropEntity entity, DestroyReason reason)
        {
            OnEntityDestroyed.Invoke(entity.Into(), reason);
        }

        internal void OnCommandCallback(InteropEntity entity, MessageTarget target, UInt32 commandType, IntPtr data, Int32 dataSize)
        {
            var command = componentInteropHandler.GetCommand(commandType, data, dataSize);
            command.Entity = entity.Into();
            command.Routing = target;

            OnCommand.Invoke(command, target, entity.Into());
        }

        internal void OnInputCallback(InteropEntity entity, Int64 frame, UInt32 inputType, IntPtr data, Int32 dataSize)
        {
            var input = componentInteropHandler.GetInput(inputType, data, dataSize);
            input.Entity = entity.Into();

            OnInput.Invoke(input, frame, entity.Into());
        }

        internal void OnAuthorityRequestedCallback(InteropAuthorityRequest request)
        {
            OnAuthorityRequested.Invoke(request.Into());
        }

        internal void OnAuthorityRequestRejectedCallback(InteropAuthorityRequestRejection change)
        {
            OnAuthorityRequestRejected.Invoke(change.Into());
        }

        internal void OnAuthorityChangedCallback(InteropAuthorityChange change)
        {
            OnAuthorityChange.Invoke(change.Into());
        }

        internal void OnAuthorityTransferredCallback(InteropEntity entity)
        {
            OnAuthorityTransferred.Invoke(entity.Into());
        }

        internal void OnSceneIndexChangedCallback(InteropSceneIndexChange change)
        {
            OnSceneIndexChanged?.Invoke(change.Into());
        }

        internal void OnDebugPacketSentCallback(IntPtr outgoingEntityUpdates, Int32 count, Int32 totalChanges, UInt32 octetCount)
        {
            var managedOutgoingUpdates = new Span<InteropOutgoingEntityUpdate>(outgoingEntityUpdates.ToPointer(), count);

            var outgoingChanges = new Dictionary<Entity, OutgoingEntityUpdate>(count);

            foreach (var managedOutgoingEntityUpdate in managedOutgoingUpdates)
            {
                var managedComponents = new Span<ComponentDataContainer>(managedOutgoingEntityUpdate.Components, managedOutgoingEntityUpdate.ComponentCount);

                var comps = new ICoherenceComponentData[managedOutgoingEntityUpdate.ComponentCount];
                for (var i = 0; i < managedComponents.Length; i++)
                {
                    comps[i] = componentInteropHandler.GetComponent(
                        managedComponents[i].ComponentID,
                        managedComponents[i].Data,
                        managedComponents[i].DataSize,
                        managedComponents[i].SimFrames,
                        managedComponents[i].SimFrameCount);

                    comps[i].FieldsMask = managedComponents[i].FieldMask;
                    comps[i].StoppedMask = managedComponents[i].StoppedMask;
                }

                var managedDestroys = new Span<UInt32>(managedOutgoingEntityUpdate.DestroyedComponents, managedOutgoingEntityUpdate.DestroyedCount);

                var destroys = new UInt32[managedOutgoingEntityUpdate.DestroyedCount];
                for (var i = 0; i < managedOutgoingEntityUpdate.DestroyedCount; i++)
                {
                    destroys[i] = managedDestroys[i];
                }

                var outgoingEntityUpdate = OutgoingEntityUpdate.New();
                outgoingEntityUpdate.Priority = managedOutgoingEntityUpdate.Priority;
                outgoingEntityUpdate.Operation = managedOutgoingEntityUpdate.Operation;
                outgoingEntityUpdate.Components.UpdateComponents(ComponentUpdates.New(comps));
                outgoingEntityUpdate.Components.RemoveComponents(destroys);

                outgoingChanges[managedOutgoingEntityUpdate.ID.Into()] = outgoingEntityUpdate;
            }

            var changesSent = new Dictionary<ChannelID, Dictionary<Entity, OutgoingEntityUpdate>>
                { { ChannelID.Default, outgoingChanges }, };
            debugOnPacketSent.Invoke(new PacketSentDebugInfo
            {
                ChangesSentPerChannel = changesSent,
                TotalChanges = totalChanges,
                OctetCount = octetCount
            });

            foreach (var outgoingEntityUpdate in outgoingChanges)
            {
                outgoingEntityUpdate.Value.Return();
            }
        }

        internal void OnDebugPacketReceivedCallback(Int32 octetsReceived)
        {
            DebugOnPacketReceived?.Invoke(octetsReceived);
        }

        internal void OnDebugEntityAckedCallback(InteropEntity entity)
        {
            DebugOnEntityAcked?.Invoke(entity.Into());
        }

        internal void OnDebugOnNextOutPacketDroppedCallback()
        {
            onDebugOnNextOutPacketDroppedCallback?.Invoke();
        }

        internal void OnDebugNextPacketSentCallback()
        {
            onDebugNextPacketSentCallback?.Invoke();
        }
    }
}
#endif
