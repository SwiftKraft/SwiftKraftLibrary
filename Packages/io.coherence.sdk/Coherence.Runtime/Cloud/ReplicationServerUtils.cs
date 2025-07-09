// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
// Any changes to the Unity version of the request should be reflected
// in the HttpClient version.
// TODO: Separate Http client impl. with common options/policy layer (coherence/unity#1764)
#define UNITY
#endif

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.Cloud
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using System.Net.Http;
#if UNITY
    using Common;
    using UnityEngine.Networking;
#endif

    public static class ReplicationServerUtils
    {
        /// <summary>
        /// Request timeout.
        /// </summary>
        /// <remarks>
        /// In seconds. Assigned to the inner <see cref="UnityWebRequest.timeout"/>.
        /// </remarks>
        public static int Timeout { get; set; }

        public static async Task<bool> PingHttpServerAsync(string host, int port)
        {
#if UNITY
            var url = $"http://{host}:{port}/health";

            using var request = new UnityWebRequest(url, HttpMethod.Get.Method);
            request.timeout = Timeout;

            _ = request.SendWebRequest();

            while (!request.isDone)
            {
                await Task.Yield();
            }

            return request.responseCode == (long)HttpStatusCode.OK;
#else
            return await Task.FromResult(false);
#endif
        }

        public static void PingHttpServer(string host, int port, Action<bool> onCompleted)
        {
#if UNITY
            PingHttpServerAsync(host, port).ContinueWith((antecedent) =>
            {
                onCompleted?.Invoke(antecedent.Result);
            }, TaskUtils.Scheduler);
#else
            onCompleted?.Invoke(false);
#endif
        }

        [Deprecated("02/2025", 1, 5, 1)]
        [Obsolete("Use void PingHttpServer(string host, int port, Action<bool> onCompleted) instead.")]
        public static bool PingHttpServer(string host, int port)
        {
#if UNITY
            var url = $"http://{host}:{port}/health";

            var request = new UnityWebRequest(url, HttpMethod.Get.Method);
            request.timeout = Timeout;

            _ = request.SendWebRequest();

            while (!request.isDone)
            {
            }

            return request.responseCode == (long)HttpStatusCode.OK;
#else
            return false;
#endif
        }
    }
}

