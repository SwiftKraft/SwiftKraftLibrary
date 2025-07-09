// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.ComponentActions
{
    using UnityEngine;

    [ComponentAction(typeof(Renderer), "Handle Material")]
    public class MaterialRendererComponentAction : ComponentAction
    {
        public Material authority;
        public Material remote;

        public override void OnAuthority()
        {
            var r = component as Renderer;
            r.material = authority;
        }

        public override void OnRemote()
        {
            var r = component as Renderer;
            r.material = remote;
        }
    }
}
