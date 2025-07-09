// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using System;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using Brisk;
    using Coherence.SimulationFrame;
    using Coherence.Transport;
    using Common;
    using Connection;
    using Entities;

    [StructLayout(LayoutKind.Sequential)]
    public struct InteropClientID
    {
        public UInt32 Id;

        public InteropClientID(ClientID clientID)
        {
            Id = (uint)clientID;
        }

        public ClientID Into()
        {
            return (ClientID)Id;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InteropAbsoluteSimulationFrame
    {
        public Int64 Frame;

        public InteropAbsoluteSimulationFrame(AbsoluteSimulationFrame frame)
        {
            Frame = frame.Frame;
        }

        public AbsoluteSimulationFrame Into()
        {
            return (AbsoluteSimulationFrame)Frame;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InteropAuthorityRequestRejection
    {
        public InteropEntity ID;
        public AuthorityType AuthType;

        public AuthorityRequestRejection Into()
        {
            return new AuthorityRequestRejection()
            {
                EntityID = ID.Into(),
                AuthorityType = AuthType
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InteropAuthorityChange
    {
        public InteropEntity ID;
        public AuthorityType AuthType;

        public AuthorityChange Into()
        {
            return new AuthorityChange()
            {
                EntityID = ID.Into(),
                NewAuthorityType = AuthType
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct InteropSceneIndexChange
    {
        public InteropEntity EntityId;
        public Int32 SceneIndex;

        public SceneIndexChanged Into()
        {
            return new SceneIndexChanged()
            {
                EntityID = EntityId.Into(),
                SceneIndex = SceneIndex
            };
        }
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct InteropAuthorityRequest
    {
        public InteropEntity ID;
        public InteropClientID RequesterID;
        public AuthorityType AuthType;

        public AuthorityRequest Into()
        {
            return new AuthorityRequest()
            {
                EntityID = ID.Into(),
                RequesterID = RequesterID.Into(),
                AuthorityType = AuthType
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct InteropOutgoingEntityUpdate
    {
        public InteropEntity ID;
        public ComponentDataContainer* Components;
        public Int32 ComponentCount;
        public UInt32* DestroyedComponents;
        public Int32 DestroyedCount;
        public Int64 Priority;
        public EntityOperation Operation;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ComponentDataContainer
    {
        public UInt32 ComponentID;
        public UInt32 FieldMask;
        public UInt32 StoppedMask;
        public IntPtr Data;
        public Int32 DataSize;
        public InteropAbsoluteSimulationFrame* SimFrames;
        public Int32 SimFrameCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EntityMessageContainer
    {
        public InteropEntity Entity;
        public IntPtr Data;
        public Int32 DataSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ByteArray
    {
        public void* Data;
        public Int64 Length;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InteropPingSettings
    {
        public UInt32 MinSamplesForStability;
        public UInt32 MaxStableDeviation;
        public UInt32 MaxSamples;

        public override string ToString()
        {
            return
                $"[MinSamplesForStability: {MinSamplesForStability}, MaxStableDeviation: {MaxStableDeviation}, MaxSamples: {MaxSamples}]";
        }

        public InteropPingSettings(ConnectionSettings.PingSettings data)
        {
            MinSamplesForStability = (uint)data.MinSamplesForStability;
            MaxStableDeviation = (uint)data.MaxStablePingDeviation;
            MaxSamples = (uint)data.MaxSamples;
        }

        public ConnectionSettings.PingSettings Into()
        {
            return new ConnectionSettings.PingSettings
            {
                MinSamplesForStability = (int)MinSamplesForStability,
                MaxStablePingDeviation = (int)MaxStableDeviation,
                MaxSamples = (int)MaxSamples,
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InteropPing
    {
        public Int32 AverageLatencyMs;
        public byte IsStable;
        public Int32 LatestLatencyMs;

        public Ping Into()
        {
            return new Ping()
            {
                AverageLatencyMs = AverageLatencyMs,
                IsStable = IsStable != 0,
                LatestLatencyMs = LatestLatencyMs
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InteropConnectionSettings
    {
        public InteropPingSettings PingSettings;
        public UInt32 DisconnectTimeoutMilliseconds;
        public byte UseDebugStreams;

        public override string ToString()
        {
            return
                $"[Ping: {PingSettings.ToString()}, DisconnectTimeoutMilliseconds: {DisconnectTimeoutMilliseconds}, UseDebugStreams: {UseDebugStreams}]";
        }

        public InteropConnectionSettings(ConnectionSettings settings)
        {
            PingSettings = new InteropPingSettings(settings.Ping);
            DisconnectTimeoutMilliseconds = (uint)settings.DisconnectTimeoutMilliseconds;
            UseDebugStreams = settings.UseDebugStreams ? (byte)1 : (byte)0;
        }

        public ConnectionSettings Into()
        {
            return new ConnectionSettings
            {
                UseDebugStreams = UseDebugStreams != 0,
                DisconnectTimeoutMilliseconds = DisconnectTimeoutMilliseconds,
                Ping = PingSettings.Into(),
            };
        }
    }

    public enum EntityIDType : byte
    {
        Relative = 0,
        Absolute = 1,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InteropEntity
    {
        public UInt16 Index;
        public byte Version;
        public EntityIDType Type;

        public override string ToString()
        {
            return $"{Index}({Version}):r";
        }

        public InteropEntity(Entity entity)
        {
            Index = entity.Index;
            Version = entity.Version;
            Type = entity.IsAbsolute ? EntityIDType.Absolute : EntityIDType.Relative;
        }

        public Entity Into()
        {
            return new Entity(Index, Version, Type == EntityIDType.Absolute);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct InteropEndpointData
    {
        public const int HostMaxLength = 128;
        public const int AuthTokenMaxLength = 512;
        public const int RuntimeKeyMaxLength = 128;
        public const int RegionMaxLength = 32;
        public const int SchemaIdMaxLength = 128;
        public const int SimulatorTypeMaxLength = 32;
        public const int RoomSecretMaxLength = 128;
        public const int RsVersionMaxLength = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = HostMaxLength)]
        public string Host;
        public UInt32 Port; // Must be 32 bit to correctly align with C++ equivalent struct on wasm with emscripten
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = AuthTokenMaxLength)]
        public string AuthToken;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RuntimeKeyMaxLength)]
        public string RuntimeKey;

        public UInt32 RoomId; // Must be 32 bit to correctly align with C++ equivalent struct on wasm with emscripten
        public UInt64 UniqueRoomId;
        public UInt64 WorldId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RegionMaxLength)]
        public string Region;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SchemaIdMaxLength)]
        public string SchemaId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = SimulatorTypeMaxLength)]
        public string SimulatorType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RoomSecretMaxLength)]
        public string RoomSecret;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RsVersionMaxLength)]
        public string RSVersion;

        public byte CustomLocalToken;

        public override string ToString()
        {
            return
                $"[Host: {Host}, Port: {Port}, AuthToken: {AuthToken}, RuntimeKey: {RuntimeKey}, RoomId: {RoomId}, UniqueRoomId: {UniqueRoomId}, WorldId: {WorldId}, Region: {Region}, SchemaId: {SchemaId}, SimulatorType: {SimulatorType}, RoomSecret: {RoomSecret}, RSVersion: {RSVersion}, CustomLocalToken: {CustomLocalToken.ToString().ToLower()}]";
        }

        public InteropEndpointData(EndpointData data)
        {
            Host = data.host;
            Port = (UInt16)data.port;
            AuthToken = data.authToken;
            RuntimeKey = data.runtimeKey;
            RoomId = data.roomId;
            UniqueRoomId = data.uniqueRoomId;
            WorldId = data.worldId;
            Region = data.region;
            SchemaId = data.schemaId;
            SimulatorType = data.simulatorType;
            RoomSecret = data.roomSecret;
            RSVersion = data.rsVersion;
            CustomLocalToken = data.customLocalToken ? (byte)1 : (byte)0;
        }

        public EndpointData Into()
        {
            return new EndpointData
            {
                host = Host,
                port = (int)Port,
                authToken = AuthToken,
                runtimeKey = RuntimeKey,
                roomId = (ushort)RoomId,
                uniqueRoomId = UniqueRoomId,
                worldId = WorldId,
                region = Region,
                schemaId = SchemaId,
                simulatorType = SimulatorType,
                roomSecret = RoomSecret,
                rsVersion = RSVersion,
                customLocalToken = CustomLocalToken != 0,
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InteropVector3d
    {
        public double X;
        public double Y;
        public double Z;

        public InteropVector3d(Vector3d vector)
        {
            X = vector.x;
            Y = vector.y;
            Z = vector.z;
        }

        public Vector3d Into()
        {
            return new Vector3d(X, Y, Z);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InteropVector3f
    {
        public float X;
        public float Y;
        public float Z;

        public InteropVector3f(Vector3 vector)
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }

        public Vector3 Into()
        {
            return new Vector3(X, Y, Z);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InteropNetworkConditions
    {
        public float SendDelaySec;
        public float SendDropRate;
        public float ReceiveDelaySec;
        public float ReceiveDropRate;

        public InteropNetworkConditions(Condition condition)
        {
            SendDelaySec = condition.sendDelaySec;
            SendDropRate = condition.sendDropRate;
            ReceiveDelaySec = condition.receiveDelaySec;
            ReceiveDropRate = condition.receiveDropRate;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InteropEntityWithMeta
    {
        public InteropEntity EntityId;
        public Byte HasMeta;
        public Byte HasStateAuthority;
        public Byte HasInputAuthority;
        public Byte IsOrphan;
        public UInt32 LOD;
        public EntityOperation Operation;
        public DestroyReason DestroyReason;

        public EntityWithMeta Into()
        {
            return new EntityWithMeta
            {
                EntityId = EntityId.Into(),
                HasMeta = HasMeta != 0,
                HasStateAuthority = HasStateAuthority != 0,
                HasInputAuthority = HasInputAuthority != 0,
                IsOrphan = IsOrphan != 0,
                LOD = LOD,
                Operation = Operation,
                DestroyReason = DestroyReason
            };
        }
    }

    public enum CoherenceContextInitError : UInt32
    {
        Ok = 0,
        InvalidSchema = 1,
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe internal struct CoherenceContextInitResult
    {
        public CoherenceContext* Context;
        public CoherenceContextInitError ErrorCode;
    };

    internal struct CoherenceContext
    {
    }

#if COHERENCE_FEATURE_NATIVE_CORE
    internal static unsafe partial class NativeWrapper
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        public const string IMPORT_PATH = "__Internal";
#else
        public const string IMPORT_PATH = DLL_PATH;
#endif

        #region Callbacks

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnLog(UInt32 cookie, Int32 level, [MarshalAs(UnmanagedType.LPUTF8Str)] string msg);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnConnectedCallback(UInt32 cookie, InteropClientID clientID, InteropEndpointData data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnDisconnectedCallback(UInt32 cookie, ConnectionCloseReason reason);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnConnectionErrorCallback(UInt32 cookie, [MarshalAs(UnmanagedType.LPUTF8Str)] string msg);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnEntityCreatedCallback(UInt32 cookie, InteropEntityWithMeta meta, IntPtr data, Int32 length, InteropVector3f floatingOriginDelta);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnEntityUpdatedCallback(UInt32 cookie, InteropEntityWithMeta meta, IntPtr updatedComponents, Int32 updatedCount, UIntPtr destroyedComponents, Int32 destroyedCount, InteropVector3f floatingOriginDelta);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnEntityDestroyCallback(UInt32 cookie, InteropEntity entity, DestroyReason reason);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnCommandCallback(UInt32 cookie, InteropEntity entity, MessageTarget target, UInt32 commandType,
            IntPtr data, Int32 dataSize);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnInputCallback(UInt32 cookie, InteropEntity entity, Int64 frame, UInt32 inputType, IntPtr data, Int32 dataSize);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnAuthorityRequestedCallback(UInt32 cookie, InteropAuthorityRequest request);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnAuthorityRequestRejectedCallback(UInt32 cookie, InteropAuthorityRequestRejection change);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnAuthorityChangedCallback(UInt32 cookie, InteropAuthorityChange change);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnAuthorityTransferredCallback(UInt32 cookie, InteropEntity entity);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnSceneIndexChangedCallback(UInt32 cookie, InteropSceneIndexChange change);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NetworkTimeOnTimeResetCallback(UInt32 cookie);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NetworkTimeOnFixedNetworkUpdateCallback(UInt32 cookie);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NetworkTimeOnLateFixedNetworkUpdateCallback(UInt32 cookie);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NetworkTimeOnServerSimulationFrameReceivedCallback(UInt32 cookie,
            InteropAbsoluteSimulationFrame serverSimFrame, InteropAbsoluteSimulationFrame clientSimFrame);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DebugOnPacketSentCallback(UInt32 cookie, IntPtr outgoingEntityUpdates, Int32 count, Int32 totalChanges, UInt32 octetCount);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DebugOnPacketReceivedCallback(UInt32 cookie, Int32 octetsReceived);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DebugOnNextOutPacketDroppedCallback(UInt32 cookie);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DebugOnNextPacketSentCallback(UInt32 cookie);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DebugOnEntityAckedCallback(UInt32 cookie, InteropEntity entity);
        #endregion

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern CoherenceContextInitResult ConstructCoherenceCore(
            UInt32 cookie,
            OnLog onLog,
            OnConnectedCallback onConnected,
            OnDisconnectedCallback onDisconnected,
            OnConnectionErrorCallback onConnectionError,
            OnEntityCreatedCallback onEntityCreated,
            OnEntityUpdatedCallback onEntityUpdated,
            OnEntityDestroyCallback onEntityDestroy,
            OnCommandCallback onCommand,
            OnInputCallback onInput,
            OnAuthorityRequestedCallback onAuthorityRequested,
            OnAuthorityRequestRejectedCallback onAuthorityRequestRejectedCallback,
            OnAuthorityChangedCallback onAuthorityChangedCallback,
            OnAuthorityTransferredCallback onAuthorityTransferredCallback,
            OnSceneIndexChangedCallback onSceneIndexChangedCallback,
            string schemaText);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 ShutdownCoherenceCore(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Receive(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Send(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Connect(CoherenceContext* ctx, InteropEndpointData data,
            InteropConnectionSettings connectionSettings,
            ConnectionType connectionType);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern InteropClientID GetClientID(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern ConnectionType GetConnectionType(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetHostName(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern ConnectionState GetConnectionState(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern InteropEndpointData GetLastEndpointData(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern InteropConnectionSettings GetConnectionSettings(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern InteropPing GetPing(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern UInt32 GetInitialScene(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetInitialScene(CoherenceContext* ctx, UInt32 scene);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern byte GetSendFrequency(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IsConnected(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IsDisconnected(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IsConnecting(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Disconnect(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Reconnect(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern InteropEntity CreateEntity(CoherenceContext* ctx, bool orphan);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void UpdateComponent(CoherenceContext* ctx, InteropEntity entity, ComponentDataContainer comp);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RemoveComponent(CoherenceContext* ctx, InteropEntity entity, UInt32 componentId);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DestroyEntity(CoherenceContext* ctx, InteropEntity entity);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool EntityExists(CoherenceContext* ctx, InteropEntity entity);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SendCommand(CoherenceContext* ctx, UInt32 commandType, EntityMessageContainer message,
            MessageTarget target);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SendInput(CoherenceContext* ctx, UInt32 inputType, EntityMessageContainer message,
            Int64 frame);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool HasAuthorityOverEntity(CoherenceContext* ctx, InteropEntity entity,
            AuthorityType authorityType);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SendAuthorityRequest(CoherenceContext* ctx, InteropEntity entity,
            AuthorityType authorityType);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SendAdoptOrphanRequest(CoherenceContext* ctx, InteropEntity entity);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SendAuthorityTransfer(CoherenceContext* ctx, InteropEntity entity, InteropClientID clientID,
            AuthorityType authorityType, bool authorized);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CanSendUpdates(CoherenceContext* ctx, InteropEntity entity);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IsEntityInAuthTransfer(CoherenceContext* ctx, InteropEntity entity);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetFloatingOrigin(CoherenceContext* ctx, InteropVector3d origin);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern InteropVector3d GetFloatingOrigin(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetTransportType(CoherenceContext* ctx, TransportType type);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetTransportFactory(CoherenceContext* ctx,
            NativeTransport.TransportFactoryConstruct construct, NativeTransport.TransportFactoryDestruct destruct);

        // NetworkTime interface
        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetNetworkTimeFixedTimeStep(CoherenceContext* ctx);
        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetNetworkTimeFixedTimeStep(CoherenceContext* ctx, double fixedTimeStep);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetNetworkTimeMaximumDeltaTime(CoherenceContext* ctx);
        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetNetworkTimeMaximumDeltaTime(CoherenceContext* ctx, double maximumDeltaTime);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetNetworkTimeMultiClientMode(CoherenceContext* ctx);
        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetNetworkTimeMultiClientMode(CoherenceContext* ctx, bool multiClientMode);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetNetworkTimeAccountForPing(CoherenceContext* ctx);
        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetNetworkTimeAccountForPing(CoherenceContext* ctx, bool accountForPing);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetNetworkTimeSmoothTimeScaleChange(CoherenceContext* ctx);
        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetNetworkTimeSmoothTimeScaleChange(CoherenceContext* ctx, bool smoothTimeScaleChange);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetNetworkTimePause(CoherenceContext* ctx);
        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetNetworkTimePause(CoherenceContext* ctx, bool pause);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetNetworkTimeOnTimeResetCallback(
            CoherenceContext* ctx,
            NetworkTimeOnTimeResetCallback callback
        );
        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetNetworkTimeOnFixedNetworkUpdateCallback(
            CoherenceContext* ctx,
            NetworkTimeOnFixedNetworkUpdateCallback callback
        );
        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetNetworkTimeOnLateFixedNetworkUpdateCallback(
            CoherenceContext* ctx,
            NetworkTimeOnLateFixedNetworkUpdateCallback callback
        );
        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetNetworkTimeOnServerSimulationFrameReceivedCallback(
            CoherenceContext* ctx,
            NetworkTimeOnServerSimulationFrameReceivedCallback callback
        );

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetNetworkTime(CoherenceContext* ctx);
        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetSessionTime(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IsNetworkTimeSynced(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern InteropAbsoluteSimulationFrame GetNetworkTimeClientSimulationFrame(CoherenceContext* ctx);
        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern InteropAbsoluteSimulationFrame GetNetworkTimeClientFixedSimulationFrame(CoherenceContext* ctx);
        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern InteropAbsoluteSimulationFrame GetNetworkTimeServerSimulationFrame(CoherenceContext* ctx);
        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern InteropAbsoluteSimulationFrame GetNetworkTimeConnectionSimulationFrame(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetNetworkTimeScale(CoherenceContext* ctx);
        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern double GetTargetNetworkTimeScale(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void StepNetworkTime(CoherenceContext* ctx, double currentTime);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ResetNetworkTime(CoherenceContext* ctx, InteropAbsoluteSimulationFrame newClientAndServerSimFrame, bool notify);
        // End of NetworkTime interface

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetLogLevel(CoherenceContext* ctx, Int32 level);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern Int32 TestInteropStructSizes(CoherenceContext* ctx, InteropStructSizes sizes, Int32 structSize);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DebugSetOnPacketSent(CoherenceContext* ctx, DebugOnPacketSentCallback callback);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DebugSetOnPacketReceived(CoherenceContext* ctx, DebugOnPacketReceivedCallback callback);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DebugSetOnEntityAcked(CoherenceContext* ctx, DebugOnEntityAckedCallback callback);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DebugEcho(CoherenceContext* ctx, string msg);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DebugInteropCallbacks(CoherenceContext* ctx, string cbName);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DebugInteropSetters(CoherenceContext* ctx, ConnectionType connectionType,
            string hostname, InteropEndpointData endpoint, InteropConnectionSettings connectionSettings);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DebugInteropCreateEntityCallback(CoherenceContext* ctx, InteropEntity entity,
            ComponentDataContainer* data, Int32 length);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DebugHoldAllPackets(CoherenceContext* ctx, bool hold);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DebugReleaseAllHeldPackets(CoherenceContext* ctx);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DebugSetNetworkCondition(CoherenceContext* ctx, InteropNetworkConditions condition);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DebugStopSerializingUpdates(CoherenceContext* ctx, bool stop);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DebugDropNextOutPacket(CoherenceContext* ctx, DebugOnNextOutPacketDroppedCallback callback);

        [DllImport(IMPORT_PATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DebugOnNextPacketSentOneShot(CoherenceContext* ctx, DebugOnNextPacketSentCallback callback);
    }
#endif
}
