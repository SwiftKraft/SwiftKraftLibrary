// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

namespace Coherence.Cloud
{
    using Common;
    using Connection;
    using Log;
    using Runtime;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Logger = Log.Logger;

    public class RequestFactory : IUpdatable, IRequestFactoryInternal, IDisposableInternal
    {
        public event Action OnWebSocketConnect;
        public event Action OnWebSocketDisconnect;
        public event Action OnWebSocketConnectionError;
        public event Action<string> OnWebSocketParametersNotValid;

        private Lazy<WebSocketManager> lazyWebSocket;
        private WebSocketManager WebSocket => lazyWebSocket.Value;

        private readonly IRuntimeSettings runtimeSettings;
        private readonly RequestIdSource idSource;
        private readonly Logger logger = Log.GetLogger<RequestFactory>();
        private readonly RequestThrottle throttle = new RequestThrottle(TimeSpan.FromSeconds(1.1f));

        private Dictionary<(string, string), string> responsesDictionary = new Dictionary<(string, string), string>();

        private Dictionary<string, List<Action<string>>> pushCallbacks = new Dictionary<string, List<Action<string>>>();
        private Dictionary<string, List<Action<string>>> delayedPushCallbackRemoval = new Dictionary<string, List<Action<string>>>();
        private Dictionary<string, OnRequest> delayedWebSocketCallbackAddition = new Dictionary<string, OnRequest>();
        private bool useWebSocket;

        public bool IsReady
        {
            get
            {
                if (!useWebSocket || (WebSocket.Enabled && WebSocket.IsConnected()))
                {
                    return true;
                }

                return false;
            }
        }

        string IDisposableInternal.InitializationContext { get; set; }
        string IDisposableInternal.InitializationStackTrace{ get; set; }
        bool IDisposableInternal.IsDisposed { get; set; }
        RequestThrottle IRequestFactoryInternal.Throttle => throttle;

        public RequestFactory(IRuntimeSettings runtimeSettings, bool useWebSocket = true)
        {
            this.OnInitialized();
            this.runtimeSettings = runtimeSettings;
            this.useWebSocket = useWebSocket;
            idSource = new RequestIdSource();

            lazyWebSocket = new Lazy<WebSocketManager>(() =>
            {
                var ws = new WebSocketManager(runtimeSettings, idSource);
                ws.OnConnect += OnWebSocketConnected;
                ws.OnDisconnect += OnWebSocketDisconnected;
                ws.OnWebSocketFail += OnWebSocketConnectionHasError;
                ws.OnWebSocketParametersNotValid += OnWebSocketParamsNotValid;

                if (!useWebSocket || string.IsNullOrEmpty(runtimeSettings.RuntimeKey))
                {
                    return ws;
                }

                foreach (var kv in delayedWebSocketCallbackAddition)
                {
                    ws.AddPushCallback(kv.Key, kv.Value);
                }

                delayedWebSocketCallbackAddition.Clear();

                ws.Connect();
                Updater.RegisterForUpdate(this);

                return ws;
            });
        }

