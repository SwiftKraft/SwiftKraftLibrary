// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.ComponentActions
{
    using UnityEngine;

    [ComponentAction(typeof(Behaviour), "Disable")]
    public class DisableBehaviourComponentAction : ComponentAction
    {
        public override void OnAuthority()
        {
            var b = component as Behaviour;
            b.enabled = true;
        }

        public override void OnRemote()
        {
            var b = component as Behaviour;
            b.enabled = false;
        }
    }
}
