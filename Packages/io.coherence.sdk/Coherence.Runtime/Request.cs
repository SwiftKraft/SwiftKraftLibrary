// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
// Any changes to the Unity version of the request should be reflected
// in the HttpClient version.
// TODO: Separate Http client impl. with common options/policy layer (coherence/unity#1764)
#define UNITY
#endif

namespace Coherence.Runtime
{
    using Cloud;
    using Common;
    using Connection;
    using Log;
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Net;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using Logger = Log.Logger;

#if UNITY
    using UnityEngine;
    using UnityEngine.Networking;
#endif

    public delegate void OnRequest(int code, string resultText, string requestId = null);
    public struct RequestCallback
    {
        public OnRequest onRequest;
        public System.DateTime maxTime;
        public string requestId;
        internal RequestMeta meta;
    }

    internal static class Request
    {
        private static readonly Logger logger = Log.GetLogger(typeof(Request));

#if !UNITY
        private static Lazy<HttpClient> httpClient = new Lazy<HttpClient>(() => new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(10f)
        });
#endif

        internal static void ExecuteCustom(string endpoint, string path, string method, string body, Action<RequestResponse<string>> callback)
        {
            string uri = $"{endpoint}{path}";
            logger.Debug($"Request uri={uri}, method={method}, body={ReplacePasswordJSON(body)}");
#if UNITY
            var req = new UnityWebRequest(uri, method);
            req.timeout = 10;
            if (!string.IsNullOrEmpty(body))
            {
                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {AuthToken.LocalDevelopmentSecret}");
            var op = req.SendWebRequest();

            op.completed += (res) =>
            {
#if UNITY_EDITOR
                // Exit early if we have exited play mode
                if (!UnityEditor.EditorApplication.isPlaying)
                {
                    req.Dispose();
                    return;
                }
#endif
                RequestResponse<string> response = default;

                try
                {
                    switch (req.result)
                    {
                        case UnityWebRequest.Result.ProtocolError:
                        case UnityWebRequest.Result.ConnectionError:
                        case UnityWebRequest.Result.DataProcessingError:
                            logger.Debug($"Response status={req.responseCode} result={req.result} error={req.error}");

                            response = new RequestResponse<string>()
                            {
                                Status = RequestStatus.Fail, Result = req.downloadHandler?.text
                            };

                            if (!string.IsNullOrEmpty(response.Result) &&
                                RequestException.TryParse(response.Result, (int)req.responseCode,
                                    out RequestException exception, logger))
                            {
                                response.Exception = exception;
                            }
                            else
                            {
                                response.Exception = new RequestException((int)req.responseCode, req.error);
                            }

                            return;
                    }

                    logger.Debug($"Status code={req.responseCode}, Response text={req.downloadHandler.text}");

                    response = new RequestResponse<string>()
                    {
                        Status = RequestStatus.Success, Result = req.downloadHandler.text
                    };
                }
                finally
                {
                    req.Dispose();
                    callback.Invoke(response);
                }
            };
#endif
        }

        internal static async Task<string> ExecuteCustomAsync(string endpoint, string path, string method, string body)
        {
            string uri = $"{endpoint}{path}";
            logger.Debug($"Request uri={uri}, method={method}, body={ReplacePasswordJSON(body)}");

#if UNITY
            var req = new UnityWebRequest(uri, method);
            req.timeout = 10;
            if (!string.IsNullOrEmpty(body))
            {
                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {AuthToken.LocalDevelopmentSecret}");
            var op = req.SendWebRequest();

            var tsc = new TaskCompletionSource<string>();
            op.completed += (res) =>
            {
                try
                {
                    switch (req.result)
                    {
                        case UnityWebRequest.Result.ProtocolError:
                        case UnityWebRequest.Result.ConnectionError:
                        case UnityWebRequest.Result.DataProcessingError:
                            logger.Debug($"Response status={req.responseCode} result={req.result} error={req.error}");

                            string response = req.downloadHandler?.text;
                            if (!string.IsNullOrEmpty(response) &&
                                RequestException.TryParse(response, (int)req.responseCode, out RequestException exception, logger))
                            {
                                tsc.SetException(exception);
                            }
                            else
                            {
                                tsc.SetException(new RequestException((int)req.responseCode, req.error));
                            }
                            return;
                    }

                    logger.Debug($"Status code={req.responseCode}, Response text={req.downloadHandler.text}");
                    tsc.SetResult(req.downloadHandler.text);
                }
                finally
                {
                    req.Dispose();
                }
            };
            return await tsc.Task;
#else // PURE_DOTNET_CLIENT
            HttpClient client = httpClient.Value;

            var request = new HttpRequestMessage(GetMethod(method), uri);
            if (!string.IsNullOrEmpty(body))
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuthToken.LocalDevelopmentSecret);

            int statusCode = 0;
            try
            {
                HttpResponseMessage response = await client.SendAsync(request);
                statusCode = (int)response.StatusCode;

                if (!response.IsSuccessStatusCode)
                {
                    string error = null;
                    try
                    {
                        error = await response.Content.ReadAsStringAsync();
                    }
                    catch
                    {
                        // We couldn't pull body, continue with a generic error
                    }

                    if (!string.IsNullOrEmpty(error) && RequestException.TryParse(error, (int)response.StatusCode, out RequestException exception, logger))
                    {
                        throw exception;
                    }

                    throw new RequestException((int)response.StatusCode, "Request error");
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeRequestError,
                    ("uri", uri),
                    ("method", method),
                    ("exception", exception.Message));

                if (exception is RequestException)
                {
                    throw;
                }

                throw new RequestException(statusCode, exception.Message);
            }
#endif
        }

        internal static void Execute(string path, string method, string body,
           Dictionary<string, string> headers, IRuntimeSettings settings, string sessionToken, string requestId, Action<RequestResponse<string>> callback)
        {
            if (string.IsNullOrEmpty(settings.RuntimeKey))
            {
                logger.Warning(Warning.RuntimeRequestNoKey);

                throw new RequestException(HttpStatusCode.ServiceUnavailable, null);
            }

            string uri = settings.ApiEndpoint + path;
            logger.Debug("Request", ("requestID", requestId),
                ("path", path), ("method", method), ("endpoint", settings.ApiEndpoint),
                ("headers", $"[{string.Join(", ", headers?.Select(kv => $"{kv.Key}: {kv.Value}") ?? Array.Empty<string>())}]"));
#if UNITY
            UnityWebRequest req = new UnityWebRequest(uri, method);
            req.timeout = 15; // TODO: make this configurable in the future
            if (!string.IsNullOrEmpty(body))
            {
                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Coherence-Runtime-Key", settings.RuntimeKey);
            req.SetRequestHeader(CloudService.SchemaIdHeader, settings.SchemaID);
            req.SetRequestHeader(CloudService.ClientVersionHeader, "unity-sdk-v" + settings.VersionInfo.Sdk);
            req.SetRequestHeader(CloudService.RSVersionHeader, settings.VersionInfo.Engine);
            req.SetRequestHeader(CloudService.RequestIDHeader, requestId);

            if (!(headers is null))
            {
                foreach (var header in headers)
                {
                    req.SetRequestHeader(header.Key, header.Value);
                }
            }

            if (!string.IsNullOrEmpty(sessionToken))
            {
                req.SetRequestHeader("X-Coherence-Play-Session", sessionToken);
            }

            var tsc = new TaskCompletionSource<string>();
            var op = req.SendWebRequest();
            op.completed += (_) =>
            {
                RequestResponse<string> response = default;

                try
                {
                    switch (req.result)
                    {
                        case UnityWebRequest.Result.ProtocolError:
                        case UnityWebRequest.Result.ConnectionError:
                        case UnityWebRequest.Result.DataProcessingError:
                            response = new RequestResponse<string>() { Status = RequestStatus.Fail, Result = req.downloadHandler?.text };

                            if (string.IsNullOrEmpty(response.Result) ||
                                !RequestException.TryParse(response.Result, (int)req.responseCode, out RequestException exception, logger))
                            {
                                exception = new RequestException((int)req.responseCode, req.error);
                                response.Exception = exception;
                            }

                            logger.Warning(Warning.RuntimeRequestFailed,
                                ("requestID", requestId),
                                ("path", path),
                                ("method", method),
                                ("statusCode", req.responseCode),
                                ("error", req.error),
                                ("result", req.result),
                                ("endpoint", settings.ApiEndpoint),
                                ("errorCode", exception.ErrorCode),
                                ("message", exception.Message),
                                ("userMessage", exception.UserMessage),
                                ("stackTrace", exception.StackTrace)
#if UNITY_EDITOR
                                , ("body", response)
#endif
                            );

                            callback.Invoke(response);
                            return;
                    }

                    string responseId = req.GetResponseHeader(CloudService.RequestIDHeader);

                    logger.Debug($"Response", ("requestID", requestId), ("responseID", responseId),
                        ("path", path), ("method", method), ("statusCode", req.responseCode),
                        ("endpoint", settings.ApiEndpoint)
#if UNITY_EDITOR
                        ,("body", body)
#endif
                        );

                    tsc.SetResult(req.downloadHandler.text);
                    response = new RequestResponse<string>() { Status = RequestStatus.Success, Result = req.downloadHandler.text };

                }
                finally
                {
                    req.Dispose();
                    callback.Invoke(response);
                }
            };
#endif
        }

        internal static async Task<string> ExecuteAsync(string path, string method, string body,
            Dictionary<string, string> headers, IRuntimeSettings settings, string sessionToken, string requestId, bool silenceWarning = false)
        {
            if (string.IsNullOrEmpty(settings.RuntimeKey))
            {
                logger.Warning(Warning.RuntimeRequestNoKey);

                throw new RequestException(HttpStatusCode.ServiceUnavailable, null);
            }

            string uri = settings.ApiEndpoint + path;
            logger.Debug($"Request", ("requestID", requestId),
                ("path", path), ("method", method), ("endpoint", settings.ApiEndpoint),
                ("headers", $"[{string.Join(", ", headers?.Select(kv => $"{kv.Key}: {kv.Value}") ?? Array.Empty<string>())}]"));


#if UNITY
            UnityWebRequest req = new UnityWebRequest(uri, method);
            req.timeout = 15; // TODO: make this configurable in the future
            if (!string.IsNullOrEmpty(body))
            {
                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Coherence-Runtime-Key", settings.RuntimeKey);
            req.SetRequestHeader("X-Coherence-Schema-ID", settings.SchemaID);
            req.SetRequestHeader("X-Coherence-Client", "unity-sdk-v" + settings.VersionInfo.Sdk);
            req.SetRequestHeader("X-Coherence-Engine", settings.VersionInfo.Engine);
            req.SetRequestHeader(CloudService.RequestIDHeader, requestId);

            if (!(headers is null))
            {
                foreach (var header in headers)
                {
                    req.SetRequestHeader(header.Key, header.Value);
                }
            }

            if (!string.IsNullOrEmpty(sessionToken))
            {
                req.SetRequestHeader("X-Coherence-Play-Session", sessionToken);
            }

            var tsc = new TaskCompletionSource<string>();
            var op = req.SendWebRequest();
            op.completed += (_) =>
            {
                try
                {
                    switch (req.result)
                    {
                        case UnityWebRequest.Result.ProtocolError:
                        case UnityWebRequest.Result.ConnectionError:
                        case UnityWebRequest.Result.DataProcessingError:
                            string response = req.downloadHandler?.text;

                            if (string.IsNullOrEmpty(response) ||
                                !RequestException.TryParse(response, (int)req.responseCode, out RequestException exception, logger))
                            {
                                exception = new RequestException((int)req.responseCode, req.error);
                            }

                            if (!silenceWarning)
                            {
                                logger.Warning(Warning.RuntimeRequestFailed,
                                    ("requestID", requestId),
                                    ("path", path),
                                    ("method", method),
                                    ("statusCode", req.responseCode),
                                    ("error", req.error),
                                    ("result", req.result),
                                    ("endpoint", settings.ApiEndpoint),
                                    ("errorCode", exception.ErrorCode),
                                    ("message", exception.Message)
#if UNITY_EDITOR
                                , ("body", response)
#endif
                                );
                            }

                            tsc.SetException(exception);

                            return;
                    }

                    string responseId = req.GetResponseHeader(CloudService.RequestIDHeader);

                    logger.Debug($"Response", ("requestID", requestId), ("responseID", responseId),
                        ("path", path), ("method", method), ("statusCode", req.responseCode),
                        ("endpoint", settings.ApiEndpoint)
#if UNITY_EDITOR
                        ,("body", body)
#endif
                        );

                    tsc.SetResult(req.downloadHandler.text);
                }
                finally
                {
                    req.Dispose();
                }
            };

            return await tsc.Task;
#else // PURE_DOTNET_CLIENT
            HttpClient client = httpClient.Value;

            var request = new HttpRequestMessage(GetMethod(method), uri);
            if (!string.IsNullOrEmpty(body))
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            request.Headers.Add("X-Coherence-Runtime-Key", settings.RuntimeKey);
            request.Headers.Add("X-Coherence-Schema-ID", settings.SchemaID);
            request.Headers.Add("X-Coherence-Client", "unity-sdk-v" + settings.VersionInfo.Sdk);
            request.Headers.Add("X-Coherence-Engine", settings.VersionInfo.Engine);
            request.Headers.Add("X-Coherence-Request-ID", requestId);

            if (!(headers is null))
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            if (!string.IsNullOrEmpty(sessionToken))
            {
                request.Headers.Add("X-Coherence-Play-Session", sessionToken);
            }

            int statusCode = 0;
            try
            {
                HttpResponseMessage response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    string error = null;
                    try
                    {
                        error = await response.Content.ReadAsStringAsync();
                    }
                    catch
                    {
                        // We couldn't pull body, continue with a generic error
                    }

                    if (!string.IsNullOrEmpty(error) && RequestException.TryParse(error, (int)response.StatusCode, out RequestException exception, logger))
                    {
                        throw exception;
                    }

                    throw new RequestException((int)response.StatusCode, "Request error");
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception exception)
            {
                logger.Error(Error.RuntimeRequestFailed,
                    ("uri", uri),
                    ("method", method),
                    ("exception", exception.Message));

                if (exception is RequestException)
                {
                    throw;
                }

                throw new RequestException(statusCode, exception.Message);
            }
#endif
        }

#if !UNITY
        static HttpMethod GetMethod(string method)
        {
            return method.ToLower() switch
            {
                "get" => HttpMethod.Get,
                "post" => HttpMethod.Post,
                "put" => HttpMethod.Put,
                "delete" => HttpMethod.Delete,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
#endif

        private static string ReplacePasswordJSON(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return Regex.Replace(input, @"""password"":""(.*?)""", @"""password"":""********""");
        }
    }
}
