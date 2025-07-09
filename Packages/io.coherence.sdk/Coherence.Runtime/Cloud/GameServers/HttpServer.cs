// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Log;
    using Newtonsoft.Json;
    using Logger = Log.Logger;

    public struct GameServerStats
    {
        [JsonProperty("connected_players")]
        public int ConnectedPlayers;
    }

    public class HttpServer
    {
        private delegate Task HandleEndpoint(HttpListenerContext ctx, HttpServer serv, Logger logger);
        private static readonly Dictionary<(string method, string path), HandleEndpoint> endpoints = new()
        {
            { ("GET", "/health"), HandleEndpointGetHealth },
            { ("GET", "/stats"), HandleEndpointGetStats },
            { ("GET", "/metrics"), HandleEndpointGetMetrics },
        };

        private HttpListener listener;
        private CancellationTokenSource cts;
        private static Logger logger = Log.GetLogger<HttpServer>();
        private int port;
        private Func<GameServerStats> statsFn;

        public Task<bool> Start(int port, Func<GameServerStats> statsFn)
        {
            cts = new CancellationTokenSource();
            listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{port}/");
            listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;

            this.port = port;
            this.statsFn = statsFn;

            return Task.Run(Listen, cts.Token);
        }

        public void Stop()
        {
            if (listener == null)
            {
                return;
            }

            logger.Info("Stopping HTTP server");

            cts.Cancel();
            cts.Dispose();
            listener.Stop();

            listener = null;
            cts = null;
        }

        private async Task<bool> Listen()
        {
            try
            {
                listener.Start();
                logger.Info("HTTP Server listening on " + port);
            }
            catch (Exception e)
            {
                logger.Error(Error.RuntimeCloudGameServersErrorMsg, e.ToString());
                return false;
            }

            while (!cts.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await listener.GetContextAsync();
                }
                catch (Exception ex)
                {
                    switch (ex)
                    {
                        case HttpListenerException { ErrorCode: 995, }:
                            // ERROR_OPERATION_ABORTED
                            return false;
                        case ObjectDisposedException or OperationCanceledException:
                            // The listener was closed via call to `Close()`
                            // so that's fine.
                            return true;
                        default:
                            throw;
                    }
                }

                if (ctx == null)
                {
                    continue;
                }

                _ = RouteRequest(ctx);
            }

            return true;
        }

        private async Task RouteRequest(HttpListenerContext ctx)
        {
            logger.Debug("RouteRequest", ("method", ctx.Request.HttpMethod), ("path", ctx.Request.Url.AbsolutePath));

            if (endpoints.TryGetValue((ctx.Request.HttpMethod, ctx.Request.Url.AbsolutePath), out var fn))
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
            r.ContentType ??= "text/plain";
            r.ContentEncoding = Encoding.UTF8;
            r.StatusCode = (int)status;

            if (body != null)
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                r.ContentLength64 = body.Length;
                await r.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                r.OutputStream.Close();
            }

            r.Close();
        }

        #region Endpoints
        private static async Task HandleEndpointGetHealth(HttpListenerContext ctx, HttpServer serv, Logger logger)
        {
            await WriteResponse(ctx, HttpStatusCode.OK, logger);
        }

        private static async Task HandleEndpointGetStats(HttpListenerContext ctx, HttpServer serv, Logger logger)
        {
            var stats = GetStats(serv);
            var respBody = Utils.CoherenceJson.SerializeObject(stats);

            ctx.Response.ContentType = "application/json";
            await WriteResponse(ctx, HttpStatusCode.OK, respBody, logger);
        }

        private static async Task HandleEndpointGetMetrics(HttpListenerContext ctx, HttpServer serv, Logger logger)
        {
            var stats = GetStats(serv);
            var respBody = PrometheusMetric(
                type: "gauge",
                name: "connected_players",
                description: "Number of connected players.",
                value: stats.ConnectedPlayers);

            await WriteResponse(ctx, HttpStatusCode.OK, respBody, logger);
        }

        private static GameServerStats GetStats(HttpServer serv)
        {
            return serv.statsFn?.Invoke() ?? new GameServerStats();
        }

        private static string PrometheusMetric(string type, string name, string description, IFormattable value)
        {
            StringBuilder sb = new();
            sb.Append($"# HELP {name} {description}\n");
            sb.Append($"# TYPE {name} {type}\n");
            sb.Append($"{name} {value}\n\n");
            return sb.ToString();
        }
        #endregion
    }
}
