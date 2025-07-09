// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.ComponentActions
{
    using UnityEngine;

    [ComponentAction(typeof(Collider), "Disable")]
    public class DisableColliderComponentAction : ComponentAction
    {
        public override void OnAuthority()
        {
            var c = component as Collider;
            c.enabled = true;
        }

        public override void OnRemote()
        {
            var c = component as Collider;
            c.enabled = false;
        }
    }
}
