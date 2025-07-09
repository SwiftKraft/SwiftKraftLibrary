namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    internal class SyncPrefabInstanceUniqueId
    {
        static SyncPrefabInstanceUniqueId()
        {
            ObjectChangeEvents.changesPublished += ObjectChangeEventsOnchangesPublished;
        }

        private static void ObjectChangeEventsOnchangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; i++)
            {
                var changeEvent = stream.GetEventType(i);

                switch (changeEvent)
                {
                    case ObjectChangeKind.CreateGameObjectHierarchy:
                        {
                            stream.GetCreateGameObjectHierarchyEvent(i, out var data);

                            var go = EditorUtility.InstanceIDToObject(data.instanceId);
                            PrefabHierarchyUpdated(go as GameObject, true);
                            break;
                        }
                    case ObjectChangeKind.UpdatePrefabInstances:
                        {
                            stream.GetUpdatePrefabInstancesEvent(i, out var data);

                            for (int j = 0; j < data.instanceIds.Length; j++)
                            {
                                var instanceId = data.instanceIds[j];
                                var go = EditorUtility.InstanceIDToObject(instanceId);
                                PrefabHierarchyUpdated(go as GameObject, false);
                            }

                            break;
                        }
                    case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                        {
                            stream.GetChangeGameObjectStructureHierarchyEvent(i, out var data);

                            var go = EditorUtility.InstanceIDToObject(data.instanceId);
                            PrefabHierarchyUpdated(go as GameObject, false);
                            break;
                        }
                }
            }
        }

        internal static void SyncInstanceUpdated(CoherenceSync instance, int id, bool forceUpdate = false)
        {
            if (instance == null || !instance.IsUnique || instance.ManualUniqueId == id.ToString())
            {
                return;
            }
          
            using var so = new SerializedObject(instance);

            var property = so.FindProperty("scenePrefabInstanceUUID");

            if (string.IsNullOrEmpty(property.stringValue) || forceUpdate)
            {
                property.stringValue = id.ToString();
                so.ApplyModifiedProperties();
            }
        }

        private static void PrefabHierarchyUpdated(GameObject go, bool forceUpdate)
        {
            if (go == null)
            {
                return;
            }
            
            var syncs = go.GetComponentsInChildren<CoherenceSync>(true);

            foreach (var sync in syncs)
            {
                SyncInstanceUpdated(sync, sync.GetInstanceID(), forceUpdate);
            }
        }
    }
}
