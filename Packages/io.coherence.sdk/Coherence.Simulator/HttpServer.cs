// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Simulator
{
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Coherence.Connection;
    using Coherence.Log;
    using System;
    using UnityEngine;
    using Logger = Log.Logger;

    public class HttpServer
    {
        private delegate Task HandleEndpoint(HttpListenerContext ctx, HttpServer serv, Logger logger);

        public delegate void JoinDelegate(EndpointData endpointData);
        public event JoinDelegate OnJoinRequested;

        private static readonly Dictionary<(string method, string path), HandleEndpoint> endpoints = new Dictionary<(string method, string path), HandleEndpoint>
        {
            { ("GET", "/rooms"), HandleEndpointListRooms },
            { ("PUT", "/rooms"), HandleEndpointJoinRoom },
            { ("GET", "/health"), HandleEndpointGetHealth },
        };

        public static readonly int defaultPort = Constants.localHttpServerPort;

        private HttpListener listener;
        private CancellationTokenSource cts;
        private Logger logger;

        public int Port { get; private set; }

        public Task Start()
        {
            return Start(defaultPort);
        }

        public Task<bool> Start(int port)
        {
            cts = new CancellationTokenSource();
            listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{port}/");
            listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;

            Port = port;

            logger = Log.GetLogger<HttpServer>();

            return Task.Run(Listen, cts.Token);
        }

        public void Stop()
        {
            if (listener == null)
            {
                return;
            }

            logger.Debug("Stop");

            cts.Cancel();
            cts.Dispose();
            listener.Stop();

            listener = null;
            cts = null;
        }

        private async Task<bool> Listen()
        {
            logger.Debug("Listen");

            try
            {
                listener.Start();
            }
            catch (Exception e)
            {
                logger.Error(Coherence.Log.Error.SimulatorHTTPListenException,
                    e.Message + "\n" + e.StackTrace);
                return false;
            }

            logger.Debug("HTTP Server listening on " + Port);

            while (!cts.IsCancellationRequested)
            {
                HttpListenerContext ctx = null;
                try
                {
                    ctx = await listener.GetContextAsync();
                }
                catch (Exception exception)
                {
                    if (exception is HttpListenerException httpException)
                    {
                        if (httpException.ErrorCode == 995)
                        {
                            return false;
                        }
                    }

                    if (exception is ObjectDisposedException || exception is OperationCanceledException)
                    {
                        // The listener was closed via call to `Close()`
                        // so that's fine.
                        return true;
                    }

                    throw;
                }

                if (ctx == null)
                {
                    continue;
                }

                // Route request concurrently
                _ = RouteRequest(ctx, logger);
            }

            return true;
        }

        private async Task RouteRequest(HttpListenerContext ctx, Logger logger)
        {
            logger.Debug("RouteRequest", ("method", ctx.Request.HttpMethod), ("path", ctx.Request.Url.AbsolutePath));

            if (endpoints.TryGetValue((ctx.Request.HttpMethod, ctx.Request.Url.AbsolutePath), out HandleEndpoint fn))
            {
                await fn(ctx, this, logger);
            }
            else
            {
                await WriteResponse(ctx, HttpStatusCode.NotFound, logger);
            }
        }

        private static async Task WriteResponse(HttpListenerContext ctx, HttpStatusCode status, Logger logger)
        {
            await WriteResponse(ctx, status, null, logger);
        }

        private static async Task WriteResponse(HttpListenerContext ctx, HttpStatusCode status, string body, Logger logger)
        {
            logger.Debug("WriteResponse", ("method", ctx.Request.HttpMethod), ("path", ctx.Request.Url.AbsolutePath), ("status", status), ("body", body));

            var r = ctx.Response;
            r.Headers.Add(HttpResponseHeader.CacheControl, "private, no-store");
            r.Headers.Add(HttpResponseHeader.Server, "coherence");
            r.ContentType = "application/json";
            r.StatusCode = (int)status;

            if (body != null)
            {
                var bytes = body != null ? Encoding.UTF8.GetBytes(body) : new byte[] { };
                r.ContentLength64 = body.Length;
                await r.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }

            r.OutputStream.Close();
            r.Close();
        }

        #region Endpoints
        private static async Task HandleEndpointGetHealth(HttpListenerContext ctx, HttpServer serv, Logger logger)
        {
            await WriteResponse(ctx, HttpStatusCode.OK, logger);
        }

        private static async Task HandleEndpointListRooms(HttpListenerContext ctx, HttpServer serv, Logger logger)
        {
            await WriteResponse(ctx, HttpStatusCode.InternalServerError, "{\"error\":\"not implemented\"}", logger);
        }

        private static async Task HandleEndpointJoinRoom(HttpListenerContext ctx, HttpServer serv, Logger logger)
        {
            try
            {
                string data = string.Empty;
                using (var sr = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
                {
                    data = await sr.ReadToEndAsync();
                }

                var joinRoomRequest = Utils.CoherenceJson.DeserializeObject<JoinRoomRequest>(data);
                var endpointData = joinRoomRequest.ToEndpointData();
                endpointData.authToken = SimulatorUtility.AuthToken;

                var (isValid, validationError) = endpointData.Validate();

                if (!isValid)
                {
                    await Error(ctx, validationError, logger);
                }
                else
                {
                    serv.OnJoinRequested?.Invoke(endpointData);
                    await WriteResponse(ctx, HttpStatusCode.OK, "", logger);
                }
            }
            catch (System.Exception e)
            {
                logger.Error(Coherence.Log.Error.SimulatorHTTPSystemException, ("exception", e));
                return;
            }
        }

        private static async Task Error(HttpListenerContext ctx, string message, Logger logger)
        {
            var err = new Dictionary<string, string>()
            {
                ["error"] = message,
            };
            await WriteResponse(ctx,
                HttpStatusCode.InternalServerError,
                Utils.CoherenceJson.SerializeObject(err), logger);
        }
        #endregion
    }
}
