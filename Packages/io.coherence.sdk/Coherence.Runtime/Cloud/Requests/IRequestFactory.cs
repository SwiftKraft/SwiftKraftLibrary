// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Cloud
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IRequestFactory
    {
        event Action OnWebSocketConnect;
        event Action OnWebSocketDisconnect;
        event Action OnWebSocketConnectionError;

        bool IsReady { get; }

        void ForceCreateWebSocket();

        void AddPushCallback(string requestId, Action<string> onPushCallback);
        void RemovePushCallback(string requestPath, Action<string> onPushCallback);

        void SetRequestThrottling(TimeSpan requestInterval);
        TimeSpan GetRequestCooldown(string request, string method);

        Task<string> SendRequestAsync(string basePath, string method, string body,
            Dictionary<string, string> headers, string requestName, string sessionToken);
        Task<string> SendRequestAsync(string basePath, string pathParams, string method, string body,
            Dictionary<string, string> headers, string requestName, string sessionToken);
        Task<string> SendCustomRequestAsync(string endpoint,
            string path, string method, string body);

        void SendRequest(string basePath, string method, string body,
            Dictionary<string, string> headers, string requestName, string sessionToken,
            Action<RequestResponse<string>> callback);
        void SendRequest(string basePath, string pathParams, string method, string body,
            Dictionary<string, string> headers, string requestName, string sessionToken,
            Action<RequestResponse<string>> callback);
        void SendCustomRequest(string endpoint,
            string path, string method, string body, Action<RequestResponse<string>> callback);
    }
}
