// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Interpolation
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "InterpolationSettings.asset", menuName = "coherence/Interpolation Settings")]
    public class InterpolationSettings : ScriptableObject
    {
        public static readonly string EmptyInterpolationName = "None";
        public static readonly string DefaultInterpolationName = "Interpolation";
        public const double SimulationFramesPerSecond = 60;
        public const float DefaultSampleRate = 20;
        public const float DefaultMaxOvershootAllowed = 5f;

        /// <summary>
        /// Asset path to the default interpolation settings.
        /// </summary>
        public static readonly string DefaultSettingsPath = "Packages/io.coherence.sdk/Coherence.Interpolation/Default Interpolation.asset";

        /// <summary>
        /// Reference to the <see cref="Interpolator"/> implementation that performs blending between samples.
        /// By default, latest sample interpolation is used, which simply snaps the value to the latest sample.
        /// </summary>
        [SerializeReference] public Interpolator interpolator = new LinearInterpolator();

        /// <summary>
        /// <see cref="SmoothingSettings"/> is applied after the <see cref="Interpolator"/> blends values from samples around the current <see cref="Time"/>.
        /// This is useful to smooth out jerky movement, e.g., when moving into dead reckoning.
        /// </summary>
        public SmoothingSettings smoothing = SmoothingSettings.Default;

        /// <summary>
        /// LatencySettings provides parameters used to tweak interpolation <see cref="Delay"/>.
        /// </summary>
        public LatencySettings latencySettings = LatencySettings.Default;

        /// <summary>
        /// If any two consecutive samples exceed the maximum distance, the buffer is cleared to teleport to the latest sample.
        /// </summary>
        [Tooltip("If any two consecutive samples exceed the maximum distance, the buffer is cleared to teleport to the latest sample.")]
        public float maxDistance;

        /// <summary>
        /// Determines how far the interpolation is allowed to enter into overshooting.
        /// Overshooting occurs when samples do not arrive within the expected time.
        /// A value of 1 means the binding is allowed to extrapolate 1 sample into the future.
        /// </summary>
        [Tooltip("Max number of samples to proceed into extrapolation after overshooting the final sample in the buffer.")]
        public float maxOvershootAllowed = DefaultMaxOvershootAllowed;

        private static InterpolationSettings empty;

        /// <summary>
        /// Stores a singleton InterpolationSettings created with <see cref="CreateEmpty"/>. This is used so bindings can share a
        /// single instance of the settings, thus it shouldn't be modified.
        /// </summary>
        public static InterpolationSettings Empty => empty != null ? empty : empty = CreateEmpty();

        /// <summary>
        /// Creates an empty interpolation object which simply snaps to the latest sample value without any blending.
        /// </summary>
        public static InterpolationSettings CreateEmpty()
        {
            var interpolationSettings = CreateInstance<InterpolationSettings>();

            interpolationSettings.interpolator = new Interpolator();
            interpolationSettings.name = EmptyInterpolationName;
            interpolationSettings.latencySettings = LatencySettings.Empty;
            interpolationSettings.smoothing = SmoothingSettings.Empty;
            interpolationSettings.maxOvershootAllowed = 0;

            return interpolationSettings;
        }

        /// <summary>
        /// Creates a default interpolation object that uses linear interpolation.
        /// </summary>
        public static InterpolationSettings CreateDefault()
        {
            var interpolationSettings = CreateInstance<InterpolationSettings>();

            interpolationSettings.interpolator = new LinearInterpolator();
            interpolationSettings.name = DefaultInterpolationName;
            interpolationSettings.latencySettings = LatencySettings.Default;
            interpolationSettings.smoothing = SmoothingSettings.Default;
            interpolationSettings.maxOvershootAllowed = DefaultMaxOvershootAllowed;

            return interpolationSettings;
        }

        /// <summary>
        /// Returns true if the settings is the "None" interpolation mode.
        /// </summary>
        public bool IsInterpolationNone
        {
            get
            {
                if (!isInterpolationNone.HasValue)
                {
                    isInterpolationNone = name == EmptyInterpolationName;
                }

                return isInterpolationNone.Value;
            }
        }
        private bool? isInterpolationNone;
    }
}
