namespace Coherence.Toolkit.Bindings.TransformBindings
{
    using Coherence.Interpolation;
    using System;
    using System.Reflection;
    using UnityEngine;
    using ValueBindings;

    [Serializable]
    public class RotationBinding : QuaternionBinding
    {
        public override string CoherenceComponentName => "WorldOrientation";
        public override string MemberNameInComponentData => "value";
        public override string MemberNameInUnityComponent => nameof(CoherenceSync.coherenceRotation);
        public override string BakedSyncScriptGetter => nameof(CoherenceSync.coherenceRotation);
        public override string BakedSyncScriptSetter => nameof(CoherenceSync.coherenceRotation);

        protected RotationBinding() {}

        public RotationBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent) {}

        public override Quaternion Value
        {
            get => coherenceSync.coherenceRotation;
            set => coherenceSync.coherenceRotation = value;
        }

        public void RotateSamples(Quaternion delta, bool transformLastSampleToo)
        {
            static Quaternion RotateValue(Quaternion value, Quaternion delta) => delta * value;
            static Sample<Quaternion> RotateSample(Sample<Quaternion> sample, Quaternion delta) => new(RotateValue(sample.Value, delta), sample.Stopped, sample.Time, sample.Latency);

            var buffer = Interpolator.Buffer;

            var count = (transformLastSampleToo ? buffer.Count : buffer.Count - 1);

            for (var index = 0; index < count; index++)
            {
                buffer[index] = RotateSample(buffer[index], delta);
            }

            if (buffer.VirtualSamples.HasValue)
            {
                var (virtual1, virtual2) = buffer.VirtualSamples.Value;

                var newVirtual1 = RotateSample(virtual1, delta);
                Sample<Quaternion> newVirtual2;

                if (!transformLastSampleToo && buffer.Last.HasValue && virtual2.Time >= buffer.Last.Value.Time)
                {
                    newVirtual2 = new Sample<Quaternion>(Interpolator.GetSecondVirtualSampleValue(virtual2.Time),
                        virtual2.Stopped, virtual2.Time, virtual2.Latency);
                }
                else
                {
                    newVirtual2 = RotateSample(buffer.VirtualSamples.Value.Second, delta);
                }

                buffer.VirtualSamples = (newVirtual1, newVirtual2);
            }

            if (Interpolator.HasLastInterpolatedValue)
            {
                Interpolator.LastInterpolatedValue = RotateValue(Interpolator.LastInterpolatedValue, delta);
            }

            Interpolator.Smoothing.CurrentVelocity = delta * Interpolator.Smoothing.CurrentVelocity;
        }

        public override void OnConnectedEntityChanged()
        {
            MarkAsReadyToSend();
        }

        internal override (bool, string) IsBindingValid()
        {
            bool isValid = unityComponent.transform.parent == null;
            string reason = string.Empty;

            if (!isValid)
            {
                reason = "World rotation binding shouldn't be in a child object.";
            }

            return (isValid, reason);
        }
    }
}
