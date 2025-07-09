// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Tests
{
    using Common;
    using Moq;
    using System;
    using Transport;

    public class BriskServicesMocks
    {
        public Mock<IStopwatch> SendTimer = new Mock<IStopwatch>();
        public Mock<IStopwatch> ConnectionTimer = new Mock<IStopwatch>();
        internal Mock<ITransport> TransportMock = new Mock<ITransport>();

        public BriskServicesMocks(bool configureStopwatchReset = true)
        {
            if (configureStopwatchReset)
            {
                ConfigureStopwatchMockReset(SendTimer);
                ConfigureStopwatchMockReset(ConnectionTimer);
            }
        }

        public BriskServices GetServices()
        {
            return new BriskServices()
            {
                SendTimerProvider = () => SendTimer.Object,
                ConnectionTimerProvider = () => ConnectionTimer.Object,
                TransportFactory = (_) => TransportMock.Object,
                KeepAliveProvider = () => false,
            };
        }

        private static void ConfigureStopwatchMockReset(Mock<IStopwatch> stopwatchMock)
        {
            stopwatchMock.Setup(m => m.Reset())
                .Callback(() =>
                {
                    stopwatchMock.Setup(m => m.Elapsed).Returns(TimeSpan.Zero);
                    stopwatchMock.Setup(m => m.ElapsedMilliseconds).Returns(0);
                });

            stopwatchMock.Setup(m => m.Restart())
                .Callback(() =>
                {
                    stopwatchMock.Setup(m => m.Elapsed).Returns(TimeSpan.Zero);
                    stopwatchMock.Setup(m => m.ElapsedMilliseconds).Returns(0);
                });
        }
    }
}
