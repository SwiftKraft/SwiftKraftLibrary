// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Interpolation.Tests
{
    using System;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools.Utils;
    using Coherence.Tests;

    public class OvershootingTests : CoherenceTest
    {
        public const float Epsilon = 10e-3f;
        private static readonly Vector3EqualityComparer Comparer = new Vector3EqualityComparer(Epsilon);

        private static readonly Sample<Vector3> SampleA = new Sample<Vector3>(new Vector3(1, 2, 3), false, 0, null);
        private static readonly Sample<Vector3> SampleB = new Sample<Vector3>(new Vector3(4, 5, 6), false, 1, null);
        private static readonly Sample<Vector3> SampleBStopped = new Sample<Vector3>(new Vector3(4, 5, 6), true, 2, null);
        private static readonly Sample<Vector3> NewSampleStoppedEarly = new Sample<Vector3>(new Vector3(7, 8, 9), true, 1.5, null);
        private static readonly Sample<Vector3> NewSampleStoppedOnTime = new Sample<Vector3>(new Vector3(7, 8, 9), true, 2, null);
        private static readonly Sample<Vector3> NewSampleStoppedLate = new Sample<Vector3>(new Vector3(7, 8, 9), true, 2.25, null);
        private static readonly Sample<Vector3> SampleD = new Sample<Vector3>(new Vector3(4.1f, 5.1f, 6.1f), true, 2, null);

        public readonly struct SampleTestData
        {
            public SampleTestData(float time, double sampleRate, Vector3 expectedPosition, Sample<Vector3>[] samples)
            {
                Time = time;
                ExpectedPosition = expectedPosition;
                Samples = samples;
                SampleRate = sampleRate;
            }

            public Sample<Vector3>[] Samples { get; }
            public double Time { get; }
            public double SampleRate { get; }
            public Vector3? ExpectedPosition { get; }

            public override string ToString()
            {
                return $"{nameof(Samples)}: {Samples}, {nameof(Time)}: {Time}, {nameof(SampleRate)}: {SampleRate}, " +
                    $"{nameof(ExpectedPosition)}: {ExpectedPosition}";
            }
        }

        private static SampleTestData[] testSource = {
            // Small overshooting
            new SampleTestData(1.1f, 1, SampleA.Value + (SampleB.Value - SampleA.Value) * 1.1f, new []{SampleA, SampleB}),
            new SampleTestData(2.1f, 1, NewSampleStoppedEarly.Value, new []{SampleA, SampleB, NewSampleStoppedEarly}),
            new SampleTestData(2.1f, 1, NewSampleStoppedOnTime.Value, new []{SampleA, SampleB, NewSampleStoppedOnTime}),

            // Big overshooting
            new SampleTestData(2.5f, 1, SampleA.Value + (SampleB.Value - SampleA.Value) * 2.5f, new []{SampleA, SampleB}),
            new SampleTestData(2.5f, 1, NewSampleStoppedEarly.Value, new []{SampleA, SampleB, NewSampleStoppedEarly}),
            new SampleTestData(2.5f, 1, NewSampleStoppedOnTime.Value, new []{SampleA, SampleB, NewSampleStoppedOnTime}),
            new SampleTestData(2.5f, 1, NewSampleStoppedLate.Value, new []{SampleA, SampleB, NewSampleStoppedLate}),

            // Mid-overshoot
            new SampleTestData(1.25f, 1, SampleB.Value + (SampleB.Value - SampleA.Value) * 0.25f, new []{SampleA, SampleB}),
            new SampleTestData(1.25f, 1, SampleB.Value + (NewSampleStoppedOnTime.Value - SampleB.Value) * 0.25f, new []{SampleA, SampleB, NewSampleStoppedOnTime}), // new sample arrived mid overshoot
            new SampleTestData(1.25f, 1, SampleBStopped.Value, new []{SampleA, SampleB, SampleBStopped}), // repeat sample with stop mid overshoot

            // Mid-overshoot retraction - pops to the last sample?
            new SampleTestData(2f, 1, SampleA.Value + (SampleB.Value - SampleA.Value) * 2f, new []{SampleA, SampleB}),
            new SampleTestData(2.26f, 1, NewSampleStoppedLate.Value, new []{SampleA, SampleB, NewSampleStoppedLate}),
        };

        [Test]
        [TestCaseSource(nameof(testSource))]
        public void TestOvershooting(SampleTestData testData)
        {
            // Arrange
            var interpolator = new BindingInterpolator<Vector3>(InterpolationSettings.CreateDefault(), testData.SampleRate);

            // Act
            foreach (var sample in testData.Samples)
            {
                interpolator.AppendSample(sample.Value, sample.Stopped, sample.Frame, sample.Frame);
            }

            // Assert
            var actualPosition = interpolator.GetValueAt(testData.Time);
            Assert.That(actualPosition, Is.EqualTo(testData.ExpectedPosition).Using(Comparer));
        }

        [Test]
        public void TestOvershootRetraction()
        {
            var interpolator = new BindingInterpolator<Vector3>(InterpolationSettings.CreateDefault(), 1);

            interpolator.AppendSample(SampleA.Value, SampleA.Stopped, SampleA.Frame, SampleA.Frame);
            interpolator.AppendSample(SampleB.Value, SampleB.Stopped, SampleB.Frame, SampleB.Frame);

            var actualPosition = interpolator.PerformInterpolation(Vector3.zero, 1.2f + InterpolationSettings.DefaultMaxOvershootAllowed + interpolator.TargetDelay);
            Assert.That(actualPosition, Is.EqualTo(SampleB.Value).Using(Comparer));
        }

        [Test]
        [Description("Verifies that when we receive a future sample in the middle of an overshoot," +
            "that the interpolation doesn't snap to some other value but actually interpolates from the current.")]
        public void TestGettingSampleDuringOvershoot()
        {
            var settings = InterpolationSettings.CreateDefault();
            settings.smoothing = SmoothingSettings.Empty;
            var interpolator = new BindingInterpolator<Vector3>(settings, 1);

            // Append two samples, at time 0 and at time 1
            interpolator.AppendSample(SampleA.Value, SampleA.Stopped, SampleA.Frame, SampleA.Frame);
            interpolator.AppendSample(SampleB.Value, SampleB.Stopped, SampleB.Frame, SampleB.Frame);

            // Overshoot by 0.1f
            var overshootTime = 2.1f;
            var result1 = interpolator.PerformInterpolation(Vector3.zero, overshootTime + interpolator.TargetDelay);
            Assert.That(result1, Is.EqualTo(SampleA.Value + (SampleB.Value - SampleA.Value) * overshootTime).Using(Comparer));

            // Append another sample at time 2.25 (in future relative to current time (2.1))
            interpolator.AppendSample(NewSampleStoppedLate.Value, NewSampleStoppedLate.Stopped, NewSampleStoppedLate.Frame, NewSampleStoppedLate.Frame);

            // Assert that interpolating at the same time (2.1) returns the same value as before
            var result2 = interpolator.PerformInterpolation(Vector3.zero, overshootTime + interpolator.TargetDelay);
            Assert.That(result2, Is.EqualTo(result1).Using(Comparer));

            // Assert that interpolation continued off the previous result (result2) and not from the SampleB
            var nextTime = 2.2f;
            var t = (nextTime - overshootTime) / (interpolator.MeasuredSampleInterval);
            var result3 = interpolator.PerformInterpolation(Vector3.zero, nextTime + interpolator.TargetDelay);
            Assert.That(result3, Is.EqualTo(result2 + (NewSampleStoppedLate.Value - result2) * (float)t).Using(Comparer));
        }

        [Test]
        [Description("Verifies that when we receive a sample, while not overshooting," +
            "that no virtual samples are created.")]
        public void TestVirtual_WhenNotOvershooting_ShouldNotCreate()
        {
            var settings = InterpolationSettings.CreateDefault();
            settings.smoothing = SmoothingSettings.Empty;
            var interpolator = new BindingInterpolator<Vector3>(settings, 1);

            // Append two samples, at time 0 and at time 1
            interpolator.AppendSample(SampleA.Value, SampleA.Stopped, SampleA.Frame, SampleA.Frame);
            interpolator.AppendSample(SampleB.Value, SampleB.Stopped, SampleB.Frame, SampleB.Frame);

            // Interpolate at middle of the two samples
            var result1 = interpolator.PerformInterpolation(Vector3.zero, 0.5f + interpolator.TargetDelay);
            Assert.That(result1, Is.EqualTo(SampleA.Value + (SampleB.Value - SampleA.Value) * 0.5f).Using(Comparer));

            // Assert there are no virtual samples
            Assert.That(interpolator.Buffer.VirtualSamples.HasValue, Is.False);
        }

        [Test]
        [Description("Verifies that when we receive a sample, while overshooting," +
            "that virtual samples are created.")]
        public void TestVirtual_WhenOvershooting_ShouldCreate()
        {
            var sampleC = new Sample<Vector3>(new Vector3(7, 8, 9), false, 2, null);

            var settings = InterpolationSettings.CreateDefault();
            settings.smoothing = SmoothingSettings.Empty;
            var interpolator = new BindingInterpolator<Vector3>(settings, 1);

            // Append two samples, at time 0 and at time 1
            interpolator.AppendSample(SampleA.Value, SampleA.Stopped, SampleA.Frame, SampleA.Frame);
            interpolator.AppendSample(SampleB.Value, SampleB.Stopped, SampleB.Frame, SampleB.Frame);

            // Overshoot by 0.1f
            var overshootTime = 2.1f;
            var overshootExpectedValue = SampleA.Value + (SampleB.Value - SampleA.Value) * overshootTime;
            var result1 = interpolator.PerformInterpolation(Vector3.zero, overshootTime + interpolator.TargetDelay);
            Assert.That(result1, Is.EqualTo(overshootExpectedValue).Using(Comparer));

            // Append another sample, at time 2
            interpolator.AppendSample(sampleC.Value, sampleC.Stopped, sampleC.Frame, sampleC.Frame);

            // Assert there are virtual samples
            Assert.That(interpolator.Buffer.VirtualSamples.HasValue, Is.True);

            // Assert virtual samples are correct
            var (virtualFirst, virtualSecond) = interpolator.Buffer.VirtualSamples.Value;

            Assert.That(virtualFirst.Value, Is.EqualTo(overshootExpectedValue).Using(Comparer));
            Assert.That(virtualFirst.Time, Is.EqualTo(overshootTime).Within(Epsilon));

            var overshotTimeAmount = overshootTime - SampleB.Time;
            var secondVirtualSampleTime = (float)(sampleC.Time + overshotTimeAmount);
            var secondVirtualSampleT = (float)((secondVirtualSampleTime - SampleB.Time) / (sampleC.Time - SampleB.Time));
            Assert.That(virtualSecond.Value, Is.EqualTo(SampleB.Value + (sampleC.Value - SampleB.Value) * secondVirtualSampleT).Using(Comparer));
            Assert.That(virtualSecond.Time, Is.EqualTo(secondVirtualSampleTime).Within(Epsilon));
        }

        [Test]
        [Description("Verifies that when we receive a sample, while overshooting, and the new sample is stopped," +
            "that virtual samples are created at the stopped position and current time + sample interval.")]
        public void TestVirtual_WhenOvershooting_AndReceivedStopped_ShouldCreate()
        {
            var sampleC = new Sample<Vector3>(new Vector3(7, 8, 9), true, 2, null);

            var settings = InterpolationSettings.CreateDefault();
            settings.smoothing = SmoothingSettings.Empty;
            var interpolator = new BindingInterpolator<Vector3>(settings, 1);

            // Append two samples, at time 0 and at time 1
            interpolator.AppendSample(SampleA.Value, SampleA.Stopped, SampleA.Frame, SampleA.Frame);
            interpolator.AppendSample(SampleB.Value, SampleB.Stopped, SampleB.Frame, SampleB.Frame);

            // Overshoot by 0.1f
            var overshootTime = 2.1f;
            var overshootExpectedValue = SampleA.Value + (SampleB.Value - SampleA.Value) * overshootTime;
            var result1 = interpolator.PerformInterpolation(Vector3.zero, overshootTime + interpolator.TargetDelay);
            Assert.That(result1, Is.EqualTo(overshootExpectedValue).Using(Comparer));

            // Append another sample, at time 2
            interpolator.AppendSample(sampleC.Value, sampleC.Stopped, sampleC.Frame, sampleC.Frame);

            // Assert there are virtual samples
            Assert.That(interpolator.Buffer.VirtualSamples.HasValue, Is.True);

            // Assert virtual samples are correct
            var (virtualFirst, virtualSecond) = interpolator.Buffer.VirtualSamples.Value;

            Assert.That(virtualFirst.Value, Is.EqualTo(overshootExpectedValue).Using(Comparer));
            Assert.That(virtualFirst.Time, Is.EqualTo(overshootTime).Within(Epsilon));
            
            Assert.That(virtualSecond.Value, Is.EqualTo(sampleC.Value).Using(Comparer));
            Assert.That(virtualSecond.Time, Is.EqualTo(overshootTime + interpolator.MeasuredSampleInterval).Within(Epsilon));
        }

        [Test]
        [Description("Verifies that when we receive a sample on expected time, while interpolating over virtual samples," +
            "that new virtual samples are correctly created.")]
        public void TestVirtual_WhenHasVirtual_ShouldUpdate_OnTime()
        {
            var sampleC = new Sample<Vector3>(new Vector3(7, 8, 9), false, 2, null);
            var sampleD = new Sample<Vector3>(new Vector3(10, 11, 12), false, 3, null);

            var settings = InterpolationSettings.CreateDefault();
            settings.smoothing = SmoothingSettings.Empty;
            var interpolator = new BindingInterpolator<Vector3>(settings, 1);

            // Append two samples, at time 0 and at time 1
            interpolator.AppendSample(SampleA.Value, SampleA.Stopped, SampleA.Frame, SampleA.Frame);
            interpolator.AppendSample(SampleB.Value, SampleB.Stopped, SampleB.Frame, SampleB.Frame);

            // Overshoot by 0.1f
            var overshootTime = 2.1f;
            var overshootExpectedValue = SampleA.Value + (SampleB.Value - SampleA.Value) * overshootTime;
            var result1 = interpolator.PerformInterpolation(Vector3.zero, overshootTime + interpolator.TargetDelay);
            Assert.That(result1, Is.EqualTo(overshootExpectedValue).Using(Comparer));

            // Append another sample, at time 2
            interpolator.AppendSample(sampleC.Value, sampleC.Stopped, sampleC.Frame, sampleC.Frame);

            // Assert there are virtual samples
            Assert.That(interpolator.Buffer.VirtualSamples.HasValue, Is.True);

            // Interpolate over virtuals
            var (virtualFirst, virtualSecond) = interpolator.Buffer.VirtualSamples.Value;
            var overshoot2Time = 2.2f;
            var t = (float)((overshoot2Time - virtualFirst.Time) / (virtualSecond.Time - virtualFirst.Time));
            var overshoot2ExpectedValue = virtualFirst.Value + (virtualSecond.Value - virtualFirst.Value) * t;
            var result2 = interpolator.PerformInterpolation(Vector3.zero, overshoot2Time + interpolator.TargetDelay);
            Assert.That(result2, Is.EqualTo(overshoot2ExpectedValue).Using(Comparer));

            // Append another sample, at time 3
            interpolator.AppendSample(sampleD.Value, sampleD.Stopped, sampleD.Frame, sampleD.Frame);

            // Assert virtual samples are correct
            Assert.That(interpolator.Buffer.VirtualSamples.HasValue, Is.True);
            (virtualFirst, virtualSecond) = interpolator.Buffer.VirtualSamples.Value;
            Assert.That(virtualFirst.Value, Is.EqualTo(overshoot2ExpectedValue).Using(Comparer));
            Assert.That(virtualFirst.Time, Is.EqualTo(overshoot2Time).Within(Epsilon));

            var overshotTimeAmount = overshoot2Time - sampleC.Time;
            var secondVirtualSampleTime = (float)(sampleD.Time + overshotTimeAmount);
            var secondVirtualSampleT = (float)((secondVirtualSampleTime - sampleC.Time) / (sampleD.Time - sampleC.Time));
            Assert.That(virtualSecond.Value, Is.EqualTo(sampleC.Value + (sampleD.Value - sampleC.Value) * secondVirtualSampleT).Using(Comparer));
            Assert.That(virtualSecond.Time, Is.EqualTo(secondVirtualSampleTime).Within(Epsilon));
        }

        [Test]
        [Description("Verifies that when we receive a sample later than expected, while interpolating over virtual samples," +
            "that new virtual samples are correctly created.")]
        public void TestVirtual_WhenHasVirtual_ShouldUpdate_Late()
        {
            var sampleC = new Sample<Vector3>(new Vector3(7, 8, 9), false, 2, null);
            var sampleD = new Sample<Vector3>(new Vector3(10, 11, 12), false, 4, null);

            var settings = InterpolationSettings.CreateDefault();
            settings.smoothing = SmoothingSettings.Empty;
            var interpolator = new BindingInterpolator<Vector3>(settings, 1);

            // Append two samples, at time 0 and at time 1
            interpolator.AppendSample(SampleA.Value, SampleA.Stopped, SampleA.Frame, SampleA.Frame);
            interpolator.AppendSample(SampleB.Value, SampleB.Stopped, SampleB.Frame, SampleB.Frame);

            // Overshoot by 0.1f
            var overshootTime = 2.1f;
            var overshootExpectedValue = SampleA.Value + (SampleB.Value - SampleA.Value) * overshootTime;
            var result1 = interpolator.PerformInterpolation(Vector3.zero, overshootTime + interpolator.TargetDelay);
            Assert.That(result1, Is.EqualTo(overshootExpectedValue).Using(Comparer));

            // Append another sample, at time 2
            interpolator.AppendSample(sampleC.Value, sampleC.Stopped, sampleC.Frame, sampleC.Frame);

            // Assert there are virtual samples
            Assert.That(interpolator.Buffer.VirtualSamples.HasValue, Is.True);

            // Interpolate over virtuals
            var (virtualFirst, virtualSecond) = interpolator.Buffer.VirtualSamples.Value;
            var overshoot2Time = 2.2f;
            var t = (float)((overshoot2Time - virtualFirst.Time) / (virtualSecond.Time - virtualFirst.Time));
            var overshoot2ExpectedValue = virtualFirst.Value + (virtualSecond.Value - virtualFirst.Value) * t;
            var result2 = interpolator.PerformInterpolation(Vector3.zero, overshoot2Time + interpolator.TargetDelay);
            Assert.That(result2, Is.EqualTo(overshoot2ExpectedValue).Using(Comparer));

            // Append another sample, at time 4
            interpolator.AppendSample(sampleD.Value, sampleD.Stopped, sampleD.Frame, sampleD.Frame);

            // Assert virtual samples are correct
            Assert.That(interpolator.Buffer.VirtualSamples.HasValue, Is.True);
            (virtualFirst, virtualSecond) = interpolator.Buffer.VirtualSamples.Value;
            Assert.That(virtualFirst.Value, Is.EqualTo(overshoot2ExpectedValue).Using(Comparer));
            Assert.That(virtualFirst.Time, Is.EqualTo(overshoot2Time).Within(Epsilon));

            var overshotTimeAmount = overshoot2Time - sampleC.Time;
            var secondVirtualSampleTime = (float)(sampleD.Time + overshotTimeAmount);
            var secondVirtualSampleT = (float)((secondVirtualSampleTime - sampleC.Time) / (sampleD.Time - sampleC.Time));
            Assert.That(virtualSecond.Value, Is.EqualTo(sampleC.Value + (sampleD.Value - sampleC.Value) * secondVirtualSampleT).Using(Comparer));
            Assert.That(virtualSecond.Time, Is.EqualTo(secondVirtualSampleTime).Within(Epsilon));
        }

        [Test]
        [Description("Verifies that when we receive a sample eariler than expected, while interpolating over virtual samples," +
            "that new virtual samples are correctly created.")]
        public void TestVirtual_WhenHasVirtual_ShouldUpdate_Early()
        {
            var sampleC = new Sample<Vector3>(new Vector3(7, 8, 9), false, 2, null);
            var sampleD = new Sample<Vector3>(new Vector3(10, 11, 12), false, 2.5f, null);

            var settings = InterpolationSettings.CreateDefault();
            settings.smoothing = SmoothingSettings.Empty;
            var interpolator = new BindingInterpolator<Vector3>(settings, 1);

            // Append two samples, at time 0 and at time 1
            interpolator.AppendSample(SampleA.Value, SampleA.Stopped, SampleA.Frame, SampleA.Frame);
            interpolator.AppendSample(SampleB.Value, SampleB.Stopped, SampleB.Frame, SampleB.Frame);

            // Overshoot by 0.1f
            var overshootTime = 2.1f;
            var overshootExpectedValue = SampleA.Value + (SampleB.Value - SampleA.Value) * overshootTime;
            var result1 = interpolator.PerformInterpolation(Vector3.zero, overshootTime + interpolator.TargetDelay);
            Assert.That(result1, Is.EqualTo(overshootExpectedValue).Using(Comparer));

            // Append another sample, at time 2
            interpolator.AppendSample(sampleC.Value, sampleC.Stopped, sampleC.Frame, sampleC.Frame);

            // Assert there are virtual samples
            Assert.That(interpolator.Buffer.VirtualSamples.HasValue, Is.True);

            // Interpolate over virtuals
            var (virtualFirst, virtualSecond) = interpolator.Buffer.VirtualSamples.Value;
            var overshoot2Time = 2.2f;
            var t = (float)((overshoot2Time - virtualFirst.Time) / (virtualSecond.Time - virtualFirst.Time));
            var overshoot2ExpectedValue = virtualFirst.Value + (virtualSecond.Value - virtualFirst.Value) * t;
            var result2 = interpolator.PerformInterpolation(Vector3.zero, overshoot2Time + interpolator.TargetDelay);
            Assert.That(result2, Is.EqualTo(overshoot2ExpectedValue).Using(Comparer));

            // Append another sample, at time 2.5
            interpolator.AppendSample(sampleD.Value, sampleD.Stopped, sampleD.Frame, sampleD.Frame);

            // Assert virtual samples are correct
            Assert.That(interpolator.Buffer.VirtualSamples.HasValue, Is.True);
            (virtualFirst, virtualSecond) = interpolator.Buffer.VirtualSamples.Value;
            Assert.That(virtualFirst.Value, Is.EqualTo(overshoot2ExpectedValue).Using(Comparer));
            Assert.That(virtualFirst.Time, Is.EqualTo(overshoot2Time).Within(Epsilon));

            var secondVirtualSampleTime = overshoot2Time + BindingInterpolator<Vector3>.VirtualSamplesInvervalFactor; // * 1/SampleRate which is one
            var secondVirtualSampleT = (float)((secondVirtualSampleTime - sampleC.Time) / (sampleD.Time - sampleC.Time));
            Assert.That(virtualSecond.Value, Is.EqualTo(sampleC.Value + (sampleD.Value - sampleC.Value) * secondVirtualSampleT).Using(Comparer));
            Assert.That(virtualSecond.Time, Is.EqualTo(secondVirtualSampleTime).Within(Epsilon));
        }

        [Test]
        [Description("Verifies that when we receive a sample, while interpolating over virtual samples," +
            "and we aren't overshooting previous real sample, that new virtual samples are correctly created.")]
        public void TestVirtual_WhenHasVirtual_ShouldUpdate_WihoutOvershootingPrevious()
        {
            var sampleC = new Sample<Vector3>(new Vector3(7, 8, 9), false, 3, null);
            var sampleD = new Sample<Vector3>(new Vector3(10, 11, 12), false, 4, null);

            var settings = InterpolationSettings.CreateDefault();
            settings.smoothing = SmoothingSettings.Empty;
            var interpolator = new BindingInterpolator<Vector3>(settings, 1);

            // Append two samples, at time 0 and at time 1
            interpolator.AppendSample(SampleA.Value, SampleA.Stopped, SampleA.Frame, SampleA.Frame);
            interpolator.AppendSample(SampleB.Value, SampleB.Stopped, SampleB.Frame, SampleB.Frame);

            // Overshoot by 0.1f
            var overshootTime = 2.1f;
            var overshootExpectedValue = SampleA.Value + (SampleB.Value - SampleA.Value) * overshootTime;
            var result1 = interpolator.PerformInterpolation(Vector3.zero, overshootTime + interpolator.TargetDelay);
            Assert.That(result1, Is.EqualTo(overshootExpectedValue).Using(Comparer));

            // Append another sample, at time 3
            interpolator.AppendSample(sampleC.Value, sampleC.Stopped, sampleC.Frame, sampleC.Frame);

            // Assert there are virtual samples
            Assert.That(interpolator.Buffer.VirtualSamples.HasValue, Is.True);

            // Interpolate over virtuals
            var (virtualFirst, virtualSecond) = interpolator.Buffer.VirtualSamples.Value;
            var overshoot2Time = 2.2f;
            var t = (float)((overshoot2Time - virtualFirst.Time) / (virtualSecond.Time - virtualFirst.Time));
            var overshoot2ExpectedValue = virtualFirst.Value + (virtualSecond.Value - virtualFirst.Value) * t;
            var result2 = interpolator.PerformInterpolation(Vector3.zero, overshoot2Time + interpolator.TargetDelay);
            Assert.That(result2, Is.EqualTo(overshoot2ExpectedValue).Using(Comparer));

            // Append another sample, at time 4
            interpolator.AppendSample(sampleD.Value, sampleD.Stopped, sampleD.Frame, sampleD.Frame);

            // Assert virtual samples are correct
            Assert.That(interpolator.Buffer.VirtualSamples.HasValue, Is.True);
            (virtualFirst, virtualSecond) = interpolator.Buffer.VirtualSamples.Value;
            Assert.That(virtualFirst.Value, Is.EqualTo(overshoot2ExpectedValue).Using(Comparer));
            Assert.That(virtualFirst.Time, Is.EqualTo(overshoot2Time).Within(Epsilon));

            Assert.That(virtualSecond.Value, Is.EqualTo(sampleD.Value).Using(Comparer));
            Assert.That(virtualSecond.Time, Is.EqualTo(sampleD.Time).Within(Epsilon));
        }
    }
}
