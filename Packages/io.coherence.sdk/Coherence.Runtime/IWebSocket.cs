// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime
{
    using System;
    using System.Threading.Tasks;
    using Log;

    public interface IWebSocket
    {
        bool PingWebSocket { get; }

        bool IsConnected();
        void OpenSocket(string endpoint, Action onConnect, Action onDisconnect,
            Action<string> onReceive, Action<Error, string> onError, Action<int, string, Error, string> onSendFail);
        void Send(int requestCounter, string requestId, string message);
        void Update();
        Task CloseAsync();
    }
}
