// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core.Tests
{
    using System;
    using Coherence.Core;
    using NUnit.Framework;
    using SimulationFrame;
    using Ping = Common.Ping;
    using Coherence.Tests;

    public class NetworkTimeTests : CoherenceTest
    {
        private const double TimeStep50FPS = 1 / 50d;
        private const double TimeStep60FPS = 1 / 60d;
        private const double TimeStep200FPS = 1 / 200d;

        [TestCase(0)]
        [TestCase(100)]
        [TestCase(1000)]
        public void ClientFrameNotIncreasedWithZeroDeltaTime(long serverFrame)
        {
            var networkTime = new NetworkTime();
            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = serverFrame }, default);

            Assert.That(networkTime.ClientSimulationFrame.Frame, Is.EqualTo(serverFrame), $"{nameof(NetworkTime.ClientSimulationFrame)} should be equal to server frame");
            networkTime.Step(0, false);
            Assert.That(networkTime.ClientSimulationFrame.Frame, Is.EqualTo(serverFrame), $"{nameof(NetworkTime.ClientSimulationFrame)} should still be equal to server frame");
        }

        [TestCase(0)]
        [TestCase(100)]
        [TestCase(1000)]
        public void StepTenFrames(long serverFrame)
        {
            var networkTime = new NetworkTime();
            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = serverFrame }, default);

            var time = 0d;

            for (var i = 1; i <= 10; i++)
            {
                // Client simulation frame should increment by 1 with each call
                time += TimeStep60FPS;
                networkTime.Step(time, false);
                Assert.AreEqual(serverFrame + i, networkTime.ClientSimulationFrame.Frame);
            }
        }

        [Test]
        public void FractionalFramesAt50FPS()
        {
            var networkTime = new NetworkTime { FixedTimeStep = TimeStep60FPS };

            // The client simulation frame will increase by 6 every 5 ticks when the client runs at 50 fps.
            var time = 0d;
            time += TimeStep50FPS;
            networkTime.Step(time, false);
            Assert.AreEqual(1, networkTime.ClientSimulationFrame.Frame);
            time += TimeStep50FPS;
            networkTime.Step(time, false);
            Assert.AreEqual(2, networkTime.ClientSimulationFrame.Frame);
            time += TimeStep50FPS;
            networkTime.Step(time, false);
            Assert.AreEqual(3, networkTime.ClientSimulationFrame.Frame);
            time += TimeStep50FPS;
            networkTime.Step(time, false);
            Assert.AreEqual(4, networkTime.ClientSimulationFrame.Frame);
            time += TimeStep50FPS;
            networkTime.Step(time, false);
            Assert.AreEqual(6, networkTime.ClientSimulationFrame.Frame);    // 5 is skipped
            time += TimeStep50FPS;
            networkTime.Step(time, false);
            Assert.AreEqual(7, networkTime.ClientSimulationFrame.Frame);
            time += TimeStep50FPS;
            networkTime.Step(time, false);
            Assert.AreEqual(8, networkTime.ClientSimulationFrame.Frame);
            time += TimeStep50FPS;
            networkTime.Step(time, false);
            Assert.AreEqual(9, networkTime.ClientSimulationFrame.Frame);
            time += TimeStep50FPS;
            networkTime.Step(time, false);
            Assert.AreEqual(10, networkTime.ClientSimulationFrame.Frame);
            time += TimeStep50FPS;
            networkTime.Step(time, false);
            Assert.AreEqual(12, networkTime.ClientSimulationFrame.Frame);    // 11 is skipped
            time += TimeStep50FPS;
            networkTime.Step(time, false);
            Assert.AreEqual(13, networkTime.ClientSimulationFrame.Frame);
            time += TimeStep50FPS;
            networkTime.Step(time, false);
            Assert.AreEqual(14, networkTime.ClientSimulationFrame.Frame);
            time += TimeStep50FPS;
            networkTime.Step(time, false);
            Assert.AreEqual(15, networkTime.ClientSimulationFrame.Frame);
            time += TimeStep50FPS;
            networkTime.Step(time, false);
            Assert.AreEqual(16, networkTime.ClientSimulationFrame.Frame);
        }

        [TestCase(0, 0, 1)]
        [TestCase(100, 100, 1)]
        [TestCase(1000, 1000, 1)]
        [TestCase(1000, 1000 + 10, 1.1)]
        [TestCase(1000 + 10, 1000, 0.9)]
        [TestCase(1000, 1000 + 100, NetworkTime.maxTimeScale)]
        [TestCase(1000 + 100, 1000, NetworkTime.minTimeScale)]
        public void NetworkScale(long clientFrame, long serverFrame, double expectedTimeScale)
        {
            // Arrange
            var networkTime = new NetworkTime();

            // Act
            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = clientFrame }, default);
            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = serverFrame }, default);

            // Assert
            Assert.That(networkTime.ClientSimulationFrame.Frame, Is.EqualTo(clientFrame));
            Assert.That(networkTime.ServerSimulationFrame.Frame, Is.EqualTo(serverFrame));
            Assert.That(networkTime.TargetTimeScale, Is.EqualTo(expectedTimeScale));
        }

        [TestCase(TimeStep50FPS)]
        [TestCase(TimeStep60FPS)]
        public void CallbackInvokedOncePerTick(double fixedTimeStep)
        {
            var ticks = 0;
            var networkTime = new NetworkTime { FixedTimeStep = fixedTimeStep };
            networkTime.OnFixedNetworkUpdate += () => ticks++;

            var time = 0d;

            // OnFixedNetworkUpdate should be invoked once per tick
            time += fixedTimeStep;
            networkTime.Step(time, false);
            Assert.AreEqual(1, ticks);
            time += fixedTimeStep;
            networkTime.Step(time, false);
            Assert.AreEqual(2, ticks);
            time += fixedTimeStep;
            networkTime.Step(time, false);
            Assert.AreEqual(3, ticks);
        }

        [TestCase(TimeStep50FPS)]
        [TestCase(TimeStep60FPS)]
        public void MultiClientModeInSync(double fixedTimeStep)
        {
            var ticks = 0;
            var networkTime = new NetworkTime
            {
                FixedTimeStep = fixedTimeStep,
                MultiClientMode = true
            };
            networkTime.OnFixedNetworkUpdate += () => ticks++;

            // Step one frame, ensure client and server are in sync
            networkTime.Step(fixedTimeStep, false);
            Assert.AreEqual(1, ticks);
            Assert.AreEqual(1, networkTime.NetworkTimeScale);
            Assert.AreEqual(1, networkTime.ClientSimulationFrame.Frame);
        }

        [TestCase(TimeStep60FPS)]
        public void MultiClientModeScaleMax(double fixedTimeStep)
        {
            var ticks = 0;
            var time = 0d;
            var networkTime = new NetworkTime
            {
                FixedTimeStep = fixedTimeStep,
                MultiClientMode = true,
                SmoothTimeScaleChange = false
            };
            networkTime.OnFixedNetworkUpdate += () => ticks++;

            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = 0 }, default);
            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = 100 }, default);

            // Each tick will increase client simulation frame by 150%
            Assert.That(networkTime.TargetTimeScale, Is.EqualTo(NetworkTime.maxTimeScale), $"{nameof(networkTime.TargetTimeScale)} should be 150%.");

            // Frame 1
            time += fixedTimeStep;
            networkTime.Step(time, false);
            Assert.AreEqual(1, ticks);
            Assert.AreEqual(1, networkTime.ClientSimulationFrame.Frame);

            // Frame 3 (Frame 2 is skipped due to 150% NetworkTimeScale)
            time += fixedTimeStep;
            networkTime.Step(time, false);
            Assert.AreEqual(3, ticks);
            Assert.AreEqual(3, networkTime.ClientSimulationFrame.Frame);

            // Frame 4
            time += fixedTimeStep;
            networkTime.Step(time, false);
            Assert.AreEqual(4, ticks);
            Assert.AreEqual(4, networkTime.ClientSimulationFrame.Frame);

            // Frame 6 (Frame 5 is skipped due to 150% NetworkTimeScale)
            time += fixedTimeStep;
            networkTime.Step(time, false);
            Assert.AreEqual(6, ticks);
            Assert.AreEqual(6, networkTime.ClientSimulationFrame.Frame);

            // Frame 7
            time += fixedTimeStep;
            networkTime.Step(time, false);
            Assert.AreEqual(7, ticks);
            Assert.AreEqual(7, networkTime.ClientSimulationFrame.Frame);
        }

        [TestCase(TimeStep50FPS)]
        public void MultiClientMode50FPS(double fixedTimeStep)
        {
            var ticks = 0;
            var time = 0d;
            var scaledTime = 0d;
            var networkTime = new NetworkTime
            {
                FixedTimeStep = fixedTimeStep,
                MultiClientMode = true,
                SmoothTimeScaleChange = false
            };
            networkTime.OnFixedNetworkUpdate += () => ticks++;

            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = 0 }, default);
            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = 100 }, default);
            networkTime.UpdateTimeScale(0);

            for (var i = 1; i <= 10; i++)
            {
                // Game time is scaled by NetworkTimeSync when calling NetworkTime.FixedUpdate
                scaledTime += fixedTimeStep * networkTime.NetworkTimeScale;
                time += fixedTimeStep;
                networkTime.Step(time, false);

                // Current simulation frame is defined as elapsed game time in seconds multiplied by 60
                Assert.AreEqual((long)Math.Round(scaledTime * 60, 6), networkTime.ClientSimulationFrame.Frame);
                // Client physics runs at 50 fps so it will be invoked at 5/6 the rate of simulation frames
                Assert.AreEqual((long)Math.Round(scaledTime * 50, 6), ticks);
                // NetworkTimeScale should be maximum of 150% because the server frame is very far in the future
                Assert.AreEqual(NetworkTime.maxTimeScale, networkTime.NetworkTimeScale);
            }
        }

        [TestCase(TimeStep60FPS, TimeStep200FPS)]
        [TestCase(TimeStep60FPS, TimeStep50FPS)]
        public void HighRenderSpeed(double fixedTimeStep, double renderTimeStep)
        {
            var ticks = 0;
            var networkTime = new NetworkTime
            {
                FixedTimeStep = fixedTimeStep,
            };
            networkTime.OnFixedNetworkUpdate += () => ticks++;

            for (var i = 1; i <= 10; i++)
            {
                // Incrementing time by the fixedTimeStep should should invoke the callback once
                var time = i * fixedTimeStep;
                networkTime.Step(time, false);
                Assert.AreEqual((long)(i * fixedTimeStep / TimeStep60FPS), networkTime.ClientSimulationFrame.Frame);
                Assert.AreEqual(i, ticks);

                time += renderTimeStep;
                while (time < (i + 1) * fixedTimeStep)
                {
                    // Subsequent render frames should not invoke the callback
                    networkTime.Step(time, false);
                    Assert.AreEqual(i, ticks);
                    time += renderTimeStep;
                }
            }
        }

        [TestCase(0, 10, 1.10)]
        [TestCase(150, 100, 0.50)]
        public void UpdatesTimeScaleOnlyIfServerFrameHasChanged(long clientFrame, long serverFrame, double expectedTimeScale)
        {
            // Arrange
            var networkTime = new NetworkTime
            {
                FixedTimeStep = TimeStep60FPS
            };

            // Act
            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = clientFrame }, default);
            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = serverFrame }, default);

            // Assert
            Assert.That(networkTime.TargetTimeScale, Is.EqualTo(expectedTimeScale), $"Expected no change in {nameof(NetworkTime.TargetTimeScale)} due to the same server frame");
            networkTime.Step(0.01f, false);
            Assert.That(networkTime.TargetTimeScale, Is.EqualTo(expectedTimeScale), $"Expected no change in {nameof(NetworkTime.TargetTimeScale)} due to the same server frame");

            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = serverFrame + 1 }, default);
            networkTime.UpdateTimeScale(0.01f);
            Assert.That(networkTime.TargetTimeScale, Is.Not.EqualTo(expectedTimeScale), $"Expected change in {nameof(NetworkTime.TargetTimeScale)} due to a different server frame");
        }

        [Test]
        public void ReturnsConstantScaleOnUnstablePing()
        {
            // Arrange
            var networkTime = new NetworkTime
            {
                AccountForPing = true
            };

            // Act
            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = 0 }, new Ping { IsStable = false });
            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = 2 }, new Ping { IsStable = false });

            // Assert
            Assert.That(networkTime.TargetTimeScale, Is.EqualTo(1f), $"Expected constant {nameof(NetworkTime.TargetTimeScale)} due to unstable ping");
        }

        [TestCase(0, 1.00f)]
        [TestCase(10, 1.10f)]
        [TestCase(20, 1.20f)]
        [TestCase(50, 1.50f)]
        [TestCase(100, NetworkTime.maxTimeScale)]
        public void AdjustsTimeScaleByPing(int pingInFrames, double expectedTimeScale)
        {
            // Arrange
            var networkTime = new NetworkTime
            {
                AccountForPing = true
            };

            // Act
            networkTime.SetServerSimulationFrame(
                new AbsoluteSimulationFrame { Frame = 1000 },
                new Ping
                {
                    IsStable = true,
                    LatestLatencyMs = (int)Math.Round(NetworkTime.timeStepMs * pingInFrames)
                });

            // Assert - Requires some Within wiggle room due to integer millisecond ping
            Assert.That(networkTime.TargetTimeScale, Is.EqualTo(expectedTimeScale).Within(1).Percent, $"{nameof(NetworkTime.TargetTimeScale)} should be {expectedTimeScale} due to +{pingInFrames} frames from ping");
        }

        [TestCase(20, NetworkTime.maxTimeScale - 0.01f)]
        [TestCase(40, NetworkTime.maxTimeScale - 0.001f)]
        public void TimeScaleSmoothing_WhenCatchingUp_Works(int maxIterations, double minimumEndValue)
        {
            // Arrange
            const float deltaTime = 0.05f;

            var networkTime = new NetworkTime
            {
                FixedTimeStep = TimeStep60FPS,
            };

            // Act
            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = 0 }, default);
            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = 100 }, default);

            var iterations = 0;
            var lastTimeScale = networkTime.NetworkTimeScale;

            // Assert
            while (networkTime.NetworkTimeScale < NetworkTime.maxTimeScale && ++iterations <= maxIterations)
            {
                networkTime.UpdateTimeScale(deltaTime);
                Assert.That(networkTime.NetworkTimeScale, Is.GreaterThan(lastTimeScale));
                lastTimeScale = networkTime.NetworkTimeScale;
            }

            Assert.That(networkTime.NetworkTimeScale, Is.GreaterThanOrEqualTo(minimumEndValue));
        }

        [TestCase(20, NetworkTime.minTimeScale + 0.01d)]
        [TestCase(40, NetworkTime.minTimeScale + 0.001d)]
        public void TimeScaleSmoothing_WhenSlowingDown_Works(int maxIterations, double maximumEndValue)
        {
            // Arrange
            const float deltaTime = 0.05f;

            var networkTime = new NetworkTime
            {
                FixedTimeStep = TimeStep60FPS,
            };

            // Trigger time scale recalculation
            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = 1000 }, default);
            networkTime.SetServerSimulationFrame(new AbsoluteSimulationFrame { Frame = 1000 - 100 }, default);

            var iterations = 0;
            var lastTimeScale = networkTime.NetworkTimeScale;

            // Act
            while (networkTime.NetworkTimeScale < NetworkTime.maxTimeScale && ++iterations <= maxIterations)
            {
                networkTime.UpdateTimeScale(deltaTime);
                Assert.That(networkTime.NetworkTimeScale, Is.LessThanOrEqualTo(lastTimeScale));
                lastTimeScale = networkTime.NetworkTimeScale;
            }

            // Assert
            Assert.That(networkTime.NetworkTimeScale, Is.LessThanOrEqualTo(maximumEndValue));
        }

        [TestCase(0, 0, 0)]
        [TestCase(5, 0, 5)]
        [TestCase(5, 10, 5)]
        [TestCase(10, 5, 5)]
        [TestCase(10, -1, 10)]
        public void MaximumDeltaTimeWorks(double deltaTime, double maximumDeltaTime, double expectedDeltaTime)
        {
            // Arrange
            var networkTime = new NetworkTime
            {
                MaximumDeltaTime = maximumDeltaTime
            };

            networkTime.Step(deltaTime, false);
            Assert.That(networkTime.TimeAsDouble, Is.EqualTo(expectedDeltaTime), $"{nameof(NetworkTime.TimeAsDouble)} was {networkTime.TimeAsDouble} but was expected to be {expectedDeltaTime}");
        }

        [Test]
        public void StopApplyingServerSimFrame()
        {
            var frame1 = new AbsoluteSimulationFrame { Frame = 1000 };
            var frame2 = new AbsoluteSimulationFrame { Frame = 2000 };

            var networkTime = new NetworkTime();

            // Act - apply
            networkTime.Step(1, stopApplyingServerSimFrame: false);
            networkTime.SetServerSimulationFrame(frame1, new Ping());

            // Assert
            Assert.That(networkTime.ClientSimulationFrame, Is.EqualTo(frame1));

            // Act - don't apply
            networkTime.Step(1, stopApplyingServerSimFrame: true);
            networkTime.SetServerSimulationFrame(frame2, new Ping());

            // Assert
            Assert.That(networkTime.ClientSimulationFrame, Is.EqualTo(frame1));

            // Act - still don't apply
            networkTime.Step(1, stopApplyingServerSimFrame: true);

            // Assert
            Assert.That(networkTime.ClientSimulationFrame, Is.EqualTo(frame1));

            // Act - apply previous
            networkTime.Step(1, stopApplyingServerSimFrame: false);

            // Assert
            Assert.That(networkTime.ClientSimulationFrame, Is.EqualTo(frame2));
        }

        [Test]
        [Description("Verifies that after Reset() is called, that the old server frame is not kept or reapplied, " +
            "until the new server frame is set.")]
        public void Reset_DoesntKeepOldServerFrame()
        {
            var frame1 = new AbsoluteSimulationFrame { Frame = 1000 };
            var frame2 = new AbsoluteSimulationFrame { Frame = 100 };
            var time1 = 1;
            var time2 = 1.1f;
            var deltaTime = time2 - time1;
            var deltaTimeFrames = (long)(deltaTime * 60);

            // Arrange
            var networkTime = new NetworkTime();
            networkTime.Step(time1, stopApplyingServerSimFrame: false);
            networkTime.SetServerSimulationFrame(frame1, new Ping());

            // Act 1
            networkTime.Reset();

            networkTime.Step(time2, stopApplyingServerSimFrame: false);

            // Assert 1
            Assert.That(networkTime.IsTimeSynced, Is.False, $"{nameof(networkTime.IsTimeSynced)} should be false after reset");
            Assert.That(networkTime.ClientSimulationFrame.Frame, Is.EqualTo(deltaTimeFrames));

            // Act 2
            networkTime.SetServerSimulationFrame(frame2, new Ping());

            // Assert 2
            Assert.That(networkTime.IsTimeSynced, Is.True, $"{nameof(networkTime.IsTimeSynced)} should be true after setting server frame");
            Assert.That(networkTime.ClientSimulationFrame.Frame, Is.EqualTo(frame2.Frame), $"{nameof(networkTime.ClientSimulationFrame)} should be equal to server frame");
            Assert.That(networkTime.ServerSimulationFrame.Frame, Is.EqualTo(frame2.Frame), $"{nameof(networkTime.ServerSimulationFrame)} should be equal to server frame");
            Assert.That(networkTime.TargetTimeScale, Is.EqualTo(1f), $"{nameof(networkTime.TargetTimeScale)} should be 1 after setting server frame");
        }
    }
}
