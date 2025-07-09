// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Connection;
    using Log;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.Serialization;
    using Logger = Log.Logger;

    /// <summary>
    ///     <see cref="CoherenceInputSimulation{TState}" /> helps building a deterministic simulation by taking care of most
    ///     complexities related to keeping a simulation state, handling predictions, rolling back state, resimulating state,
    ///     etc..
    /// </summary>
    /// <typeparam name="TState">Type that holds the simulation state for a given frame.</typeparam>
    public abstract class CoherenceInputSimulation<TState> : MonoBehaviour
    {
        [FormerlySerializedAs("monoBridge")]
        [Tooltip("Bridge used by the SimulationCore. If null, will be searched for using the resolver function and a CoherenceBridgeStore.")]
        public CoherenceBridge coherenceBridge;

        /// <inheritdoc cref="CoherenceBridgeResolver{T}"/>
        public event CoherenceBridgeResolver<CoherenceInputSimulation<TState>> BridgeResolve;

        /// <summary>
        ///     If set to true, the simulation will automatically handle the <see cref="CoherenceInputManager.ShouldPause" />
        ///     changes, stopping the fixed simulation updates as needed.
        /// </summary>
        [Tooltip("If set to true, the simulation will automatically handle pausing, " +
                 "stopping the fixed simulation updates when needed.")]
        public bool PauseAutomatically;

        /// <summary>A network client used for communication with the Replication Server.</summary>
        protected IClient CoherenceClient => coherenceBridge.Client;

        /// <summary>Instance of the local <see cref="CoherenceClientConnection" />.</summary>
        protected CoherenceClientConnection LocalClient { get; private set; }

        /// <summary>All instances of the connected <see cref="CoherenceClientConnection" />s, including a local one.</summary>
        protected IList<CoherenceClientConnection> AllClients => allClients.Values;

        /// <summary>Stores the simulation states used for rollbacks in case of a misprediction.</summary>
        /// <remarks>
        ///     Used internally by the <see cref="CoherenceInputSimulation{TState}" />, manual use recommended only in advanced
        ///     cases.
        /// </remarks>
        protected SimulationStateStore<TState> StateStore { get; } = new SimulationStateStore<TState>();

        /// <summary>A fixed time step as set in the <see cref="NetworkTime" />. By default set to <see cref="Time.fixedDeltaTime" />.</summary>
        protected float FixedTimeStep => coherenceBridge != null ? (float)(coherenceBridge.Client?.NetworkTime.FixedTimeStep ?? 0f) : 0f;

        protected long CurrentSimulationFrame => inputManager.CurrentFixedSimulationFrame;

        /// <summary>
        ///     If set to false, the simulation code won't be run, however:
        ///     <para>* Frame progression is not stopped</para>
        ///     <para>* The <see cref="OnBeforeSimulate" /> is still called</para>
        ///     Can be used for example to wait for all players before starting the simulation.
        /// </summary>
        protected bool SimulationEnabled
        {
            get => inputManager.ProcessingEnabled;
            set => inputManager.ProcessingEnabled = value;
        }

        /// <summary>
        ///     Input debugger that stores various information about the input system for each frame. Upon disconnect the
        ///     information is dumped to a specified location defined by the XXX (by default to a file).
        /// </summary>
        protected CoherenceInputDebugger Debugger { get; private set; }

        private CoherenceInputManager inputManager;
        private readonly Logger logger = Log.GetLogger(typeof(CoherenceInputSimulation<>));
        private readonly SortedList<ClientID, CoherenceClientConnection> allClients = new SortedList<ClientID, CoherenceClientConnection>();
        private readonly Dictionary<ClientID, CoherenceClientConnection> clientById = new Dictionary<ClientID, CoherenceClientConnection>();

        /// <summary>Called when the local client should set inputs using the <see cref="CoherenceInput" />.</summary>
        /// <param name="client">The local client which should set inputs.</param>
        protected abstract void SetInputs(CoherenceClientConnection client);

        /// <summary>
        ///     Called when a simulation must happen for a given <paramref name="simulationFrame" />. This is where the core of the
        ///     simulation logic should be implemented.
        /// </summary>
        /// <remarks>This method is called for both standard simulation and resimulation.</remarks>
        /// <param name="simulationFrame">Frame for which the simulation is run.</param>
        protected abstract void Simulate(long simulationFrame);

        /// <summary>
        ///     Called when the simulation should be rolled back to a given <paramref name="state" />. The
        ///     <paramref name="state" /> should be used to set simulation state back to how it was as of the
        ///     <paramref name="toFrame" />.
        /// </summary>
        /// <param name="toFrame">Frame to which we're rolling back.</param>
        /// <param name="state">State as of the <paramref name="toFrame" />.</param>
        protected abstract void Rollback(long toFrame, TState state);

        /// <summary>
        ///     Called at the end of the simulation loop to get a snapshot of the simulation that can be later used in the
        ///     <see cref="Rollback" /> if needed.
        /// </summary>
        /// <returns>Full simulation state.</returns>
        protected abstract TState CreateState();

        /// <summary>Called when a new <see cref="CoherenceClientConnection" /> joins the session (including a local one).</summary>
        /// <param name="client">A client that has joined.</param>
        protected virtual void OnClientJoined(CoherenceClientConnection client) { }

        /// <summary>Called when a <see cref="CoherenceClientConnection" /> leaves the session.</summary>
        /// <param name="client">A client that has left.</param>
        protected virtual void OnClientLeft(CoherenceClientConnection client) { }

        /// <summary>
        ///     Called as a first thing in a given simulation loop, before any of the simulation or rollback code. Can be used for
        ///     example to control the <see cref="SimulationEnabled" /> flag based on the players readiness.
        /// </summary>
        protected virtual void OnBeforeSimulate() { }

        /// <summary>Called when we successfully connect to the Replication Server.</summary>
        protected virtual void OnConnected() { }

        /// <summary>Called when we disconnect from the Replication Server.</summary>
        protected virtual void OnDisconnected() { }

        /// <summary>
        ///     Called when a pause or unpause happens. See the <see cref="PauseAutomatically" /> and
        ///     <see cref="CoherenceInputManager.ShouldPause" /> for more information.
        /// </summary>
        protected virtual void OnPauseChange(bool isPaused) { }

        /// <summary>Called after the simulation initializes in the Unity's `Start` callback.</summary>
        protected virtual void OnStart() { }

        /// <summary>Called after the simulation cleanup in the Unity's `OnDestroy` callback.</summary>
        protected virtual void Destroy() { }

        /// <summary>Returns a <see cref="CoherenceClientConnection" /> by its <see cref="CoherenceClientConnection.ClientId" />.</summary>
        /// <param name="clientId">The <see cref="CoherenceClientConnection.ClientId" />.</param>
        /// <param name="client">A client for given ID or null if none was found.</param>
        /// <returns>True if the client with a given ID was found.</returns>
        protected bool TryGetClient(ClientID clientId, out CoherenceClientConnection client)
        {
            return clientById.TryGetValue(clientId, out client);
        }

        /// <summary>Used internally, DON'T hide it by defining another `Start` function - use the <see cref="OnStart" /> instead.</summary>
        protected void Start()
        {
            if (coherenceBridge == null)
            {
                if (!CoherenceBridgeStore.TryGetBridge(gameObject.scene, BridgeResolve, this, out coherenceBridge))
                {
                    logger.Error(Error.ToolkitInputBridgeNotFound);
                    return;
                }
            }

            coherenceBridge.OnFixedNetworkUpdate += FixedNetworkUpdate;
            coherenceBridge.OnLateFixedNetworkUpdate += LateFixedNetworkUpdate;
            coherenceBridge.ClientConnections.OnCreated += HandleConnectionCreated;
            coherenceBridge.ClientConnections.OnDestroyed += HandleConnectionDestroyed;
            coherenceBridge.Client.OnDisconnected += HandleDisconnected;
            coherenceBridge.Client.OnConnected += HandleConnected;

            inputManager = coherenceBridge.InputManager;
            inputManager.OnPauseChange += HandlePauseChange;

            OnStart();
        }

        private void OnDestroy()
        {
            inputManager.OnPauseChange -= HandlePauseChange;

            if (coherenceBridge != null)
            {
                if (coherenceBridge.Client != null)
                {
                    coherenceBridge.Client.OnConnected -= HandleConnected;
                    coherenceBridge.Client.OnDisconnected -= HandleDisconnected;
                }

                coherenceBridge.ClientConnections.OnCreated -= HandleConnectionDestroyed;
                coherenceBridge.ClientConnections.OnDestroyed -= HandleConnectionCreated;
                coherenceBridge.OnLateFixedNetworkUpdate -= LateFixedNetworkUpdate;
                coherenceBridge.OnFixedNetworkUpdate -= FixedNetworkUpdate;
            }

            Destroy();
        }

        private void HandleConnected(ClientID _)
        {
            Debugger = new CoherenceInputDebugger(inputManager);

            OnConnected();
        }

        private void HandleDisconnected(ConnectionCloseReason unused)
        {
            foreach (CoherenceClientConnection client in AllClients)
            {
                if (client != null)
                {
                    Destroy(client.GameObject);
                }
            }

            OnDisconnected();

            LocalClient = null;
            allClients.Clear();
            clientById.Clear();
            StateStore.Clear();

            if (PauseAutomatically)
            {
                coherenceBridge.Client.NetworkTime.Pause = false;
            }

            Debugger.Dump();
        }

        private void HandlePauseChange(bool isPaused)
        {
            Debugger.AddEvent(isPaused ? CoherenceInputDebugger.Event.Pause : CoherenceInputDebugger.Event.UnPause,
                null);

            if (PauseAutomatically)
            {
                coherenceBridge.Client.NetworkTime.Pause = isPaused;
            }

            OnPauseChange(isPaused);
        }

        private void FixedNetworkUpdate()
        {
            if (LocalClient == null || !LocalClient.Sync.Input.IsReadyToProcessInputs)
            {
                return;
            }

            OnBeforeSimulate();

            if (!SimulationEnabled)
            {
                return;
            }

            Debugger.PushSample();

            // Handle misprediction & rollback
            if (inputManager.MispredictionFrame.HasValue)
            {
                long mispredictionFrame = inputManager.MispredictionFrame.Value;

                if (!StateStore.TryRollback(mispredictionFrame, out TState state) && StateStore.Count > 0)
                {
                    logger.Error(Error.ToolkitInputSimulationFailedToRollBack,
                        ("RollbackFrame", mispredictionFrame),
                        ("StoreNewestFrame", StateStore.NewestFrame),
                        ("StoreOldestFrame", StateStore.OldestFrame),
                        ("CurrentFrame", inputManager.CurrentFixedSimulationFrame),
                        ("AcknowledgedFrame", inputManager.AcknowledgedFrame));

                    Debugger.AddEvent(CoherenceInputDebugger.Event.Error, new
                    {
                        Error = "Rollback failure",
                        mispredictionFrame
                    });

                    CoherenceClient.Disconnect();
                    return;
                }

                Debugger.AddEvent(CoherenceInputDebugger.Event.Rollback, new
                {
                    mispredictionFrame
                });

                Rollback(mispredictionFrame - 1, state);

                for (long simFrame = mispredictionFrame;
                    simFrame < inputManager.CurrentFixedSimulationFrame;
                    simFrame++)
                {
                    Simulate(simFrame);
                    SaveState(simFrame);

                    Debugger.AddInputs(simFrame,
                        AllClients.Select(c => new DebugInput { Input = c.Sync.Input, Id = c.ClientId.ToString() }),
                        true);
                }
            }

            StateStore.Acknowledge(inputManager.AcknowledgedFrame);
            Debugger.AcknowledgeFrame(inputManager.AcknowledgedFrame);

            SetInputs(LocalClient);
            Simulate(inputManager.CurrentFixedSimulationFrame);
            SaveState(inputManager.CurrentFixedSimulationFrame);

            Debugger.SetInputBufferStates();
        }

        private void LateFixedNetworkUpdate()
        {
            Debugger?.AddInputs(
                inputManager.CurrentFixedSimulationFrame,
                AllClients.Select(c => new DebugInput { Input = c.Sync.Input, Id = c.ClientId.ToString() }),
                SimulationEnabled && LocalClient != null && LocalClient.Sync.Input.IsReadyToProcessInputs);
        }

        private void SaveState(long simulationFrame)
        {
            TState simulationState = CreateState();
            Debugger.AddState(simulationFrame, simulationState);
            StateStore.Add(simulationState, simulationFrame);
        }

        private void HandleConnectionCreated(CoherenceClientConnection connection)
        {
            allClients.Add(connection.ClientId, connection);
            clientById.Add(connection.ClientId, connection);

            if (!connection.IsMyConnection)
            {
                Debugger.AddEvent(CoherenceInputDebugger.Event.ClientJoined, new
                {
                    connection.ClientId
                });
                Debugger.AddInput(connection.Sync.Input, connection.ClientId.ToString());

                OnClientJoined(connection);
                return;
            }

            if (LocalClient != null)
            {
                logger.Error(Error.ToolkitInputSimulationTooManySources,
                    $"Detected multiple input sources. {nameof(CoherenceInputSimulation<TState>)} can handle only one input source per user.",
                    ("OriginalInputSource", connection.GameObject));
                return;
            }

            Debugger.AddEvent(CoherenceInputDebugger.Event.ClientJoined, new
            {
                connection.ClientId
            });
            Debugger.AddInput(connection.Sync.Input, connection.ClientId.ToString());

            LocalClient = connection;
            OnClientJoined(connection);
        }

        private void HandleConnectionDestroyed(CoherenceClientConnection connection)
        {
            allClients.Remove(connection.ClientId);
            clientById.Remove(connection.ClientId);

            if (connection.IsMyConnection)
            {
                LocalClient = null;
            }

            Debugger.AddEvent(CoherenceInputDebugger.Event.ClientLeft, new
            {
                connection.ClientId
            });
            Debugger.RemoveInput(connection.Sync.Input);

            OnClientLeft(connection);
        }
    }
}
