// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk
{
    using Log;
    using System;
    using System.Diagnostics;

    public class SendRateCounter
    {
        private static readonly TimeSpan ValidationPeriod = TimeSpan.FromSeconds(3);
        private const double SendRateErrorMargin = 1.05;

        private readonly Logger logger;

        private int sendCount;
        private TimeSpan lastValidationTime;

        public SendRateCounter(Logger logger)
        {
            this.logger = logger.With<SendRateCounter>();
        }

        [Conditional(LogConditionals.Debug)]
        public void Reset()
        {
            lock (this)
            {
                sendCount = 0;
                lastValidationTime = TimeSpan.Zero;
            }
        }

        [Conditional(LogConditionals.Debug)]
        public void Bump(double expectedSendRate, TimeSpan now)
        {
            bool validate = false;
            int count;
            TimeSpan validationDelta;

            lock (this)
            {
                count = sendCount++;

                validationDelta = now - lastValidationTime;
                if (validationDelta > ValidationPeriod)
                {
                    validate = true;
                    sendCount = 0;
                    lastValidationTime = now;
                }
            }

            if (validate)
            {
                double sendRate = count / validationDelta.TotalSeconds;
                if (sendRate > expectedSendRate * SendRateErrorMargin)
                {
                    logger.Error(Error.BriskSendRateExceeded,
                        ("expectedRate", expectedSendRate),
                        ("currentRate", sendRate),
                        ("sendCount", count),
                        ("period", validationDelta.TotalSeconds));
                }
            }
        }
    }
}
