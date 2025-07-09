// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common
{
    using System;

    public partial class ConnectionSettings
    {
        /// <summary>
        ///     Settings related to time syncing (ping calculation).
        /// </summary>
        [Serializable]
        public class PingSettings
        {
            public static PingSettings Default => new PingSettings();

            /// <summary>
            ///     The minimum number of samples required for a ping to be considered stable. The
            ///     higher the number the more accurate ping value will be.
            /// </summary>
            public int MinSamplesForStability { get; set; } = 3;

            /// <summary>
            ///     The maximum standard deviation in ping that is considered stable.
            /// </summary>
            public int MaxStablePingDeviation { get; set; } = 10 + 7; // 7 is expected ping deviation because of RS default send frequency

            /// <summary>
            ///     The maximum number of time sync samples that is used for calculating the average.
            /// </summary>
            public int MaxSamples { get; set; } = 10;
        }
    }
}
