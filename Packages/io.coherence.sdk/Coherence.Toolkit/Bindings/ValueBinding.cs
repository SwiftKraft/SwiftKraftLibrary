namespace Coherence.Toolkit.Bindings
{
    using Log;
    using System;
    using System.Linq;
    using System.Reflection;
    using Interpolation;
    using SimulationFrame;
    using Entities;
    using UnityEngine;

    /// <summary>
    /// Base class used for all synced fields, properties and parameters.
    /// </summary>
    /// <typeparam name="T">The coherence value type used to serialize the field or property.
    /// The coherence value type may differ from the unity value type.
    /// For example, all integer types: uint, short, ushort, byte, char are backed by int as their coherence value type.
    /// </typeparam>
    public abstract class ValueBinding<T> : Binding
    {
        /// <summary>
        /// The current value of the target field/property on the binding's Unity component.
        /// This is generally the value you'll see in the inspector.
        /// </summary>
        public virtual T Value { get; set; }

        /// <summary>
        /// The current Value expressed as an object.
        /// This is useful when the exact binding type is unknown (e.g., in unit tests) but should be avoided in hotpaths due to boxing.
        /// </summary>
        public override object UntypedValue => Value;

        public override string SignatureRichText => $"{Name} <color=grey>{TypeUtils.GetNiceTypeString(GetValueTypeOrNull())}</color>";
        public override string SignaturePlainText => $"{Name} ({TypeUtils.GetNiceTypeString(GetValueTypeOrNull())})";

        /// <summary>
        /// Returns a FieldInfo or PropertyInfo depending on the member type of the target member on the unity component.
        /// The MemberInfo is retrieved using reflection but cached to speed up subsequent access.
        /// </summary>
        /// <returns>The MemberInfo for the target field or property, or null for Custom member types (e.g., animation parameters).</returns>
        /// <exception cref="Exception">Thrown if the member type is neither Field, Property or Custom.</exception>
        public override MemberInfo GetMemberInfo()
        {
            if (descriptor == null)
            {
                return null;
            }

            return descriptor.MemberType switch
            {
                MemberTypes.Field => GetFieldInfo(),
                MemberTypes.Property => GetPropertyInfo(),
                MemberTypes.Custom => null,
                _ => throw new Exception("Unexpected member kind: " + descriptor.MemberType)
            };
        }

        private BindingInterpolator<T> interpolator;
        public BindingInterpolator<T> Interpolator => interpolator;

        private FieldInfo fieldInfo;
        private PropertyInfo propertyInfo;
        private FieldInfo componentFieldInfo;
        private FieldInfo componentFieldSimulationFrameInfo;
        private Type valueType;
        private MethodInfo cachedCallback;
        private T lastSentCompressed;
        private T lastCheckedForDirty;
        private bool performedInitialSync;
        protected T valueSyncOld;
        protected T valueSyncNew;
        protected bool valueSyncPrepared;

        // For interpolated values when the sampled values are no longer
        // changing we set a stopped flag and message this state to the
        // replicated fields to prevent extrapolation.
        private bool stopped;

        protected ValueBinding() { }
        public ValueBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        internal override bool Activate()
        {
            if (!base.Activate())
            {
                return false;
            }

            interpolator = new BindingInterpolator<T>(interpolationSettings, archetypeData.SampleRate);

            return true;
        }

        public override bool Equals(object other)
        {
            if (other is ValueBinding<T> otherBinding)
            {
                return descriptor == otherBinding.descriptor &&
                       FullName == otherBinding.FullName &&
                       unityComponent == otherBinding.unityComponent;
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = descriptor?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ FullName.GetHashCode();
                hashCode = (hashCode * 397) ^ unityComponent.GetHashCode();

                return hashCode;
            }
        }

        public override void CloneTo(Binding binding)
        {
            base.CloneTo(binding);
        }

        private Type GetValueType()
        {
            if (descriptor == null)
            {
                return null;
            }

            return valueType ??= descriptor.MemberType switch
            {
                MemberTypes.Field => GetFieldInfo()?.FieldType,
                MemberTypes.Property => GetPropertyInfo()?.PropertyType,
                MemberTypes.Custom => null,
                _ => throw new Exception("Unexpected member kind: " + descriptor.MemberType)
            };
        }

        /// <summary>
        /// Gets the type of the member that the binding targets if possible; otherwise, <see cref="null"/>.
        /// <para>
        /// This is only intended to be used in contexts like <see cref="ToString"/>, and in particular situations
        /// where we want to acquire information about an invalid binding for debugging purposes.
        /// </para>
        /// </summary>
        private Type GetValueTypeOrNull()
        {
            try
            {
                return GetValueType();
            }
            catch
            {
                return null;
            }
        }

        private FieldInfo GetFieldInfo()
        {
            if (fieldInfo != null)
            {
                return fieldInfo;
            }

            fieldInfo = GetUnityComponentType()?.GetField(Name, BindingFlags.Public | BindingFlags.Instance);
            if (fieldInfo == null)
            {
                Logger.Warning(Warning.ToolkitBindingFailedToReloadFieldInfo,
                    $"{GetUnityComponentType()}.{Name} failed to reload FieldInfo.");
            }

            return fieldInfo;
        }

        private PropertyInfo GetPropertyInfo()
        {
            if (propertyInfo != null)
            {
                return propertyInfo;
            }

            propertyInfo = GetUnityComponentType()?.GetProperty(Name, BindingFlags.Public | BindingFlags.Instance);
            if (propertyInfo == null)
            {
                Logger.Warning(Warning.ToolkitBindingFailedToReloadPropertyInfo,
                        $"{GetUnityComponentType()}.{Name} failed to reload PropertyInfo.");
            }

            return propertyInfo;
        }

        protected object GetValueUsingReflection()
        {
            if (descriptor == null)
            {
                return null;
            }

            return descriptor.MemberType switch
            {
                MemberTypes.Field => GetFieldInfo()?.GetValue(unityComponent),
                MemberTypes.Property => GetPropertyInfo()?.GetValue(unityComponent),
                _ => throw new Exception("Unexpected member kind: " + descriptor.MemberType)
            };
        }

        protected void SetValueUsingReflection(object value)
        {
            if (descriptor == null)
            {
                return;
            }

            switch (descriptor.MemberType)
            {
                case MemberTypes.Field:
                    GetFieldInfo()?.SetValue(unityComponent, value);
                    break;
                case MemberTypes.Property:
                    GetPropertyInfo()?.SetValue(unityComponent, value);
                    break;
                default:
                    throw new Exception("Unexpected member kind: " + descriptor.MemberType);
            }
        }

        public override void SetToLastSample()
        {
            var lastSample = Interpolator.GetLastSample();
            var oldValue = Value;

            if (!lastSample.HasValue || !DiffersFrom(oldValue, lastSample.Value.Value))
            {
                return;
            }

            var newValue = lastSample.Value.Value;
            Value = newValue;
            PrepareValueSyncCallback(oldValue, newValue);
        }

        public override void ResetLastSentData()
        {
            var lastSample = Interpolator.GetLastSample();
            if (lastSample.HasValue)
            {
                lastCheckedForDirty = lastSample.Value.Value;
                lastSentCompressed = GetCompressedValue(lastSample.Value.Value);
            }

            performedInitialSync = true;
        }

        private FieldInfo GetComponentDataFieldInfo(ICoherenceComponentData componentData)
        {
            if (descriptor == null)
            {
                return null;
            }

            if (componentFieldInfo == null)
            {
                var fieldName = SchemaFieldName;
                componentFieldInfo = componentData.GetType().GetField(fieldName);
                if (componentFieldInfo == null)
                {
                    Logger.Warning(Warning.ToolkitBindingFailedToReloadComponentFieldInfo,
                            $"{GetUnityComponentType()}.{Signature} failed to reload ComponentFieldInfo: {componentData.GetType()}.{fieldName}");
                }
            }

            return componentFieldInfo;
        }

        private FieldInfo GetComponentDataFieldSimulationFrameInfo(ICoherenceComponentData componentData)
        {
            if (descriptor == null)
            {
                return null;
            }

            if (componentFieldSimulationFrameInfo == null)
            {
                componentFieldSimulationFrameInfo = componentData.GetType().GetField(SchemaFieldSimulationFrameName);
                if (componentFieldSimulationFrameInfo == null)
                {
                    Logger.Warning(Warning.ToolkitBindingFailedToReloadComponentFieldSimFrameInfo,
                        $"{GetUnityComponentType()}.{Signature} failed to reload " +
                            $"ComponentFieldSimulationFrameInfo: {componentData.GetType()}.{SchemaFieldSimulationFrameName}");
                }
            }

            return componentFieldSimulationFrameInfo;
        }

        public override void ReceiveComponentData(ICoherenceComponentData coherenceComponent, AbsoluteSimulationFrame clientFrame, Vector3 floatingOriginDelta)
        {
            // Set the stopped state of the replicated binding.
            stopped = (coherenceComponent.StoppedMask & FieldMask) != 0;

            var (value, simFrame) = ReadComponentData(coherenceComponent, floatingOriginDelta);
            ReceiveSampleFromNetwork(value, stopped, simFrame, clientFrame);
        }

        public void ReceiveSampleFromNetwork(T data, bool stopped, AbsoluteSimulationFrame sampleFrame, AbsoluteSimulationFrame clientFrame)
        {
            if (!IsCurrentlyPredicted())
            {
                Interpolator.AppendSample(data, stopped, sampleFrame, clientFrame);
            }

            RaiseNetworkSampleReceived(data, stopped, sampleFrame);
        }

        internal T PeekComponentData(ICoherenceComponentData coherenceComponent)
        {
            return ReadComponentData(coherenceComponent, default).value;
        }

        protected virtual (T value, AbsoluteSimulationFrame simFrame) ReadComponentData(ICoherenceComponentData coherenceComponent, Vector3 floatingOriginDelta)
        {
            return ((T)GetComponentDataFieldInfo(coherenceComponent).GetValue(coherenceComponent),
                (AbsoluteSimulationFrame)GetComponentDataFieldSimulationFrameInfo(coherenceComponent).GetValue(coherenceComponent));
        }

        public override ICoherenceComponentData WriteComponentData(ICoherenceComponentData coherenceComponent, AbsoluteSimulationFrame simFrame)
        {
            var value = Interpolator.IsInterpolationNone ? Value : GetInterpolatedAt(simFrame / InterpolationSettings.SimulationFramesPerSecond);
            GetComponentDataFieldInfo(coherenceComponent).SetValue(coherenceComponent, value);

            GetComponentDataFieldSimulationFrameInfo(coherenceComponent).SetValue(coherenceComponent, simFrame);

            return coherenceComponent;
        }

        internal override bool IsReadyToSample(double currentTime)
        {
            if (!base.IsReadyToSample(currentTime))
            {
                return false;
            }

            var minTimeDiff = 1d / Interpolator.SampleRate;
            var diff = currentTime - lastSampledTime;
            var readyToSample = diff >= minTimeDiff;

            if (readyToSample)
            {
                lastSampledTime = currentTime - (diff % minTimeDiff); // Correct for time drift.
            }

            return readyToSample;
        }

        public override void SampleValue()
        {
            if (!Interpolator.IsInterpolationNone)
            {
                var time = coherenceSync.CoherenceBridge.NetworkTime.TimeAsDouble;
                Interpolator.AppendSample(Value, stopped, isSampleTimeValid: true, time, time);
            }
        }

        public override void Interpolate(double time)
        {
            T oldValue = Value;
            T newValue;

            if (Interpolator.IsInterpolationNone)
            {
                var lastSample = Interpolator.GetLastSample();
                newValue = lastSample.HasValue ? lastSample.Value.Value : oldValue;
            }
            else
            {
                newValue = Interpolate(time, oldValue);
            }

            var shouldClamp = !archetypeData.IsFloatType || archetypeData.FloatCompression == FloatCompression.FixedPoint;
            if (shouldClamp)
            {
                newValue = ClampToRange(newValue, archetypeData.MinRange, archetypeData.MaxRange);
            }

            if (DiffersFrom(oldValue, newValue))
            {
                Value = newValue;
                PrepareValueSyncCallback(oldValue, newValue);
            }
        }

        public override void RemoveOutdatedSamples(double time)
        {
            if (!Interpolator.IsInterpolationNone)
            {
                Interpolator.RemoveOutdatedSamples(time);
            }
        }

        protected virtual T ClampToRange(in T value, long minRange, long maxRange)
        {
            return value;
        }

        protected T Interpolate(double time, T currentValue)
        {
            return Interpolator.PerformInterpolation(currentValue, time);
        }

        public T GetInterpolatedAt(double time)
        {
            return Interpolator.GetValueAt(time);
        }

        protected MethodInfo GetCallbackMethodInfo()
        {
            if (descriptor == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(descriptor.ValueSyncCallbackName))
            {
                return null;
            }

            if (cachedCallback == null)
            {
                cachedCallback = GetUnityComponentType()
                    .GetMethods(Descriptor.OnValueSyncedCallbackBindingFlags)
                    .FirstOrDefault(m => m.Name == descriptor.ValueSyncCallbackName);
            }

            return cachedCallback;
        }

        private void PrepareValueSyncCallback(T currentValue, T newValue)
        {
            valueSyncOld = currentValue;
            valueSyncNew = newValue;
            valueSyncPrepared = true;
        }

        public override void InvokeValueSyncCallback()
        {
            if (!valueSyncPrepared)
            {
                return;
            }

            try
            {
                _ = GetCallbackMethodInfo()?.Invoke(UnityComponent, new object[]
                {
                    valueSyncOld,
                    valueSyncNew
                });
            }
            catch (Exception handlerException)
            {
                Logger.Error(Error.ToolkitBindingOnValueSyncedException,
                    ("exception", handlerException));
            }

            valueSyncPrepared = false;
        }

        public override void ValidateNotBound()
        {
            var callbackAttribute = GetMemberInfo()?.GetCustomAttribute<OnValueSyncedAttribute>();
            if (callbackAttribute == null)
            {
                return;
            }

            if (!callbackAttribute.SuppressNotBoundError)
            {
                Logger.Warning(Warning.ToolkitBindingOnValueSyncedNotSynced,
                    $"'{FullName}' has a {nameof(OnValueSyncedAttribute)}, but is not marked as synced. " +
                        $"Consider syncing this member or removing the attribute. " +
                        $"To suppress this error please use the '{nameof(OnValueSyncedAttribute)}.{nameof(OnValueSyncedAttribute.SuppressNotBoundError)}",
                    ("component", UnityComponent));
            }
        }

        public override void IsDirty(AbsoluteSimulationFrame simulationFrame, out bool dirty, out bool justStopped)
        {
            var isInterpolated = !Interpolator.IsInterpolationNone;

            justStopped = false;

            T value;
            if (!isInterpolated)
            {
                value = Value;
            }
            else
            {
                // If there are no samples in the buffer we are not ready for sending an update.
                // This could happen with entities which are sampled in FixedUpdate, so it cannot
                // be assumed that we will sample at least once before the LateUpdate because FixedUpdate
                // doesn't have to be called.
                if (Interpolator.Buffer.Count == 0)
                {
                    dirty = false;
                    return;
                }

                value = GetInterpolatedAt(simulationFrame / InterpolationSettings.SimulationFramesPerSecond);
                RemoveOutdatedSamples(simulationFrame / InterpolationSettings.SimulationFramesPerSecond);
            }

            var changed = DiffersFrom(value, lastCheckedForDirty);

            if (!changed && performedInitialSync)
            {
                dirty = false;

                if (isInterpolated && !stopped)
                {
                    stopped = true;
                    justStopped = true;
                }

                return;
            }

            lastCheckedForDirty = value;

            var currentCompressed = GetCompressedValue(value);
            var changedCompressed = DiffersFrom(lastSentCompressed, currentCompressed);

            if (!changedCompressed && performedInitialSync)
            {
                dirty = false;

                if (isInterpolated && !stopped)
                {
                    stopped = true;
                    justStopped = true;
                }

                return;
            }

            lastSentCompressed = currentCompressed;

            stopped = false;
            performedInitialSync = true;
            dirty = true;
        }

        public override void MarkAsReadyToSend()
        {
            performedInitialSync = false;
            lastSentCompressed = default;
            lastCheckedForDirty = default;
            ClearSampleTime();
        }

        protected abstract bool DiffersFrom(T first, T second);

        protected virtual T GetCompressedValue(T value)
        {
            return value;
        }

        internal override void ResetInterpolation()
        {
            base.ResetInterpolation();

            Interpolator?.Reset();
        }
    }
}
