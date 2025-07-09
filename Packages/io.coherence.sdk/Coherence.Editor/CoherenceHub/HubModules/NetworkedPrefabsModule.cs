// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using Toolkit;
    using UnityEditor;
    using UnityEngine;

    [HubModule(Priority = 90)]
    public class NetworkedPrefabsModule : HubModule
    {
        private static class ModuleGUIContents
        {
            public static readonly GUIContent HeaderFolded = new("What are Networked Prefabs?");

            public static readonly GUIContent HeaderUnfolded = new($"Prefabs with the CoherenceSync component attached.\n\nAdd a CoherenceSync component to any GameObject you want to network to get started. When you select a GameObject, the Inspector window shows a coherence header where you can easily network it at the click of a button.\n\nNetworked Prefabs will be automatically instantiated and synchronized by the {nameof(CoherenceBridge)}, to reflect the state of the network.");
            public static readonly GUIContent Bake = EditorGUIUtility.TrTextContent("Bake", "Create network code based on active schemas.");
            public static readonly GUIContent BakeOutdated = EditorGUIUtility.TrTextContentWithIcon("Network code is outdated. Bake now to fix it.", Icons.GetPath("Coherence.Bake.Warning"));
        }

        public override HelpSection Help => new()
        {
            title = ModuleGUIContents.HeaderFolded,
            content = ModuleGUIContents.HeaderUnfolded,
        };

        public override string ModuleName => "Networked Prefabs";

        private CoherenceSyncConfigRegistryEditor registryEditor;

        public override void OnModuleEnable()
        {
            registryEditor = Editor.CreateEditor(CoherenceSyncConfigRegistry.Instance) as CoherenceSyncConfigRegistryEditor;
            if (registryEditor != null)
            {
                registryEditor.UseMargins = false;
                registryEditor.DrawCustomHeader = false;
            }
        }

        public override void OnModuleDisable()
        {
            DestroyImmediate(registryEditor);
        }

        public override void OnGUI()
        {
            if (CoherenceSyncConfigRegistry.Instance.Count == 0)
            {
                GUILayout.Label($"To create network entities, add a {nameof(CoherenceSync)} to the GameObjects you want to network.\n\nAlternatively, we can network a cube for you.", ContentUtils.GUIStyles.wrappedLabel);
                if (GUILayout.Button("Create a networkable cube on the scene", ContentUtils.GUIStyles.bigButton))
                {
                    var cube = ObjectFactory.CreatePrimitive(PrimitiveType.Cube);
                    var prefabPath = "Assets/Cube.prefab";
                    if (CoherenceSyncUtils.TryConvertToCoherenceSyncPrefab(cube, prefabPath, out var prefab))
                    {
                        Selection.activeGameObject = prefab;
                        EditorGUIUtility.PingObject(prefab);
                    }
                }
            }
            else
            {
                registryEditor.OnMainGUI();
            }
        }

        private void DrawSection()
        {
            var bakeOutdated = BakeUtil.Outdated;
            var bakeButtonStyle = bakeOutdated ? ContentUtils.GUIStyles.bigBoldButton : ContentUtils.GUIStyles.bigButton;
            if (GUILayout.Button(ModuleGUIContents.Bake, bakeButtonStyle))
            {
                BakeUtil.Bake();
                GUIUtility.ExitGUI();
            }
            if (bakeOutdated)
            {
                EditorGUILayout.LabelField(ModuleGUIContents.BakeOutdated, EditorStyles.miniLabel);
            }

            EditorGUILayout.Space();

            SharedModuleSections.DrawSchemasInPortal();
        }
    }
}
