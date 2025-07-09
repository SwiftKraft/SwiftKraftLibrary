// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Interpolation.Tests
{
    using System;
    using Coherence.SimulationFrame;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools.Utils;
    using Coherence.Tests;

    public class InterpolationTests : CoherenceTest
    {
        public const float Epsilon = 10e-3f;
        private static readonly Vector3EqualityComparer Comparer = new Vector3EqualityComparer(Epsilon);

        private static readonly Sample<Vector3> Sample0 = new Sample<Vector3>(new Vector3(0, 1, 2), false, AbsoluteSimulationFrame.Invalid / InterpolationSettings.SimulationFramesPerSecond, null);
        private static readonly Sample<Vector3> Sample0Stopped = new Sample<Vector3>(new Vector3(0, 1, 2), true, AbsoluteSimulationFrame.Invalid / InterpolationSettings.SimulationFramesPerSecond, null);
        private static readonly Sample<Vector3> SampleA = new Sample<Vector3>(new Vector3(1, 2, 3), false, 0, null);
        private static readonly Sample<Vector3> SampleAStopped = new Sample<Vector3>(new Vector3(1, 2, 3), true, 0, null);
        private static readonly Sample<Vector3> SampleB = new Sample<Vector3>(new Vector3(4, 5, 6), false, 1, null);
        private static readonly Sample<Vector3> SampleC = new Sample<Vector3>(new Vector3(10, 20, 30), false, 0, null);
        private static readonly Sample<Vector3> SampleD = new Sample<Vector3>(new Vector3(40, 50, 60), false, 10, null);

        private static readonly Sample<long> SampleALong = new Sample<long>(0, false, 0, null);
        private static readonly Sample<long> SampleBLong = new Sample<long>(long.MaxValue, false, 1, null);
        private static readonly Sample<long> SampleCLong = new Sample<long>(long.MinValue, false, 0, null);
        private static readonly Sample<long> SampleDLong = new Sample<long>(long.MaxValue / 2 + 50, false, 1, null);

        private static readonly Sample<float> SampleAFloat = new Sample<float>(0, false, 0, null);
        private static readonly Sample<float> SampleBFloat = new Sample<float>(float.MaxValue, false, 1, null);
        private static readonly Sample<float> SampleCFloat = new Sample<float>(float.MinValue, false, 0, null);
        private static readonly Sample<float> SampleDFloat = new Sample<float>(float.MaxValue / 2 + 50, false, 1, null);

        private static readonly Sample<double> SampleADouble = new Sample<double>(0, false, 0, null);
        private static readonly Sample<double> SampleBDouble = new Sample<double>(double.MaxValue, false, 1, null);
        private static readonly Sample<double> SampleCDouble = new Sample<double>(double.MinValue, false, 0, null);
        private static readonly Sample<double> SampleDDouble = new Sample<double>(double.MaxValue / 2 + 50, false, 1, null);

        public readonly struct SampleTestData<T>
        {
            public SampleTestData(float time, double sampleRate, T expectedValue, Sample<T>[] samples)
            {
                Time = time;
                ExpectedValue = expectedValue;
                Samples = samples;
                SampleRate = sampleRate;
            }

            public Sample<T>[] Samples { get; }
            public double Time { get; }
            public double SampleRate { get; }
            public T ExpectedValue { get; }

            public override string ToString()
            {
                return $"{nameof(Samples)}: {Samples}, {nameof(Time)}: {Time}, {nameof(SampleRate)}: {SampleRate}, {nameof(ExpectedValue)}: {ExpectedValue}";
            }
        }

        private static SampleTestData<Vector3>[] latestSampleInterpolationTestSource = {
            new SampleTestData<Vector3>(0, 1, default, Array.Empty<Sample<Vector3>>()),
            new SampleTestData<Vector3>(0, 1, Sample0.Value, new []{Sample0}),
            new SampleTestData<Vector3>(0, 1, SampleB.Value, new []{SampleB}),
            new SampleTestData<Vector3>(0, 1, SampleB.Value, new []{SampleA, SampleB}),
            new SampleTestData<Vector3>(0, 1, SampleB.Value, new []{Sample0, SampleB}),
            new SampleTestData<Vector3>(0.49f, 1, SampleB.Value, new []{SampleA, SampleB}),
            new SampleTestData<Vector3>(0.5f, 1, SampleB.Value, new []{SampleA, SampleB}),
            new SampleTestData<Vector3>(1f, 1, SampleB.Value, new []{SampleA, SampleB}),
            new SampleTestData<Vector3>(-1f, 1, SampleB.Value, new []{SampleA, SampleB}),
        };

        [Test]
        [TestCaseSource(nameof(latestSampleInterpolationTestSource))]
        public void TestLatestSampleInterpolation(SampleTestData<Vector3> testData)
        {
            // Arrange
            var interpolator = new BindingInterpolator<Vector3>(InterpolationSettings.Empty, testData.SampleRate);

            // Act
            foreach (var sample in testData.Samples)
            {
                interpolator.AppendSample(sample.Value, sample.Stopped, sample.Frame, sample.Frame);
            }

            // Assert
            var actualPosition = interpolator.GetValueAt(testData.Time);
            Assert.That(actualPosition, Is.EqualTo(testData.ExpectedValue).Using(Comparer));
        }

        private static SampleTestData<Vector3>[] linearInterpolationTestSource = {
            new(0, 1, default, Array.Empty<Sample<Vector3>>()),
            new(0, 1, Sample0.Value, new []{Sample0}),
            new(0, 1, SampleB.Value, new []{SampleB}),
            new(0, 1, SampleA.Value, new []{SampleA, SampleB}),
            new(0, 1, SampleA.Value, new []{Sample0, SampleA}),
            new(0.25f, 1, 0.75f * SampleA.Value + 0.25f * SampleB.Value, new []{SampleA, SampleB}),
            new(0.5f, 1, 0.5f * SampleA.Value + 0.5f * SampleB.Value, new []{SampleA, SampleB}),
            new(0.75f, 1, 0.25f * SampleA.Value + 0.75f * SampleB.Value, new []{SampleA, SampleB}),
            new(1f, 1, SampleB.Value, new []{SampleA, SampleB}),
            new(1f + InterpolationSettings.DefaultMaxOvershootAllowed, 1,
                SampleA.Value + (SampleB.Value - SampleA.Value) * (1f + InterpolationSettings.DefaultMaxOvershootAllowed), new []{SampleA, SampleB}),
            new(2f, 0.1, 0.8f * SampleC.Value + 0.2f * SampleD.Value, new []{SampleC, SampleD}),
            new(-1f, 1, SampleA.Value, new []{SampleA, SampleB}),

            // virtual sample in case we were stopped and received another sample much later
            new(9f, 1, SampleAStopped.Value, new []{ SampleAStopped, SampleD }),
            new(9.2f, 1, SampleAStopped.Value * 0.8f + SampleD.Value * 0.2f, new []{ SampleAStopped, SampleD }),

            // we receive a stale (not stopped) sample while interpolating - the sample should be ignored
            new(2f, 1, SampleA.Value + (SampleB.Value - SampleA.Value) * 2f, new []{ SampleA, SampleB, Sample0 }),

            // we receive a stale (stopped) sample while interpolating - the sample time is set at last + measured interval
            new(1.3f, 1, SampleB.Value + (Sample0Stopped.Value - SampleB.Value) * 0.3f, new []{ SampleA, SampleB, Sample0Stopped})
        };

        [Test]
        [TestCaseSource(nameof(linearInterpolationTestSource))]
        public void TestLinearInterpolation(SampleTestData<Vector3> testData)
        {
            // Arrange
            var interpolationSettings = InterpolationSettings.CreateEmpty();
            interpolationSettings.name = "Testing";
            interpolationSettings.interpolator = new LinearInterpolator();
            interpolationSettings.maxOvershootAllowed = InterpolationSettings.DefaultMaxOvershootAllowed;

            var interpolator = new BindingInterpolator<Vector3>(interpolationSettings, testData.SampleRate);

            // Act
            foreach (var sample in testData.Samples)
            {
                interpolator.AppendSample(sample.Value, sample.Stopped, sample.Frame, sample.Frame);
                interpolator.PerformInterpolation(Vector3.zero, testData.Time + interpolator.TargetDelay); // necessary to set IsStopped
            }

            // Assert
            var actualValue = interpolator.GetValueAt(testData.Time);
            Assert.That(actualValue, Is.EqualTo(testData.ExpectedValue).Using(Comparer));
        }

        private static SampleTestData<long>[] linearInterpolationLongTestSource = {
            new SampleTestData<long>(0, 1, SampleALong.Value, new []{SampleALong, SampleBLong}),
            new SampleTestData<long>(0.5f, 1, (long)Math.Round(SampleALong.Value * 0.5m) + (long)Math.Round(SampleBLong.Value * 0.5m), new []{SampleALong, SampleBLong}),
            new SampleTestData<long>(1f, 1, SampleBLong.Value, new []{SampleALong, SampleBLong}),

            new SampleTestData<long>(0, 1, SampleCLong.Value, new []{SampleCLong, SampleDLong}),
            new SampleTestData<long>(0.5f, 1, (long)Math.Round(SampleCLong.Value * 0.5m + SampleDLong.Value * 0.5m), new []{SampleCLong, SampleDLong}),
            new SampleTestData<long>(1f, 1, SampleDLong.Value, new []{SampleCLong, SampleDLong}),
        };

        [Test]
        [TestCaseSource(nameof(linearInterpolationLongTestSource))]
        public void TestLinearInterpolationLong(SampleTestData<long> testData)
        {
            // Arrange
            var interpolationSettings = InterpolationSettings.CreateEmpty();
            interpolationSettings.name = "Testing";
            interpolationSettings.interpolator = new LinearInterpolator();

            var interpolator = new BindingInterpolator<long>(interpolationSettings, testData.SampleRate);

            // Act
            foreach (var sample in testData.Samples)
            {
                interpolator.AppendSample(sample.Value, sample.Stopped, sample.Frame, sample.Frame);
            }

            // Assert
            var actualValue = interpolator.GetValueAt(testData.Time);
            Assert.That(actualValue, Is.EqualTo(testData.ExpectedValue));
        }

        private static SampleTestData<float>[] linearInterpolationFloatTestSource = {
            new SampleTestData<float>(0, 1, SampleAFloat.Value, new []{ SampleAFloat, SampleBFloat}),
            new SampleTestData<float>(0.5f, 1, SampleAFloat.Value * 0.5f + SampleBFloat.Value * 0.5f, new []{ SampleAFloat, SampleBFloat}),
            new SampleTestData<float>(1f, 1, SampleBFloat.Value, new []{ SampleAFloat, SampleBFloat}),

            new SampleTestData<float>(0, 1, SampleCFloat.Value, new []{ SampleCFloat, SampleDFloat}),
            new SampleTestData<float>(0.5f, 1, SampleCFloat.Value * 0.5f + SampleDFloat.Value * 0.5f, new []{ SampleCFloat, SampleDFloat}),
            new SampleTestData<float>(1f, 1, SampleDFloat.Value, new []{ SampleCFloat, SampleDFloat}),
        };

        [Test]
        [TestCaseSource(nameof(linearInterpolationFloatTestSource))]
        public void TestLinearInterpolationFloat(SampleTestData<float> testData)
        {
            // Arrange
            var interpolationSettings = InterpolationSettings.CreateEmpty();
            interpolationSettings.name = "Testing";
            interpolationSettings.interpolator = new LinearInterpolator();

            var interpolator = new BindingInterpolator<float>(interpolationSettings, testData.SampleRate);

            // Act
            foreach (var sample in testData.Samples)
            {
                interpolator.AppendSample(sample.Value, sample.Stopped, sample.Frame, sample.Frame);
            }

            // Assert
            var actualValue = interpolator.GetValueAt(testData.Time);
            Assert.That(actualValue, Is.EqualTo(testData.ExpectedValue));
        }

        private static SampleTestData<double>[] linearInterpolationDoubleTestSource = {
            new SampleTestData<double>(0, 1, SampleADouble.Value, new []{ SampleADouble, SampleBDouble}),
            new SampleTestData<double>(0.5f, 1, SampleADouble.Value * 0.5 + SampleBDouble.Value * 0.5, new []{ SampleADouble, SampleBDouble}),
            new SampleTestData<double>(1f, 1, SampleBDouble.Value, new []{ SampleADouble, SampleBDouble}),

            new SampleTestData<double>(0, 1, SampleCDouble.Value, new []{ SampleCDouble, SampleDDouble}),
            new SampleTestData<double>(0.5f, 1, SampleCDouble.Value * 0.5 + SampleDDouble.Value * 0.5, new []{ SampleCDouble, SampleDDouble}),
            new SampleTestData<double>(1f, 1, SampleDDouble.Value, new []{ SampleCDouble, SampleDDouble}),
        };

        [Test]
        [TestCaseSource(nameof(linearInterpolationDoubleTestSource))]
        public void TestLinearInterpolationDouble(SampleTestData<double> testData)
        {
            // Arrange
            var interpolationSettings = InterpolationSettings.CreateEmpty();
            interpolationSettings.name = "Testing";
            interpolationSettings.interpolator = new LinearInterpolator();

            var interpolator = new BindingInterpolator<double>(interpolationSettings, testData.SampleRate);

            // Act
            foreach (var sample in testData.Samples)
            {
                interpolator.AppendSample(sample.Value, sample.Stopped, sample.Frame, sample.Frame);
            }

            // Assert
            var actualValue = interpolator.GetValueAt(testData.Time);
            Assert.That(actualValue, Is.EqualTo(testData.ExpectedValue));
        }
    }
}
