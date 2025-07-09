// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if COHERENCE_FEATURE_NATIVE_CORE
namespace Coherence.Core
{
    using System;
    using Coherence.SimulationFrame;

    internal class NativeNetworkTime : INetworkTime
    {
        public event Action OnTimeReset;
        public event Action OnFixedNetworkUpdate;
        public event Action OnLateFixedNetworkUpdate;
        public event Action<AbsoluteSimulationFrame, AbsoluteSimulationFrame> OnServerSimulationFrameReceived;

        public double TimeAsDouble => core.GetNetworkTime();

        public float SessionTime => (float)core.GetSessionTime();

        public double SessionTimeAsDouble => core.GetSessionTime();

        public float NetworkTimeScale => (float)core.GetNetworkTimeScale();

        public double NetworkTimeScaleAsDouble => core.GetNetworkTimeScale();

        public double TargetTimeScale => core.GetTargetNetworkTimeScale();

        public double FixedTimeStep
        {
            get => core.GetNetworkTimeFixedTimeStep();
            set => core.SetNetworkTimeFixedTimeStep(value);
        }

        public double MaximumDeltaTime
        {
            get => core.GetNetworkTimeMaximumDeltaTime();
            set => core.SetNetworkTimeMaximumDeltaTime(value);
        }

        public bool MultiClientMode
        {
            get => core.GetNetworkTimeMultiClientMode();
            set => core.SetNetworkTimeMultiClientMode(value);
        }

        public bool AccountForPing
        {
            get => core.GetNetworkTimeAccountForPing();
            set => core.SetNetworkTimeAccountForPing(value);
        }

        public bool SmoothTimeScaleChange
        {
            get => core.GetNetworkTimeSmoothTimeScaleChange();
            set => core.SetNetworkTimeSmoothTimeScaleChange(value);
        }

        public bool Pause
        {
            get => core.GetNetworkTimePause();
            set => core.SetNetworkTimePause(value);
        }

        public bool IsTimeSynced => core.IsNetworkTimeSynced();

        public AbsoluteSimulationFrame ClientSimulationFrame => core.GetNetworkTimeClientSimulationFrame();

        public AbsoluteSimulationFrame ClientFixedSimulationFrame => core.GetNetworkTimeClientFixedSimulationFrame();

        public AbsoluteSimulationFrame ServerSimulationFrame => core.GetNetworkTimeServerSimulationFrame();

        public AbsoluteSimulationFrame ConnectionSimulationFrame => core.GetNetworkTimeConnectionSimulationFrame();

        private readonly NativeCore core;

        public NativeNetworkTime(NativeCore core)
        {
            this.core = core;
        }

        public void Reset(AbsoluteSimulationFrame newClientAndServerFrame = default, bool notify = true)
        {
            core.ResetNetworkTime(new InteropAbsoluteSimulationFrame(newClientAndServerFrame), notify);
        }

        public void Step(double currentTime, bool stopApplyingServerSimFramebool)
        {
            core.StepNetworkTime(currentTime);
        }

        internal void OnTimeResetCallback()
        {
            OnTimeReset?.Invoke();
        }

        internal void OnFixedNetworkUpdateCallback()
        {
            OnFixedNetworkUpdate?.Invoke();
        }

        internal void OnLateFixedNetworkUpdateCallback()
        {
            OnLateFixedNetworkUpdate?.Invoke();
        }

        internal void OnServerSimulationFrameReceivedCallback(InteropAbsoluteSimulationFrame serverSimulationFrame, InteropAbsoluteSimulationFrame clientSimulationFrame)
        {
            OnServerSimulationFrameReceived?.Invoke(serverSimulationFrame.Into(), clientSimulationFrame.Into());
        }
    }
}
#endif
