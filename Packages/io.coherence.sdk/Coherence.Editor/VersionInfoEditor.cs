// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using Coherence.Toolkit;
    using Simulator;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    [CustomEditor(typeof(VersionInfo))]
    internal class VersionInfoEditor : Editor
    {
        private class GUIContents
        {
            public static readonly GUIContent fetchVersions = EditorGUIUtility.TrTextContent("Fetch tool versions", "Access the Portal to fetch latest available versions of each tool.");
            public static readonly GUIContent editVersionInfo = EditorGUIUtility.TrTextContent("Edit");
            public static readonly GUIContent refreshRevisionHash = EditorGUIUtility.TrTextContent("Refresh");
            public static readonly GUIContent engineLabel = EditorGUIUtility.TrTextContent("Tools", "Version of the command line tools to use.");
        }

        private VersionData[] versionDatas;

        private SerializedProperty sdkProperty;
        private SerializedProperty sdkRevisionHashProperty;
        private SerializedProperty engineProperty;
        private SerializedProperty docsSlugProperty;

        private static Dictionary<Type, DocumentationKeys> helpUrlKeys = new()
        {
            { typeof(CoherenceBridge), DocumentationKeys.CoherenceBridge },
            { typeof(CoherenceLiveQuery), DocumentationKeys.LiveQuery },
            { typeof(CoherenceInput), DocumentationKeys.InputQueues },
            { typeof(CoherenceSync), DocumentationKeys.CoherenceSync },
            { typeof(CoherenceTagQuery), DocumentationKeys.TagQuery },
            { typeof(AutoSimulatorConnection), DocumentationKeys.AutoSimulatorConnection },
            { typeof(CoherenceGlobalQuery), DocumentationKeys.GlobalQuery },
            { typeof(CoherenceScene), DocumentationKeys.CoherenceScene },
            { typeof(CoherenceSceneLoader), DocumentationKeys.CoherenceSceneLoader },
            { typeof(CoherenceNode), DocumentationKeys.CoherenceNode },
        };

        private void OnEnable()
        {
            sdkProperty = serializedObject.FindProperty("sdk");
            sdkRevisionHashProperty = serializedObject.FindProperty("sdkRevisionHash");
            engineProperty = serializedObject.FindProperty("engine");
            docsSlugProperty = serializedObject.FindProperty("docsSlug");

            if (versionDatas == null)
            {
                versionDatas = new[]
                {
                    new VersionData(GUIContents.engineLabel, "engine", engineProperty)
                };
            }
            else
            {
                versionDatas[0].prop = engineProperty;
            }
        }

        private void OnDisable()
        {
            sdkProperty.Dispose();
            sdkRevisionHashProperty.Dispose();
            engineProperty.Dispose();
            docsSlugProperty.Dispose();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            _ = EditorGUILayout.BeginHorizontal();
            _ = EditorGUILayout.PropertyField(sdkProperty);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button(GUIContents.editVersionInfo, EditorStyles.miniButton, GUILayout.Width(36f)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(Paths.packageManifestPath);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(true);
            _ = EditorGUILayout.BeginHorizontal();
            _ = EditorGUILayout.PropertyField(sdkRevisionHashProperty);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button(GUIContents.refreshRevisionHash, EditorStyles.miniButton, GUILayout.Width(60f)))
            {
                RevisionInfo.GetAndSaveHash();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GUILayout.Button(GUIContents.fetchVersions))
            {
                foreach (var vd in versionDatas)
                {
                    _ = vd.FetchReleases();
                }
            }


            EditorGUILayout.Space();

            foreach (var vd in versionDatas)
            {
                vd.OnGUI();
            }

            _ = EditorGUILayout.PropertyField(docsSlugProperty);

            if (GUILayout.Button("Update HelpURL links"))
            {
                RefreshHelpUrls();
            }

            if (GUILayout.Button("Update package.json links"))
            {
                RefreshPackageLinks();
            }

            _ = serializedObject.ApplyModifiedProperties();
        }

        private void RefreshPackageLinks()
        {
            var file = Path.GetFullPath("Packages/io.coherence.sdk/package.json");
            var content = File.ReadAllText(file);
            var version = docsSlugProperty.stringValue;
            const string pattern = @"(https://docs\.coherence\.io/)(\d+\.\d+)(.*)";
            var updatedContent = Regex.Replace(content, pattern,
                match => match.Groups[1].Value + version + match.Groups[3].Value);
            var updated = !content.Equals(updatedContent);
            if (updated)
            {
                File.WriteAllText(file, updatedContent);
                AssetDatabase.Refresh();
                Debug.Log("package.json updated");
            }
            else
            {
                Debug.Log("package.json was already up-to-date");
            }
        }

        private static void RefreshHelpUrls()
        {
            var types = TypeCache.GetTypesWithAttribute<HelpURLAttribute>();

            var updatedAny = false;
            foreach (var type in types)
            {
                if (!typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    continue;
                }

                if (type.Namespace == null || !type.Namespace.StartsWith("Coherence"))
                {
                    continue;
                }

                if (!helpUrlKeys.TryGetValue(type, out var docKey))
                {
                    Debug.LogError($"Type '{type.Name}' not found in {nameof(VersionInfoEditor)}.{nameof(helpUrlKeys)}, please add it.");
                    continue;
                }

                var go = ObjectFactory.CreateGameObject(SceneManager.GetActiveScene(), HideFlags.HideAndDontSave, "RefreshHelpUrls");
                var monoBehaviour = go.AddComponent(type) as MonoBehaviour;
                var monoScript = MonoScript.FromMonoBehaviour(monoBehaviour);

                if (!monoScript)
                {
                    Debug.LogError($"No MonoScript found for type '{type.Name}'");
                    DestroyImmediate(go);
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(monoScript);
                var text = monoScript.text;

                var docsUrl = DocumentationLinks.GetDocsUrl(docKey);

                var attribute = type.GetCustomAttribute<HelpURLAttribute>();
                if (docsUrl.Equals(attribute.URL))
                {
                    Debug.Log($"Skipped {type.Name}. Already up-to-date.");
                    DestroyImmediate(go);
                    continue;
                }

                text = text.Replace($"[HelpURL(\"{attribute.URL}\")]", $"[HelpURL(\"{docsUrl}\")]");
                File.WriteAllText(path, text);
                updatedAny = true;
                Debug.Log($"Updated {type.Name} from {attribute.URL} to {docsUrl}");
                DestroyImmediate(go);
            }

            if (updatedAny)
            {
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.Log("All HelpURL attributes are up to date!");
            }
        }
    }
}
