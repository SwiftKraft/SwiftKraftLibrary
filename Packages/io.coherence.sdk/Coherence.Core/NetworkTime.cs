// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Core
{
    using System;
    using Log;
    using SimulationFrame;
    using Ping = Common.Ping;

    /// <summary>
    /// NetworkTime encapsulates state and functionality for client-server synchronization.
    /// <see cref="Step" /> should be called during Update and FixedUpdate to increment the ClientSimulationFrame.
    /// NetworkTime can operate in two different modes: normal mode and multi-client mode.
    /// In normal mode, the NetworkTimeScale should be applied to Time.timeScale each frame.
    /// This will affect the rate at which Unity triggers FixedUpdate on all game objects, adjusting the speed of the simulation to smoothly catch up with the server clock.
    /// This works well for most applications, however, since Time.timeScale is a global property, it cannot be used to synchronize multiple clients within the same application.
    /// For these scenarios, you can use the MultiClientMode which makes NetworkTime operate differently.
    /// With MultiClientMode enabled, <see cref="Step" /> will apply NetworkTimeScale internally when incrementing simulation frames and when invoking the <see cref="OnFixedNetworkUpdate" />.
    /// In order to adapt the simulation speed in multi-client mode, all MonoBehaviour simulation code should be moved from FixedUpdate into OnFixedNetworkUpdate event handlers.
    /// When working with multiple networked scenes with multi-client mode, it is also important to trigger PhysicsScene.Simulate for each scene from their respective <see cref="OnFixedNetworkUpdate" />.
    /// </summary>
    public class NetworkTime : INetworkTime
    {
        /// <summary>
        /// Monotonic clock that increases with each call to <see cref="Step" /> regardless if connected to a replication server or not.
        /// Value will jump considerably during connect, disconnect and time reset as client network time adapts to server time, see <see cref="Reset" />.
        /// </summary>
        public double TimeAsDouble => ClientSimulationFrame * timeStep + accumulatedTime;

        /// <summary>
        /// Monotonic clock that only increases when connected to a replication server and that resets back to zero on disconnect.
        /// Value will jump considerably during time reset as client network time adapts to server time, see <see cref="Reset" />.
        /// </summary>
        public float SessionTime => (float)SessionTimeAsDouble;
        /// <summary>
        /// Similar to <see cref="SessionTime" /> but with double precision.
        /// </summary>
        public double SessionTimeAsDouble => IsTimeSynced ? (ClientSimulationFrame - ConnectionSimulationFrame) * timeStep + accumulatedTime : 0;

        /// <summary>
        /// The recommended time scale that should be applied to Time.timeScale for the client clock to smoothly catch up/slow down to server clock.
        /// <see cref="TargetTimeScale" /> is calculated during <see cref="SetServerSimulationFrame" /> based on the current client/server frame diff,
        /// and NetworkTimeScale will smoothly approach this value over time.
        /// </summary>
        public float NetworkTimeScale => (float)NetworkTimeScaleAsDouble;

        /// <summary>
        /// Similar to <see cref="NetworkTimeScale" /> with double precision.
        /// </summary>
        public double NetworkTimeScaleAsDouble { get; private set; } = 1;

        /// <summary>
        /// The time scale that <see cref="NetworkTimeScale"/> is smoothly approaching over time.
        /// When <see cref="SmoothTimeScaleChange"/> is set to false, TargetTimeScale and <see cref="NetworkTimeScale"/> are always equal.
        /// </summary>
        public double TargetTimeScale { get; private set; } = 1f;

        /// <summary>
        /// The rate at which which <see cref="OnFixedNetworkUpdate" /> is invoked. Should normally be set to Time.fixedTimeStep.
        /// If set to zero, <see cref="OnFixedNetworkUpdate" /> will not be invoked.
        /// </summary>
        public double FixedTimeStep { get; set; }

        /// <summary>
        /// Limits time incremented in <see cref="Step"/> in case of cpu spikes. Should normally be set to Unity's <see cref="Time.maximumDeltaTime"/>.
        /// </summary>
        public double MaximumDeltaTime { get; set; }

        /// <summary>
        /// Allows multiple CoherenceBridge instances within one application to maintain independent time scales.
        /// This is useful for testing multiple connections within the Unity editor without making standalone builds.
        /// Instead of adapting Time.timeScale to catch up with server clock, it applies the frequency of <see cref="OnFixedNetworkUpdate" />.
        /// </summary>
        public bool MultiClientMode { get; set; } = false;

        /// <summary>
        /// If true, the server frame will be adjusted by ping, resulting in the client frame matching the server frame as it
        /// is on the Replication Server the moment packet is received. If false, the client frame will aim to match the server
        /// frame as it was at the moment of being sent from the Replication Server.
        /// </summary>
        public bool AccountForPing { get; set; } = false;

        /// <summary>
        /// Enables smoothing time scale changes, i.e. if there is a big gap between client and server simulation frames,
        /// instead of sudden jumps in time scale it will smoothly transition <see cref="NetworkTimeScale" /> towards <see cref="TargetTimeScale" />.
        /// </summary>
        public bool SmoothTimeScaleChange { get; set; } = true;

        /// <summary>
        /// Pauses updating the client simulation frame. When set to true any calls to
        /// <see cref="Step" /> will have no effect on the simulation frame and fixed simulation frame.
        /// </summary>
        public bool Pause { get; set; } = false;

        /// <summary>
        /// IsTimeSynced will be set to true the first time <see cref="SetServerSimulationFrame" /> is called (usually when connecting).
        /// IsTimeSynced will be reset back to false by <see cref="Reset" />.
        /// </summary>
        public bool IsTimeSynced { get; private set; }

        /// <summary>
        /// ClientSimulationFrame is the current network time quantized to 60hz.
        /// It is used to timestamp outgoing packets.
        /// </summary>
        public AbsoluteSimulationFrame ClientSimulationFrame { get; private set; }

        /// <summary>
        /// Similar to <see cref="ClientSimulationFrame" /> but quantized to <see cref="FixedTimeStep"/>.
        /// </summary>
        public AbsoluteSimulationFrame ClientFixedSimulationFrame { get; private set; }

        /// <summary>
        /// The last ServerSimulationFrame received from the replication server.
        /// Used for calculating <see cref="TargetTimeScale"/> in order to synchronize client with server.
        /// </summary>
        public AbsoluteSimulationFrame ServerSimulationFrame { get; private set; }

        /// <summary>
        /// The first ServerSimulationFrame received from the replication server.
        /// Used as a baseline for calculating <see cref="SessionTime"/>.
        /// </summary>
        public AbsoluteSimulationFrame ConnectionSimulationFrame { get; private set; }

        /// <summary>
        /// Triggered the first time <see cref="SetServerSimulationFrame" /> is called and at any subsequent call if
        /// <see cref="ClientSimulationFrame" /> and <see cref="ServerSimulationFrame" /> have drifter too far apart.
        /// </summary>
        public event Action OnTimeReset;

        /// <summary>
        /// With <see cref="MultiClientMode" /> enabled it is recommended to put simulation code in OnFixedNetworkUpdate event handlers instead of FixedUpdate.
        /// This allows multiple connected clients within the application to execute at different frequencies for correct network synchronization.
        /// The Time.fixedDeltaTime should be applied similar to how it is normally used in FixedUpdate.
        /// </summary>
        public event Action OnFixedNetworkUpdate;

        /// <summary>
        /// Similar to <see cref="OnFixedNetworkUpdate" /> but guaranteed to be called later.
        /// </summary>
        public event Action OnLateFixedNetworkUpdate;

        /// <summary>
        /// Triggered in response to <see cref="SetServerSimulationFrame" /> after <see cref="TargetTimeScale" /> has been recalculated.
        /// The first parameter is the server simulation frame.
        /// The second parameter is the client simulation frame.
        /// </summary>
        public event Action<AbsoluteSimulationFrame, AbsoluteSimulationFrame> OnServerSimulationFrameReceived;

        private double accumulatedTime; // Time accumulated with each call to <see cref="Step" />. Resets with every 1/60 second
        private double accumulatedSyncTime; // Time accumulated with each call to <see cref="Step" />. Resets with every FixedTimeStep
        private double previousTime; // Used to calculate delta increment from previous call to <see cref="Step" />
        private double timeScaleVelocity; // Used for time scale smooth damp
        private bool stopApplyingServerSimFrame; // See Step() function parameter description

        private AbsoluteSimulationFrame lastReceivedServerSimulationFrame;
        private Ping lastReceivedPing;

        private const double timeDilationFactor = 1d;    // How many percents to speed up or slow down the game per frame between client and server
        public const double maxTimeScale = 1.50d; // Max percents allowed to speed up the game
        public const double minTimeScale = 0.50d; // Max percents allowed to slow down the game
        public const double timeStep = 1 / 60d; // Constant monotonic clock timestep
        public const double timeStepMs = 1000d * timeStep; // Timestep in milliseconds
        public const double floatingPointTolerance = 0.000001d;
        public const int maxFrameDiffForHoldingTimeScale = 3; // Maximum difference between server and client frames that still allows for holding on to a constant time scale during unstable ping
        public const long simulationFrameResetTreshold = 256;   // Allow client and server drift up to a few seconds before resetting frames and triggering OnTimeReset. This aligns with the entity update max simFrame delta.

        private readonly Logger logger;

        public NetworkTime(Logger logger = null)
        {
            this.logger = logger != null
                ? logger.With<NetworkTime>()
                : Log.GetLogger<NetworkTime>();
        }

        /// <summary>
        /// Updates the <see cref="ClientSimulationFrame" /> using the current game time, advancing one frame every 1/60 second.
        /// The <see cref="NetworkTimeScale" /> is smoothly interpolated towards TargetTimeScale with each call.
        /// With multiClientMode enabled, however, the callback trigger rate is scaled by <see cref="NetworkTimeScale" />.
        /// </summary>
        /// <param name="currentTime">The current game time, usually just Time.time. Must be non-zero for OnFixedNetworkUpdate to be invoked.</param>
        /// <param name="stopApplyingServerSimFrame">Set this to true during frames when a received ServerSimulationFrame shouldn't be applied instantly.
        ///     Setting this to true will apply the last received ServerSimulationFrame.</param>
        public void Step(double currentTime, bool stopApplyingServerSimFrame)
        {
            this.stopApplyingServerSimFrame = stopApplyingServerSimFrame;
            if (!stopApplyingServerSimFrame && ServerSimulationFrame != lastReceivedServerSimulationFrame)
            {
                SetServerSimulationFrame(lastReceivedServerSimulationFrame, lastReceivedPing);
            }

            var deltaTime = currentTime - previousTime;
            if (MaximumDeltaTime > 0)
            {
                deltaTime = Math.Min(deltaTime, MaximumDeltaTime);
            }

            previousTime = currentTime;

            if (Pause)
            {
                return;
            }

            if (deltaTime <= floatingPointTolerance)
            {
                deltaTime = 0;
            }

            UpdateTimeScale(deltaTime);

            // Accumulate time
            if (MultiClientMode)
            {
                accumulatedTime += NetworkTimeScale * deltaTime;
                accumulatedSyncTime += NetworkTimeScale * deltaTime;
            }
            else
            {
                accumulatedTime += deltaTime;
                accumulatedSyncTime += deltaTime;
            }

            // Increment ClientSimulationFrame once every 1/60 seconds
            while (accumulatedTime + floatingPointTolerance >= timeStep)
            {
                ClientSimulationFrame = new AbsoluteSimulationFrame { Frame = ClientSimulationFrame.Frame + 1 };
                accumulatedTime -= timeStep;
                if (accumulatedTime < floatingPointTolerance)
                {
                    accumulatedTime = 0;
                }
            }

            // Increment ClientFixedSimulationFrame and invoke FixedNetworkUpdate event once every FixedTimeStep
            while (accumulatedSyncTime + floatingPointTolerance >= FixedTimeStep && FixedTimeStep > 0)
            {
                ClientFixedSimulationFrame++;
                accumulatedSyncTime -= FixedTimeStep;
                if (accumulatedSyncTime < floatingPointTolerance)
                {
                    accumulatedSyncTime = 0;
                }

                try
                {
                    OnFixedNetworkUpdate?.Invoke();
                }
                catch (Exception handlerException)
                {
                    logger.Error(Error.CoreNetworkTimeExceptionInHandler,
                        ("caller", nameof(OnFixedNetworkUpdate)),
                        ("exception", handlerException));
                }

                try
                {
                    OnLateFixedNetworkUpdate?.Invoke();
                }
                catch (Exception handlerException)
                {
                    logger.Error(Error.CoreNetworkTimeExceptionInHandler,
                        ("caller", nameof(OnLateFixedNetworkUpdate)),
                        ("exception", handlerException));
                }
            }
        }

        /// <summary>
        /// Updates <see cref="ServerSimulationFrame" /> and recalculates <see cref="TargetTimeScale" />.
        /// <see cref="TargetTimeScale" /> will be proportional to the distance from the client simulation frame to the server simulation frame.
        /// The first time this method is called, it will trigger <see cref="Reset" />, causing <see cref="ClientSimulationFrame" /> to reset to <see cref="ServerSimulationFrame" />.
        /// Subsequent calls will only trigger <see cref="Reset" /> if the client frame has drifted away from the server frame by at least <see cref="simulationFrameResetTreshold" /> frames.
        /// </summary>
        private void ApplyServerSimulationFrame(AbsoluteSimulationFrame frame, Ping ping)
        {
            logger.Trace("ApplyServerSimulationFrame", ("frame", frame));

            ServerSimulationFrame = frame;

            if (IsOutOfSync())
            {
                Reset(ServerSimulationFrame);
            }

            if (!IsTimeSynced)
            {
                ConnectionSimulationFrame = frame;  // Set initial server frame that is used to calculate SessionTime
                IsTimeSynced = true;
            }

            CalculateTargetTimeScale(ping);

            OnServerSimulationFrameReceived?.Invoke(ServerSimulationFrame, ClientSimulationFrame);
        }

        /// <summary>
        /// Sets the last received server simulation frame and ping and applies it only if the last Step() call didn't have stopApplyingServerSimFrame.
        /// Else, that frame (and ping) will be applied on the next Step() call when stopApplyingServerSimFrame is false.
        /// </summary>
        public void SetServerSimulationFrame(AbsoluteSimulationFrame frame, Ping ping)
        {
            logger.Trace("SetServerSimulationFrame", ("frame", frame));

            this.lastReceivedServerSimulationFrame = frame;
            this.lastReceivedPing = ping;

            if (!stopApplyingServerSimFrame)
            {
                ApplyServerSimulationFrame(frame, ping);
            }
        }

        /// <summary>
        /// Resets client and server frames to the given value. Happens on connect, disconnect and when client/server frames are too far apart.
        /// Sets <see cref="IsTimeSynced" /> to false, causing <see cref="SessionTime" /> to reset back to zero.
        /// Triggers <see cref="OnTimeReset" />.
        /// <param name="newClientAndServerFrame">The new frame value that will be applied to both <see cref="ClientSimulationFrame" />  and <see cref="ServerSimulationFrame" />. Default is frame zero.</param>
        /// </summary>
        public void Reset(AbsoluteSimulationFrame newClientAndServerFrame = default, bool notify = true)
        {
            if (notify)
            {
                logger.Debug("Time reset",
                    ("clientFrame", ClientSimulationFrame),
                    ("serverFrame", ServerSimulationFrame),
                    ("clientFixedFrame", ClientFixedSimulationFrame),
                    ("newServerFrame", newClientAndServerFrame));
            }

            IsTimeSynced = false;

            var time = newClientAndServerFrame * timeStep;
            var fixedFrame = (long)(time / FixedTimeStep);

            ServerSimulationFrame = newClientAndServerFrame;
            ClientSimulationFrame = newClientAndServerFrame;
            ClientFixedSimulationFrame = new AbsoluteSimulationFrame { Frame = fixedFrame };

            lastReceivedServerSimulationFrame = newClientAndServerFrame;

            if (notify)
            {
                try
                {
                    OnTimeReset?.Invoke();
                }
                catch (Exception handlerException)
                {
                    logger.Error(Error.CoreNetworkTimeExceptionInHandler,
                        ("caller", nameof(OnTimeReset)),
                        ("exception", handlerException));
                }
            }
        }

        private bool IsOutOfSync()
        {
            if (!IsTimeSynced)
            {
                return true;
            }

            var frameDiff = ClientSimulationFrame.Frame - ServerSimulationFrame.Frame;

            return Math.Abs(frameDiff) > simulationFrameResetTreshold;
        }

        private void CalculateTargetTimeScale(Ping ping)
        {
            long serverFrame = ServerSimulationFrame.Frame;
            long frameDiff = ClientSimulationFrame.Frame - serverFrame;

            if (AccountForPing)
            {
                // Wait until ping is stable to prevent timescale jitter
                if (!ping.IsStable)
                {
                    TargetTimeScale = 1.0;
                    return;
                }

                // Shift server frame by latency from the latest packet received
                serverFrame += (long)(ping.LatestLatencyMs / timeStepMs);
                frameDiff = ClientSimulationFrame.Frame - serverFrame;
            }

            // Avoid jittery network scale at tiny frame diffs that normally occur even on local connections
            if (Math.Abs(frameDiff) <= maxFrameDiffForHoldingTimeScale)
            {
                TargetTimeScale = 1.0;
                return;
            }

            var timeScalePercent = 100 - frameDiff / timeDilationFactor;
            var timeScale = timeScalePercent / 100f;
            timeScale = Math.Max(timeScale, minTimeScale);
            timeScale = Math.Min(timeScale, maxTimeScale);

            TargetTimeScale = timeScale;
        }

        internal void UpdateTimeScale(double deltaTime)
        {
            // Update time scale to counteract client-server drift. smoothTime and maxSpeed chosen by trial and error.
            NetworkTimeScaleAsDouble = SmoothTimeScaleChange ? SmoothDamp(NetworkTimeScale, TargetTimeScale, ref timeScaleVelocity, 0.33f, 2f, deltaTime) : TargetTimeScale;
        }

        // From Game Programming Gems 4 : 1.10
        private static double SmoothDamp(double from, double to, ref double vel, double smoothTime, double maxSpeed, double dt)
        {
            var omega = 2f / smoothTime;
            var x = omega * dt;
            var exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            var change = from - to;
            var maxChange = maxSpeed * smoothTime;
            change = Math.Min(Math.Max(-maxChange, change), maxChange);
            var temp = (vel + omega * change) * dt;
            vel = (vel - omega * temp) * exp;
            return to + (change + temp) * exp;
        }
    }
}
