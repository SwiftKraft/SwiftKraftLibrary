// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEngine;

    public class GameObjectModule : HubModule
    {
        public override string ModuleName => "GameObject";

        public override HelpSection Help => new()
        {
            title = new GUIContent("This tab has been deprecated."),
            content = new GUIContent("Please, go to the Hub."),
        };
    }
}
