// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEngine;
    using UnityEditor;
    using Coherence.Toolkit;

    [InitializeOnLoad]
    internal static class ComponentAddedReporter
    {
        private class ComponentAddedEventProperties : Analytics.BaseProperties
        {
            public int gameobject_iid;
            public string name;
            public string type;
            public string prefab_asset_type;
            public string prefab_instance_status;
        }

        static ComponentAddedReporter()
        {
            CoherenceBehaviour.OnReset += OnBehaviourReset;
        }

        private static void OnBehaviourReset(CoherenceBehaviour behaviour)
        {
            if (!behaviour)
            {
                return;
            }

            if ((behaviour.hideFlags & HideFlags.DontSave) != 0)
            {
                return;
            }

            Analytics.Capture(new Analytics.Event<ComponentAddedEventProperties>(
                Analytics.Events.ComponentAdded,
                new ComponentAddedEventProperties
                {
                    gameobject_iid = behaviour.gameObject.GetInstanceID(),
                    name = behaviour.name,
                    type = behaviour.GetType().Name,
                    prefab_asset_type = PrefabUtility.GetPrefabAssetType(behaviour).ToString(),
                    prefab_instance_status = PrefabUtility.GetPrefabInstanceStatus(behaviour).ToString(),
                }
            ));
        }
    }
}
