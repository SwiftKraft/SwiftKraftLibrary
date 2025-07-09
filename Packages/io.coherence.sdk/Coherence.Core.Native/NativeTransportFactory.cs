// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

namespace Coherence.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net;
    using System.Runtime.InteropServices;
    using Coherence.Brook;
    using Coherence.Common.Pooling;
    using Coherence.Connection;
    using Coherence.Log;
    using Coherence.Stats;
    using Coherence.Transport;
#if UNITY
    using AOT;
#endif

    [StructLayout(LayoutKind.Sequential)]
    public struct InteropTransport
    {
        public UInt32 Cookie;
        public NativeTransport.SetOnErrorCallback SetOnErrorCallback;
        public NativeTransport.SetOnOpenCallback SetOnOpenCallback;
        public NativeTransport.GetState GetState;
        public NativeTransport.IsReliable IsReliable;
        public NativeTransport.CanSend CanSend;
        public NativeTransport.GetHeaderSize GetHeaderSize;
        public NativeTransport.Open Open;
        public NativeTransport.Close Close;
        public NativeTransport.Send Send;
        public NativeTransport.BeginReceive BeginReceive;
        public NativeTransport.ReceiveOne ReceiveOne;
        public NativeTransport.PrepareDisconnect PrepareDisconnect;
    }

    public enum InteropTransportError : Byte
    {
        Unknown,
        Timeout
    }

    public unsafe class NativeTransport
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate Int32 TransportFactoryConstruct(UInt32 cookie, IntPtr outFunctions, UInt16 mtu);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void TransportFactoryDestruct(UInt32 cookie, UInt32 transportCookie);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnErrorCallback(IntPtr context, InteropTransportError error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SetOnErrorCallback(UInt32 cookie, IntPtr context, OnErrorCallback onError);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OnOpenCallback(IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void SetOnOpenCallback(UInt32 cookie, IntPtr context, OnOpenCallback onOpen);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate TransportState GetState(UInt32 cookie);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate Byte IsReliable(UInt32 cookie);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate Byte CanSend(UInt32 cookie);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate Int32 GetHeaderSize(UInt32 cookie);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate Int32 Open(UInt32 cookie, InteropEndpointData endpoint, InteropConnectionSettings settings);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Close(UInt32 cookie);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate Int32 Send(UInt32 cookie, Byte* bytes, Int32 count);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate Int32 BeginReceive(UInt32 cookie);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate Int32 ReceiveOne(UInt32 cookie, Byte* buffer, Int32 capacity, Byte* fromAddress, Int32 fromAddressLength, Byte* isIPv6, UInt16* port);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PrepareDisconnect(UInt32 cookie);

        private ITransport transport;
        private Logger logger;

        private Action<InteropTransportError> onErrorCallback;
        private Action onOpenCallback;

        private List<(IInOctetStream Buffer, IPEndPoint From)> receiveBuffer = new();
        private readonly Pool<PooledOutOctetStream> streamPool;

        internal NativeTransport(ITransport transport, Logger logger, UInt16 mtu)
        {
            this.streamPool = Pool<PooledOutOctetStream>
                .Builder(pool => new PooledOutOctetStream(pool, mtu))
                .Build();

            this.transport = transport;
            this.logger = logger.With<NativeTransport>();

            this.transport.OnOpen += OnOpenInvoked;
            this.transport.OnError += OnErrorInvoked;
        }

        internal void OnSetOnError(IntPtr context, OnErrorCallback callback)
        {
            this.onErrorCallback = e => callback(context, e);
        }

        internal void OnSetOnOpen(IntPtr context, OnOpenCallback callback)
        {
            this.onOpenCallback = () => callback(context);
        }

        internal TransportState OnGetState() => transport.State;
        internal byte OnGetIsReliable() => transport.IsReliable ? (byte)1 : (byte)0;
        internal byte OnGetCanSend() => transport.CanSend ? (byte)1 : (byte)0;
        internal Int32 OnGetHeaderSizeFn() => transport.HeaderSize;

        internal Int32 OnOpen(InteropEndpointData endpoint, InteropConnectionSettings settings)
        {
            try
            {
                transport.Open(endpoint.Into(), settings.Into());
            }
            catch (Exception e)
            {
                logger.Error(Error.CoreNativeTransportFailedToOpen, ("exception", e));
                return -1;
            }

            return 0;
        }

        internal void OnClose()
        {
            try
            {
                transport.Close();
            }
            catch (Exception e)
            {
                logger.Error(Error.CoreNativeTransportFailedToClose, ("exception", e));
            }
        }

        internal Int32 OnSend(Byte* bytes, Int32 count)
        {
            try
            {
                Span<byte> managedBytes = new Span<byte>(bytes, count);
                var stream = streamPool.Rent();

                // Leave space for transport header
                stream.Seek((uint)transport.HeaderSize);

                stream.WriteOctets(managedBytes);

                transport.Send(stream);
            }
            catch (Exception e)
            {
                logger.Error(Error.CoreNativeTransportFailedToSend, ("exception", e));
                return -1;
            }

            return 0;
        }

        internal Int32 OnBeginReceive()
        {
            try
            {
                transport.Receive(receiveBuffer);
            }
            catch (Exception e)
            {
                logger.Error(Error.CoreNativeTransportFailedToReceive, ("exception", e));
                return -1;
            }

            return receiveBuffer.Count;
        }

        internal Int32 OnReceiveOne(Byte* buffer, Int32 capacity, Byte* fromAddress, Int32 fromAddressLength, Byte* isIPv6, UInt16* port)
        {
            if (receiveBuffer.Count == 0)
            {
                return 0;
            }

            try
            {
                var inPacket = receiveBuffer[0];

                // Copy the IPEndpoint
                if (inPacket.From != null)
                {
                    if (!inPacket.From.Address.TryWriteBytes(new Span<Byte>(fromAddress, fromAddressLength), out _))
                    {
                        logger.Error(Error.CoreNativeTransportFailedToWriteIPAddress);
                        return -1;
                    }

                    *isIPv6 = inPacket.From.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? (byte)1 : (byte)0;
                    *port = (UInt16)inPacket.From.Port;
                }

                // Copy the buffer
                var destinationBuffer = new Span<Byte>(buffer, capacity);
                var sourceBuffer = inPacket.Buffer.GetOffsetBuffer();
                sourceBuffer.CopyTo(destinationBuffer);

                receiveBuffer.RemoveAt(0);

                return sourceBuffer.Length;
            }
            catch (Exception e)
            {
                logger.Error(Error.CoreNativeTransportFailedToReceiveOnePacket, ("exception", e));
                return -1;
            }
        }

        internal void OnPrepareDisconnect()
        {
            try
            {
                transport.PrepareDisconnect();
            }
            catch (Exception e)
            {
                logger.Error(Error.CoreNativeTransportFailedToPrepareDisconnect, ("exception", e));
            }
        }

        private void OnOpenInvoked()
        {
            onOpenCallback?.Invoke();
        }

        private void OnErrorInvoked(ConnectionException e)
        {
            logger.Error(Error.CoreNativeTransportError, ("exception", e));

            var error = InteropTransportError.Unknown;
            if (e is ConnectionTimeoutException)
            {
                error = InteropTransportError.Timeout;
            }

            onErrorCallback?.Invoke(error);
        }
    }

    internal class NativeTransportFactory
    {
        private static UInt32 nextCookie = 1;
        private static readonly ConcurrentDictionary<UInt32, NativeTransport> cookieTransportMap = new();
        private static readonly Logger logger = Log.GetLogger(typeof(NativeTransportFactory));

        private IStats stats;
        private Logger instanceLogger;

        public ITransportFactory Factory { get; set; }

        public NativeTransportFactory(IStats stats, Logger logger)
        {
            this.stats = stats;
            this.instanceLogger = logger;
        }

        public unsafe Int32 Construct(IntPtr outFunctions, UInt16 mtu)
        {
            if (Factory == null)
            {
                instanceLogger.Error(Error.CoreNativeTransportFailedToConstruct);
                return -1;
            }

            var transport = new NativeTransport(Factory.Create(mtu, stats, instanceLogger), instanceLogger, mtu);

            var functions = new InteropTransport()
            {
                Cookie = nextCookie++,
                SetOnErrorCallback = SetOnError,
                SetOnOpenCallback = SetOnOpen,
                GetState = GetState,
                IsReliable = IsReliable,
                CanSend = CanSend,
                GetHeaderSize = GetHeaderSize,
                Open = Open,
                Close = Close,
                Send = Send,
                BeginReceive = BeginReceive,
                ReceiveOne = ReceiveOne,
                PrepareDisconnect = PrepareDisconnect
            };
            Marshal.StructureToPtr(functions, outFunctions, false);

            if (!cookieTransportMap.TryAdd(functions.Cookie, transport))
            {
                instanceLogger.Error(Error.CoreNativeTransportFailedToAddCookie, ("cookie", functions.Cookie));
                return -1;
            }

            return 0;
        }

        public void Destruct(UInt32 cookie)
        {
            cookieTransportMap.TryRemove(cookie, out _);
        }

        private static NativeTransport GetTransport(UInt32 cookie)
        {
            if (!cookieTransportMap.TryGetValue(cookie, out NativeTransport transport))
            {
                logger.Error(Error.CoreNativeTransportFailedToGetCookie, ("cookie", cookie));
                return null;
            }

            return transport;
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeTransport.SetOnErrorCallback))]
#endif
        private static void SetOnError(UInt32 cookie, IntPtr context, NativeTransport.OnErrorCallback callback)
        {
            GetTransport(cookie)?.OnSetOnError(context, callback);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeTransport.OnOpenCallback))]
