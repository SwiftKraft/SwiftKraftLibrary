// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Interpolation.Tests
{
    using System;
    using NUnit.Framework;
    using UnityEngine;
    using Coherence.Tests;

    public class SmoothingTests : CoherenceTest
    {
        const int nOfSteps = 5;

        Smoothing smoothing;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            smoothing = new Smoothing();
        }

        [TestCase(0, 10, 0.0016f, 10)] // time to complete factor is necessary because
        [TestCase(0, 10, 100000f, 10)] // value is smoothed 99% of the way after around 2 * smoothTime
        [TestCase(10, 0, 0.0016f, 10)] // but that is not enough to pass Approx.Eq
        [TestCase(10, 0, 100000f, 10)]
        [TestCase(0, float.MaxValue, 0.0016f, 10)]
        [TestCase(0, float.MaxValue, 100000f, 10)]
        [TestCase(float.MinValue, 10, 0.0016f, 60)] // smoothing from big number towards zero takes more time
        [TestCase(float.MinValue, 10, 100000f, 60)] // because of the floating point precision
        [TestCase(float.MaxValue, 10, 0.0016f, 60)] // so we increase timeToCompleteFactor
        [TestCase(float.MaxValue, 10, 100000f, 60)]
        [TestCase(float.MinValue, float.MaxValue, 0.0016f, 10)]
        [TestCase(float.MinValue, float.MaxValue, 100000f, 10)]
        [TestCase(float.MaxValue, float.MinValue, 0.0016f, 10)]
        [TestCase(float.MaxValue, float.MinValue, 100000f, 10)]
        public void SmoothingFloatWorks(float startValue, float endValue, float smoothTime, int timeToCompleteFactor)
        {
            // Arrange
            var settings = new SmoothingSettings()
            {
                smoothTime = smoothTime
            };

            var goingPositive = startValue < endValue;
            var currentValue = startValue;
            var time = 0f;
            var step = 0;

            // Act
            // we allow for more steps then necessary with timeToCompleteFactor because smoothTime is not exact when using smoothDamp
            while (!Approx.Eq(currentValue, endValue) && step < nOfSteps * timeToCompleteFactor)
            {
                var nextValue = smoothing.SmoothFloat(settings, currentValue, endValue, time);

                Assert.That(!float.IsNaN(nextValue) && !float.IsInfinity(nextValue));

                if (time == 0)
                {
                    Assert.That(nextValue, Is.EqualTo(currentValue));
                }
                else
                {
                    if (goingPositive)
                    {
                        Assert.That(nextValue, Is.GreaterThanOrEqualTo(currentValue));
                    }
                    else
                    {
                        Assert.That(nextValue, Is.LessThanOrEqualTo(currentValue));
                    }
                }

                currentValue = nextValue;
                time += smoothTime / nOfSteps;
                step++;
            }

            // Assert
            Assert.That(Approx.Eq(currentValue, endValue), $"End value ({currentValue}) is not approximately equal to target value ({endValue})");
        }

        [TestCase(0, 10, 0.0016f, 15)] // time to complete factor is necessary because
        [TestCase(0, 10, 100000f, 15)] // value is smoothed 99% of the way after around 2 * smoothTime
        [TestCase(10, 0, 0.0016f, 15)] // but that is not enough to pass Approx.Eq
        [TestCase(10, 0, 100000f, 15)]
        [TestCase(0, double.MaxValue, 0.0016f, 15)]
        [TestCase(0, double.MaxValue, 100000f, 15)]
        [TestCase(double.MinValue, 10, 0.0016f, 370)] // smoothing from big number towards zero takes more time
        [TestCase(double.MinValue, 10, 100000f, 370)] // because of the floating point precision
        [TestCase(double.MaxValue, 10, 0.0016f, 370)] // so we increase timeToCompleteFactor
        [TestCase(double.MaxValue, 10, 100000f, 370)]
        [TestCase(double.MinValue, double.MaxValue, 0.0016f, 15)]
        [TestCase(double.MinValue, double.MaxValue, 100000f, 15)]
        [TestCase(double.MaxValue, double.MinValue, 0.0016f, 15)]
        [TestCase(double.MaxValue, double.MinValue, 100000f, 15)]
        public void SmoothingDoubleWorks(double startValue, double endValue, float smoothTime, int timeToCompleteFactor)
        {
            // Arrange
            var settings = new SmoothingSettings()
            {
                smoothTime = smoothTime
            };

            var goingPositive = startValue < endValue;
            var currentValue = startValue;
            var time = 0f;
            var step = 0;

            // Act
            // we allow for more steps then necessary with timeToCompleteFactor because smoothTime is not exact when using smoothDamp
            while (!Approx.Eq(currentValue, endValue) && step < nOfSteps * timeToCompleteFactor)
            {
                var nextValue = smoothing.SmoothDouble(settings, currentValue, endValue, time);

                Assert.That(!double.IsNaN(nextValue) && !double.IsInfinity(nextValue));

                if (time == 0)
                {
                    Assert.That(nextValue, Is.EqualTo(currentValue));
                }
                else
                {
                    if (goingPositive)
                    {
                        Assert.That(nextValue, Is.GreaterThanOrEqualTo(currentValue));
                    }
                    else
                    {
                        Assert.That(nextValue, Is.LessThanOrEqualTo(currentValue));
                    }
                }

                currentValue = nextValue;
                time += smoothTime / nOfSteps;
                step++;
            }

            // Assert
            Assert.That(Approx.Eq(currentValue, endValue), $"End value ({currentValue}) is not approximately equal to target value ({endValue})");
        }

        [TestCase(0, 10, 0.0016f, 10)] // time to complete factor is necessary because
        [TestCase(0, 10, 100000f, 10)] // value is smoothed 99% of the way after around 2 * smoothTime
        [TestCase(10, 0, 0.0016f, 10)] // but that is not enough to pass Approx.Eq
        [TestCase(10, 0, 100000f, 10)]
        [TestCase(0, float.MaxValue, 0.0016f, 10)]
        [TestCase(0, float.MaxValue, 100000f, 10)]
        [TestCase(float.MinValue, 10, 0.0016f, 60)] // smoothing from big number towards zero takes more time
        [TestCase(float.MinValue, 10, 100000f, 60)] // because of the floating point precision
        [TestCase(float.MaxValue, 10, 0.0016f, 60)] // so we increase timeToCompleteFactor
        [TestCase(float.MaxValue, 10, 100000f, 60)]
        [TestCase(float.MinValue, float.MaxValue, 0.0016f, 10)]
        [TestCase(float.MinValue, float.MaxValue, 100000f, 10)]
        [TestCase(float.MaxValue, float.MinValue, 0.0016f, 10)]
        [TestCase(float.MaxValue, float.MinValue, 100000f, 10)]
        public void SmoothingVector2Works(float startValue, float endValue, float smoothTime, int timeToCompleteFactor)
        {
            // Arrange
            var settings = new SmoothingSettings()
            {
                smoothTime = smoothTime
            };

            var endVector2 = new Vector2(endValue, endValue);
            var goingPositive = startValue < endValue;
            var currentValue = new Vector2(startValue, startValue);
            var time = 0f;
            var step = 0;

            // Act
            // we allow for more steps then necessary with timeToCompleteFactor because smoothTime is not exact when using smoothDamp
            while (!Approx.Eq(currentValue, endVector2) && step < nOfSteps * timeToCompleteFactor)
            {
                var nextValue = smoothing.SmoothVector2(settings, currentValue, endVector2, time);

                Assert.That(!float.IsNaN(nextValue.x) && !float.IsInfinity(nextValue.x) &&
                    !float.IsNaN(nextValue.y) && !float.IsInfinity(nextValue.y));

                if (time == 0)
                {
                    Assert.That(nextValue, Is.EqualTo(currentValue));
                }
                else
                {
                    if (goingPositive)
                    {
                        Assert.That(nextValue.x, Is.GreaterThanOrEqualTo(currentValue.x));
                        Assert.That(nextValue.y, Is.GreaterThanOrEqualTo(currentValue.y));
                    }
                    else
                    {
                        Assert.That(nextValue.x, Is.LessThanOrEqualTo(currentValue.x));
                        Assert.That(nextValue.y, Is.LessThanOrEqualTo(currentValue.y));
                    }
                }

                currentValue = nextValue;
                time += smoothTime / nOfSteps;
                step++;
            }

            // Assert
            Assert.That(Approx.Eq(currentValue, endVector2), $"End value ({currentValue}) is not approximately equal to target value ({endVector2})");
        }

        [TestCase(0, 10, 0.0016f, 10)] // time to complete factor is necessary because
        [TestCase(0, 10, 100000f, 10)] // value is smoothed 99% of the way after around 2 * smoothTime
        [TestCase(10, 0, 0.0016f, 10)] // but that is not enough to pass Approx.Eq
        [TestCase(10, 0, 100000f, 10)]
        [TestCase(0, float.MaxValue, 0.0016f, 10)]
        [TestCase(0, float.MaxValue, 100000f, 10)]
        [TestCase(float.MinValue, 10, 0.0016f, 60)] // smoothing from big number towards zero takes more time
        [TestCase(float.MinValue, 10, 100000f, 60)] // because of the floating point precision
        [TestCase(float.MaxValue, 10, 0.0016f, 60)] // so we increase timeToCompleteFactor
        [TestCase(float.MaxValue, 10, 100000f, 60)]
        [TestCase(float.MinValue, float.MaxValue, 0.0016f, 10)]
        [TestCase(float.MinValue, float.MaxValue, 100000f, 10)]
        [TestCase(float.MaxValue, float.MinValue, 0.0016f, 10)]
        [TestCase(float.MaxValue, float.MinValue, 100000f, 10)]
        public void SmoothingVector3Works(float startValue, float endValue, float smoothTime, int timeToCompleteFactor)
        {
            // Arrange
            var settings = new SmoothingSettings()
            {
                smoothTime = smoothTime
            };

            var endVector3 = new Vector3(endValue, endValue);
            var goingPositive = startValue < endValue;
            var currentValue = new Vector3(startValue, startValue);
            var time = 0f;
            var step = 0;

            // Act
            // we allow for more steps then necessary with timeToCompleteFactor because smoothTime is not exact when using smoothDamp
            while (!Approx.Eq(currentValue, endVector3) && step < nOfSteps * timeToCompleteFactor)
            {
                var nextValue = smoothing.SmoothVector3(settings, currentValue, endVector3, time);

                Assert.That(!float.IsNaN(nextValue.x) && !float.IsInfinity(nextValue.x) &&
                    !float.IsNaN(nextValue.y) && !float.IsInfinity(nextValue.y) &&
                    !float.IsNaN(nextValue.z) && !float.IsInfinity(nextValue.z));

                if (time == 0)
                {
                    Assert.That(nextValue, Is.EqualTo(currentValue));
                }
                else
                {
                    if (goingPositive)
                    {
                        Assert.That(nextValue.x, Is.GreaterThanOrEqualTo(currentValue.x));
                        Assert.That(nextValue.y, Is.GreaterThanOrEqualTo(currentValue.y));
                        Assert.That(nextValue.z, Is.GreaterThanOrEqualTo(currentValue.z));
                    }
                    else
                    {
                        Assert.That(nextValue.x, Is.LessThanOrEqualTo(currentValue.x));
                        Assert.That(nextValue.y, Is.LessThanOrEqualTo(currentValue.y));
                        Assert.That(nextValue.z, Is.LessThanOrEqualTo(currentValue.z));
                    }
                }

                currentValue = nextValue;
                time += smoothTime / nOfSteps;
                step++;
            }

            // Assert
            Assert.That(Approx.Eq(currentValue, endVector3), $"End value ({currentValue}) is not approximately equal to target value ({endVector3})");
        }

        public struct QuaternionSmoothingTestData
        {
            public Quaternion StartValue;
            public Quaternion EndValue;
            public float SmoothTime;

            // time to complete factor is necessary because
            // value is smoothed 99% of the way after around 2 * smoothTime
            // but that is not enough to pass Approx.Eq
            public int TimeToCompleteFactor;

            public QuaternionSmoothingTestData(Quaternion startValue, Quaternion endValue, float smoothTime, int timeToCompleteFactor)
            {
                this.StartValue = startValue;
                this.EndValue = endValue;
                this.SmoothTime = smoothTime;
                this.TimeToCompleteFactor = timeToCompleteFactor;
            }
        }

        private static QuaternionSmoothingTestData[] smoothingQuaternionTestSource =
        {
            new(Quaternion.identity, Quaternion.Euler(10, 20, 30), 0.0016f, 10),
            new(Quaternion.identity, Quaternion.Euler(10, 20, 30), 100000f, 10),
            new(Quaternion.Euler(10, 20, 30), Quaternion.identity, 0.0016f, 10),
            new(Quaternion.Euler(10, 20, 30), Quaternion.identity, 100000f, 10),
            new(Quaternion.Euler(-89, 0, 0), Quaternion.Euler(-91, 0, 0), 0.0016f, 10),
            new(Quaternion.Euler(-89, 0, 0), Quaternion.Euler(-91, 0, 0), 100000f, 10),
            new(Quaternion.Euler(-91, 0, 0), Quaternion.Euler(-89, 0, 0), 0.0016f, 10),
            new(Quaternion.Euler(-91, 0, 0), Quaternion.Euler(-89, 0, 0), 100000f, 10),
            new(Quaternion.Euler(123, -10, 440), Quaternion.Euler(-89, 230, 33), 0.0016f, 10),
            new(Quaternion.Euler(123, -10, 440), Quaternion.Euler(-89, 230, 33), 100000f, 10),
        };

        [TestCaseSource(nameof(smoothingQuaternionTestSource))]
        public void SmoothingQuaternionWorks(QuaternionSmoothingTestData testData)
        {
            // Arrange
            var settings = new SmoothingSettings()
            {
                smoothTime = testData.SmoothTime
            };

            var delta = testData.EndValue * Quaternion.Inverse(testData.StartValue);
            delta.ToAngleAxisShortest(out _, out var axis);

            var currentValue = testData.StartValue;
            var time = 0f;
            var step = 0;

            // Act
            // we allow for more steps then necessary with timeToCompleteFactor because smoothTime is not exact when using smoothDamp
            while (!Approx.Eq(Quaternion.Angle(currentValue, testData.EndValue), 0) && step < nOfSteps * testData.TimeToCompleteFactor)
            {
                var nextValue = smoothing.SmoothQuaternion(settings, currentValue, testData.EndValue, time);

                Assert.That(!float.IsNaN(nextValue.x) && !float.IsNaN(nextValue.y) && !float.IsNaN(nextValue.z) && !float.IsNaN(nextValue.w) &&
                    !float.IsInfinity(nextValue.x) && !float.IsInfinity(nextValue.y) && !float.IsInfinity(nextValue.z) && !float.IsInfinity(nextValue.w));

                if (time == 0)
                {
                    Assert.That(nextValue, Is.EqualTo(currentValue));
                }
                else
                {
                    var stepDelta = nextValue * Quaternion.Inverse(currentValue);
                    stepDelta.ToAngleAxisShortest(out var stepAngle, out var stepAxis);

                    Assert.That(stepAngle, Is.GreaterThanOrEqualTo(0));

                    if (stepAngle > 0)
                    {
                        Assert.That(Approx.Eq(Vector3.Dot(stepAxis.normalized, axis.normalized), 1f));
                    }
                }

                currentValue = nextValue;

                time += testData.SmoothTime / nOfSteps;
                step++;
            }

            // Assert
            Assert.That(Approx.Eq(Quaternion.Angle(currentValue, testData.EndValue), 0), $"End value ({currentValue}) is not approximately equal to target value ({testData.EndValue})");
        }

        public struct QuaternionSmoothingMaxDataTestData
        {
            public Quaternion StartValue;
            public Quaternion EndValue;
            public float SmoothTime;
            public float MaxSpeed;

            // time to complete factor is necessary because
            // value is smoothed 99% of the way after around 2 * smoothTime
            // but that is not enough to pass Approx.Eq
            public int TimeToCompleteFactor;

            public QuaternionSmoothingMaxDataTestData(Quaternion startValue, Quaternion endValue, float smoothTime, float maxSpeed, int timeToCompleteFactor)
            {
                this.StartValue = startValue;
                this.EndValue = endValue;
                this.SmoothTime = smoothTime;
                this.MaxSpeed = maxSpeed;
                this.TimeToCompleteFactor = timeToCompleteFactor;
            }
        }

        private static QuaternionSmoothingMaxDataTestData[] smoothingQuaternionMaxSpeedTestSource =
        {
            new(Quaternion.identity, Quaternion.Euler(10, 20, 30), 0.0016f, 1, 10),
            new(Quaternion.identity, Quaternion.Euler(10, 20, 30), 100000f, 1, 10),
            new(Quaternion.Euler(10, 20, 30), Quaternion.identity, 0.0016f, 1, 10),
            new(Quaternion.Euler(10, 20, 30), Quaternion.identity, 100000f, 1, 10),
            new(Quaternion.Euler(-89, 0, 0), Quaternion.Euler(-91, 0, 0), 0.0016f, 1, 10),
            new(Quaternion.Euler(-89, 0, 0), Quaternion.Euler(-91, 0, 0), 100000f, 1, 10),
            new(Quaternion.Euler(-91, 0, 0), Quaternion.Euler(-89, 0, 0), 0.0016f, 1, 10),
            new(Quaternion.Euler(-91, 0, 0), Quaternion.Euler(-89, 0, 0), 100000f, 1, 10),
            new(Quaternion.Euler(123, -10, 440), Quaternion.Euler(-89, 230, 33), 0.0016f, 1, 10),
            new(Quaternion.Euler(123, -10, 440), Quaternion.Euler(-89, 230, 33), 100000f, 1, 10),
            new(Quaternion.identity, Quaternion.Euler(10, 20, 30), 0.0016f, 20, 10),
            new(Quaternion.identity, Quaternion.Euler(10, 20, 30), 100000f, 20, 10),
            new(Quaternion.Euler(10, 20, 30), Quaternion.identity, 0.0016f, 20, 10),
            new(Quaternion.Euler(10, 20, 30), Quaternion.identity, 100000f, 20, 10),
            new(Quaternion.Euler(-89, 0, 0), Quaternion.Euler(-91, 0, 0), 0.0016f, 20, 10),
            new(Quaternion.Euler(-89, 0, 0), Quaternion.Euler(-91, 0, 0), 100000f, 20, 10),
            new(Quaternion.Euler(-91, 0, 0), Quaternion.Euler(-89, 0, 0), 0.0016f, 20, 10),
            new(Quaternion.Euler(-91, 0, 0), Quaternion.Euler(-89, 0, 0), 100000f, 20, 10),
            new(Quaternion.Euler(123, -10, 440), Quaternion.Euler(-89, 230, 33), 0.0016f, 20, 10),
            new(Quaternion.Euler(123, -10, 440), Quaternion.Euler(-89, 230, 33), 100000f, 20, 10),
        };

        [TestCaseSource(nameof(smoothingQuaternionMaxSpeedTestSource))]
        public void SmoothingQuaternionMaxSpeedWorks(QuaternionSmoothingMaxDataTestData testData)
        {
            // Arrange
            var settings = new SmoothingSettings()
            {
                smoothTime = testData.SmoothTime,
                maxSpeed = testData.MaxSpeed,
            };

            var currentValue = testData.StartValue;

            var time = 0f;
            var stepTime = settings.smoothTime / nOfSteps;

            for (var i = 0; i < nOfSteps * 10; i++)
            {
                var nextValue = smoothing.SmoothQuaternion(settings, currentValue, testData.EndValue, time);

                var stepDelta = nextValue * Quaternion.Inverse(currentValue);
                stepDelta.ToAngleAxisShortest(out var stepAngle, out _);

                Assert.That(Mathf.Abs(stepAngle), Is.LessThanOrEqualTo(settings.maxSpeed * stepTime));

                currentValue = nextValue;
                time += stepTime;
            }
        }

        [Test]
        [Description("Verifies that an exception is thrown if attempting to smooth time in reverse.")]
        public void SmoothingThrowsExceptionWhenTimeGoingBackwards()
        {
            var settings = new SmoothingSettings();

            // Act - Floats
            smoothing.SmoothFloat(settings, 0, 1, 10f);

            // Assert
            // Subsequent time is smaller than the initial time because time
            // is going backwards and we don't support that, this is not a Tardis.
            Assert.Throws<Exception>(() =>
                {
                    smoothing.SmoothFloat(settings, 0, 1, 9f);
                });

            // Act - Double
            smoothing.SmoothDouble(settings, 0, 1, 10);

            // Assert
            Assert.Throws<Exception>(() =>
                {
                    smoothing.SmoothDouble(settings, 0, 1, 9);
                });

            // Act - Vector2
            smoothing.SmoothVector2(settings, Vector2.zero, Vector2.one, 10f);

            // Assert
            Assert.Throws<Exception>(() =>
                {
                    smoothing.SmoothVector2(settings, Vector2.zero, Vector2.one, 9f);
                });

            // Act - Vector3
            smoothing.SmoothVector3(settings, Vector3.zero, Vector3.one, 10f);

            // Assert
            Assert.Throws<Exception>(() =>
                {
                    smoothing.SmoothVector3(settings, Vector3.zero, Vector3.one, 9f);
                });

            // Act - Quaternion
            smoothing.SmoothQuaternion(settings, Quaternion.identity, Quaternion.identity, 10f);

            // Assert
            Assert.Throws<Exception>(() =>
                {
                    smoothing.SmoothQuaternion(settings, Quaternion.identity, Quaternion.identity, 9f);
                });

        }
    }
}
