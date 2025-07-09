namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public class OnPrefabInstanceUpdated
    {
        static OnPrefabInstanceUpdated()
        {
            PrefabUtility.prefabInstanceUpdated += PrefabInstanceUpdated;
        }

        private static void PrefabInstanceUpdated(GameObject instance)
        {
            var coherenceSync = instance.GetComponent<CoherenceSync>();
            if (coherenceSync != null && PrefabUtility.HasPrefabInstanceAnyOverrides(instance, false))
            {
                var property = new SerializedObject(coherenceSync).FindProperty("coherenceSyncConfig");

                if (property.objectReferenceValue == null)
                {
                    PrefabUtility.RevertPropertyOverride(property, InteractionMode.AutomatedAction);
                }
            }
        }
    }
}