#endif
        private static void SetOnOpen(UInt32 cookie, IntPtr context, NativeTransport.OnOpenCallback callback)
        {
            GetTransport(cookie)?.OnSetOnOpen(context, callback);
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeTransport.GetState))]
#endif
        private static TransportState GetState(UInt32 cookie)
        {
            return GetTransport(cookie)?.OnGetState() ?? TransportState.Closed;
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeTransport.IsReliable))]
#endif
        private static byte IsReliable(UInt32 cookie)
        {
            return GetTransport(cookie)?.OnGetIsReliable() ?? default;
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeTransport.CanSend))]
#endif
        private static byte CanSend(UInt32 cookie)
        {
            return GetTransport(cookie)?.OnGetCanSend() ?? default;
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeTransport.GetHeaderSize))]
#endif
        private static Int32 GetHeaderSize(UInt32 cookie)
        {
            return GetTransport(cookie)?.OnGetHeaderSizeFn() ?? default;
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeTransport.Open))]
#endif
        private static Int32 Open(UInt32 cookie, InteropEndpointData endpoint, InteropConnectionSettings settings)
        {
            return GetTransport(cookie)?.OnOpen(endpoint, settings) ?? -1;
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeTransport.Close))]
#endif
        private static void Close(UInt32 cookie)
        {
            GetTransport(cookie)?.OnClose();
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeTransport.Send))]
#endif
        private unsafe static Int32 Send(UInt32 cookie, Byte* bytes, Int32 count)
        {
            return GetTransport(cookie)?.OnSend(bytes, count) ?? -1;
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeTransport.BeginReceive))]
#endif
        private static Int32 BeginReceive(UInt32 cookie)
        {
            return GetTransport(cookie)?.OnBeginReceive() ?? -1;
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeTransport.ReceiveOne))]
#endif
        private unsafe static Int32 ReceiveOne(UInt32 cookie, Byte* buffer, Int32 capacity, Byte* fromAddress, Int32 fromAddressLength, Byte* isIPv6, UInt16* port)
        {
            return GetTransport(cookie)?.OnReceiveOne(buffer, capacity, fromAddress, fromAddressLength, isIPv6, port) ?? -1;
        }

#if UNITY
        [MonoPInvokeCallback(typeof(NativeTransport.PrepareDisconnect))]
#endif
        private static void PrepareDisconnect(UInt32 cookie)
        {
            GetTransport(cookie)?.OnPrepareDisconnect();
        }
    }
}
