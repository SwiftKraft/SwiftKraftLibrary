// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Interpolation
{
    using System;
    using UnityEngine;
    
    [Serializable]
    public struct LatencySettings
    {
        public static readonly LatencySettings Default = new LatencySettings
        {
            networkLatencyFactor = 1f,
            additionalLatency = 0,
        };

        public static readonly LatencySettings Empty = new LatencySettings
        {
            networkLatencyFactor = 0,
            additionalLatency = 0,
        };
        
        /// <summary>
        /// Multiplier for the incoming network latency.
        /// A value of 1 will tune <see cref="InterpolationSettings.Delay"/> to the exact time an incoming sample is expected with zero headroom for network jitter. 
        /// </summary>
        [Tooltip("Network latency delay scale factor")]
        [Range(1,10)] public float networkLatencyFactor;
        
        /// <summary>
        /// Additional fixed latency added into the total <see cref="InterpolationSettings.Delay"/> to provide headroom for network jitter, in seconds.  
        /// </summary>
        [Tooltip("Additional fixed latency (seconds)")]
        [Range(0,10)] public float additionalLatency;
    }
}
