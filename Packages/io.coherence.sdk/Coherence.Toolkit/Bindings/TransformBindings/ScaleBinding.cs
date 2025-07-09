namespace Coherence.Toolkit.Bindings.TransformBindings
{
    using System;
    using System.Reflection;
    using UnityEngine;
    using ValueBindings;

    [Serializable]
    public class ScaleBinding : Vector3Binding
    {
        public override string CoherenceComponentName => "GenericScale";
        public override string MemberNameInComponentData => "value";
        public override string MemberNameInUnityComponent => nameof(CoherenceSync.coherenceLocalScale);
        public override string BakedSyncScriptGetter => nameof(CoherenceSync.coherenceLocalScale);
        public override string BakedSyncScriptSetter => nameof(CoherenceSync.coherenceLocalScale);

        protected ScaleBinding() {}

        public ScaleBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent) {}

        public override Vector3 Value
        {
            get => coherenceSync.coherenceLocalScale;
            set => coherenceSync.coherenceLocalScale = value;
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
                reason = "World scale binding shouldn't be in a child object.";
            }

            return (isValid, reason);
        }
    }
}
