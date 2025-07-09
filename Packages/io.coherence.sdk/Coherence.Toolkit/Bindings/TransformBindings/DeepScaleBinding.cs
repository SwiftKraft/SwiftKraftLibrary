namespace Coherence.Toolkit.Bindings.TransformBindings
{
    using System;
    using System.Reflection;
    using UnityEngine;
    using ValueBindings;

    [Serializable]
    public class DeepScaleBinding : Vector3Binding
    {
        public override string BakedSyncScriptGetter => nameof(Transform.localScale);
        public override string BakedSyncScriptSetter => nameof(Transform.localScale);

        protected DeepScaleBinding() {}

        public DeepScaleBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent) {}

        public override Vector3 Value
        {
            get => ((Transform)unityComponent).localScale;
            set => ((Transform)unityComponent).localScale = value;
        }

        internal override (bool, string) IsBindingValid()
        {
            bool isValid = unityComponent.transform.parent != null;
            string reason = string.Empty;

            if (!isValid)
            {
                reason = "Local scale binding shouldn't be in a root object.";
            }

            return (isValid, reason);
        }
    }
}
