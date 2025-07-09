// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport
{
    using Brook;
    using Common;
    using Connection;
    using System;
    using System.Collections.Generic;
    using System.Net;

    public interface ITransport
    {
        event Action OnOpen;
        event Action<ConnectionException> OnError;

        TransportState State { get; }
        bool IsReliable { get; }
        bool CanSend { get; }
        int HeaderSize { get; }
        string Description { get; }

        void Open(EndpointData endpoint, ConnectionSettings settings);
        void Close();

        void Send(IOutOctetStream data);
        void Receive(List<(IInOctetStream, IPEndPoint)> buffer);
        void PrepareDisconnect();
    }

    public interface IListenTransport : ITransport
    {
        void Listen(EndpointData entpointData, ConnectionSettings settings);
        void SendTo(IOutOctetStream data, IPEndPoint endpoint, SessionID sessionID);
    }
}
