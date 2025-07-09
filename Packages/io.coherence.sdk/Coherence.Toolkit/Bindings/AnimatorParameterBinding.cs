namespace Coherence.Toolkit.Bindings
{
    using System;
    using UnityEngine;
    using ValueBindings;

    [Serializable]
    public class IntAnimatorParameterBinding : IntBinding
    {
        public override string BakedSyncScriptGetter => "GetInteger(CastedDescriptor.ParameterHash)";
        public override string BakedSyncScriptSetter => "SetInteger(CastedDescriptor.ParameterHash, @)";

        protected AnimatorDescriptor CastedDescriptor
        {
            get
            {
                if (castedDescriptor == null)
                {
                    castedDescriptor = (AnimatorDescriptor)descriptor;
                }

                return castedDescriptor;
            }
        }
        
        private AnimatorDescriptor castedDescriptor;
        
        protected IntAnimatorParameterBinding() {}
        public IntAnimatorParameterBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }
        
        public override int Value
        {
            get => ((Animator)unityComponent).GetInteger(CastedDescriptor.ParameterHash);
            set => ((Animator)unityComponent).SetInteger(CastedDescriptor.ParameterHash, value);
        }
    }

    [Serializable]
    public class FloatAnimatorParameterBinding : FloatBinding
    {
        public override string BakedSyncScriptGetter => "GetFloat(CastedDescriptor.ParameterHash)";
        public override string BakedSyncScriptSetter => "SetFloat(CastedDescriptor.ParameterHash, @)";
        
        protected AnimatorDescriptor CastedDescriptor
        {
            get
            {
                if (castedDescriptor == null)
                {
                    castedDescriptor = (AnimatorDescriptor)descriptor;
                }

                return castedDescriptor;
            }
        }

        private AnimatorDescriptor castedDescriptor;

        protected FloatAnimatorParameterBinding() {}
        public FloatAnimatorParameterBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }
        
        public override float Value
        {
            get => ((Animator)unityComponent).GetFloat(CastedDescriptor.ParameterHash);
            set => ((Animator)unityComponent).SetFloat(CastedDescriptor.ParameterHash, value);
        }
    }

    [Serializable]
    public class BoolAnimatorParameterBinding : BoolBinding
    {
        public override string BakedSyncScriptGetter => "GetBool(CastedDescriptor.ParameterHash)";
        public override string BakedSyncScriptSetter => "SetBool(CastedDescriptor.ParameterHash, @)";
        
        protected AnimatorDescriptor CastedDescriptor
        {
            get
            {
                if (castedDescriptor == null)
                {
                    castedDescriptor = (AnimatorDescriptor)descriptor;
                }

                return castedDescriptor;
            }
        }
        
        private AnimatorDescriptor castedDescriptor;
        
        protected BoolAnimatorParameterBinding() {}
        public BoolAnimatorParameterBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }
        
        public override bool Value
        {
            get => ((Animator)unityComponent).GetBool(CastedDescriptor.ParameterHash);
            set => ((Animator)unityComponent).SetBool(CastedDescriptor.ParameterHash, value);
        }
    }
}
