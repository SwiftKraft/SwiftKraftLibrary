// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using System.Linq;
    using Coherence.Toolkit;
    using UnityEditor;
    using UnityEngine;
#if HAS_UGUI
    using Coherence.UI;
    using UnityEngine.EventSystems;
#endif

    [CustomEditor(typeof(CoherenceScene)), CanEditMultipleObjects]
    internal class CoherenceSceneEditor : BaseEditor
    {
        protected override string Description => "Listens CoherenceSceneLoader loads";

        private SerializedProperty deactivateOnLoadProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            deactivateOnLoadProperty = serializedObject.FindProperty("deactivateOnLoad");
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (deactivateOnLoadProperty != null)
            {
                deactivateOnLoadProperty.Dispose();
            }
        }

        protected override void OnGUI()
        {
            DrawConnection();

            serializedObject.Update();

            _ = EditorGUILayout.BeginHorizontal();
            _ = EditorGUILayout.PropertyField(deactivateOnLoadProperty);
            if (GUILayout.Button("Smart Select", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
            {
                SmartSelect(targets);
            }

            EditorGUILayout.EndHorizontal();

            DrawPropertiesExcluding(serializedObject, "m_Script", "deactivateOnLoad");
            _ = serializedObject.ApplyModifiedProperties();
        }

        internal static void SmartSelect(params Object[] targets)
        {
            var gameObjects = Enumerable.Empty<Component>()
#if UNITY_2023_1_OR_NEWER
                .Concat(FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                .Concat(FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
#if HAS_UGUI
                .Concat(FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
#endif
#else
                .Concat(FindObjectsOfType<Camera>(true))
                .Concat(FindObjectsOfType<AudioListener>(true))
#if HAS_UGUI
                .Concat(FindObjectsOfType<EventSystem>(true))
#endif
#endif
                .Select(c => c.gameObject)
                .Distinct();

            foreach (var target in targets)
            {
                var listener = target as CoherenceScene;
                if (!listener)
                {
                    continue;
                }

                var scene = listener.gameObject.scene;

                using (var so = new SerializedObject(target))
                using (var p = so.FindProperty("deactivateOnLoad"))
                {
                    p.ClearArray();
                    foreach (var go in gameObjects)
                    {
                        if (!go)
                        {
                            continue;
                        }

                        if (go.scene != scene)
                        {
                            continue;
                        }

                        var i = p.arraySize;
                        p.InsertArrayElementAtIndex(i);
                        p.GetArrayElementAtIndex(i).objectReferenceValue = go;
                    }

                    _ = so.ApplyModifiedProperties();
                }
            }
        }

        private void DrawConnection()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (targets.Length > 1)
            {
                EditorGUILayout.LabelField("Connections");
                foreach (var target in targets)
                {
                    if (!target)
                    {
                        continue;
                    }

                    var listener = target as CoherenceScene;
                    if (CoherenceSceneLoader.dataMap.TryGetValue(listener.gameObject.scene,
                            out var data))
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.SelectableLabel(data.EndpointData.ToString(),
                            GUILayout.Height(EditorGUIUtility.singleLineHeight));
                        EditorGUI.indentLevel--;
                    }
                }
            }
            else
            {
                var listener = target as CoherenceScene;
                if (CoherenceSceneLoader.dataMap.TryGetValue(listener.gameObject.scene, out CoherenceSceneData data))
                {
                    _ = EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Connection");
                    if (listener.IsConnected)
                    {
                        EditorGUILayout.SelectableLabel(data.EndpointData.ToString(), EditorStyles.label,
                            GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Disconnected.");
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}
