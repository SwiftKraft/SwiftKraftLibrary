// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Connection;
    using Core;
    using Entities;
    using Log;
    using ProtocolDef;
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.Serialization;
    using UnityEngine.UIElements;
    using Debug = UnityEngine.Debug;
    using Logger = Log.Logger;

    /// <summary>
    ///     This component can be added to a GameObject with
    ///     <see cref="Coherence.Toolkit.CoherenceSync"/> to be able to define
    ///     which inputs it accepts.
    ///
    ///     This feature can be used to improve game feel and to prevent
    ///     cheating by replaying inputs on a simulator or another client.
    ///     See the coherence docs (section "Input prediction and rollback")
    ///     for more information.
    /// </summary>
    [AddComponentMenu("coherence/Coherence Input")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CoherenceSync))]
    [DefaultExecutionOrder(ScriptExecutionOrder.CoherenceInput)]
    [NonBindable]
    [HelpURL("https://docs.coherence.io/v/1.7/manual/authority/server-authoritative-setup")]
    public class CoherenceInput : MonoBehaviour, ICoherenceInput
    {
        /// <summary>
        /// Defines the maximum number of input <see cref="Fields"/>.
        /// </summary>
        public const int MaxInputs = 32;

        /// <summary>
        /// Contains the field definitions to be used.
        /// </summary>
        public Field[] Fields => fields;

        /// <inheritdoc cref="IInputBuffer.Size"/>
        [Tooltip("Initial size of the input buffer. Defines how many simulation frames worth of inputs can be stored.")]
        public int InitialBufferSize = 32;

        /// <inheritdoc cref="IInputBuffer.Delay"/>
        [FormerlySerializedAs("InitialBufferDelay")]
        [Tooltip("Initial input delay. Defines how into the future inputs are scheduled (in frames).")]
        public int InitialInputDelay = 3;

        /// <summary>
        /// Defines whether the client should automatically disconnect in case of unexpected time resync with the server.
        /// </summary>
        /// <remarks>
        /// Works only with the client-side simulation.
        /// </remarks>
        [Tooltip("Defines whether the client should automatically disconnect in case of unexpected time reset (resync with the server). " +
                 "Works only with the client-side simulation.")]
        public bool DisconnectOnTimeReset;

        /// <summary>
        /// Defines whether the simulator or host that has the state authority over this entity should destroy it when the client with input authority disconnects from the session.
        /// </summary>
        /// <remarks>
        /// Works with server-side simulation.
        /// </remarks>
        [Tooltip("Defines whether the host that has the state authority over this entity should destroy it when the client with input authority disconnects from the session. " +
                 "Works with server-side simulation.")]
        public bool DestroyOnInputAuthorityDisconnected = true;

        private bool ShouldDestroyOnInputAuthorityGained => DestroyOnInputAuthorityDisconnected &&
                                                            IsServerSimulated &&
                                                            bridge is { IsSimulatorOrHost: true, };

        /// <summary>
        /// If <see langword="true"/> the input system assumes that there's a simulator or host running that processes all inputs sent by the clients
        /// while clients merely produce inputs and sync their entities with the host. Authority over this entity will be
        /// automatically transferred to the host.
        /// </summary>
        public bool IsServerSimulated => coherenceSync is { SimulationTypeConfig: CoherenceSync.SimulationType.ServerSideWithClientInput };

        /// <summary>
        /// If <see langword="true"/> the input system will use <see cref="NetworkTime.ClientFixedSimulationFrame" />,
        /// otherwise <see cref="NetworkTime.ClientSimulationFrame" /> will be used.
        /// </summary>
        /// <remarks>
        /// When using fixed frames, if deterministic output is desired, it is recommended to update
        /// the input driven logic through the <see cref="CoherenceBridge.OnFixedNetworkUpdate" />.
        /// </remarks>
        [Tooltip("If true the input system will use the client fixed simulation frame" +
                 " (otherwise the standard client simulation frame will be used). Recommended for deterministic output.")]
        public bool UseFixedSimulationFrames;

        /// <summary>
        /// Current simulation frame as seen by this client. If the <see cref="UseFixedSimulationFrames" />
        /// is <see langword="true"/> this returns <see cref="NetworkTime.ClientFixedSimulationFrame" />,
        /// otherwise <see cref="NetworkTime.ClientSimulationFrame" /> is returned.
        /// </summary>
        public long CurrentSimulationFrame => UseFixedSimulationFrames
            ? bridge.NetworkTime.ClientFixedSimulationFrame
            : bridge.NetworkTime.ClientSimulationFrame;

        /// <summary>
        /// Determines whether this input is ready for processing input data.
        /// Until this is <see langword="true"/>, all the inputs set through
        /// the `Set` methods are overwriting each other.
        /// Only the last value set will be sent as soon as this property becomes <see langword="true"/>.
        /// </summary>
        public bool IsReadyToProcessInputs => ProcessingEnabled
                                              && bridge != null
                                              && bridge.IsConnected
                                              && bridge.NetworkTime.IsTimeSynced
                                              && IsInputOwner
                                              && coherenceSync.EntityState != null
                                              && (!IsServerSimulated || isSimulatorOrHostConnected);

        /// <summary>
        /// Determines whether this client is responsible for producing inputs using `Set` methods.
        /// </summary>
        public bool IsInputOwner => bridge != null && coherenceSync.HasInputAuthority;

        /// <summary>
        /// If <see langword="true"/>, this client is responsible for producing inputs using `Set` methods.
        /// </summary>
        public bool IsProducer => coherenceSync.HasInputAuthority;

        /// <summary>
        /// Determines whether the input processing will happen. If <see langword="false"/>, inputs won't be stored
        /// in the buffer and won't be sent. Affects only the input-producing side. Defaults to <see langword="true"/>.
        /// </summary>
        public bool ProcessingEnabled { get; set; } = true;

        /// <inheritdoc cref="InputBuffer{T}.OnStaleInput"/>
        public event StaleInputHandler OnStaleInput
        {
            add => Buffer.OnStaleInput += value;
            remove => Buffer.OnStaleInput -= value;
        }

        /// <inheritdoc cref="InputBuffer{T}.Size"/>
        public int BufferSize => Buffer.Size;

        /// <inheritdoc cref="InputBuffer{T}.Delay"/>
        public int Delay
        {
            get => Buffer.Delay;
            set => Buffer.Delay = value;
        }

        /// <inheritdoc cref="InputBuffer{T}.LastFrame"/>
        public long LastFrame => Buffer.LastFrame;

        /// <inheritdoc cref="InputBuffer{T}.LastSentFrame"/>
        public long LastSentFrame => Buffer.LastSentFrame;

        /// <inheritdoc cref="InputBuffer{T}.LastReceivedFrame"/>
        public long LastReceivedFrame => Buffer.LastReceivedFrame;

        /// <inheritdoc cref="InputBuffer{T}.LastAcknowledgedFrame"/>
        public long LastAcknowledgedFrame => Buffer.LastAcknowledgedFrame;

        /// <inheritdoc cref="InputBuffer{T}.LastConsumedFrame"/>
        public long LastConsumedFrame => Buffer.LastConsumedFrame;

        /// <inheritdoc cref="InputBuffer{T}.MispredictionFrame"/>
        public long? MispredictionFrame => Buffer.MispredictionFrame;

        /// <summary>
        /// Returns an underlying input buffer.
        /// </summary>
        /// <remarks>
        /// For advanced use cases, use with care.
        /// </remarks>
        public IInputBuffer Buffer => inputBuffer ??= internalRequestBuffer?.Invoke();

        internal CoherenceInputDebugger Debugger { get; set; }

        [SerializeField]
        [FormerlySerializedAs("_fields")]
        private Field[] fields;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Action<string, bool> internalSetButton;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Func<string, long?, bool> internalGetButton;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Action<string, float> internalSetAxis;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Func<string, long?, float> internalGetAxis;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Action<string, Vector2> internalSetAxis2D;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Func<string, long?, Vector2> internalGetAxis2D;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Action<string, Vector3> internalSetAxis3D;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Func<string, long?, Vector3> internalGetAxis3D;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Action<string, Quaternion> internalSetRotation;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Func<string, long?, Quaternion> internalGetRotation;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Action<string, int> internalSetInteger;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Func<string, long?, int> internalGetInteger;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Action<string, string> internalSetString;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Func<string, long?, string> internalGetString;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Action<IEntityInput, long> internalOnInputReceived;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Func<IInputBuffer> internalRequestBuffer;

        private Logger logger;

        private IInputBuffer inputBuffer;
        private ICoherenceSync coherenceSync;
        private ICoherenceBridge bridge;
        private IClient client;

        private int timeResetsAllowed;
        private bool autoRequestingAuthority;

        internal bool isSimulatorOrHostConnected { get; private set; }

        // for components, we don't expose direct creation of instances - add as component instead
        private CoherenceInput()
        {
        }

        /// <summary>
        /// Initializes the references needed for this component to work.
        /// </summary>
        /// <remarks>
        /// References set by calling this method while the component is not <see cref="Behaviour.enabled"/>
        /// will be overwritten on <see cref="OnEnable"/>.
        /// </remarks>
        /// <param name="sync">The CoherenceSync to use.</param>
        /// <param name="bridge">The CoherenceBridge to use.</param>
        internal void Setup(ICoherenceSync sync, ICoherenceBridge bridge)
        {
            Cleanup();

            coherenceSync = sync;
            this.bridge = bridge;
            client = bridge?.Client;

            if (!VerifyCoherenceSync())
            {
                return;
            }

            if (coherenceSync.HasStateAuthority && DisconnectOnTimeReset && !IsServerSimulated)
            {
                timeResetsAllowed = bridge.NetworkTime.IsTimeSynced ? 0 : 1;
                bridge.NetworkTime.OnTimeReset += HandleTimeReset;
                client.OnConnected += SetAllowedTimeResets;
            }

            if (ShouldDestroyOnInputAuthorityGained)
            {
                coherenceSync.OnInputAuthority += DestroySelf;
            }

            // Do the auto request once, but if rejected or auth lost,
            // leave it to the user to call RequestAuthority() again.
            if (coherenceSync.IsSynchronizedWithNetwork)
            {
                SetAutoRequestingAuthority();
            }
            else
            {
                coherenceSync.OnStateRemote += SetAutoRequestingAuthority;
            }
        }

        private void Awake()
        {
            logger = Log.GetLogger<CoherenceInput>();
            logger.Context = this;
            var hasSync = TryGetComponent(out coherenceSync);
            Debug.Assert(hasSync);
        }

        private void OnEnable()
        {
            if (Fields.Length > MaxInputs)
            {
                logger.Warning(Warning.ToolkitInputTooManyInputs,
                    $"Too many inputs defined for {name}. The max allowed is {MaxInputs}");
            }

            if (!CoherenceBridgeStore.TryGetBridge(gameObject.scene, out var foundBridge))
            {
                logger.Warning(Warning.ToolkitInputNoBridge,
                    $"Can't find a {nameof(CoherenceBridge)} instance for '{name}'. Disabling {nameof(CoherenceInput)}.");
                enabled = false;
                return;
            }

            _ = TryGetComponent(out CoherenceSync sync);
            Setup(sync, foundBridge);
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (DisconnectOnTimeReset && !IsServerSimulated && client != null)
            {
                client.NetworkTime.OnTimeReset -= HandleTimeReset;
                client.OnConnected -= SetAllowedTimeResets;
            }

            if (ShouldDestroyOnInputAuthorityGained && coherenceSync != null)
            {
                coherenceSync.OnInputAuthority -= DestroySelf;
            }
        }

        internal void SetAutoRequestingAuthority()
        {
            autoRequestingAuthority = IsServerSimulated && !coherenceSync.HasStateAuthority;
            coherenceSync.OnStateRemote -= SetAutoRequestingAuthority;
        }

        private void DestroySelf()
        {
            Destroy(gameObject);
        }

        private void SetAllowedTimeResets(ClientID _)
        {
            timeResetsAllowed = 1; // We allow for initial time sync
        }

        private void HandleTimeReset()
        {
            // Prevents multiple disconnect calls
            if (timeResetsAllowed < 0)
            {
                return;
            }

            timeResetsAllowed--;
            if (timeResetsAllowed < 0)
            {
                logger.Warning(Warning.ToolkitInputUnexpectedReset,
                    "Detected unexpected time reset (client simulation frame drifted too far away from the server's frame). Disconnecting.");
                client.Disconnect();
            }
        }

        /// <summary>
        /// Update event.
        /// </summary>
        /// <seealso cref="MonoBehaviour"/>
        protected void Update()
        {
            if (IsServerSimulated)
            {
                DetectHost();
                ProcessAutoRequestingAuthority();
            }
        }

        private void DetectHost()
        {
            // Not detected yet
            if (isSimulatorOrHostConnected)
            {
                return;
            }

            // Must be an input owner AND...
            if (IsInputOwner)
            {
                // ... either a host/simulator with state authority (in case where host also owns an input object)
                if (bridge.IsSimulatorOrHost && coherenceSync.IsSynchronizedWithNetwork && coherenceSync.HasStateAuthority)
                {
                    isSimulatorOrHostConnected = true;
                }
                // ... or a client that has no state authority (because it was transferred)
                else if (client?.ConnectionType == ConnectionType.Client && coherenceSync.IsSynchronizedWithNetwork && !coherenceSync.HasStateAuthority)
                {
                    isSimulatorOrHostConnected = true;
                }
            }

            if (isSimulatorOrHostConnected)
            {
                try
                {
                    coherenceSync.OnInputSimulatorConnected.Invoke();
                }
                catch (Exception e)
                {
                    logger.Error(Error.ToolkitInputCallbackException,
                        ("callback", nameof(CoherenceSync.OnInputSimulatorConnected)),
                        ("exception", e.ToString()));
                }
            }
        }

        internal void ProcessAutoRequestingAuthority()
        {
            if (autoRequestingAuthority &&          // Auto requesting enabled
                !coherenceSync.HasStateAuthority && // We don't have state authority
                bridge.IsSimulatorOrHost)            // And we're a host
            {
                autoRequestingAuthority = false;

                if (coherenceSync.IsOrphaned)
                {
                    coherenceSync.Adopt();
                }
                else
                {
                    coherenceSync.RequestAuthority(AuthorityType.State);
                }
            }
        }

        /// <inheritdoc cref="InputBuffer{T}.ShouldPause"/>
        public bool ShouldPause(long commonReceivedFrame)
        {
            return !IsServerSimulated && Buffer.ShouldPause(CurrentSimulationFrame, commonReceivedFrame);
        }

        /// <summary>
        ///     Sets a button state for this input. Input is stored as of the <see cref="CurrentSimulationFrame" />.
        /// </summary>
        /// <param name="buttonName">Name of the button to update.</param>
        /// <param name="value">Value to set.</param>
        public void SetButton(string buttonName, bool value)
        {
            if (!AssertValidInputProducer(nameof(SetButton), nameof(GetButton)))
            {
                return;
            }

            internalSetButton?.Invoke(buttonName, value);
        }

        /// <summary>
        ///     Sets an axis state for this input. Input is stored as of the <see cref="CurrentSimulationFrame" />.
        /// </summary>
        /// <param name="axisName">Name of the axis to update.</param>
        /// <param name="value">Value to set.</param>
        public void SetAxis(string axisName, float value)
        {
            if (!AssertValidInputProducer(nameof(SetAxis), nameof(GetAxis)))
            {
                return;
            }

            internalSetAxis?.Invoke(axisName, value);
        }

        /// <summary>
        ///     Sets an axis2D state for this input. Input is stored as of the <see cref="CurrentSimulationFrame" />.
        /// </summary>
        /// <param name="axis2DName">Name of the axis2D to update.</param>
        /// <param name="value">Value to set.</param>
        public void SetAxis2D(string axis2DName, Vector2 value)
        {
            if (!AssertValidInputProducer(nameof(SetAxis2D), nameof(GetAxis2D)))
            {
                return;
            }

            internalSetAxis2D?.Invoke(axis2DName, value);
        }

        /// <summary>
        ///     Sets an axis3D state for this input. Input is stored as of the <see cref="CurrentSimulationFrame" />.
        /// </summary>
        /// <param name="axis3DName">Name of the axis3D to update.</param>
        /// <param name="value">Value to set.</param>
        public void SetAxis3D(string axis3DName, Vector3 value)
        {
            if (!AssertValidInputProducer(nameof(SetAxis3D), nameof(GetAxis3D)))
            {
                return;
            }

            internalSetAxis3D?.Invoke(axis3DName, value);
        }

        /// <summary>
        ///     Sets a rotation state for this input. Input is stored as of the <see cref="CurrentSimulationFrame" />.
        /// </summary>
        /// <param name="rotationName">Name of the rotation to update.</param>
        /// <param name="value">Value to set.</param>
        public void SetRotation(string rotationName, Quaternion value)
        {
            if (!AssertValidInputProducer(nameof(SetRotation), nameof(GetRotation)))
            {
                return;
            }

            internalSetRotation?.Invoke(rotationName, value);
        }

        /// <summary>
        ///     Sets an integer state for this input. Input is stored as of the <see cref="CurrentSimulationFrame" />.
        /// </summary>
        /// <param name="integerName">Name of the integer to update.</param>
        /// <param name="value">Value to set.</param>
        public void SetInteger(string integerName, int value)
        {
            if (!AssertValidInputProducer(nameof(SetInteger), nameof(GetInteger)))
            {
                return;
            }

            internalSetInteger?.Invoke(integerName, value);
        }

        /// <summary>
        ///     Sets a string state for this input. Input is stored as of the <see cref="CurrentSimulationFrame" />.
        /// </summary>
        /// <param name="stringName">Name of the string to update.</param>
        /// <param name="value">Value to set.</param>
        public void SetString(string stringName, string value)
        {
            if (!AssertValidInputProducer(nameof(SetString), nameof(GetString)))
            {
                return;
            }

            internalSetString?.Invoke(stringName, value);
        }

        /// <summary>
        ///     Returns a state of the button as of the given simulation frame.
        /// </summary>
        /// <param name="buttonName">Name of the button.</param>
        /// <param name="simulationFrame">
        ///     Simulation frame for which state is requested. If `null` the
        ///     <see cref="CurrentSimulationFrame" /> will be used.
        /// </param>
        /// <returns>Value of the button as of the given simulation frame.</returns>
        public bool GetButton(string buttonName, long? simulationFrame = null)
        {
            if (internalGetButton == null)
            {
                PrintMethodMissingError(nameof(GetButton));
            }

            return internalGetButton?.Invoke(buttonName, simulationFrame) ?? false;
        }

        /// <summary>
        ///     Returns a state of the axis as of the given simulation frame.
        /// </summary>
        /// <param name="axisName">Name of the axis.</param>
        /// <param name="simulationFrame">
        ///     Simulation frame for which state is requested. If `null` the
        ///     <see cref="CurrentSimulationFrame" /> will be used.
        /// </param>
        /// <returns>Value of the axis as of the given simulation frame.</returns>
        public float GetAxis(string axisName, long? simulationFrame = null)
        {
            if (internalGetAxis == null)
            {
                PrintMethodMissingError(nameof(GetAxis));
            }

            return internalGetAxis?.Invoke(axisName, simulationFrame) ?? 0f;
        }

        /// <summary>
        ///     Returns a state of the axis2D as of the given simulation frame.
        /// </summary>
        /// <param name="axis2DName">Name of the axis2D.</param>
        /// <param name="simulationFrame">
        ///     Simulation frame for which state is requested. If `null` the
        ///     <see cref="CurrentSimulationFrame" /> will be used.
        /// </param>
        /// <returns>Value of the axis2D as of the given simulation frame.</returns>
        public Vector2 GetAxis2D(string axis2DName, long? simulationFrame = null)
        {
            if (internalGetAxis2D == null)
            {
                PrintMethodMissingError(nameof(GetAxis2D));
            }

            return internalGetAxis2D?.Invoke(axis2DName, simulationFrame) ?? Vector2.zero;
        }

        /// <summary>
        ///     Returns a state of the axis3D as of the given simulation frame.
        /// </summary>
        /// <param name="axis3DName">Name of the axis3D.</param>
        /// <param name="simulationFrame">
        ///     Simulation frame for which state is requested. If `null` the
        ///     <see cref="CurrentSimulationFrame" /> will be used.
        /// </param>
        /// <returns>Value of the axis3D as of the given simulation frame.</returns>
        public Vector3 GetAxis3D(string axis3DName, long? simulationFrame = null)
        {
            return internalGetAxis3D?.Invoke(axis3DName, simulationFrame) ?? Vector3.zero;
        }

        /// <summary>
        ///     Returns a state of the rotation as of the given simulation frame.
        /// </summary>
        /// <param name="rotationName">Name of the rotation.</param>
        /// <param name="simulationFrame">
        ///     Simulation frame for which state is requested. If `null` the
        ///     <see cref="CurrentSimulationFrame" /> will be used.
        /// </param>
        /// <returns>Value of the rotation as of the given simulation frame.</returns>
        public Quaternion GetRotation(string rotationName, long? simulationFrame = null)
        {
            return internalGetRotation?.Invoke(rotationName, simulationFrame) ?? Quaternion.identity;
        }

        /// <summary>
        ///     Returns a state of the integer as of the given simulation frame.
        /// </summary>
        /// <param name="integerName">Name of the integer.</param>
        /// <param name="simulationFrame">
        ///     Simulation frame for which state is requested. If `null` the
        ///     <see cref="CurrentSimulationFrame" /> will be used.
        /// </param>
        /// <returns>Value of the integer as of the given simulation frame.</returns>
        public int GetInteger(string integerName, long? simulationFrame = null)
        {
            return internalGetInteger?.Invoke(integerName, simulationFrame) ?? default;
        }

        /// <summary>
        ///     Returns a string state as of the given simulation frame.
        /// </summary>
        /// <param name="stringName">Name of the string.</param>
        /// <param name="simulationFrame">
        ///     Simulation frame for which state is requested. If `null` the
        ///     <see cref="CurrentSimulationFrame" /> will be used.
        /// </param>
        /// <returns>Value of the string as of the given simulation frame.</returns>
        public string GetString(string stringName, long? simulationFrame = null)
        {
            if (internalGetString == null)
            {
                PrintMethodMissingError(nameof(GetString));
            }

            return internalGetString?.Invoke(stringName, simulationFrame);
        }

        internal void HandleInputReceived(IEntityInput input, long inputFrame)
        {
            internalOnInputReceived?.Invoke(input, inputFrame);
        }

        private void PrintMethodMissingError(string methodName)
        {
            if (coherenceSync.BakedScript == null)
            {
                logger.Error(Error.ToolkitInputBakedMethodMissing,
                    $"No baked callback found for {nameof(CoherenceInput)}.{methodName}. " +
                        $"{nameof(CoherenceInput)} requires baked code, you can bake manually " +
                        "from coherence/Bake menu item.");
            }
            else
            {
                logger.Error(Error.ToolkitInputBridgeDisconnected,
                    $"Cannot use {nameof(CoherenceInput)} when {nameof(CoherenceBridge)} is disconnected. " +
                        $"Make sure {nameof(CoherenceBridge)} is connected before calling " +
                        $"{nameof(CoherenceInput)}.{methodName}.");
            }
        }

        private bool AssertValidInputProducer(string setterName, string getterName)
        {
            if (IsProducer)
            {
                return true;
            }

            if (IsServerSimulated && coherenceSync.HasStateAuthority)
            {
                logger.Warning(Warning.ToolkitInputServerNoAuth,
                    $"Trying to call the {setterName} function on a server-simulated input buffer before the server has authority, input will be lost. " +
                        $"Wait for the CoherenceSync behavior's isSimulated property to be false before setting inputs.");
            }
            else
            {
                logger.Warning(Warning.ToolkitInputNotProducer,
                    $"Trying to call the {setterName} function on a {(coherenceSync.HasStateAuthority ? "" : "non-")}simulated object " +
                        $"with `{nameof(IsServerSimulated)}` turned {(IsServerSimulated ? "on" : "off")}. " +
                        $"Inputs should be set only on a {(IsServerSimulated ? "non-" : "")}simulated entities. " +
                        $"Those inputs are then sent to the other side that applies them using the {getterName} function.");
            }

            return true;
        }

        [Conditional(CoherenceInputDebugger.DEBUG_CONDITIONAL)]
        public void DebugOnInputReceived(long frame, object input)
        {
            Debugger?.HandleInputReceived(this, frame, input);
        }

        [Conditional(CoherenceInputDebugger.DEBUG_CONDITIONAL)]
        public void DebugOnInputSent(long frame, object input)
        {
            Debugger?.HandleInputSent(this, frame, input);
        }

        private bool VerifyCoherenceSync()
        {
            if (coherenceSync == null)
            {
                logger.Error(Error.ToolkitInputMissingSync,
                    $"Input requires a {nameof(CoherenceSync)} component. Please make sure that one is attached and enabled. Disabling input component.",
                    ("context", this));
                enabled = false;
                return false;
            }

            return true;
        }
    }
}
