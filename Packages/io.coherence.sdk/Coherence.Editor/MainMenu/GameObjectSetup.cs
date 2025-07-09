// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using Toolkit;
    using UnityEditor;
    using UnityEngine;

    public class GameObjectSetup
    {
        internal static bool ObjectHasNoSync()
        {
            var target = Selection.activeGameObject;
            if (target == null)
            {
                return false;
            }
            return target.GetComponent<CoherenceSync>() == null;
        }

        internal static void AddCoherenceSync()
        {
            var parent = Selection.activeGameObject;

            if (parent == null)
            {
                Debug.Log("Please select the prefab or GameObject that you'd like to add CoherenceSync to.");
                return;
            }

            ObjectFactory.AddComponent<CoherenceSync>(parent);
        }
    }
}
