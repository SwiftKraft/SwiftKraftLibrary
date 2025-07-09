// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEngine;

    internal class GameObjectCreationCommands
    {
        // NOTE this pretends to mimic Unity's internal GOCreationCommands.Place
        // However, it doesn't even get close to what their implementation does.
        public static void Place(GameObject go, GameObject parent)
        {
            if (go && parent)
            {
                go.transform.SetParent(parent.transform, false);
            }
        }
    }
}
