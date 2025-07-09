// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System.Linq;
    using Coherence.Toolkit;
    using Toolkit;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(CoherenceSyncConfig)), CanEditMultipleObjects]
    internal class CoherenceSyncConfigEditor : BaseEditor
    {
        private class GUIContents
        {
            public static readonly GUIContent configNotLinked = EditorGUIUtility.TrTextContentWithIcon(
                $"This {nameof(CoherenceSyncConfig)} is not linked properly to a {nameof(CoherenceSync)}.",
                MessageType.Warning);
            public static readonly GUIContent configsNotLinked = EditorGUIUtility.TrTextContentWithIcon(
                $"Some {nameof(CoherenceSyncConfig)}s are not linked properly to a {nameof(CoherenceSync)}.",
                MessageType.Warning);
            public static readonly GUIContent isSubAsset = EditorGUIUtility.TrTextContentWithIcon(
                $"This {nameof(CoherenceSyncConfig)} is part of another asset.",
                MessageType.Info);
            public static readonly GUIContent areSubAssets = EditorGUIUtility.TrTextContentWithIcon(
                $"Some {nameof(CoherenceSyncConfig)}s are part of another asset.",
                MessageType.Info);

            public static readonly GUIContent openPrefab = EditorGUIUtility.TrTextContentWithIcon(
                "Select Prefab",
                "Prefab Icon"
                );

            public static readonly GUIContent delete = new("Delete");
            public static readonly GUIContent extract = new("Extract");
        }

        protected override void OnGUI()
        {
            if (GUILayout.Button(GUIContents.openPrefab, ContentUtils.GUIStyles.bigButton))
            {
                Selection.objects = Selection.objects.Select(obj =>
                {
                    if (obj is CoherenceSyncConfig config && config.EditorTarget)
                    {
                        return config.EditorTarget;
                    }

                    return obj;
                }).ToArray();
            }

            EditorGUILayout.Space();

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "id", "selfId");
            _ = serializedObject.ApplyModifiedProperties();

            // We want to differentiate between 0, 1 and N targets in two different filters:
            //   Is SubAsset
            //   Is Not Linked
            // We do this so that we can word the user-facing messages accordingly.
            var targetsAsSubAssets = 0;
            var targetsNotLinked = 0;
            if (targets.Length > 1)
            {
                foreach (var target in targets)
                {
                    if (targetsNotLinked > 1 && targetsAsSubAssets > 1)
                    {
                        break;
                    }

                    if (target is not CoherenceSyncConfig config)
                    {
                        continue;
                    }

                    if (AssetDatabase.IsSubAsset(config))
                    {
                        targetsAsSubAssets++;
                    }

                    if (!config.IsLinked)
                    {
                        targetsNotLinked++;
                    }
                }
            }
            else
            {
                if (target is not CoherenceSyncConfig config)
                {
                    return;
                }

                if (AssetDatabase.IsSubAsset(config))
                {
                    targetsAsSubAssets++;
                }

                if (!config.IsLinked)
                {
                    targetsNotLinked++;
                }
            }

            if (targetsNotLinked > 0)
            {
                EditorGUILayout.Space();

                var multiple = targetsNotLinked > 1;

                var helpContent = multiple ? GUIContents.configsNotLinked : GUIContents.configNotLinked;
                EditorGUILayout.HelpBox(helpContent);

                if (GUILayout.Button(GUIContents.delete))
                {
                    CoherenceSyncConfigUtils.DeleteUnlinked(targets);
                }
            }

            if (targetsAsSubAssets > 0)
            {
                EditorGUILayout.Space();
                var multiple = targetsNotLinked > 1;

                var helpContent = multiple ? GUIContents.areSubAssets : GUIContents.isSubAsset;
                EditorGUILayout.HelpBox(helpContent);

                if (GUILayout.Button(GUIContents.extract))
                {
                    CoherenceSyncConfigUtils.ExtractSubAssets(targets);
                }
            }
        }

    }
}
