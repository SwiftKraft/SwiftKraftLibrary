// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Interpolation
{
    using System;
    using Coherence.SimulationFrame;
    using UnityEngine;

    /// <summary>
    /// BindingInterpolator is used to smoothly interpolate between samples received over the network.
    /// The following types are supported for interpolation: float, bool, int, long, Vector2, Vector3, Quaternion, Color, string and EntityReferences.
    /// Additional <see cref="Smoothing"/> (using SmoothDamp) is supported for types: float, Vector2, Vector3 and Quaternion.
    /// </summary>
    public sealed class BindingInterpolator<T>
    {
        /// <summary>
        /// Smooth time used for smoothing <see cref="Delay"/> towards <see cref="TargetDelay"/> when <see cref="Delay"/> is less than <see cref="TargetDelay"/>.
        /// </summary>
        private const float DelaySmoothTimeIncrease = 1f;

        /// <summary>
        /// Smooth time used for smoothing <see cref="Delay"/> towards <see cref="TargetDelay"/> when <see cref="Delay"/> is greater than <see cref="TargetDelay"/>.
        /// </summary>
        private const float DelaySmoothTimeDecrease = 5f;

        /// <summary>
        /// Used to slowly decrease the virtual samples interval. See <see cref="SetVirtualSamples(Sample{T})"/>
        /// </summary>
        public const float VirtualSamplesInvervalFactor = 0.8f;

        /// <summary>
        /// The frequency at which samples are generated and transmitted from the entity owner. Higher sample rates incur lower <see cref="Delay"/>.
        /// Value is automatically initialize by the corresponding <see cref="Binding"/>.
        /// Use the Optimize window to configure the sample rate for a binding.
        /// </summary>
        public double SampleRate
        {
            get => sampleRate > 0 ? sampleRate : InterpolationSettings.DefaultSampleRate;
            set => sampleRate = value;
        }
        private double sampleRate = InterpolationSettings.DefaultSampleRate;

        /// <summary>
        /// Interpolation settings which specify behaviour and type of the interpolator.
        /// </summary>
        public InterpolationSettings Settings;

        /// <inheritdoc cref="InterpolationSettings.IsInterpolationNone"/>
        public bool IsInterpolationNone => Settings.IsInterpolationNone;

        /// <summary>
        /// The current time for this binding. It will trail behind <see cref="NetworkTime"/> by <see cref="Delay"/> seconds in order to produce smooth movement.
        /// </summary>
        public double Time;

        /// <summary>
        /// The duration between samples at which samples are actually generated and transmitted from the entity owner.
        /// This is measured from incoming samples by finding the longest duration between samples in the current <see cref="SampleBuffer{T}"/>.
        /// </summary>
        public double MeasuredSampleInterval { get; private set; }

        /// <summary>
        /// Latency between this client and the authority client that owns this entity, in seconds.
        /// This value is updated when new samples arrives so that <see cref="Delay"/> stays tuned to current network conditions.
        /// Network latency is scaled with <see cref="LatencySettings.networkLatencyFactor"/>.
        /// </summary>
        public double NetworkLatency { get; private set; }

        /// <summary>
        /// The current internal latency for this binding, i.e., how many seconds this binding's <see cref="Time"/> trails behind <see cref="NetworkTime.Time"/>.
        /// Delay is lerped towards <see cref="TargetDelay"/> over time to avoid sudden jumps in movement.
        /// </summary>
        public double Delay { get; private set; }

        /// <summary>
        /// The target internal latency for this binding, i.e., how many seconds this binding's <see cref="Time"/> trails behind <see cref="NetworkTime.Time"/>.
        /// TargetDelay is computed from a number of factors, including sampling frequency, network latency and blending method.
        /// </summary>
        /// <seealso cref="SampleRate"/>
        /// <seealso cref="NetworkLatency"/>
        /// <seealso cref="LatencySettings"/>
        /// <seealso cref="Interpolator.NumberOfSamplesToStayBehind"/>
        public double TargetDelay => Settings.interpolator.NumberOfSamplesToStayBehind * MeasuredSampleInterval // keep 1 or 2 samples of headroom at all times
                               + NetworkLatency * Settings.latencySettings.networkLatencyFactor // network latency, scaled with delay fudge factor
                               + Settings.latencySettings.additionalLatency // additional delay
                               + 1 / InterpolationSettings.SimulationFramesPerSecond; // account for 60hz quantization errors

        /// <summary>
        /// When we are completely done with interpolation and there is nothing else to interpolate over, interpolation is marked as stopped.
        /// In other words, when we completely interpolated (t = 1) to the latest received sample (which must be stopped!), IsStopped is set true,
        /// and reset to false when we receive another sample.
        /// </summary>
        public bool IsStopped { get; private set; } = true;

        /// <summary>
        /// The last interpolated value. Should be the current value of the binding.
        /// </summary>
        public T LastInterpolatedValue { get; set; }

        /// <summary>
        /// True if lastInterpolatedValue has been set at least once.
        /// </summary>
        public bool HasLastInterpolatedValue { get; private set; } = false;

        /// <summary>
        /// Used for SmoothDamp calculation of <see cref="Delay"/> towards <see cref="TargetDelay"/>.
        /// </summary>
        private double delayVelocity;

        /// <summary>
        /// Used for SmoothDamp calculation of <see cref="Delay"/> towards <see cref="TargetDelay"/>.
        /// </summary>
        private double? lastDelaySmoothTime;

        private readonly SampleBuffer<T> buffer = new SampleBuffer<T>();
        public SampleBuffer<T> Buffer => buffer;

        private readonly IInterpolator<T> interpolator;

        /// <summary>
        /// After applying interpolation, smoothing is also applied to additionally smooth movement
        /// </summary>
        private readonly ISmoothing<T> smoothing;
        public ISmoothing<T> Smoothing => smoothing;

        public BindingInterpolator(InterpolationSettings settings, double sampleRate)
        {
            this.Settings = settings;
            this.sampleRate = sampleRate;
            this.smoothing = new Smoothing() as ISmoothing<T>;

            this.interpolator = settings.interpolator as IInterpolator<T>;

            if (this.interpolator == null)
            {
                throw new System.Exception("Interpolator doesn't implement IInterpolator<T> for type: " + typeof(T));
            }

            // setting the MeasuredSampleInterval to the expected duration as default
            MeasuredSampleInterval = 1 / SampleRate;
        }

        /// <summary>
        /// Return the last sample in the sample buffer, casted to the given type.
        /// </summary>
        /// <returns>The last sample in the sample buffer.</returns>
        public Sample<T>? GetLastSample()
        {
            if (buffer == null || buffer.Count == 0)
            {
                return null;
            }

            return buffer.Last;
        }

        /// <summary>
        /// Adds a sample to the sample buffer at the given frame.
        /// </summary>
        /// <param name="value">The sample data.</param>
        /// <param name="stopped">The samples have stopped with this value.</param>
        /// <param name="sampleFrame">The simulation frame at which the sample was generated by the entity authority.</param>
        /// <param name="localFrame">The current simulation frame on this machine, i.e. <see cref="NetworkTime.ClientSimulationFrame"/>.</param>
        public void AppendSample(T value, bool stopped, AbsoluteSimulationFrame sampleFrame, AbsoluteSimulationFrame localFrame)
        {
            AppendSample(value,
                stopped,
                isSampleTimeValid: sampleFrame != AbsoluteSimulationFrame.Invalid,
                sampleFrame / InterpolationSettings.SimulationFramesPerSecond,
                localFrame / InterpolationSettings.SimulationFramesPerSecond);
        }

        /// <summary>
        /// Adds a sample to the sample buffer at the given time.
        /// </summary>
        /// <param name="value">The sample data.</param>
        /// <param name="stopped">The samples have stopped with this value.</param>
        /// <param name="isSampleTimeValid">False if received sampleFrame is invalid. Possibly because the sample wasn't streamed
        ///     directly from the authority client, but was actually sent from the RS cache when we moved our live query or floating origin</param>
        /// <param name="sampleTime">The time at which the sample was generated by the entity authority.</param>
        /// <param name="localTime">The current time on this machine, i.e. <see cref="NetworkTime.TimeAsDouble"/>.</param>
        public void AppendSample(T value, bool stopped, bool isSampleTimeValid, double sampleTime, double localTime)
        {
            if (Settings.IsInterpolationNone)
            {
                buffer.SetLast(new Sample<T>(value, stopped, sampleTime, null));
                return;
            }

            if (IsBeyondTeleportDistance(value))
            {
                Reset();
            }

            double? sampleLatency = null;

            // Update network latency only if the sample is not stale (did not come from the RS cache).
            //  1. isSampleTimeValid => deserialized simFrame delta is not -byte.MaxValue
            //  2. sampleBuffer.Count > 0 => first sample is always from the RS cache
            //  3. sampleTime > sampleBuffer.Last.Time => if we receive two samples with the same simulation frame it means that the second one is stale
            if (isSampleTimeValid && buffer.Count > 0 && sampleTime > buffer.Last.Value.Time)
            {
                sampleLatency = localTime - sampleTime;

                // If calculated network latency is negative, it means our clientSimulationFrame is behind serverSimulationFrame
                // which means we are currently simulating history.
                // In that case timeScale will be over 1 and we will be simulating (replaying) our data at a faster speed.
                // Also we don't want to have negative latency because that will move our InterpolationSettings.Time back to real time
                // instead of keeping it in history time
                if (sampleLatency < 0)
                {
                    sampleLatency = null;
                }
            }

            // If we receive a stale sample we have no idea what its time is,
            // so we can't use it in interpolation.
            if (!isSampleTimeValid && !IsStopped)
            {
                // In case the sample isn't the last one (stopped), we can just ignore it and wait for the next one to arrive.
                if (!stopped)
                {
                    return;
                }
                // Else, we have to end up in that position, so we'll put it at the "expected" time.
                else
                {
                    sampleTime = buffer.Last.HasValue ? buffer.Last.Value.Time + MeasuredSampleInterval : Time + MeasuredSampleInterval;
                }
            }

            // If we are stopped, we have to create a "virtual" sample. This is because we weren't receiving samples
            // while the binding wasn't changing, and now when we received a sample, time gap between them is huge.
            // To fix this, we take the last sample and move it in time to the expected time, which is at sampleTime - MeasuredSampleInterval.
            // This will be wrong only in case the actual sample interval is greater than MeasuredSampleInterval.
            if (IsStopped && buffer.Last.HasValue)
            {
                var lastSample = buffer.Last.Value;
                var newTime = Math.Min(sampleTime - MeasuredSampleInterval, Time);
                buffer.SetLast(new Sample<T>(lastSample.Value, true, newTime, lastSample.Latency));
            }

            var previousSample = buffer.Last;

            buffer.PushFront(new Sample<T>(value, stopped, sampleTime, sampleLatency));

            if (buffer.TryMeasureSampleInterval(out var measuredSampleInterval))
            {
                this.MeasuredSampleInterval = measuredSampleInterval;
            }

            if (buffer.TryMeasureSampleLatency(out var measuredNetworkLatency))
            {
                UpdateNetworkLatency(measuredNetworkLatency);
            }

            if (previousSample.HasValue)
            {
                SetVirtualSamples(previousSample.Value);
            }

            this.IsStopped = false;
        }

        /// <inheritdoc cref="SampleBuffer{T}.RemoveOutdatedSamples(double, int)"/>
        public void RemoveOutdatedSamples(double time)
        {
            buffer.RemoveOutdatedSamples(time, Settings.interpolator.NumberOfSamplesToStayBehind);
        }

        /// <summary>
        /// Resets the buffer and state variables. This is useful, e.g. when teleporting and re-parenting.
        /// </summary>
        public void Reset()
        {
            buffer.Reset();
            Time = default;
            delayVelocity = default;
            lastDelaySmoothTime = default;
            MeasuredSampleInterval = 1 / SampleRate;
            IsStopped = true;
            HasLastInterpolatedValue = false;
        }

        /// <summary>
        /// Queries the sample buffer for samples adjacent to the given time
        /// and performs blending between those samples using the <see cref="InterpolationSettings.interpolator"/>.
        /// </summary>
        /// <param name="time">The time at which to query the interpolation. Usually <see cref="Time"/>.</param>
        /// <returns>Returns the interpolated value at the given time,
        /// or default if the buffer is empty,
        /// or the single sample value if the buffer holds a single sample.</returns>
        public T GetValueAt(double time, bool ignoreVirtualSamples = true)
        {
            var result = CalculateInterpolationPercentage(time, ignoreVirtualSamples);
            return interpolator.Interpolate(result.value0, result.value1, result.value2, result.value3, result.t);
        }

        /// <summary>
        /// Performs interpolation on the given binding and returns its new value.
        /// Increments <see cref="Time"/> for this binding, taking <see cref="Delay"/> into account.
        /// Calculates the interpolated value for the latency-adjusted time using the <see cref="InterpolationSettings.interpolator"/>.
        /// Applies additional <see cref="smoothing"/> for types that support it (float, double, Vector2, Vector3, Quaternion).
        /// If the sample buffer is empty, the currentValue will be returned.
        /// If the sample buffer contains a single sample, that sample value is returned with no blending or smoothing performed.
        /// </summary>
        /// <param name="currentValue">The current value for the binding.</param>
        /// <param name="time">The current game time, usually <see cref="NetworkTime"/>.</param>
        /// <returns>The interpolated value for this binding at the given time,
        /// or <see cref="currentValue"/> if the sample buffer is empty.</returns>
        public T PerformInterpolation(T currentValue, double time)
        {
            if (buffer.Count == 0)
            {
                HasLastInterpolatedValue = true;
                LastInterpolatedValue = currentValue;

                return currentValue;
            }

            var result = CalculateInterpolationPercentage(Step(time), ignoreVirtualSamples: false);
            var newValue = interpolator.Interpolate(result.value0, result.value1, result.value2, result.value3, result.t);

            IsStopped = result.isStopped;

            if (result.t > 1 + Settings.maxOvershootAllowed)
            {
                IsStopped = true;
                newValue = buffer.Last.Value.Value;

                buffer.VirtualSamples = null;

                // Setting the last sample to stopped simplifies continuation of the movement when the next sample arrives.
                buffer.SetLast(new Sample<T>(buffer.Last.Value.Value, true, buffer.Last.Value.Time, buffer.Last.Value.Latency));
            }

            HasLastInterpolatedValue = true;
            LastInterpolatedValue = newValue;

            RemoveOutdatedSamples(Time);

            if (buffer.Count > 1 && smoothing != null)
            {
                return smoothing.Smooth(Settings.smoothing, currentValue, newValue, time);
            }

            return newValue;
        }

        /// <summary>
        /// Checks if it is necessary and creates/updates virtual samples.
        /// Virtual samples are generated when a sample is received while the interpolator is overshooting or when
        /// we are currently interpolating between virtual samples and it is necessary to update them to not cause snapping.
        /// </summary>
        /// <param name="previousSample">The last sample in the buffer before appending the newly received sample. In other words, second to last sample.</param>
        private void SetVirtualSamples(Sample<T> previousSample)
        {
            // If we never interpolated, we don't have the last interpolated value, thus cannot create virtual samples.
            if (!HasLastInterpolatedValue)
            {
                return;
            }

            // The interval between the current two virtual samples
            var virtualSamplesInterval = buffer.VirtualSamples.HasValue ? buffer.VirtualSamples.Value.Second.Time - buffer.VirtualSamples.Value.First.Time : 0;

            // We will update/create virtual samples only in these 3 cases:
            //  1. We are overshooting the last sample in the buffer
            //  2. a) We have virtual samples, and the second virtual sample is after the previous real sample.
            //        This is the "virtual-samples break condition". We can stop creating virtual samples only if the second virtual
            //        sample is equal to (or before) the previous real sample.
            //  2. b) We have virtual samples, and we overshot the second virtual sample.
            //        This is an edge case which happens only if we stop at a stopped sample and then we receive a new sample.
            var isRealOvershooting = !buffer.VirtualSamples.HasValue && Time > previousSample.Time && buffer.Count > 2;
            var isSecondVirtualOvershootingReal = buffer.VirtualSamples.HasValue && buffer.VirtualSamples.Value.Second.Time > previousSample.Time;
            var isOvershootingVirtual = buffer.VirtualSamples.HasValue && Time > buffer.VirtualSamples.Value.Second.Time;
            if (isRealOvershooting || isSecondVirtualOvershootingReal || isOvershootingVirtual)
            {
                // The first virtual sample is easy, it's at the last interpolated value and the current time
                var virtual1 = new Sample<T>(LastInterpolatedValue, false, Time, null);

                // The second virtual sample is a bit more complicated, we need to calculate it.
                // First case is that the last received sample is stopped. In that case we must set the second virtual
                // sample to that last (stopped) sample.
                var virtual2Time = buffer.Last.Value.Time;
                var virtual2Value = buffer.Last.Value.Value;

                if (!buffer.Last.Value.Stopped)
                {
                    // If the last sample wasn't stopped, we need to overshoot the second virtual sample based on the last two real samples.

                    // We take the more overshot of the two:
                    //  1. Overshoot the last received sample by the amount that we overshot the previous real sample.
                    //  2. Or, to not stop virtual samples abruptly, take the second virtual sample at the same interval as the last two virtual samples,
                    //     but lowered by some amount so that it eventually stops making virtual samples.
                    var timePlusOvershooting = virtual2Time + Time - previousSample.Time;
                    var timePlusVirtualSamplesInterval = Time + virtualSamplesInterval * VirtualSamplesInvervalFactor;

                    // We want to take the one that overshoots more, but only if it actually overshoots the last received sample.
                    var newVirtual2Time = Math.Max(timePlusVirtualSamplesInterval, timePlusOvershooting);
                    if (newVirtual2Time > virtual2Time)
                    {
                        virtual2Time = newVirtual2Time;

                        // We can now calculate the position of the second virtual sample using GetValueAt(), but ignoring the existing virtual samples.
                        // We want this to be calculated only from the real samples.
                        virtual2Value = GetSecondVirtualSampleValue(virtual2Time);
                    }
                }
                else
                {
                    // If the last sample was stopped, we want to smoothly interpolate directly towards it.
                    // This is done by not changing the value of it, but by setting the time to Time + MeasuredSampleInterval.
                    virtual2Time = Math.Max(Time + MeasuredSampleInterval, virtual2Time);
                }

                var virtual2 = new Sample<T>(virtual2Value, buffer.Last.Value.Stopped, virtual2Time, null);

                buffer.VirtualSamples = (virtual1, virtual2);
            }
        }

        public T GetSecondVirtualSampleValue(double virtualSampleTime) => GetValueAt(virtualSampleTime, ignoreVirtualSamples: true);

        private double Step(double newTime)
        {
            UpdateDelay(newTime);

            Time = newTime - Delay;

            return Time;
        }

        private void UpdateDelay(double time)
        {
            // at startup we want Delay to be set to TargetDelay instead of slowly lerping from zero
            if (lastDelaySmoothTime == null)
            {
                this.Delay = this.TargetDelay;
            }
            else
            {
                var deltaTime = time - lastDelaySmoothTime;

                if (deltaTime > 0)
                {
                    // Anything above 1 will allow Delay to increase faster than 1 second per second which interpolates backwards.
                    const double maxSpeed = 1;

                    var smoothTime = DelaySmoothTimeDecrease;

                    if (Delay < TargetDelay || // the delay is too small, it means we are overshooting, we want to correct this quickly
                        delayVelocity > 0) // the delay is too big, but we are still smoothing in the positive (wrong) direction
                    {
                        smoothTime = DelaySmoothTimeIncrease;
                    }

                    this.Delay = InterpolationUtils.SmoothMixDouble(Delay, TargetDelay, ref delayVelocity, smoothTime, maxSpeed, (float)deltaTime);
                }
            }

            lastDelaySmoothTime = time;
        }

        private void UpdateNetworkLatency(double networkLatency)
        {
            this.NetworkLatency = networkLatency;

            // If we are stopped and new network latency is bigger than the old one, we can freely snap Delay to TargetDelay.
            // It will not cause any movement jitter because we are stopped, and it makes sure that Delay is where it should be,
            // instead of letting it lerp slowly towards the TargetDelay.
            if (IsStopped && this.Delay < TargetDelay)
            {
                var delta = TargetDelay - this.Delay;

                this.Delay = TargetDelay;
                this.Time -= delta;
            }
        }

        private bool IsBeyondTeleportDistance(T value)
        {
            if (Settings.maxDistance <= 0)
            {
                return false;
            }

            var newestSampleInBuffer = buffer.Last;
            if (!newestSampleInBuffer.HasValue)
            {
                return false;
            }

            return (newestSampleInBuffer.Value.Value, value) switch
            {
                (int prev, int next) => IsBeyondTeleportDistance(next, prev),
                (float prev, float next) => IsBeyondTeleportDistance(next, prev),
                (Vector2 prev, Vector2 next) => IsBeyondTeleportDistance(next, prev),
                (Vector3 prev, Vector3 next) => IsBeyondTeleportDistance(next, prev),
                (Quaternion prev, Quaternion next) => IsBeyondTeleportDistance(next, prev),
                _ => false
            };
        }

        public bool IsBeyondTeleportDistance(int a, int b)
        {
            return Settings.maxDistance > 0 && Mathf.Abs(a - b) >= Settings.maxDistance;
        }

        public bool IsBeyondTeleportDistance(float a, float b)
        {
            return Settings.maxDistance > 0 && Mathf.Abs(a - b) >= Settings.maxDistance;
        }

        public bool IsBeyondTeleportDistance(Vector2 a, Vector2 b)
        {
            return Settings.maxDistance > 0 && (a - b).sqrMagnitude >= Settings.maxDistance * Settings.maxDistance;
        }

        public bool IsBeyondTeleportDistance(Vector3 a, Vector3 b)
        {
            return Settings.maxDistance > 0 && (a - b).sqrMagnitude >= Settings.maxDistance * Settings.maxDistance;
        }

        public bool IsBeyondTeleportDistance(Quaternion a, Quaternion b)
        {
            return Settings.maxDistance > 0 && Quaternion.Angle(a, b) >= Settings.maxDistance;
        }

        public InterpolationResult<T> CalculateInterpolationPercentage(double time, bool ignoreVirtualSamples)
        {
            var result = new InterpolationResult<T>()
            {
                t = -1,
                delay = this.Delay,
                targetDelay = this.TargetDelay,
                networkLatency = this.NetworkLatency,
                lastSampleLatency = buffer.Last?.Latency ?? 0,
                lastSampleInterval = buffer.Count > 1 ? buffer.Last.Value.Time - buffer[buffer.Count - 2].Time : 0,
                measuredSampleInterval = this.MeasuredSampleInterval
            };

            // Retrieve adjacent samples
            var adjecentSamples = buffer.GetAdjacentSamples(time, ignoreVirtualSamples);

            // no samples in the buffer
            if (buffer.Count == 0)
            {
                result.isStopped = true;
                return result;
            }

            result.sample0 = adjecentSamples.Sample0;
            result.sample1 = adjecentSamples.Sample1;
            result.sample2 = adjecentSamples.Sample2;
            result.sample3 = adjecentSamples.Sample3;

            var sampleDeltaTime = adjecentSamples.Sample2.Time - adjecentSamples.Sample1.Time;

            if (sampleDeltaTime == 0)
            {
                result.t = 0;
                result.isStopped = adjecentSamples.IsLastSample;
                return result;
            }

            // Calculate the time fraction
            var timePassed = time - adjecentSamples.Sample1.Time;

            var stopped = adjecentSamples.Sample2.Stopped;

            result.t = (float)(timePassed / sampleDeltaTime);

            // Clamp t to min 0
            result.t = Mathf.Max(result.t, 0f);

            // received sample stop mark
            if (stopped)
            {
                result.t = Mathf.Min(result.t, 1f);
            }

            if (stopped && adjecentSamples.IsLastSample && result.t == 1)
            {
                result.isStopped = true;
            }

            if (buffer.VirtualSamples.HasValue && buffer.Count > 0)
            {
                result.virtualOvershoot = buffer.VirtualSamples.Value.Second.Time - buffer.Last.Value.Time;
            }

            return result;
        }
    }

    public struct InterpolationResult<T>
    {
        public Sample<T> sample0;
        public Sample<T> sample1;
        public Sample<T> sample2;
        public Sample<T> sample3;
        public float t;

        public double delay;
        public double targetDelay;
        public double networkLatency;
        public double lastSampleLatency;
        public double lastSampleInterval;
        public double measuredSampleInterval;
        public bool isStopped;
        public double virtualOvershoot;

        public T value0 => sample0.Value;
        public T value1 => sample1.Value;
        public T value2 => sample2.Value;
        public T value3 => sample3.Value;

        public override string ToString()
        {
            return $"1:{sample1} 2:{sample2} t:{t} delay:{delay} targetDelay:{targetDelay} networkLatency{networkLatency} measuredSampleInterval:{measuredSampleInterval} isStopped:{isStopped}";
        }
    }
}
