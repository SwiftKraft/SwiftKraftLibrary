// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    internal static class OverrideActions
    {
        internal static void OpenContextMenu(Rect controlRect, SerializedProperty serializedProperty, CoherenceSync sync, bool isInstance)
        {
            var rightClicks = Event.current.type == EventType.ContextClick && controlRect.Contains(Event.current.mousePosition);
            if (!rightClicks)
            {
                return;
            }

            Event.current.Use();

            if (!GUI.enabled)
            {
                return;
            }

            if (serializedProperty == null || !serializedProperty.prefabOverride)
            {
                return;
            }

            var menu = new GenericMenu();
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(sync);

            if (prefab)
            {
                var applyContent = new GUIContent(prefab ? $"Apply to Prefab '{prefab.name}'" : "Apply to Prefab");
                var revertContent = new GUIContent("Revert");
                if (isInstance)
                {
                    menu.AddDisabledItem(applyContent);
                }
                else
                {
                    menu.AddItem(applyContent, false, (obj) => OnContextMenuApply(obj, sync), serializedProperty.Copy());
                }


                if (HasOverridesInChildren(serializedProperty))
                {
                    menu.AddItem(revertContent, false, (obj) => OnContextMenuRevert(obj, sync), serializedProperty.Copy());
                }
                else
                {
                    menu.AddDisabledItem(revertContent);
                }
            }

            menu.ShowAsContext();
        }

        internal static bool PruneAvailable => true;

#if COHERENCE_PRUNE_MENU_ITEM
        [MenuItem("Assets/coherence/Prune", true)]
        private static bool PruneSelectionValidator()
        {
            var syncs = Selection.GetFiltered<CoherenceSync>(SelectionMode.Editable);
            return syncs.All(s => HasLeakedManagedIds(s));
        }

        [MenuItem("Assets/coherence/Prune")]
        private static void PruneSelection()
        {
            Analytics.Capture(Analytics.Events.MenuItem, ("menu", "assets"), ("item", "prune"));

            var syncs = Selection.GetFiltered<CoherenceSync>(SelectionMode.Editable);
            foreach (var s in syncs)
            {
                _ = PruneLeakedManagedIds(s);
            }
        }
#endif

        internal static bool HasLeakedManagedIds(CoherenceSync sync)
        {
            var propertyModifications = PrefabUtility.GetPropertyModifications(sync);
            if (propertyModifications == null)
            {
                return false;
            }

            var regex = new System.Text.RegularExpressions.Regex(@"managedReferences\[(\-?\d+)\]");
            long[] idsRecognizedByUnity = null;
#if UNITY_2022_2_OR_NEWER
            idsRecognizedByUnity = UnityEngine.Serialization.ManagedReferenceUtility.GetManagedReferenceIds(sync);
#else
            idsRecognizedByUnity = SerializationUtility.GetManagedReferenceIds(sync);
#endif

            using var so = new SerializedObject(sync);

            for (int i = 0; i < propertyModifications.Length; i++)
            {
                var pm = propertyModifications[i];

                using var p = so.FindProperty(pm.propertyPath);
                if (p != null && p.propertyType == SerializedPropertyType.ManagedReference && !idsRecognizedByUnity.Contains(p.managedReferenceId))
                {
                    return true;
                }

                var match = regex.Match(pm.propertyPath);
                if (!match.Success)
                {
                    continue;
                }

                var g = match.Groups[1];
                if (long.TryParse(g.Value, out long id))
                {
                    if (!idsRecognizedByUnity.Contains(id))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool PruneLeakedManagedIds(CoherenceSync sync)
        {
            var propertyModifications = PrefabUtility.GetPropertyModifications(sync);
            if (propertyModifications == null)
            {
                return false;
            }

            var changed = false;
            var regex = new System.Text.RegularExpressions.Regex(@"managedReferences\[(\-?\d+)\]");
            long[] idsRecognizedByUnity = null;
#if UNITY_2022_2_OR_NEWER
            idsRecognizedByUnity = UnityEngine.Serialization.ManagedReferenceUtility.GetManagedReferenceIds(sync);
#else
            idsRecognizedByUnity = SerializationUtility.GetManagedReferenceIds(sync);
#endif

            using var so = new SerializedObject(sync);
            for (int i = 0; i < propertyModifications.Length; i++)
            {
                var pm = propertyModifications[i];

                using var p = so.FindProperty(pm.propertyPath);
                if (p != null && p.propertyType == SerializedPropertyType.ManagedReference && !idsRecognizedByUnity.Contains(p.managedReferenceId))
                {
                    ArrayUtility.Remove(ref propertyModifications, pm);
                    i--;
                    changed = true;
                    continue;
                }

                var match = regex.Match(pm.propertyPath);
                if (!match.Success)
                {
                    continue;
                }

                var g = match.Groups[1];
                if (long.TryParse(g.Value, out long id))
                {
                    if (!idsRecognizedByUnity.Contains(id))
                    {
                        ArrayUtility.Remove(ref propertyModifications, pm);
                        i--;
                        changed = true;
                        continue;
                    }
                }
            }

            if (changed)
            {
                PrefabUtility.SetPropertyModifications(sync, propertyModifications);
            }

            return changed;
        }

        internal static bool HasBindingOverrides(CoherenceSync sync)
        {
            var addedComponents = PrefabUtility.GetAddedComponents(sync.gameObject);
            var isCoherenceSyncAnAddedComponent = addedComponents
                .Any(addedComp => addedComp.instanceComponent is CoherenceSync);

            // If the CoherenceSync component was added, we consider it as having overrides
            if (isCoherenceSyncAnAddedComponent)
            {
                return true;
            }

            var propertyModifications = PrefabUtility.GetPropertyModifications(sync);
            if (propertyModifications == null)
            {
                return false;
            }

            foreach (var pm in propertyModifications)
            {
                if ((pm.target as CoherenceSync) == null)
                {
                    continue;
                }

                if (pm.propertyPath.StartsWith("bindings."))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasOverrides(CoherenceSync sync, SerializedProperty property = null)
        {
            var propertyModifications = PrefabUtility.GetPropertyModifications(sync);
            if (propertyModifications == null)
            {
                return false;
            }

            var refsToDelete = property != null ? GetManagedRefsToDelete(property) : null;

            for (var i = 0; i < propertyModifications.Length; i++)
            {
                var pm = propertyModifications[i];

                if ((pm.target as CoherenceSync) == null)
                {
                    continue;
                }

                if (property != null)
                {
                    if (pm.propertyPath.StartsWith(property.propertyPath))
                    {
                        return true;
                    }
                }
                else
                {
                    if (pm.propertyPath.StartsWith("bindings.") ||
                        pm.propertyPath.StartsWith(nameof(CoherenceSync.componentActions) + "."))
                    {
                        return true;
                    }
                }

                if (property == null && pm.propertyPath.StartsWith("managedReferences["))
                {
                    return true;
                }

                if (refsToDelete != null && refsToDelete.Any(id => pm.propertyPath.StartsWith($"managedReferences[{id}]")))
                {
                    return true;
                }
            }

            return false;
        }

        internal static IEnumerable<long> GetManagedRefsToDelete(SerializedProperty property)
        {
            if (property == null)
            {
                yield break;
            }

            using var it = property.Copy();
            using var end = property.GetEndProperty();

            if (it.propertyType == SerializedPropertyType.ManagedReference)
            {
                yield return it.managedReferenceId;
            }

            while (it.NextVisible(true) && !SerializedProperty.EqualContents(it, end))
            {
                if (it == null)
                {
                    continue;
                }

                if (it.propertyType == SerializedPropertyType.ManagedReference)
                {
                    yield return it.managedReferenceId;
                }
            }
        }

        internal static void RevertOverrides(CoherenceSync sync, SerializedProperty property = null, EditorWindow window = null)
        {
            var propertyModifications = PrefabUtility.GetPropertyModifications(sync);
            if (propertyModifications == null)
            {
                return;
            }

            Undo.RecordObject(sync, "Revert");

            var refsToDelete = property != null ? GetManagedRefsToDelete(property) : null;

            for (int i = 0; i < propertyModifications.Length; i++)
            {
                var pm = propertyModifications[i];

                if ((pm.target as CoherenceSync) == null)
                {
                    continue;
                }

                if (property != null)
                {
                    if (pm.propertyPath.StartsWith(property.propertyPath))
                    {
                        ArrayUtility.Remove(ref propertyModifications, pm);
                        i--;
                        continue;
                    }
                }

                else if (pm.propertyPath.StartsWith("bindings.") ||
                         pm.propertyPath.StartsWith(nameof(CoherenceSync.componentActions) + "."))
                {
                    ArrayUtility.Remove(ref propertyModifications, pm);
                    i--;
                    continue;
                }

                // clear up ALL overrides on managed references
                // Unity is not able to handle SerializeReferences well on variants
                // So let's just wipe all of them on our own
                //
                // At time or writing, bindings and component actions use SerializeReferences (we care only about SerializeReference usage on CoherenceSync
                if (property == null && pm.propertyPath.StartsWith("managedReferences["))
                {
                    ArrayUtility.Remove(ref propertyModifications, pm);
                    i--;
                    continue;
                }

                if (refsToDelete != null && refsToDelete.Any(id => pm.propertyPath.StartsWith($"managedReferences[{id}]")))
                {
                    ArrayUtility.Remove(ref propertyModifications, pm);
                    i--;
                    continue;
                }
            }

            PrefabUtility.SetPropertyModifications(sync, propertyModifications);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sync);

            var updated = EditorCache.UpdateBindings(sync);
            if (window)
            {
                window.ShowNotification(updated ? new GUIContent("Reverted.\nRequired overrides added.") : new GUIContent("Reverted."));
            }

            EditorUtility.SetDirty(sync);
            Undo.FlushUndoRecordObjects();
        }

        private static bool HasOverridesInChildren(SerializedProperty property)
        {
            using var it = property.Copy();
            using var end = property.GetEndProperty();

            while (it.NextVisible(true) && !SerializedProperty.EqualContents(it, end))
            {
                if (it.prefabOverride)
                {
                    return true;
                }
            }

            return false;
        }

        private static void OnContextMenuApply(object obj, CoherenceSync sync)
        {
            var serializedProperty = obj as SerializedProperty;
            if (serializedProperty == null)
            {
                return;
            }

            PrefabUtility.ApplyPropertyOverride(serializedProperty, AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource(sync)), InteractionMode.UserAction);
            _ = serializedProperty.serializedObject.ApplyModifiedProperties();
            serializedProperty.Dispose();
        }

        private static void OnContextMenuRevert(object obj, CoherenceSync sync)
        {
            var serializedProperty = obj as SerializedProperty;
            if (serializedProperty == null)
            {
                return;
            }

            _ = serializedProperty.DeleteCommand();
            _ = serializedProperty.serializedObject.ApplyModifiedProperties();
            serializedProperty.Dispose();
        }

        private static bool IsInOriginalPrefab(CoherenceSync sync, SerializedProperty serializedProperty)
        {
            if (serializedProperty == null)
            {
                return false;
            }

            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(sync);
            if (!prefab)
            {
                return true;
            }
            var path = serializedProperty.propertyPath;
            var rootPropertyName = path.Substring(0, path.IndexOf('.'));
            var idxStart = path.IndexOf('[') + 1;
            var idxEnd = path.IndexOf(']');
            var idxStr = path.Substring(idxStart, idxEnd - idxStart);
            if (int.TryParse(idxStr, out var idx))
            {
                if (rootPropertyName.Equals("bindings"))
                {
                    var binding = sync.Bindings[idx];

                    if (prefab.HasBindingForDescriptor(binding.Descriptor, binding.UnityComponent))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

