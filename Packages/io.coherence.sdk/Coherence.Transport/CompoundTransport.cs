// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport
{
    using Brook;
    using Common;
    using Connection;
    using Log;
    using System;
    using System.Collections.Generic;
    using System.Net;

    internal class CompoundTransport<TPrimary, TFallback> : ITransport
    where TPrimary : ITransport
    where TFallback : ITransport
    {
        public event Action OnOpen;
        public event Action<ConnectionException> OnError;

        public TransportState State => currentTransport.State;
        public bool IsReliable => currentTransport.IsReliable;
        public bool CanSend => currentTransport.CanSend;
        public int HeaderSize => currentTransport.HeaderSize;
        public string Description => currentTransport.Description + (currentTransport == fallbackTransport ? " (fallback)" : "");

        private static bool useFallBack;

        private ITransport currentTransport;
        private readonly ITransport primaryTransport;
        private readonly ITransport fallbackTransport;
        private readonly Logger logger;

        private bool anyPacketReceived;

        private EndpointData endpoint;
        private ConnectionSettings settings;

        private DateTime? openTime;

        public CompoundTransport(TPrimary primaryTransport, TFallback fallbackTransport, Logger logger)
        {
            this.primaryTransport = primaryTransport;
            this.fallbackTransport = fallbackTransport;
            this.logger = logger.With(GetType());

            // If we used a fallback once for this [primary,fallback] pair then we keep using fallback
            // for all the subsequent connections assuming that the primary doesn't work.
            ConfigureTransportWithFallback();
        }

        public void Open(EndpointData endpoint, ConnectionSettings settings)
        {
            ConfigureTransportWithFallback();

            logger.Debug($"Open {currentTransport.GetType()}");

            this.endpoint = endpoint;
            this.settings = settings;

            openTime = null;

            currentTransport.OnOpen -= HandleOpened;
            currentTransport.OnOpen += HandleOpened;

            currentTransport.OnError -= HandleError;
            currentTransport.OnError += HandleError;

            currentTransport.Open(endpoint, settings);
        }

        public void Close()
        {
            logger.Debug($"Close {currentTransport.GetType()}");

            currentTransport.OnError -= HandleError;
            currentTransport.OnOpen -= HandleOpened;
            currentTransport.Close();
        }

        public void Send(IOutOctetStream data)
        {
            currentTransport.Send(data);
        }

        public void Receive(List<(IInOctetStream, IPEndPoint)> buffer)
        {
            currentTransport.Receive(buffer);
            anyPacketReceived |= buffer.Count > 0;
        }

        public void PrepareDisconnect()
        {
            currentTransport.PrepareDisconnect();
        }

        private void HandleOpened()
        {
            openTime = DateTime.UtcNow;
            OnOpen?.Invoke();
        }

        private void HandleError(ConnectionException exception)
        {
            if (ShouldFallBack(exception))
            {
                useFallBack = true;

                // We retry only if no packet was received so Brisk still thinks we're
                // in the 'opening' state. Otherwise Brisk might have already established
                // connection and would not send the connection request on new transport.
                bool shouldRetryOpening = !anyPacketReceived;
                if (shouldRetryOpening)
                {
                    Close();
                    ConfigureTransportWithFallback();

                    logger.Debug("Connection retry with new transport");

                    Open(endpoint, settings);

                    return;
                }
            }

            OnError?.Invoke(exception);
        }

        private bool ShouldFallBack(Exception exception)
        {
            var timeoutException = exception as ConnectionTimeoutException;
            if (timeoutException == null)
            {
                return false;
            }

            if (currentTransport != primaryTransport)
            {
                return false;
            }

            logger.Debug($"Primary transport timeout: {timeoutException.After}");

            if (anyPacketReceived)
            {
                // We've introduced fallback on timeout due to case where users would have UDP transport
                // blocked by a firewall but only after a few seconds of connection. Thus, fallback
                // only if the connection was brief.
                return openTime.HasValue && DateTime.UtcNow - openTime.Value <= TimeSpan.FromSeconds(5);
            }

            return true;
        }

        private void ConfigureTransportWithFallback()
        {
            if (!useFallBack)
            {
                currentTransport = primaryTransport;
                return;
            }

            if (currentTransport == fallbackTransport)
            {
                return;
            }

            logger.Debug($"Fallback to {fallbackTransport.GetType()}");
            currentTransport = fallbackTransport;
        }
    }
}
