// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

namespace Coherence.Runtime
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Cloud;
    using Coherence.Utils;
    using Common;
    using Connection;
    using Log;
    using Utils;

    public sealed class WebSocketManager : IUpdatable, IDisposableInternal
    {
        private static readonly TimeSpan TimeOutSpan = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan TimeoutCheckSpan = TimeSpan.FromSeconds(1);

        private readonly IRuntimeSettings runtimeSettings;
        private readonly RequestIdSource idSource;
        private readonly Logger logger = Log.GetLogger<WebSocketManager>();
        private bool wsConnected = false;
        private bool wsConnecting = false;
        private IWebSocket ws;
        private ConcurrentQueue<String> receiveQueue;
        private ConcurrentQueue<(int counter, string requestID)> failQueue;
        private List<RequestCallback> requestCallbacks;
        private Dictionary<string, RequestCallback> pushCallbacks;
        private readonly Stopwatch connectBackoffStopwatch = new Stopwatch();
        private TimeSpan connectBackoff = Constants.minBackoff;
        private DateTime nextCheck;
        private string resumeId;
        private bool validatedWsParameters;

        private const string pingEndpoint = "/health";
        private const int pingIntervalSeconds = 30;
        private DateTime nextPingTime;
        private bool isWebGlConnected;
        
        private string Endpoint => runtimeSettings.WebSocketEndpoint + "?runtime_key=" + runtimeSettings.RuntimeKey;
        private string ClientVersion => "unity-sdk-v" + runtimeSettings.VersionInfo.Sdk;

        public bool Enabled { get; private set; }
        public bool IsConnected() => ws != null && ws.IsConnected();

        public Int64 ServerTimestamp { get; private set; }
        string IDisposableInternal.InitializationContext { get; set; }
        string IDisposableInternal.InitializationStackTrace { get; set; }
        bool IDisposableInternal.IsDisposed { get; set; }

        private enum Event { Connected, Disconnected }
        private ConcurrentQueue<Event> eventQueue = new ConcurrentQueue<Event>();

        public event Action OnConnect;
        public event Action OnDisconnect;
        public event Action OnWebSocketFail;
#pragma warning disable CS0067 // this had to be added to get WebGL builds to complete successfully
#pragma warning disable CS0414
        public event Action<string> OnWebSocketParametersNotValid;
#pragma warning restore CS0067
#pragma warning restore CS0414
        public event Action<string> OnReceive;

        public WebSocketManager(IRuntimeSettings runtimeSettings, RequestIdSource idSource)
        {
            this.OnInitialized();
            this.runtimeSettings = runtimeSettings;
            this.idSource = idSource;
            receiveQueue = new ConcurrentQueue<string>();
            failQueue = new ConcurrentQueue<(int, string)>();
            requestCallbacks = new List<RequestCallback>();
            pushCallbacks = new Dictionary<string, RequestCallback>();
            nextCheck = DateTime.UtcNow;
        }

        ~WebSocketManager()
        {
#if UNITY
            if (SimulatorUtility.UseSharedCloudCredentials)
            {
                logger.Info($"Won't call {nameof(Dispose)} even through finalizer was executed, because {nameof(SimulatorUtility)}.{nameof(SimulatorUtility.UseSharedCloudCredentials)} is True, and we want to preserve WebSocket instance's integrity for Simulators running in the Cloud.");
                return;
            }
#endif

            if (!this.OnFinalized())
            {
                logger.Warning(Warning.RuntimeWebsocketResourceLeak, this.GetResourceLeakWarningMessage());
            }
        }

        public void Connect()
        {
            if (ws != null)
            {
                logger.Error(Error.RuntimeWebsocketAlreadyConnected, idSource.IdBaseLogParam);
                return;
            }

            logger.Debug("Enabled");
            Enabled = true;
        }

        public void Disconnect()
        {
            logger.Debug("Disabled", idSource.IdBaseLogParam);
            Enabled = false;

            ws?.CloseAsync();
        }

        public bool AddPushCallback(string requestID, OnRequest callback)
        {
            if (pushCallbacks.ContainsKey(requestID))
            {
                return false;
            }

            pushCallbacks[requestID] = new RequestCallback
            {
                requestId = requestID,
                onRequest = callback,
            };

            return true;
        }
        
        internal void SendRequest(string path, string method, string body, Dictionary<string, string> headers, string sessionToken, OnRequest callback)
        {
            if (!Enabled)
            {
                logger.Error(Error.RuntimeWebsocketSendFailedNotEnabled,
                    ("path", path),
                    ("method", method),
                    idSource.IdBaseLogParam);
                callback((int)HttpStatusCode.ServiceUnavailable, null);
                return;
            }

            string requestId = idSource.Next(out int counter);
            RequestMeta request = new RequestMeta
            {
                Id = counter,
                ResumeId = resumeId,
            };

            logger.Debug($"Building Request", ("requestID", requestId),
                ("path", path), ("method", method), ("resumeID", resumeId),
                ("headers", $"[{string.Join(", ", headers?.Select(kv => $"{kv.Key}: {kv.Value}") ?? Array.Empty<string>())}]"));

            request.Headers = new Dictionary<string, string>();
            request.Headers[CloudService.RequestIDHeader] = requestId;
            if (!string.IsNullOrEmpty(sessionToken))
            {
                request.Headers["X-Coherence-Play-Session"] = sessionToken;
            }

            if (!(headers is null))
            {
                foreach (var header in headers)
                {
                    request.Headers[header.Key] = header.Value;
                }
            }
            request.Method = method;
            request.Path = path;

            RequestCallback requestCallback = new RequestCallback
            {
                onRequest = callback,
                maxTime = DateTime.UtcNow.Add(TimeOutSpan),
                requestId = requestId,
                meta = request
            };
            requestCallbacks.Add(requestCallback);
            
            var textToBeSent = CoherenceJson.SerializeObject(request);
            textToBeSent += "\n";
            textToBeSent += body ?? "{}";

            SendText(request.Id, requestId, textToBeSent);

            nextPingTime = DateTime.UtcNow.AddSeconds(pingIntervalSeconds);
        }

        private void OnConnected()
        {
            wsConnected = true;
            wsConnecting = false;
            logger.Debug("Connected Successfully", idSource.IdBaseLogParam);
            eventQueue.Enqueue(Event.Connected);
        }
        
        private void OnDisconnected()
        {
            Reset();
            logger.Debug("Disconnected Successfully", idSource.IdBaseLogParam);
            eventQueue.Enqueue(Event.Disconnected);
        }

        private void OnReceivedMessage(string messageReceived)
        {
            logger.Debug($"Received message", idSource.IdBaseLogParam
#if UNITY_EDITOR
                            ,("message", messageReceived)
#endif
            );
            receiveQueue.Enqueue(messageReceived);
            nextPingTime = DateTime.UtcNow.AddSeconds(pingIntervalSeconds);
        }

        private async void OnWebSocketError(Error error, string message)
        {
            if (error == Error.RuntimeWebsocketCloudFailed && !await ValidateWebSocketParameters())
            {
                return;
            }
            
            logger.Error(error, message);
            Backoff();
        }

        private void OnSendFail(int requestCounter, string requestId, Error error, string message)
        {
            logger.Error(Error.RuntimeWebsocketSendException,
                ("requestID", requestId),
                ("exception", message));
            
            failQueue.Enqueue((requestCounter, requestId));
        }

        private void OpenSocket()
        {
            ws ??= WebSocketFactory.CreateWebSocket();
            wsConnecting = true;

            var requestID = idSource.Next();
            var finalEndpoint = GetFinalWsEndpoint(requestID);

            logger.Debug("Connecting WebSocket", ("requestID", requestID), ("endpoint", finalEndpoint), ("WebSocket Type", ws.GetType().Name));

            ws.OpenSocket(finalEndpoint, OnConnected, OnDisconnected, OnReceivedMessage, OnWebSocketError, OnSendFail);

            nextPingTime = DateTime.UtcNow.AddSeconds(pingIntervalSeconds);
        }

        private async Task<bool> ValidateWebSocketParameters()
        {
            if (validatedWsParameters)
            {
                return true;
            }

            try
            {
                await Request.ExecuteAsync("/validate", "GET", null, null, runtimeSettings, string.Empty,
                    idSource.Next(), true);
            }
            catch (RequestException requestException)
            {
                logger.Warning(Warning.RuntimeWebsocketWebError, requestException.Message);

                Disconnect();

                OnWebSocketParametersNotValid?.Invoke(requestException.Message);
                return false;
            }

            validatedWsParameters = true;

            return validatedWsParameters;
        }

        private string GetFinalWsEndpoint(string requestID)
        {
            var finalEndpoint =
                $"{Endpoint}&client={ClientVersion}&engine={runtimeSettings.VersionInfo.Engine}&schema_id={runtimeSettings.SchemaID}&req_id={requestID}";
            return finalEndpoint;
        }

        private void Backoff()
        {
            connectBackoff = TimeSpan.FromSeconds(Math.Min(connectBackoff.TotalSeconds * 2, Constants.maxBackoff.TotalSeconds));
            logger.Trace("Backoff", idSource.IdBaseLogParam, ("backoff", connectBackoff));
            OnWebSocketFail?.Invoke();
            Reset();
        }

        private void Reset()
        {
            ResetEvents();
            ws?.CloseAsync();
            ws = null;
            wsConnected = false;
            wsConnecting = false;
        }

        private void SendText(int requestCounter, string requestId, string text)
        {
            if (!IsConnected())
            {
                failQueue.Enqueue((requestCounter, requestId));
                return;
            }

            ws.Send(requestCounter, requestId, text);
        }

        public void Update()
        {
            if (Enabled && !wsConnected && !wsConnecting)
            {
                if (!connectBackoffStopwatch.IsRunning || connectBackoffStopwatch.Elapsed > connectBackoff)
                {
                    OpenSocket();
                    connectBackoffStopwatch.Restart();
                }
            }

            ws?.Update();

            while (eventQueue.TryDequeue(out Event ev))
            {
                switch (ev)
                {
                    case Event.Connected:
                        OnConnect?.Invoke();
                        break;
                    case Event.Disconnected:
                        OnDisconnect?.Invoke();
                        break;
                    default: break;
                }
            }

            while (failQueue.TryDequeue(out (int counter, string ID) request))
            {
                if (FindRequestCallback(request.counter, request.ID, out RequestCallback cb))
                {
                    logger.Warning(Warning.RuntimeWebsocketRequestFailed,
                        ("requestID", request.ID),
                        ("path", cb.meta.Path),
                        ("method", cb.meta.Method)
#if UNITY_EDITOR
                        ,("headers", $"[{string.Join(", ", cb.meta.Headers?.Select(kv => $"{kv.Key}: {kv.Value}") ?? Array.Empty<string>())}]")
#endif
                        );
                    RemoveRequestCallback(request.counter, request.ID);
                    cb.onRequest((int)HttpStatusCode.ServiceUnavailable, null);
                }
                else
                {
                    logger.Warning(Warning.RuntimeWebsocketCallbackNotFoundForFailedRequest, ("requestID", request.ID));
                }
            }

            while (receiveQueue.TryDequeue(out String s))
            {
                HandleResponse(s);
            }

            var now = DateTime.UtcNow;
            if (nextCheck < now)
            {
                nextCheck = now.Add(TimeoutCheckSpan);
                var timeouts = new List<RequestCallback>();
                foreach (var callback in requestCallbacks)
                {
                    if (callback.maxTime < now)
                    {
                        timeouts.Add(callback);
                    }
                }

                foreach (RequestCallback cb in timeouts)
                {
                    logger.Warning(Warning.RuntimeWebsocketRequestTimedOut,
                        ("requestID", cb.requestId),
                        ("path", cb.meta.Path),
                        ("method", cb.meta.Method)
#if UNITY_EDITOR
                        ,("headers", $"[{string.Join(", ", cb.meta.Headers?.Select(kv => $"{kv.Key}: {kv.Value}") ?? Array.Empty<string>())}]")
#endif
                        );
                    RemoveRequestCallback(cb.meta.Id, cb.requestId);
                    cb.onRequest?.Invoke((int)HttpStatusCode.RequestTimeout, null);
                }
            }

            if (Enabled && ws != null && ws.IsConnected() && ws.PingWebSocket && nextPingTime < now)
            {
                nextPingTime = now.AddSeconds(pingIntervalSeconds);
                SendRequest(pingEndpoint, "GET", null, null, null,(code, body, requestId) => {});
            }
        }

        private void HandleResponse(string text)
        {
            int pos = text.IndexOf("\n", StringComparison.Ordinal);
            string metaSrc = pos == -1 ? text : text.Substring(0, pos);
            string body = pos == -1 ? null : text.Substring(pos + 1);

            ResponseMeta meta;

            try
            {
                meta = CoherenceJson.DeserializeObject<ResponseMeta>(metaSrc);
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeWebsocketFailedToDeserializeResponse,
                    idSource.IdBaseLogParam,
#if UNITY_EDITOR
                    ("response", text),
#endif
                    ("exception", exception));
                return;
            }

            ServerTimestamp = meta.Timestamp;
            resumeId = meta.ResumeId;

            if (meta.Id == 0 && string.IsNullOrEmpty(meta.RequestId))
            {
                logger.Debug("Received ID-less message", idSource.IdBaseLogParam,
                    ("requestID", meta.RequestId), ("statusCode", meta.StatusCode));
                OnReceive?.Invoke(body);
                return;
            }

            RequestCallback cb;
            if (FindPushCallback(meta.RequestId, out cb))
            {
                logger.Debug($"Received Push Message", ("requestID", cb.requestId), ("resumeID", meta.ResumeId), ("statusCode", meta.StatusCode)
#if UNITY_EDITOR
                    ,("body", body)
#endif
                    );
                cb.onRequest?.Invoke(meta.StatusCode, body, meta.RequestId);
                return;
            }

            if (!FindRequestCallback(meta.Id, meta.RequestId, out cb))
            {
                logger.Error(Error.RuntimeWebsocketMissingResponseCallback,
                    idSource.IdBaseLogParam,
                    ("requestID", meta.RequestId),
                    ("statusCode", meta.StatusCode));
                return;
            }

            logger.Debug($"Received Response Message", ("requestID", cb.requestId), ("responseID", meta.RequestId), ("resumeID", meta.ResumeId),
                ("path", cb.meta.Path), ("method", cb.meta.Method), ("statusCode", meta.StatusCode)
#if UNITY_EDITOR
                ,("body", body)
#endif
                );

            RemoveRequestCallback(meta.Id, cb.requestId);
            cb.onRequest?.Invoke(meta.StatusCode, body, meta.RequestId);
        }

        private bool FindPushCallback(string requestID, out RequestCallback callback)
        {
            return pushCallbacks.TryGetValue(requestID, out callback);
        }

        private bool FindRequestCallback(int counter, string requestID, out RequestCallback callback)
        {
            foreach (RequestCallback cb in requestCallbacks)
            {
                if (cb.requestId == requestID || cb.meta.Id == counter)
                {
                    callback = cb;
                    return true;
                }
            }

            callback = default;
            return false;
        }

        private bool RemoveRequestCallback(int counter, string requestID)
        {
            for (int i = 0; i < requestCallbacks.Count; i++)
            {
                RequestCallback callback = requestCallbacks[i];
                if (callback.requestId == requestID || callback.meta.Id == counter)
                {
                    requestCallbacks.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            if (this.OnDisposed())
            {
                return;
            }

            wsConnected = false;
            eventQueue.Clear();
            logger?.Dispose();
            ResetEvents();

            if (ws is null)
            {
                return;
            }

            try
            {
                ws.CloseAsync();
            }
            catch
            {
                // This is fine.
            }

            ws = null;
        }

        private void ResetEvents()
        {
            OnConnect = null;
            OnDisconnect = null;
            OnWebSocketFail = null;
            OnWebSocketParametersNotValid = null;
            OnReceive = null;
        }
    }
}
