// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common
{
    using System;

    /// <summary>
    ///     Settings that enable fine-tuning of the server connection.
    /// </summary>
    [System.Serializable]
    public partial class ConnectionSettings
    {
        [Obsolete("use Brisk.DefaultMTU instead.")]
        public const ushort DEFAULT_MTU = 1280;

        public static ConnectionSettings Default => new ConnectionSettings();

        public bool UseDebugStreams;

        /// <inheritdoc cref="PingSettings"/>
        public PingSettings Ping = PingSettings.Default;

        /// <summary>
        ///     Amount of time allowed to pass without receiving data from server before disconnecting.
        /// </summary>
        public float DisconnectTimeoutMilliseconds
        {
            get => disconnectTimeoutMilliseconds;
            set => disconnectTimeoutMilliseconds = Math.Max(value, 0);
        }

        /// <inheritdoc cref="DisconnectTimeoutMilliseconds"/>
        public TimeSpan DisconnectTimeout
        {
            get => TimeSpan.FromMilliseconds(DisconnectTimeoutMilliseconds);
            set => disconnectTimeoutMilliseconds = (float)value.TotalMilliseconds;
        }

        public ushort Mtu
        {
            get => maximumTransmissionUnit;
            set => maximumTransmissionUnit = value;
        }

        private float disconnectTimeoutMilliseconds = 5000;

        private ushort maximumTransmissionUnit = 1280;
    }
}
