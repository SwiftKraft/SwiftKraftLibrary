// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using UnityEngine;
    using UnityEditor;
    using Coherence.Toolkit;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class ComponentActionsWindow : InspectComponentWindow<CoherenceSync>
    {
        private class ComponentActions
        {
            public Type[] types;
            public GUIContent[] contents;
        }

        private static class GUIContents
        {
            public static readonly GUIContent title = EditorGUIUtility.TrTextContentWithIcon("Component Actions", Icons.GetPath("EditorWindow"));
            public static readonly GUIContent leaveAsIs = EditorGUIUtility.TrTextContent("Leave as is");
            public static readonly GUIContent action = EditorGUIUtility.TrTextContent("Action");
            public static readonly GUIContent[] emptyPopup =
            {
                leaveAsIs,
            };
        }

        private Dictionary<Component, ComponentActions> componentActions = new();
        private Dictionary<Type, List<Component>> componentsByTypeHash = new();
        private List<KeyValuePair<Type, List<Component>>> componentsByType = new();

        protected override void OnEnable()
        {
            base.OnEnable();

            titleContent = GUIContents.title;
            minSize = new Vector2(280, 260);
            wantsMouseMove = true;

            Refresh();
        }

        protected override void OnUndoRedo()
        {
            Refresh();
            Repaint();
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (!component)
            {
                EditorGUILayout.LabelField("Select a Game Object with a CoherenceSync component attached.", ContentUtils.GUIStyles.centeredStretchedLabel);
                return;
            }

            serializedGameObject.Update();
            serializedObject.Update();

            DrawComponents();

            _ = serializedObject.ApplyModifiedProperties();

            // don't apply modified properties for GameObject
        }

        public override void Refresh(bool forceNewSelection = false)
        {
            base.Refresh(forceNewSelection);

            componentActions.Clear();
            componentsByTypeHash.Clear();

            if (!Context)
            {
                return;
            }

            IterateComponents(RefreshComponent, Context);

            componentsByType = componentsByTypeHash.ToList();
            componentsByType.Sort((p1, p2) => p1.Key.Name.CompareTo(p2.Key.Name));
        }

        private void RefreshComponent(SerializedProperty componentProperty)
        {
            var component = componentProperty.objectReferenceValue as Component;
            if (EditorCache.GetComponentActionsForComponent(component, out List<Type> actionTypes))
            {
                var contents = new List<GUIContent>();
                var types = new List<Type>();

                contents.Add(GUIContents.leaveAsIs);
                types.Add(null);

                foreach (var t in actionTypes)
                {
                    var name = EditorCache.GetComponentActionName(t);
                    contents.Add(EditorGUIUtility.TrTextContent(name));
                    types.Add(t);
                }

                var type = component.GetType();
                if (!componentsByTypeHash.TryGetValue(type, out List<Component> components))
                {
                    components = new List<Component>();
                    componentsByTypeHash.Add(type, components);
                }
                components.Add(component);

                if (!componentActions.ContainsKey(component))
                {
                    componentActions.Add(component, new ComponentActions { contents = contents.ToArray(), types = types.ToArray() });
                }
            }
        }

        public void DrawComponents()
        {
            UpdateFlags();

            EditorGUI.BeginDisabledGroup(isInstance && !isAsset && !Application.isPlaying);
            IterateComponents(DrawComponent, Context ? Context : Component.gameObject);
            DrawMissing();
            EditorGUI.EndDisabledGroup();
        }

        private void DrawMissing()
        {
            if (Component.componentActions == null)
            {
                return;
            }

            var drawn = false;
            for (var i = 0; i < Component.componentActions.Length; i++)
            {
                var ca = Component.componentActions[i];
                if (ca != null && ca.Component && ca.Component.gameObject != Context)
                {
                    continue;
                }
                if (ca == null || !componentActions.ContainsKey(ca.Component))
                {
                    if (!drawn)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Actions on components that are now missing...", EditorStyles.centeredGreyMiniLabel);
                        drawn = true;
                    }

                    _ = EditorGUILayout.BeginHorizontal(GUI.skin.box);
                    EditorGUILayout.LabelField(EditorGUIUtility.TrTextContentWithIcon("Missing component", "Warning"), EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("Action", EditorCache.GetComponentActionName(ca));
                    var rect = GUILayoutUtility.GetLastRect();
                    var width = 16;
                    rect.x += rect.width - width;
                    rect.width = width;
                    if (GUI.Button(rect, EditorGUIUtility.TrIconContent("TreeEditor.Trash", "Remove this component action."), GUIStyle.none))
                    {
                        using (var so = new SerializedObject(Component))
                        using (var p = so.FindProperty(nameof(CoherenceSync.componentActions)))
                        {
                            p.DeleteArrayElementAtIndex(i);
                            _ = so.ApplyModifiedProperties();
                        }
                        i--;
                    }
                    EditorGUI.indentLevel++;
                    EditorGUI.BeginDisabledGroup(true);
                    DrawPropertiesForComponentAction(i);
                    EditorGUI.EndDisabledGroup();
                    EditorGUI.indentLevel--;
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawComponent(SerializedProperty serializedProperty)
        {
            DrawComponent(serializedProperty.objectReferenceValue as Component);
        }

        private void DrawComponent(Component component)
        {
            if (!component)
            {
                EditorGUI.BeginDisabledGroup(true);
                _ = EditorGUILayout.Popup(GUIContents.action, 0, GUIContents.emptyPopup);
                EditorGUI.EndDisabledGroup();
                return;
            }

            if (TypeUtils.IsNonBindableType(component.GetType()))
            {
                return;
            }

            if (!componentActions.TryGetValue(component, out var actions) || actions.contents.Length <= 1)
            {
                return;
            }

            if (!IncludedInSearchFilter(component.GetType().Name))
            {
                return;
            }

            _ = EditorGUILayout.BeginVertical(GUI.skin.box);
            var header = GetComponentHeaderContentSimple(component);
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();

            EditorGUI.indentLevel++;

            _ = EditorGUILayout.BeginHorizontal();

            var arrIdx = GetIndexInSerializedArray(component);
            var isPresent = arrIdx != -1;
            var controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            serializedObject.Update();
            var path = $"{nameof(CoherenceSync.componentActions)}.Array.data[{arrIdx}]";
            using var serializedProperty = isPresent ? serializedObject.FindProperty(path) : null;
            var controlLabel = isPresent ? EditorGUI.BeginProperty(controlRect, GUIContents.action, serializedProperty) : GUIContents.action;

            EditorGUI.BeginChangeCheck();
            var idx = GetComponentActionsIndex(component); // idx = first component action registered in the internal cache that matches the drawn component
            idx = EditorGUI.Popup(controlRect, controlLabel, idx, componentActions[component].contents);
            if (EditorGUI.EndChangeCheck())
            {
                var type = componentActions[component].types[idx];
                SelectComponentAction(component, type);
            }

            if (isPresent)
            {
                OpenContextMenu(controlRect, serializedProperty);
                EditorGUI.EndProperty();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            idx = -1; // idx = first component action registered in CoherenceSync that matches the drawn component
            if (Component.componentActions != null)
            {
                for (var i = 0; i < Component.componentActions.Length; i++)
                {
                    var ca = Component.componentActions[i];
                    if (ca != null && ca.component == component)
                    {
                        idx = i;
                        break;
                    }
                }
            }
            DrawPropertiesForComponentAction(idx);
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }
        // call before EndProperty
        private void OpenContextMenu(Rect controlRect, SerializedProperty serializedProperty)
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
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(Component);

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
                    menu.AddItem(applyContent, false, OnContextMenuApply, serializedProperty.Copy());
                }


                menu.AddItem(revertContent, false, OnContextMenuRevert, serializedProperty.Copy());
            }

            menu.ShowAsContext();
        }

        private void OnContextMenuApply(object obj)
        {
            var serializedProperty = obj as SerializedProperty;
            if (serializedProperty == null)
            {
                return;
            }

            PrefabUtility.ApplyPropertyOverride(serializedProperty, AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromOriginalSource(Component)), InteractionMode.UserAction);
            _ = serializedProperty.serializedObject.ApplyModifiedProperties();
            serializedProperty.Dispose();
        }

        private void OnContextMenuRevert(object obj)
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

        private int GetIndexInSerializedArray(Component component)
        {
            if (!component)
            {
                return -1;
            }

            if (Component.componentActions == null)
            {
                return -1;
            }

            var cas = Component.componentActions;
            var length = cas.Length;
            for (int i = 0; i < length; i++)
            {
                var ca = cas[i];
                if (ca == null)
                {
                    continue;
                }

                if (ca.component == component)
                {
                    return i;
                }
            }

            return -1;
        }

        private void DrawPropertiesForComponentAction(int idx)
        {
            if (idx == -1)
            {
                return;
            }

            var path = $"{nameof(CoherenceSync.componentActions)}.Array.data[{idx}]";
            using var so = new SerializedObject(Component);
            using var property = so.FindProperty(path);
            var expanded = true;

            var end = property.GetEndProperty();
            while (property.NextVisible(expanded))
            {
                if (SerializedProperty.EqualContents(property, end))
                {
                    break;
                }

                expanded = false;

                // skip drawing the component field, since it's meant to be internal
                if (property.name == "component")
                {
                    continue;
                }

                _ = EditorGUILayout.PropertyField(property, true);

            }

            _ = so.ApplyModifiedProperties();
        }

        private void SelectComponentAction(Component component, Type componentActionType)
        {
            Undo.RecordObject(Component, "Select Component Action");

            if (Component.componentActions == null)
            {
                Component.componentActions = new ComponentAction[] { };
            }

            for (int i = 0; i < Component.componentActions.Length; i++)
            {
                var ca = Component.componentActions[i];
                if (ca.Component == component)
                {
                    ArrayUtility.RemoveAt(ref Component.componentActions, i);
                    i--;
                }
            }

            if (componentActionType != null)
            {
                // NOTE we should not use the "cached" reusable instance, but create one that we can store and modify
                var action = Activator.CreateInstance(componentActionType) as ComponentAction;
                action.component = component;
                ArrayUtility.Add(ref Component.componentActions, action);
            }

            EditorUtility.SetDirty(Component);
        }

        private int GetComponentActionsIndex(Component component)
        {
            if (TryGetComponentAction(component, out ComponentAction componentAction))
            {
                var ca = componentActions[component];
                for (int i = 0; i < ca.types.Length; i++)
                {
                    var t = ca.types[i];
                    if (t != null && t == componentAction.GetType())
                    {
                        return i;
                    }
                }
                return 0;
            }
            else
            {
                return 0;
            }
        }

        private bool TryGetComponentAction(Component component, out ComponentAction componentAction)
        {
            if (Component.componentActions == null)
            {
                componentAction = default;
                return false;
            }

            foreach (var ca in Component.componentActions)
            {
                if (ca == null)
                {
                    continue;
                }

                if (ca.component == component)
                {
                    componentAction = ca;
                    return true;
                }
            }

            componentAction = default;
            return false;
        }

        private GUIContent GetComponentHeaderContentSimple(Component component)
        {
            return EditorGUIUtility.TrTextContentWithIcon(ObjectNames.GetInspectorTitle(component), AssetPreview.GetMiniThumbnail(component));
        }
    }
}
