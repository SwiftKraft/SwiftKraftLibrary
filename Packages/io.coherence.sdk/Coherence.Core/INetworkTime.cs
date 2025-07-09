// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence
{
    using System;
    using Coherence.SimulationFrame;

    public interface INetworkTime
    {
        /// <inheritdoc cref="Core.NetworkTime.OnTimeReset"/>
        event Action OnTimeReset;

        /// <inheritdoc cref="Core.NetworkTime.OnFixedNetworkUpdate"/>
        event Action OnFixedNetworkUpdate;

        /// <inheritdoc cref="Core.NetworkTime.OnLateFixedNetworkUpdate"/>
        event Action OnLateFixedNetworkUpdate;

        /// <inheritdoc cref="Core.NetworkTime.OnServerSimulationFrameReceived"/>
        event Action<AbsoluteSimulationFrame, AbsoluteSimulationFrame> OnServerSimulationFrameReceived;

        /// <inheritdoc cref="Core.NetworkTime.TimeAsDouble"/>
        double TimeAsDouble { get; }

        /// <inheritdoc cref="Core.NetworkTime.SessionTime"/>
        float SessionTime { get; }

        /// <inheritdoc cref="Core.NetworkTime.SessionTimeAsDouble"/>
        double SessionTimeAsDouble { get; }

        /// <inheritdoc cref="Core.NetworkTime.NetworkTimeScale"/>
        float NetworkTimeScale { get; }

        /// <inheritdoc cref="Core.NetworkTime.NetworkTimeScaleAsDouble"/>
        double NetworkTimeScaleAsDouble { get; }

        /// <inheritdoc cref="Core.NetworkTime.TargetTimeScale"/>
        double TargetTimeScale { get; }

        /// <inheritdoc cref="Core.NetworkTime.FixedTimeStep"/>
        double FixedTimeStep { get; set; }

        /// <inheritdoc cref="Core.NetworkTime.MaximumDeltaTime"/>
        public double MaximumDeltaTime { get; set; }

        /// <inheritdoc cref="Core.NetworkTime.MultiClientMode"/>
        bool MultiClientMode { get; set; }

        /// <inheritdoc cref="Core.NetworkTime.AccountForPing"/>
        bool AccountForPing { get; set; }

        /// <inheritdoc cref="Core.NetworkTime.SmoothTimeScaleChange"/>
        bool SmoothTimeScaleChange { get; set; }

        /// <inheritdoc cref="Core.NetworkTime.Pause"/>
        bool Pause { get; set; }

        /// <inheritdoc cref="Core.NetworkTime.IsTimeSynced"/>
        bool IsTimeSynced { get; }

        /// <inheritdoc cref="Core.NetworkTime.ClientSimulationFrame"/>
        AbsoluteSimulationFrame ClientSimulationFrame { get; }

        /// <inheritdoc cref="Core.NetworkTime.ClientFixedSimulationFrame"/>
        AbsoluteSimulationFrame ClientFixedSimulationFrame { get; }

        /// <inheritdoc cref="Core.NetworkTime.ServerSimulationFrame"/>
        AbsoluteSimulationFrame ServerSimulationFrame { get; }

        /// <inheritdoc cref="Core.NetworkTime.ConnectionSimulationFrame"/>
        AbsoluteSimulationFrame ConnectionSimulationFrame { get; }

        /// <inheritdoc cref="Core.NetworkTime.Step"/>
        void Step(double currentTime, bool stopApplyingServerSimFrame);

        /// <inheritdoc cref="Core.NetworkTime.Reset"/>
        void Reset(AbsoluteSimulationFrame newClientAndServerFrame = default, bool notify = true);
    }
}
