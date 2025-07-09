// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Bindings
{
    using Log;
    using System;
    using System.Reflection;
    using Coherence.SimulationFrame;
    using Entities;
    using Interpolation;
    using UnityEngine;
    using Logger = Log.Logger;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Base class for everything that can be synced over the network.
    /// A binding holds the metadata that associates a user-facing unity component
    /// with its network replicated coherence component.
    /// </summary>
    /// <seealso cref="ValueBinding{T}">ValueBinding{T} for synced fields and properties.</seealso>
    /// <seealso cref="CommandBinding">CommandBinding for synced methods</seealso>
    [Serializable]
    public abstract class Binding : IDisposable
    {
        [SerializeReference] protected Descriptor descriptor;
        //////////////////////////////////////////////////////////
        // Fields
        //////////////////////////////////////////////////////////
        [SerializeField] public MessageTarget routing;

        [SerializeField]
        protected SyncMode syncMode = SyncMode.Always;

        [Obsolete("Please use predictionMode instead.")]
        [Deprecated("17/01/2024", 1, 2, 0, Reason = "Replaced by predictionMode.")]
        public bool isPredicted;

        public PredictionMode predictionMode = PredictionMode.Never;

        public string guid;
        [SerializeField]
        internal BindingArchetypeData archetypeData;
        public Component unityComponent;
        public CoherenceSync coherenceSync;
        [InterpolationPicker] public InterpolationSettings interpolationSettings;

        public event Action<object, bool, long> OnNetworkSampleReceived;

        private Logger logger = Log.GetLogger<Binding>();
        protected double lastSampledTime;

        //////////////////////////////////////////////////////////
        // Properties
        //////////////////////////////////////////////////////////
        public string Name => descriptor != null ? descriptor.Name : "Null";
        public virtual string MemberNameInComponentData => Name;
        public virtual string MemberNameInUnityComponent => Name;
        public string FullName => unityComponent ? $"{unityComponent.GetType().FullName}.{Name}" : Name;
        public Type MonoAssemblyRuntimeType => descriptor != null ? TypeUtils.GetMemoizedType(descriptor.MonoAssemblyType) : null;

        private Type coherenceComponentType;
        public virtual Type CoherenceComponentType => coherenceComponentType ??= TypeUtils.GetMemoizedType($"{CoherenceComponentNamespace}.{CoherenceComponentName}, {CoherenceComponentAssemblyName}");
        public virtual string CoherenceComponentName => null;
        public virtual string CoherenceComponentNamespace => "Coherence.Generated";
        public virtual string CoherenceComponentAssemblyName => "Coherence.Interop.Generated";
        public virtual bool EmitSchemaComponentDefinition => true;
        public string SchemaFieldName => TypeUtils.GetSchemaFieldName(MonoAssemblyRuntimeType);
        public string SchemaFieldSimulationFrameName => SchemaFieldName + "SimulationFrame";
        public virtual uint FieldMask => 1;
        public string BakedSyncScriptCSharpType => descriptor != null ? descriptor.BakedCSharpType : null;
        public virtual string BakedSyncScriptGetter => null;
        public virtual string BakedSyncScriptSetter => null;
        public virtual bool OverrideSetter => false;
        public virtual bool OverrideGetter => false;
        public Component UnityComponent => unityComponent;
        public bool IsValid => unityComponent != null;
        public bool IsMethod => descriptor != null && descriptor.MemberType == MemberTypes.Method;
        public bool EnforcesLODingWhenFieldsOverriden => descriptor != null && descriptor.EnforcesLODingWhenFieldsOverriden;
        public virtual object UntypedValue => null;

        /// <summary>
        /// Rich text representation of the binding's name and type that is displayed in the Configure and Optimize binding windows.
        /// </summary>
        public virtual string SignatureRichText => SignaturePlainText;
        public virtual string SignaturePlainText => GetType().Name;

        public string Signature =>
#if UNITY_EDITOR
            SignatureRichText;
#else
            SignaturePlainText;
#endif

        public SyncMode SyncMode => syncMode;
        internal BindingArchetypeData BindingArchetypeData => archetypeData;
        public Descriptor Descriptor => descriptor;
        protected Logger Logger => logger ??= Log.GetLogger<Binding>();

        protected Binding() { }
        public Binding(Descriptor descriptor, Component unityComponent)
        {
            this.descriptor = descriptor;
            this.unityComponent = unityComponent;
            this.coherenceSync = unityComponent.GetComponentInParent<CoherenceSync>() ?? unityComponent.GetComponent<CoherenceSync>();
            _ = EnsureGuid();
            routing = descriptor.DefaultRouting;
            syncMode = descriptor.DefaultSyncMode;

#if UNITY_EDITOR
            if (descriptor.ShouldDefaultToNoneInterpolation())
            {
                interpolationSettings = InterpolationSettings.Empty;
            }
            else
            {
                interpolationSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<InterpolationSettings>(InterpolationSettings.DefaultSettingsPath);
            }
#endif
        }

        public void Dispose()
        {
            logger?.Dispose();
        }

        internal void AssignComponent(Component component)
        {
            unityComponent = component;
        }

        internal bool EnsureGuid()
        {
            if (string.IsNullOrEmpty(guid))
            {
                guid = Guid.NewGuid().ToString("N");
                return true;
            }

            return false;
        }

        internal virtual bool Activate()
        {
            if (!Application.isPlaying)
            {
                return false;
            }

            if (interpolationSettings == null)
            {
                interpolationSettings = InterpolationSettings.Empty;
            }

            if (CoherenceComponentType == null)
            {
                Logger.Warning(Warning.ToolkitBindingMissingComponent,
                    $"Cannot find component '{CoherenceComponentName}'. This binding will not be synced: {Name}");
            }

            return true;
        }

        internal Type GetUnityComponentType() => descriptor.OwnerType;

        public virtual void CloneTo(Binding clone)
        {
            clone.unityComponent = unityComponent;
            clone.coherenceSync = coherenceSync;

            clone.archetypeData = archetypeData;
            clone.descriptor = descriptor;
            clone.guid = guid;

            clone.interpolationSettings = interpolationSettings;
            clone.predictionMode = predictionMode;

            clone.routing = routing;
            clone.syncMode = syncMode;

            clone.OnBindingCloned();
        }

        public Binding Clone(Component c)
        {
            var newBinding = (Binding)MemberwiseClone();
            newBinding.AssignComponent(c);
            newBinding.guid = Guid.NewGuid().ToString("N");

            return newBinding;
        }

        internal bool CreateArchetypeData(SchemaType schemaType, int maxLods)
        {
            if (archetypeData == null)
            {
                archetypeData = new BindingArchetypeData(schemaType, MonoAssemblyRuntimeType, IsMethod);
                _ = archetypeData.AddLODStep(maxLods);

                return true;
            }

            return false;
        }

        public virtual bool IsCurrentlyPredicted()
        {
            return predictionMode switch
            {
                PredictionMode.Never => false,
                PredictionMode.Always => true,
                PredictionMode.InputAuthority => coherenceSync.HasInputAuthority,
                var _ => throw new ArgumentOutOfRangeException()
            };
        }

        internal virtual bool IsReadyToSample(double currentTime)
        {
            return true;
        }

        internal void ClearSampleTime()
        {
            lastSampledTime = 0;
        }

        internal virtual void ResetInterpolation()
        {

        }

        public virtual void OnConnectedEntityChanged()
        {

        }

        public virtual ICoherenceComponentData CreateComponentData()
        {
            var componentType = CoherenceComponentType;
            if (componentType == null)
            {
                return null;
            }

            return Activator.CreateInstance(componentType) as ICoherenceComponentData;
        }

        protected void RaiseNetworkSampleReceived(object data, bool stopped, AbsoluteSimulationFrame sampleFrame)
        {
            OnNetworkSampleReceived?.Invoke(data, stopped, sampleFrame);
        }

        public virtual void SampleValue() { }
        public virtual void Interpolate(double time) { }
        public virtual void RemoveOutdatedSamples(double time) { }
        public virtual void InvokeValueSyncCallback() { }
        public virtual ICoherenceComponentData WriteComponentData(ICoherenceComponentData coherenceComponent, AbsoluteSimulationFrame simFrame) { return coherenceComponent; }
        public virtual void ReceiveComponentData(ICoherenceComponentData coherenceComponent, AbsoluteSimulationFrame clientFrame, Vector3 floatingOriginDelta) { }
        public virtual MemberInfo GetMemberInfo() { return null; }
        public virtual void SetToLastSample() { }
        public virtual void ResetLastSentData() { }
        public virtual void ValidateNotBound() { }
        public abstract void IsDirty(AbsoluteSimulationFrame simulationFrame, out bool dirty, out bool justStopped);
        public abstract void MarkAsReadyToSend();
        protected virtual void OnBindingCloned() { }
        internal virtual (bool IsValid, string Reason) IsBindingValid() { return (true, string.Empty); }
    }
}
