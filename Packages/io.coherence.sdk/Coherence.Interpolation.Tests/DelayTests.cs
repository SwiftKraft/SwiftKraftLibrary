// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Interpolation.Tests
{
    using Coherence.SimulationFrame;
    using NUnit.Framework;
    using Coherence.Tests;

    public class InterpolationDelayTests : CoherenceTest
    {
        public readonly struct DelayTestData
        {
            public readonly float SampleRate;
            public readonly Interpolator Interpolator;
            public readonly float NetworkLatencyFactor;
            public readonly float AdditionalLatency;
            public readonly double ExpectedDelay;
            public readonly (long sampleFrame, long localFrame)[] SampleTimes;

            public DelayTestData(float sampleRate, Interpolator interpolator, float networkLatencyFactor, float additionalLatency, double expectedDelay, params (long sampleFrame, long localFrame)[] sampleTimes)
            {
                SampleRate = sampleRate;
                Interpolator = interpolator;
                NetworkLatencyFactor = networkLatencyFactor;
                AdditionalLatency = additionalLatency;
                ExpectedDelay = expectedDelay;
                SampleTimes = sampleTimes;
            }
        }


        private static DelayTestData[] delayTestSource = {
            // Sample Delay = 1/sampleRate + 1 frame
            new DelayTestData(0.5f, new LinearInterpolator(), 1, 0, 1/0.5f + 1/60f),
            new DelayTestData(1, new LinearInterpolator(), 1, 0, 1/1 + 1/60f),
            new DelayTestData(60, new LinearInterpolator(), 1, 0, 1/60f + 1/60f),

            // Ignore first sample, delay = expectedSampleRate(1/60) + 1 frame
            new DelayTestData(60, new LinearInterpolator(), 1, 0, 1/60f + 1/60f,
                (100, 200)),

            // Delay = measuredSampleRate (second largest sample interval) + networkLatency + 1 frame
            new DelayTestData(15, new LinearInterpolator(), 1, 0, 5/60f + 100/60f + 1/60f,
                (100, 200), (105, 205), (115, 215)),

            // Low send rate, Delay = sampleRate + networkLatency + 1 frame
            new DelayTestData(60, new LinearInterpolator(), 1, 0, 10/60f + 100/60f + 1/60f,
                (100, 200), (110, 210)),
            new DelayTestData(60, new LinearInterpolator(), 1, 0, 50/60f + 100/60f + 1/60f,
                (100, 200), (150, 250)),

            // SplineInterpolation incurs double Sample Delay
            // Delay = 2 * measuredSampleRate + networkLatency + 1 frame
            new DelayTestData(15, new SplineInterpolator(), 1, 0, 2*10/60f + 100/60f + 1/60f,
                (100, 200), (110, 210)),

            // NetworkLatency is scaled with networkLatencyDelayFactor
            // Delay = measuredSampleRate + networkLatency * scale + 1 frame
            new DelayTestData(15, new LinearInterpolator(), 3, 0, 10/60f + 3*100/60f + 1/60f,
                (100, 200), (110, 210)),

            // AdditionalLatency is added
            // Delay = measuredSampleRate + networkLatency * scale + additionalLatency(seconds) + 1 frame
            new DelayTestData(15, new LinearInterpolator(), 3, 2f, 10/60f + 3*100/60f + 2f + 1/60f,
                (100, 200), (110, 210)),

            // Negative NetworkLatency is clamped to zero
            // Delay = measuredSampleRate + networkLatency + 1 frame
            new DelayTestData(15, new LinearInterpolator(), 1, 0, 10/60f + 0 + 1/60f,
                (100, 95), (110, 105)),

            // Latency is measured by taking the average (without the largest)
            // Delay = measuredSampleRate + networkLatency + 1 frame
            new DelayTestData(15, new LinearInterpolator(), 1, 0, 10/60f + (105 + 102)/2f/60f + 1/60f,
                (99, 99), (100, 205), (110, 220), (120, 222)),

            // Ignore network latency if two packets have same simFrame -> stale
            // Delay = measuredSampleRate + networkLatency + 1 frame
            new DelayTestData(15, new LinearInterpolator(), 1, 0, 10/60f + 100/60f + 1/60f,
                (100, 200), (110, 210), (110, 220)),

            // Ignore network latency if packet is marked as stale
            // Delay = sampleRate + 1 frame
            new DelayTestData(15, new LinearInterpolator(), 1, 0, 1/15f + 1/60f,
                (AbsoluteSimulationFrame.Invalid, 200), (AbsoluteSimulationFrame.Invalid, 210))
        };

        [TestCaseSource(nameof(delayTestSource))]
        public void TestInterpolationDelay(DelayTestData testData)
        {
            // Arrange
            var interpolationSettings = InterpolationSettings.CreateEmpty();
            interpolationSettings.name = "Testing";
            interpolationSettings.interpolator = testData.Interpolator;
            interpolationSettings.latencySettings.networkLatencyFactor = testData.NetworkLatencyFactor;
            interpolationSettings.latencySettings.additionalLatency = testData.AdditionalLatency;

            var interpolator = new BindingInterpolator<int>(interpolationSettings, testData.SampleRate);

            // Act
            foreach (var (sampleFrame, localFrame) in testData.SampleTimes)
            {
                interpolator.AppendSample(0, false, sampleFrame, localFrame);
            }

            // Assert
            Assert.That(interpolator.TargetDelay, Is.EqualTo(testData.ExpectedDelay).Within(InterpolationTests.Epsilon));
        }

        [Test]
        [Description("Tests that Delay snaps to TargetDelay when a sample is received and we are stopped.")]
        public void TestDelaySnapsToTargetDelayIfStopped()
        {
            // Arrange
            var interpolationSettings = InterpolationSettings.CreateEmpty();
            interpolationSettings.name = "Testing";
            interpolationSettings.latencySettings.networkLatencyFactor = 1;
            interpolationSettings.interpolator = new LinearInterpolator();

            var interpolator = new BindingInterpolator<int>(interpolationSettings, 1);

            // TargetDelay = 1/SampleRate + networkLatency ((300-160)/60f) + 1 frame
            var expectedDelay = 1f + 140 / 60f + 1 / 60f;

            interpolator.AppendSample(0, true, 100, 200);
            interpolator.PerformInterpolation(0, double.MaxValue); // necessary to set IsStopped

            // Act
            interpolator.AppendSample(0, false, 160, 300);

            // Assert
            Assert.That(interpolator.Delay, Is.EqualTo(expectedDelay).Within(InterpolationTests.Epsilon));
        }
    }
}
