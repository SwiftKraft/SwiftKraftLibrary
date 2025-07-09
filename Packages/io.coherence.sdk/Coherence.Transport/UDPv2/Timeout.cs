// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Transport
{
    using System;
    using System.Diagnostics;
    using Common;
    using Connection;
    using Debugging;

    public class Timeout
    {
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly Action<ConnectionTimeoutException> onTimeout;

        private TimeSpan? timeout;
        private DateTime lastResetTime;

        public Timeout(
            IDateTimeProvider dateTimeProvider,
            Action<ConnectionTimeoutException> onTimeout)
        {
            this.dateTimeProvider = dateTimeProvider;
            this.onTimeout = onTimeout;
            this.lastResetTime = this.dateTimeProvider.UtcNow;
        }

        public void SetTimeout(in TimeSpan newTimeout)
        {
            this.timeout = newTimeout;
        }

        public void Check(bool reset)
        {
            DbgAssert.That(timeout.HasValue, "Timeout not set");

            if (reset)
            {
                lastResetTime = dateTimeProvider.UtcNow;
                return;
            }

            if (dateTimeProvider.UtcNow - lastResetTime < timeout)
            {
                return;
            }

            var exception = new ConnectionTimeoutException(
                timeout.Value,
                "Connection timeout: no valid message received in time");
            onTimeout?.Invoke(exception);
        }
    }
}
