namespace Coherence.Toolkit.Relay
{
    using Brook;
    using Brook.Octet;
    using Common;
    using Connection;
    using Log;
    using Stats;
    using Transport;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using UnityEngine;
    using Logger = Log.Logger;

    internal class RelayConnectionHolder
    {
        public event Action<IRelayConnection, ConnectionException> OnError;

        private readonly Logger logger;
        private readonly ITransport replicationServerTransport;
        private readonly IRelayConnection relayConnection;

        private readonly List<(IInOctetStream, IPEndPoint)> serverToClientBuffer = new List<(IInOctetStream, IPEndPoint)>();
        private readonly List<ArraySegment<byte>> clientToServerBuffer = new List<ArraySegment<byte>>();

        internal RelayConnectionHolder(EndpointData endpointData, IRelayConnection relayConnection, Logger logger, ITransport transport = null)
        {
            replicationServerTransport = transport ?? new UdpTransport(new StubStats(), logger);
            replicationServerTransport.OnError += HandleConnectionError;
            replicationServerTransport.Open(endpointData, ConnectionSettings.Default);

            this.relayConnection = relayConnection;
            this.logger = logger;

            relayConnection.OnConnectionOpened();
        }

        internal void Close()
        {
            // Drain the connection and pass it all to the RS
            Update();

            replicationServerTransport.Close();
            replicationServerTransport.OnError -= HandleConnectionError;

            try
            {
                relayConnection.OnConnectionClosed();
            }
            catch (Exception e)
            {
                logger.Error(Error.ToolkitRelayCloseException, e.ToString());
            }
        }

        internal void Update()
        {
            if (replicationServerTransport.State == TransportState.Closed)
            {
                logger.Error(Error.ToolkitRelayUpdateFailed);
                return;
            }

            try
            {
                clientToServerBuffer.Clear();
                relayConnection.ReceiveMessagesFromClient(clientToServerBuffer);

                foreach (var packet in clientToServerBuffer)
                {
                    RelayToServer(packet);
                }

                serverToClientBuffer.Clear();
                replicationServerTransport.Receive(serverToClientBuffer);

                foreach (var (packet, _) in serverToClientBuffer)
                {
                    relayConnection.SendMessageToClient(packet.GetOffsetBuffer());
                }
            }
            catch (Exception e)
            {
                logger.Error(Error.ToolkitRelayUpdateException, e.ToString());
                HandleConnectionError(new ConnectionException("Update error", e));
            }
        }

        private void RelayToServer(ArraySegment<byte> packet)
        {
            var segmentCoversEntireArray = packet.Offset == 0 && packet.Count == packet.Array.Length;

            // Don't allocate if the entire underlying array is used
            var data = segmentCoversEntireArray ?
                packet.Array :
                packet.ToArray();

            var stream = new OutOctetStream(data.Length + replicationServerTransport.HeaderSize);

            stream.Seek((uint)replicationServerTransport.HeaderSize);
            stream.WriteOctets(data);

            replicationServerTransport.Send(stream);
        }

        private void HandleConnectionError(ConnectionException e)
        {
            OnError?.Invoke(relayConnection, e);
        }
    }
}
