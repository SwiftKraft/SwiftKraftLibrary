// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Bindings
{
    using System;
    using UnityEngine;

    [Serializable]
    public class AnimatorDescriptor : Descriptor
    {
        [SerializeField] private int parameterHash;

        public int ParameterHash => parameterHash;

     //   public override string BakedCSharpType => null;

        public AnimatorDescriptor(Type bindingType, AnimatorControllerParameter parameter) : 
            base(parameter.name, typeof(Animator), bindingType)
        {
            parameterHash = Animator.StringToHash(parameter.name);
        }
    }
}

