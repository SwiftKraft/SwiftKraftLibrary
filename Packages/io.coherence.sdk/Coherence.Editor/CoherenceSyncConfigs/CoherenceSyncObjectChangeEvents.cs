namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using Coherence.Toolkit;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Profiling;

    [InitializeOnLoad]
    internal static class CoherenceSyncObjectChangeEvents
    {
        static CoherenceSyncObjectChangeEvents()
        {
            ObjectChangeEvents.changesPublished += ObjectChangeEventsOnChangesPublished;
        }

        private static void TryUpdateBindings(GameObject gameObject)
        {
            if (!gameObject)
            {
                return;
            }

            var sync = gameObject.GetComponentInParent<CoherenceSync>(true);
            if (sync)
            {
                _ = EditorCache.UpdateBindings(sync);
            }
        }

        private static void ObjectChangeEventsOnChangesPublished(ref ObjectChangeEventStream stream)
        {
            try
            {
                // We're not necessarily importing new assets - but if that happens, we want to batch-import them.
                AssetDatabase.StartAssetEditing();
                for (var i = 0; i < stream.length; i++)
                {
                    var changeKind = stream.GetEventType(i);
                    switch (changeKind)
                    {
                        // this event cannot handle a few operations like
                        // the creation of duplicate prefabs their deletion.
                        // such cases are handled by the AssetModificationProcessor and AssetPostprocessor.

                        case ObjectChangeKind.DestroyGameObjectHierarchy:
                            {
                                stream.GetDestroyGameObjectHierarchyEvent(i, out var ev);
                                var parent = EditorUtility.InstanceIDToObject(ev.parentInstanceId) as GameObject;
                                TryUpdateBindings(parent);
                                break;
                            }
                        case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                            {
                                stream.GetChangeGameObjectStructureHierarchyEvent(i, out var ev);
                                var gameObject = EditorUtility.InstanceIDToObject(ev.instanceId) as GameObject;
                                TryUpdateBindings(gameObject);
                                break;
                            }
                        case ObjectChangeKind.ChangeGameObjectStructure:
                            {
                                stream.GetChangeGameObjectStructureEvent(i, out var ev);
                                var gameObject = EditorUtility.InstanceIDToObject(ev.instanceId) as GameObject;
                                TryUpdateBindings(gameObject);
                                break;
                            }
                        case ObjectChangeKind.CreateGameObjectHierarchy:
                            {
                                stream.GetCreateGameObjectHierarchyEvent(i, out var ev);
                                var gameObject = EditorUtility.InstanceIDToObject(ev.instanceId) as GameObject;
                                TryUpdateBindings(gameObject);
                                break;
                            }
                        case ObjectChangeKind.CreateAssetObject:
                            {
                                stream.GetCreateAssetObjectEvent(i, out var ev);

                                // the Object that is sent as part of this event on prefab creation is a native type
                                // called PrefabInstance. This type exists purely on the unmanaged side of the engine,
                                // hence there's no way we can retrieve managed type information from it.
                                // however, Unity's serialization system allows us to access its data.
                                var obj = EditorUtility.InstanceIDToObject(ev.instanceId);
                                using var serializedObject = new SerializedObject(obj);
                                using var sourcePrefabProperty = serializedObject.FindProperty("m_SourcePrefab");

                                // if there's a properly constructed SerializedProperty,
                                // we know we're handling a prefab creation.
                                if (sourcePrefabProperty == null || !sourcePrefabProperty.objectReferenceValue)
                                {
                                    break;
                                }

                                // PrefabInstance's m_SourcePrefab points to an instance of the native type Prefab,
                                // so we access its data through the serialization APIs once again
                                using var sourcePrefabSerializedObject =
                                    new SerializedObject(sourcePrefabProperty.objectReferenceValue);
                                using var rootGameObjectProperty =
                                    sourcePrefabSerializedObject.FindProperty("m_RootGameObject");
                                if (rootGameObjectProperty == null || !rootGameObjectProperty.objectReferenceValue)
                                {
                                    break;
                                }

                                var gameObject = rootGameObjectProperty.objectReferenceValue as GameObject;
                                TryUpdateBindings(gameObject);
                                break;
                            }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
}
