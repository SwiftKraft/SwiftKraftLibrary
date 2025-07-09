// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.ComponentActions
{
    using UnityEngine;

    [ComponentAction(typeof(Renderer), "Disable")]
    public class DisableRendererComponentAction : ComponentAction
    {
        public override void OnAuthority()
        {
            var r = component as Renderer;
            r.enabled = true;
        }

        public override void OnRemote()
        {
            var r = component as Renderer;
            r.enabled = false;
        }
    }
}
