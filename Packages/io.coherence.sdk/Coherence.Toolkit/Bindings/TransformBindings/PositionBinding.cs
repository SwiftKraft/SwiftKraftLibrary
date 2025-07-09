namespace Coherence.Toolkit.Bindings.TransformBindings
{
    using Coherence.Entities;
    using Coherence.SimulationFrame;
    using Interpolation;
    using System;
    using UnityEngine;
    using ValueBindings;

    [Serializable]
    public class PositionBinding : Vector3Binding
    {
        public override string CoherenceComponentName => "WorldPosition";
        public override string MemberNameInComponentData => "value";
        public override string MemberNameInUnityComponent => nameof(CoherenceSync.coherencePosition);
        public override string BakedSyncScriptGetter => nameof(CoherenceSync.coherencePosition);
        public override string BakedSyncScriptSetter => nameof(CoherenceSync.coherencePosition);

        protected PositionBinding() { }

        public PositionBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent) { }

        public override Vector3 Value
        {
            get => coherenceSync.coherencePosition;
            set => coherenceSync.coherencePosition = value;
        }

        protected override (Vector3 value, AbsoluteSimulationFrame simFrame) ReadComponentData(ICoherenceComponentData coherenceComponent, Vector3 floatingOriginDelta)
        {
            var (position, simFrame) = base.ReadComponentData(coherenceComponent, floatingOriginDelta);

            if (!coherenceSync.HasParentWithCoherenceSync)
            {
                position += floatingOriginDelta;
            }

            return (position, simFrame);
        }

        public void ShiftSamples(Vector3 delta)
        {
            static Vector3 ShiftValue(Vector3 value, Vector3 shift) => value - shift;
            static Sample<Vector3> ShiftSample(Sample<Vector3> sample, Vector3 shift) => new(ShiftValue(sample.Value, shift), sample.Stopped, sample.Time, sample.Latency);

            var buffer = Interpolator.Buffer;
            for (var index = 0; index < buffer.Count; index++)
            {
                buffer[index] = ShiftSample(buffer[index], delta);
            }

            if (buffer.VirtualSamples.HasValue)
            {
                buffer.VirtualSamples =
                    (ShiftSample(buffer.VirtualSamples.Value.First, delta),
                    ShiftSample(buffer.VirtualSamples.Value.Second, delta));
            }

            if (Interpolator.HasLastInterpolatedValue)
            {
                Interpolator.LastInterpolatedValue = ShiftValue(Interpolator.LastInterpolatedValue, delta);
            }
        }

        public void TransformSamples(Matrix4x4 transform, bool transformLastSampleToo)
        {
            static Vector3 TransformValue(Vector3 value, Matrix4x4 transform) => transform.MultiplyPoint3x4(value);
            static Sample<Vector3> TransformSample(Sample<Vector3> sample, Matrix4x4 transform) => new(TransformValue(sample.Value, transform), sample.Stopped, sample.Time, sample.Latency);

            var buffer = Interpolator.Buffer;

            var count = (transformLastSampleToo ? buffer.Count : buffer.Count - 1);

            for (var index = 0; index < count; index++)
            {
                buffer[index] = TransformSample(buffer[index], transform);
            }

            if (buffer.VirtualSamples.HasValue)
            {
                var (virtual1, virtual2) = buffer.VirtualSamples.Value;

                var newVirtual1 = TransformSample(virtual1, transform);
                Sample<Vector3> newVirtual2;

                if (!transformLastSampleToo && buffer.Last.HasValue && virtual2.Time >= buffer.Last.Value.Time)
                {
                    newVirtual2 = new Sample<Vector3>(Interpolator.GetSecondVirtualSampleValue(virtual2.Time),
                        virtual2.Stopped, virtual2.Time, virtual2.Latency);
                }
                else
                {
                    newVirtual2 = TransformSample(buffer.VirtualSamples.Value.Second, transform);
                }

                buffer.VirtualSamples = (newVirtual1, newVirtual2);
            }

            if (Interpolator.HasLastInterpolatedValue)
            {
                Interpolator.LastInterpolatedValue = TransformValue(Interpolator.LastInterpolatedValue, transform);
            }

            Interpolator.Smoothing.CurrentVelocity = transform.MultiplyVector(Interpolator.Smoothing.CurrentVelocity);
        }

        public override void OnConnectedEntityChanged()
        {
            MarkAsReadyToSend();
        }

        internal override (bool IsValid, string Reason) IsBindingValid()
        {
            var isValid = unityComponent.TryGetComponent(out CoherenceSync _);
            var reason = isValid ? string.Empty : "World position binding shouldn't be in a child object.";

            return (isValid, reason);
        }
    }
}
