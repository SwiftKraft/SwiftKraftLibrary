namespace Coherence.Toolkit.Bindings.TransformBindings
{
    using System;
    using System.Reflection;
    using UnityEngine;
    using ValueBindings;

    [Serializable]
    public class DeepRotationBinding : QuaternionBinding
    {
        public override string BakedSyncScriptGetter =>  nameof(Transform.localRotation);
        public override string BakedSyncScriptSetter => nameof(Transform.localRotation);

        protected DeepRotationBinding() {}

        public DeepRotationBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent) {}

        public override Quaternion Value
        {
            get => ((Transform)unityComponent).localRotation;
            set => ((Transform)unityComponent).localRotation = value;
        }

        internal override (bool, string) IsBindingValid()
        {
            bool isValid = unityComponent.transform.parent != null;
            string reason = string.Empty;

            if (!isValid)
            {
                reason = "Local rotation binding shouldn't be in a root object.";
            }

            return (isValid, reason);
        }
    }
}