        ~RequestFactory()
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
                logger.Warning(Warning.RuntimeCloudRequestFactoryResourceLeak, this.GetResourceLeakWarningMessage());
            }
        }

        public void ForceCreateWebSocket()
        {
            _ = lazyWebSocket.Value;
        }

        public void AddPushCallback(string requestPath, Action<string> onPushCallback)
        {
            if (!pushCallbacks.TryGetValue(requestPath, out var actions))
            {
                pushCallbacks.Add(requestPath, new List<Action<string>>() { onPushCallback });

                OnRequest callback = (int code, string body, string requestId) =>
                {
                    foreach (var callback in pushCallbacks[requestPath])
                    {
                        callback.Invoke(body);
                    }
                };

                if (lazyWebSocket.IsValueCreated)
                {
                    WebSocket.AddPushCallback(requestPath, callback);
                }
                else
                {
                    delayedWebSocketCallbackAddition.Add(requestPath, callback);
                }
            }
            else
            {
                pushCallbacks[requestPath].Add(onPushCallback);
            }
        }

        public void RemovePushCallback(string requestPath, Action<string> onPushCallback)
        {
            if (!pushCallbacks.TryGetValue(requestPath, out var actions))
            {
                return;
            }

            if (!delayedPushCallbackRemoval.TryGetValue(requestPath, out var actionsToRemove))
            {
                delayedPushCallbackRemoval.Add(requestPath, new List<Action<string>>() { onPushCallback });
            }
            else
            {
                actionsToRemove.Add(onPushCallback);
            }
        }

        public void SetRequestThrottling(TimeSpan requestInterval)
        {
            logger.Debug("Set request throttling", ("interval", requestInterval));
            throttle.RequestInterval = requestInterval;
        }

        public TimeSpan GetRequestCooldown(string request, string method)
        {
            return throttle.RequestCooldown(request, method);
        }

        public void SendRequest(string basePath, string method, string body,
            Dictionary<string, string> headers, string requestName, string sessionToken, Action<RequestResponse<string>> callback)
        {
            SendRequest(basePath, string.Empty, method, body, headers, requestName, sessionToken, callback);
        }

        public void SendRequest(string basePath, string pathParams, string method, string body,
            Dictionary<string, string> headers, string requestName, string sessionToken, Action<RequestResponse<string>> callback)
        {
            if (throttle.HandleTooManyRequests(basePath + pathParams, method, requestName))
            {
                RequestResponse<string> response = default;

                if (method.Equals("GET"))
                {
                    responsesDictionary.TryGetValue((basePath, method), out var responseString);

                    response = new RequestResponse<string>()
                    {
                        Status = RequestStatus.Success,
                        Result = responseString
                    };
                }
                else
                {
                    response = new RequestResponse<string>()
                    {
                        Status = RequestStatus.Fail,
                        Exception = new RequestException(ErrorCode.TooManyRequests, 0,
                            $"Too many '{requestName}' requests. Please try again in a moment.")
                    };
                }

                callback.Invoke(response);

                return;
            }

            string path = string.IsNullOrEmpty(pathParams) ? basePath : $"{basePath}{pathParams}";

            if (!WebSocket.Enabled)
            {
                Request.Execute(path, method, body, headers, runtimeSettings, sessionToken, idSource.Next(), callback);
                return;
            }

            WebSocket.SendRequest(path, method, body, headers, sessionToken, (statusCode, responseBody, requestId) =>
            {
                RequestResponse<string> response;

                if (!StatusCodes.IsSuccess(statusCode))
                {
                    if (RequestException.TryParse(responseBody, statusCode, out var exception, logger))
                    {
                        logger.Warning(Warning.RuntimeResponseFailed,
                            ("requestID", requestId),
                            ("path", path),
                            ("method", method),
                            ("statusCode", statusCode),
                            ("message", exception.Message),
                            ("errorCode", exception.ErrorCode));

                        response = new RequestResponse<string>()
                        {
                            Status = RequestStatus.Fail,
                            Result = string.Empty,
                            Exception = exception
                        };

                        callback.Invoke(response);
                        return;
                    }

                    logger.Warning(Warning.RuntimeResponseFailed,
                        ("requestID", requestId),
                        ("path", path),
                        ("method", method),
                        ("statusCode", statusCode));

                    response = new RequestResponse<string>()
                    {
                        Status = RequestStatus.Fail,
                        Result = string.Empty,
                        Exception = new RequestException(statusCode, "Request failure")
                    };

                    callback.Invoke(response);

                    return;
                }

                responsesDictionary[(basePath, method)] = responseBody;

                response = new RequestResponse<string>()
                {
                    Status = RequestStatus.Success,
                    Result = responseBody
                };

                callback.Invoke(response);
            });
        }

        public Task<string> SendRequestAsync(string basePath, string method, string body,
            Dictionary<string, string> headers, string requestName, string sessionToken)
            => SendRequestAsync(basePath, string.Empty, method, body, headers, requestName, sessionToken);

        public async Task<string> SendRequestAsync(string basePath, string pathParams, string method, string body,
            Dictionary<string, string> headers, string requestName, string sessionToken)
        {
            if (throttle.HandleTooManyRequests(basePath + pathParams, method, requestName))
            {
                if (method.Equals("GET"))
                {
                    responsesDictionary.TryGetValue((basePath, method), out var response);
                    return response;
                }

                throw new RequestException(ErrorCode.TooManyRequests, 0,
                    $"Too many '{requestName}' requests. Please try again in a moment.");
            }

            string path = string.IsNullOrEmpty(pathParams) ? basePath : $"{basePath}{pathParams}";

            if (!WebSocket.Enabled)
            {
                return await Request.ExecuteAsync(path, method, body, headers, runtimeSettings, sessionToken, idSource.Next());
            }

            var tsc = new TaskCompletionSource<string>();
            WebSocket.SendRequest(path, method, body, headers, sessionToken, (statusCode, responseBody, requestId) =>
            {
                if (!StatusCodes.IsSuccess(statusCode))
                {
                    if (RequestException.TryParse(responseBody, statusCode, out var exception, logger))
                    {
                        logger.Warning(Warning.RuntimeResponseFailed,
                            ("requestID", requestId),
                            ("path", path),
                            ("method", method),
                            ("statusCode", statusCode),
                            ("message", exception.Message),
                            ("errorCode", exception.ErrorCode));
                        tsc.SetException(exception);
                        return;
                    }

                    logger.Warning(Warning.RuntimeResponseFailed,
                        ("requestID", requestId),
                        ("path", path),
                        ("method", method),
                        ("statusCode", statusCode));

                    tsc.SetException(new RequestException(statusCode, "Request failure"));
                    return;
                }

                responsesDictionary[(basePath, method)] = responseBody;

                tsc.SetResult(responseBody);
            });
            return await tsc.Task;
        }

        public void SendCustomRequest(string endpoint,
            string path, string method, string body, Action<RequestResponse<string>> callback)
        {
            Request.ExecuteCustom(endpoint, path, method, body, callback);
        }

        public async Task<string> SendCustomRequestAsync(string endpoint,
                                                                 string path, string method, string body)
        {
            return await Request.ExecuteCustomAsync(endpoint, path, method, body);
        }

        public void Dispose()
        {
            if (this.OnDisposed())
            {
                return;
            }

            if (lazyWebSocket.IsValueCreated)
            {
                lazyWebSocket.Value.Dispose();
            }

            OnWebSocketConnect = null;
            OnWebSocketDisconnect = null;
            OnWebSocketConnectionError = null;
            OnWebSocketParametersNotValid = null;

            Updater.DeregisterForUpdate(this);

            logger?.Dispose();
        }

        private void OnWebSocketConnected()
        {
            OnWebSocketConnect?.Invoke();
        }

        private void OnWebSocketDisconnected()
        {
            OnWebSocketDisconnect?.Invoke();
        }

        private void OnWebSocketConnectionHasError()
        {
            OnWebSocketConnectionError?.Invoke();
        }

        private void OnWebSocketParamsNotValid(string msg)
        {
            OnWebSocketParametersNotValid?.Invoke(msg);
        }

        void IUpdatable.Update()
        {
            WebSocket?.Update();

            CleanStalePushCallbacks();
        }

        private void CleanStalePushCallbacks()
        {
            foreach (var kv in delayedPushCallbackRemoval)
            {
                foreach (var actionToBeRemoved in kv.Value)
                {
                    pushCallbacks[kv.Key].Remove(actionToBeRemoved);
                }
            }

            delayedPushCallbackRemoval.Clear();
        }
    }
}
