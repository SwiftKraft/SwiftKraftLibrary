namespace Coherence.Toolkit.Bindings.TransformBindings
{
    using System;
    using System.Reflection;
    using UnityEngine;
    using ValueBindings;

    [Serializable]
    public class DeepPositionBinding : Vector3Binding
    {
        public override string BakedSyncScriptGetter => nameof(Transform.localPosition);
        public override string BakedSyncScriptSetter => nameof(Transform.localPosition);

        protected DeepPositionBinding() {}

        public DeepPositionBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent) {}

        public override Vector3 Value
        {
            get => ((Transform)unityComponent).localPosition;
            set => ((Transform)unityComponent).localPosition = value;
        }

        internal override (bool, string) IsBindingValid()
        {
            bool isValid = unityComponent.transform.parent != null;
            string reason = string.Empty;

            if (!isValid)
            {
                reason = "Local position binding shouldn't be in a root object.";
            }

            return (isValid, reason);
        }
    }
}
