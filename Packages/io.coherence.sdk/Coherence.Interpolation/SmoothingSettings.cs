// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

using System;
using UnityEngine;

namespace Coherence.Interpolation
{
    [Serializable]
    public struct SmoothingSettings
    {
        public static readonly SmoothingSettings Default = new SmoothingSettings
        {
            smoothTime = 0.05f,
            maxSpeed = 0
        };

        public static readonly SmoothingSettings Empty = new SmoothingSettings
        {
            smoothTime = 0,
            maxSpeed = 0
        };

        /// <summary>
        /// Seconds to remain behind the current interpolation point.
        /// Applied using Mathf.SmoothDamp (or Mathf.SmoothDampAngle for Quaternions).
        /// </summary>
        /// <seealso cref="Mathf.SmoothDamp(float,float,ref float,float,float)"/>
        /// <seealso cref="Mathf.SmoothDampAngle(float,float,ref float,float,float)"/>
        [Tooltip("Seconds to remain behind the current interpolation point. Applied using Mathf.SmoothDamp (or Mathf.SmoothDampAngle for Quaternions).")]
        [Range(0, 1)] public float smoothTime;

        /// <summary>
        /// Maximum SmoothDamp speed allowed. Zero means no maximum is imposed.
        /// </summary>
        /// <seealso cref="Mathf.SmoothDamp(float,float,ref float,float,float)"/>
        [Tooltip("Maximum SmoothDamp speed allowed. Zero means no maximum is imposed.")]
        public float maxSpeed;
    }
}
