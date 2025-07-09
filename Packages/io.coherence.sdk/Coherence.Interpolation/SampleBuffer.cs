// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Interpolation
{
    using Coherence.SimulationFrame;
    using Log;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;
    using Logger = Log.Logger;

    /// <summary>
    ///     SampleBuffer stores sample points in a circular buffer.
    ///     The buffer is backed by an array that currently cannot be resized.
    ///     The buffer is guaranteed to be ever increasing, i.e. this[n].Time > this[n-1].
    /// </summary>
    /// <typeparam name="T">The sample type (e.g., float, int, Vector3) corresponding to the value type of the binding.</typeparam>
    public class SampleBuffer<T> : IEnumerable<Sample<T>>
    {
        private const int initialCapacity = 20;
        private const int sampleCountForMeasuringInterval = 5;
        private const int sampleCountForMeasuringLatency = 5;

        private readonly Logger logger = Log.GetLogger(typeof(SampleBuffer<>));

        private Sample<T>[] data;
        private int head;
        private int tail;

        /// <summary>
        /// Set by <see cref="BindingInterpolator{T}.SetVirtualSamples(Sample{T})"/>.
        /// </summary>
        public (Sample<T> First, Sample<T> Second)? VirtualSamples { get; set; }

        /// <summary>
        ///     Creates an empty SampleBuffer.
        /// </summary>
        public SampleBuffer()
        {
            data = new Sample<T>[initialCapacity];
        }

        /// <summary>
        ///     The maximum number of samples in the buffer.
        /// </summary>
        public int Capacity => data.Length;

        /// <summary>
        ///     The current number of samples in the buffer.
        /// </summary>
        public int Count { get; private set; }

        public Sample<T> this[int index]
        {
            get => data[(index + tail) % Capacity];
            set => data[(index + tail) % Capacity] = value;
        }

        public Sample<T>? Last
        {
            get => Count == 0 ? null : this[Count - 1];
            private set => this[Count - 1] = value.Value;
        }

        public IEnumerator<Sample<T>> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void SetLast(Sample<T> value)
        {
            if (Count == 0)
            {
                PushFront(value);
            }
            else
            {
                Last = value;
            }
        }

        /// <summary>
        ///     Adds a new sample to the front of the buffer.
        ///     If the buffer <see cref="Capacity" /> has already been reached, the oldest sample is removed.
        ///     Samples should always be added with increasing timestamps.
        ///     If multiple samples with identical timestamps are added, only the first sample is added and the rest are discarded.
        ///     This ensures that buffer times are always incrementing.
        /// </summary>
        /// <exception cref="ArgumentException">
        ///     Throws an ArgumentException if the new sample time is older than the latest sample
        ///     in the buffer.
        /// </exception>
        /// <param name="sample">The new sample to add to the buffer.</param>
        public void PushFront(Sample<T> sample)
        {
            if (Capacity == 0)
            {
                return;
            }

            if (Count > 0)
            {
                // This may happen after authority transfer
                var lastSampleTime = Last.Value.Time;
                if (lastSampleTime >= sample.Time)
                {
                    logger.Debug(
                        $"Out-of-order sample. Last: @{lastSampleTime} ({Last.Value.Value}), Got: @{sample.Time} ({sample.Value})");

                    // Overwrite last sample with a value of the new one while keeping the timestamp
                    Last = new Sample<T>(sample.Value, sample.Stopped, lastSampleTime, sample.Latency);
                    return;
                }
            }

            // Increase buffer size if it's full
            if (Count >= Capacity)
            {
                GrowBuffer();
            }

            data[head] = sample;
            head = (head + 1) % Capacity;

            Count++;
        }

        /// <summary>
        ///     Removes the oldest sample in the buffer.
        /// </summary>
        /// <returns>The sample that was removed.</returns>
        public Sample<T>? PopBack()
        {
            if (Count == 0)
            {
                return null;
            }

            var sample = data[tail];
            data[tail] = default;
            tail = (tail + 1) % Capacity;
            Count--;

            return sample;
        }

        /// <summary>
        ///     Searches the buffer for the two samples adjacent to the given time.
        ///     Returns (default, default, -1) if the buffer is empty.
        ///     Returns (sample[0], sample[0], 0) if the buffer contains less than two samples - or if time pre-dates the first
        ///     sample.
        ///     Otherwise, it returns the sample before (or equal to) the given time, and the next sample in the buffer.
        ///     IsLastSample is true if the second returned sample is the last sample in the buffer.
        /// </summary>
        /// <param name="time">The time used to compare with sample times in the buffer.</param>
        /// <param name="ignoreVirtualSamples">If false and virtual samples are present, it will return them as sample1 and sample2,
        ///     else it will ignore virtual samples and return only real.</param>
        public AdjecentSamplesResult GetAdjacentSamples(double time, bool ignoreVirtualSamples)
        {
            if (!ignoreVirtualSamples && VirtualSamples.HasValue)
            {
                var previousIndex = GetLastSampleIndexBefore(VirtualSamples.Value.First.Time);
                var sample0 = previousIndex == -1 ? VirtualSamples.Value.First : this[previousIndex];
                var nextIndex = GetFirstSampleIndexAfter(VirtualSamples.Value.Second.Time);
                var sample3 = nextIndex == -1 ? VirtualSamples.Value.Second : this[nextIndex];

                return new AdjecentSamplesResult(sample0, VirtualSamples.Value.First, VirtualSamples.Value.Second, sample3, nextIndex == -1);
            }

            switch (Count)
            {
                case 0:
                    logger.Warning(Warning.InterpolationSampleBufferJitter);
                    return new AdjecentSamplesResult(default, default, default, default, true);
                case 1:
                    return new AdjecentSamplesResult(this[0], this[0], this[0], this[0], true);
            }

            // Find the last sample before current frame
            var index = GetLastSampleIndexBefore(time);

            if (index == -1)
            {
                return new AdjecentSamplesResult(this[0], this[0], this[0], this[ClampIndex(1)], Count == 1);
            }

            if (index == Count - 1 && index > 0)
            {
                // If it is the very last sample, rewind one step to return the last two samples in the buffer
                index--;
            }

            // Return last sample before time and the next sample
            return new AdjecentSamplesResult(this[ClampIndex(index - 1)], this[index], this[index + 1], this[ClampIndex(index + 2)], index == Count - 2);
        }

        private int ClampIndex(int index)
        {
            return Mathf.Clamp(index, 0, Count - 1);
        }

        /// <summary>
        ///     Returns the last sample index before (or equal to) the given time.
        ///     Returns -1 if the buffer contains no sample before (or equal to) the given time.
        /// </summary>
        /// <param name="time">The given time.</param>
        /// <returns>The index of the last sample at (or equal to) the given time.</returns>
        private int GetLastSampleIndexBefore(double time)
        {
            // Return null if the buffer is empty
            if (Count == 0)
            {
                return -1;
            }

            // Return null if time pre-dates the first sample in the buffer
            if (time < this[0].Time)
            {
                return -1;
            }

            for (var i = 0; i < Count - 1; i++)
            {
                if (this[i].Time <= time && time < this[i + 1].Time)
                {
                    return i;
                }
            }

            return Count - 1;
        }

        /// <summary>
        ///     Returns the first sample index after the given time.
        ///     Returns -1 if the buffer contains no sample after the given time.
        /// </summary>
        /// <param name="time">The given time.</param>
        /// <returns>The index of the first sample after the given time.</returns>
        private int GetFirstSampleIndexAfter(double time)
        {
            // Return null if the buffer is empty
            if (Count == 0)
            {
                return -1;
            }

            // Return null if time is after (or equal to) the last sample in the buffer
            if (time >= Last.Value.Time)
            {
                return -1;
            }

            for (var i = 0; i < Count; i++)
            {
                if (this[i].Time > time)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        ///     Removes outdated samples from the samples buffer.
        ///     A sample is considered outdated if its timestamp is equal to or older than the given time.
        ///     The number of samples left guarantees that both interpolation and extrapolation can happen.
        /// </summary>
        /// <param name="time">
        ///     The current time used to determine which samples are outdated.
        ///     Usually <see cref="InterpolationSettings.Time" />.
        /// </param>
        /// <param name="numberOfSamplesToStayBehind">
        ///     The <see cref="Interpolator.NumberOfSamplesToStayBehind" /> of
        ///     the <see cref="Interpolator" /> used for this binding.
        /// </param>
        public void RemoveOutdatedSamples(double time, int numberOfSamplesToStayBehind)
        {
            if (VirtualSamples.HasValue)
            {
                var secondVirtualSampleTime = VirtualSamples.Value.Second.Time;
                if (Last.HasValue && Last.Value.Time > secondVirtualSampleTime && time > secondVirtualSampleTime)
                {
                    VirtualSamples = null;
                }
            }

            // For latest sample interpolation, simply remove all but the latest sample
            // If the last sample is stopped and we passed it, just keep the last sample
            if (numberOfSamplesToStayBehind == 0 || Last.HasValue && Last.Value.Stopped && time >= Last.Value.Time)
            {
                RemoveAllButLastSample();

                return;
            }

            // Extrapolation requires additional samples in the past.
            // There are 3 cases:
            // 1. Latest sample interpolation - only one sample is needed which is handled by the code above
            // 2. Linear - requires a minimum of two samples behind for extrapolation
            // 3. Spline - requires a minimum of three samples behind for extrapolation
            var numberOfSamplesToKeep = numberOfSamplesToStayBehind + 1;
            var sampleIndex = numberOfSamplesToStayBehind;

            while (Count > numberOfSamplesToKeep)
            {
                // As soon as we encounter future sample we stop
                bool isFutureSample = this[sampleIndex].Time > time;
                if (isFutureSample)
                {
                    break;
                }

                // This shifts the collection hence no need to increment sampleIndex
                PopBack();
            }
        }

        /// <summary>
        ///     Finds the second largest duration between two samples frames in the last <see cref="sampleCountForMeasuringInterval"/> samples.
        ///     It also stops measuring when it reaches a stopped sample.
        ///     Used for measuring real SampleRate.
        /// </summary>
        /// <returns>True if there are enough samples to measure the interval, with
        /// <paramref name="measuredSampleInterval"/> set to maximum duration between two adjecent samples.</returns>
        public bool TryMeasureSampleInterval(out double measuredSampleInterval)
        {
            measuredSampleInterval = 0;

            double max = -1;
            double secondMax = -1;

            //measuredSampleInterval = 0;
            var hasInterval = false;

            // limit the number of samples to `sampleCountForMeasuringInterval` because our buffer is resizable,
            // and there is no need to take into account more than `sampleCountForMeasuringInterval` samples
            // because this variable is only important if we are interpolating between the last few samples
            var sampleCount = Math.Min(Count, sampleCountForMeasuringInterval);

            for (var i = 0; i < sampleCount - 1; i++)
            {
                var sample1 = this[Count - i - 2];
                var sample2 = this[Count - i - 1];

                if (sample1.Stopped)
                {
                    break;
                }

                if (sample1.Frame == AbsoluteSimulationFrame.Invalid || sample2.Frame == AbsoluteSimulationFrame.Invalid)
                {
                    continue;
                }

                var duration = (sample2.Frame - sample1.Frame) /
                               InterpolationSettings.SimulationFramesPerSecond;

                hasInterval = true;

                if (duration > max)
                {
                    secondMax = max;
                    max = duration;
                }
                else if (duration > secondMax)
                {
                    secondMax = duration;
                }
            }

            if (hasInterval)
            {
                if (secondMax > 0)
                {
                    measuredSampleInterval = secondMax;
                }
                else
                {
                    measuredSampleInterval = max;
                }
            }

            return hasInterval;
        }

        /// <summary>
        ///     Calculates the average sample latency in the last <see cref="sampleCountForMeasuringLatency"/> samples,
        ///     ignoring the highest latency (if there are multiple samples).
        /// </summary>
        /// <returns>True if there are enough samples to measure the latency, with
        /// <paramref name="measuredNetworkLatency"/> set to average latency.</returns>
        public bool TryMeasureSampleLatency(out double measuredNetworkLatency)
        {
            measuredNetworkLatency = 0;

            double networkLatencySum = 0;
            var networkLatencyCount = 0;

            var max = -1.0;

            var sampleCount = Math.Min(Count, sampleCountForMeasuringLatency);

            for (var i = 0; i < sampleCount; i++)
            {
                var sample = this[Count - i - 1];

                if (sample.Stopped)
                {
                    break;
                }

                if (sample.Latency.HasValue)
                {
                    networkLatencySum += sample.Latency.Value;
                    networkLatencyCount++;

                    if (sample.Latency.Value > max)
                    {
                        max = sample.Latency.Value;
                    }
                }
            }

            if (max != -1 && networkLatencyCount > 1)
            {
                networkLatencySum -= max;
                networkLatencyCount--;
            }

            if (networkLatencyCount == 0)
            {
                return false;
            }

            measuredNetworkLatency = networkLatencySum / networkLatencyCount;

            return true;
        }

        public void Reset()
        {
            Count = 0;
            head = tail = 0;
        }

        /// <summary>
        ///     Increase buffer capacity by <see cref="capacityGrowFactor" />.
        /// </summary>
        private void GrowBuffer()
        {
            var oldCapacity = Capacity;
            var newCapacity = Mathf.CeilToInt((Count + 1) / (float)initialCapacity) * initialCapacity;

            if (newCapacity <= oldCapacity)
            {
                return;
            }

            Array.Resize(ref data, newCapacity);

            if (tail >= head && Count > 0) // loops around
            {
                for (var i = 0; i < oldCapacity - tail; i++)
                {
                    data[newCapacity - i - 1] = data[oldCapacity - i - 1];
                }

                tail += newCapacity - oldCapacity;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("[SampleBuffer ");

            if (VirtualSamples == null)
            {
                sb.Append("{No virtual samples} ");
            }
            else
            {
                sb.Append("{Virtual samples: ");
                sb.Append(VirtualSamples.Value.First);
                sb.Append(", ");
                sb.Append(VirtualSamples.Value.Second);
                sb.Append("} ");
            }

            sb.Append("[");

            if (Count == 0)
            {
                sb.Append("{empty}");
            }
            else
            {
                for (var i = 0; i < Count; i++)
                {
                    sb.Append(this[i]);
                    if (i < Count - 1)
                    {
                        sb.Append(", ");
                    }
                }
            }

            sb.Append("]]");

            return sb.ToString();
        }

        private void RemoveAllButLastSample()
        {
            while (Count > 1)
            {
                PopBack();
            }
        }

        public struct AdjecentSamplesResult
        {
            public Sample<T> Sample0;
            public Sample<T> Sample1;
            public Sample<T> Sample2;
            public Sample<T> Sample3;
            public bool IsLastSample;

            public AdjecentSamplesResult(Sample<T> sample0, Sample<T> sample1, Sample<T> sample2, Sample<T> sample3, bool isLastSample)
            {
                Sample0 = sample0;
                Sample1 = sample1;
                Sample2 = sample2;
                Sample3 = sample3;
                IsLastSample = isLastSample;
            }
        }
    }
}
