// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using UnityEngine;

    [System.Serializable]
    public abstract class ComponentAction
    {
        [SerializeField] internal Component component;
        public Component Component => component;

        public virtual void OnAuthority() { }
        public virtual void OnRemote() { }
    }
}
