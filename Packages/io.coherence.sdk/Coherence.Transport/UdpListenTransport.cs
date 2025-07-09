// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport
{
    using System;
    using Flux;
    using Brook;
    using Common;
    using Connection;
    using Log;
    using Stats;
    using System.Net;
    using Debugging;

    public class UdpListenTransport : UdpTransport, IListenTransport
    {
        public UdpListenTransport(IStats stats, Logger logger, IDateTimeProvider dateTimeProvider = null) : base(stats, logger, dateTimeProvider)
        {

        }

        public void Listen(EndpointData endpoint, ConnectionSettings settings)
        {
            logger.Debug($"Listening", ("host", endpoint.host), ("port", endpoint.port));

            try
            {
                flux.SetRoomId(endpoint.roomId);
                flux.Listen(endpoint);
            }
            catch (Exception exception)
            {
                RaiseOnError(new ConnectionException("Failed to listen to UDP transport", exception));
                return;
            }

            State = TransportState.Open;
            this.settings = settings;
            lastValidPacketTime = dateTimeProvider.UtcNow;

            RaiseOnOpen();
        }

        public void SendTo(IOutOctetStream stream, IPEndPoint endpoint, SessionID toSession)
        {
            if (dev_blockTraffic)
            {
                return;
            }

            WriteHeaderWithSpaceForRoomID(stream, toSession);
            flux.SendPacketTo(stream.Close(), endpoint);
            stream.ReturnIfPoolable();
        }

        private void WriteHeaderWithSpaceForRoomID(IOutOctetStream stream, SessionID toSession)
        {
            var streamEnd = stream.Position;
            stream.Seek(Flux.roomByteCount);

            SessionID.Write(toSession, stream);

            DbgAssert.ThatFmt(stream.Position == HeaderSize,
                "Header size mismatch, was: {}, expected: {}",
                stream.Position, HeaderSize);

            stream.Seek(streamEnd);
        }

        protected override void CheckForTimeout(bool anyValidPacketReceived)
        {
            // intentionally does not check for timeout, since it's muxed and listening.
        }

        protected override bool HandleSessionID(IInOctetStream stream)
        {
            // the session id is handled in the manager.
            return true;
        }
    }
}
