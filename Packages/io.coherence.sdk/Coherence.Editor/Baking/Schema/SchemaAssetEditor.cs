// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Coherence.Toolkit;
    using Coherence.Toolkit.Bindings;
    using Toolkit;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <remarks>
    /// The implementation avoids using the IMGUI layout system for improved performance.
    /// It also tries to allocate as little as possible on hot paths.
    /// </remarks>
    [CustomEditor(typeof(SchemaAsset))]
    internal class SchemaAssetEditor : BaseEditor
    {
        [Serializable]
        class DefinitionView
        {
            public string Id { get; }
            public string Name { get; }
            public Component Component { get; }
            public CoherenceSync Sync { get; }
            public Descriptor Descriptor { get; }
            public string DescriptorSignature { get; }

            public DefinitionView(BaseDefinition baseDefinition)
            {
                Id = baseDefinition.id.ToString();
                Name = baseDefinition.name;
                (Sync, Component, Descriptor) = ParseDefinitionName(Name);
                DescriptorSignature = Descriptor?.Signature;
            }
        }

        private static class GUIContents
        {
            public static readonly GUIContent Refresh = EditorGUIUtility.TrTextContent("Refresh");
            public static readonly GUIContent NotFound = EditorGUIUtility.TrTextContentWithIcon("Asset associated with this definition not found.", "Warning");
        }

        private static readonly Regex DefinitionNameRegex = new(@"^_([a-z0-9]{32})(?:_([a-z0-9]{32}|[0-9]+))?$");

        private readonly GUIContent[] toolbarContents =
        {
            new("Components"),
            new("Commands"),
            new("Inputs"),
            new("LODs"),
        };

        private readonly Func<SchemaAsset, IEnumerable<BaseDefinition>>[] sectionFunctions =
        {
            (asset) => asset.SchemaDefinition.ComponentDefinitions,
            (asset) => asset.SchemaDefinition.CommandDefinitions,
            (asset) => asset.SchemaDefinition.InputDefinitions,
            (asset) => asset.SchemaDefinition.ArchetypeDefinitions,
        };

        private List<DefinitionView> definitionViews = new();

        private string searchString = string.Empty;
        private Vector2 scrollPos;
        private int selected;
        private GUIContent showingContent;

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        protected override void OnAfterHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            for (var i = 0; i < toolbarContents.Length; i++)
            {
                EditorGUI.BeginChangeCheck();
                var selection = GUILayout.Toggle(selected == i, toolbarContents[i], EditorStyles.toolbarButton);
                if (EditorGUI.EndChangeCheck() && selection)
                {
                    selected = i;
                    RefreshDefinitionViews();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUI.BeginChangeCheck();
            searchString = ContentUtils.DrawSearchField(searchString);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshDefinitionViews();
            }
        }

        private void RefreshDefinitionViews()
        {
            var asset = (SchemaAsset)target;
            var baseDefinitions = sectionFunctions[selected](asset);
            RefreshDefinitionViews(baseDefinitions);
        }

        private void RefreshDefinitionViews(IEnumerable<BaseDefinition> baseDefinitions)
        {
            definitionViews = baseDefinitions
                .Select(definition => new DefinitionView(definition))
                .Where(definitionView => definitionView.Id.Contains(searchString))
                .ToList();

            showingContent = new GUIContent($"Showing {definitionViews.Count} definitions");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshDefinitionViews();
        }

        protected override void OnGUI()
        {
            if (GUILayout.Button(GUIContents.Refresh))
            {
                RefreshDefinitionViews();
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            DrawDefinitions();
            EditorGUILayout.EndScrollView();
        }

        private void DrawDefinitions()
        {
            var elementHeight = EditorGUIUtility.singleLineHeight;
            var elementsHeight = elementHeight * definitionViews.Count;
            var elementsRect = EditorGUILayout.GetControlRect(false, elementsHeight);

            var elementRect = elementsRect;
            elementRect.yMax = elementRect.yMin + elementHeight;

            foreach (var definition in definitionViews)
            {
                var definitionId = definition.Id;
                var definitionName = definition.Name;
                var idRect = elementRect;
                var alt = Event.current.alt;
                GUI.Label(idRect, alt ? definitionName : definitionId);

                // Checking for underscore tells apart generated schema components possibly associated with Unity assets
                // We only draw buttons for those
                if (!alt && definitionName[0] == '_')
                {
                    DrawControl(elementRect, definition);
                }

                elementRect.y += elementHeight;
            }

            EditorGUILayout.LabelField(showingContent, ContentUtils.GUIStyles.centeredMiniLabel);
        }

        private static void DrawControl(Rect elementRect, DefinitionView definitionView)
        {
            var objectRect = elementRect;
            objectRect.xMin += 38;

            var hasDescriptor = definitionView.Descriptor != null;
            if (hasDescriptor)
            {
                objectRect.xMax -= 200;

                var descriptorRect = elementRect;
                descriptorRect.xMin = objectRect.xMax + 2f;
                EditorGUI.LabelField(descriptorRect, definitionView.DescriptorSignature, ContentUtils.GUIStyles.richMiniLabelNoWrap);
            }

            if (definitionView.Component)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.ObjectField(objectRect, definitionView.Component, typeof(Component), false);
                EditorGUI.EndDisabledGroup();
            }
            else if (definitionView.Sync)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.ObjectField(objectRect, definitionView.Sync, typeof(CoherenceSync), false);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                var color = GUI.color;
                GUI.color = Color.yellow;
                EditorGUI.LabelField(objectRect, GUIContents.NotFound, ContentUtils.GUIStyles.richMiniLabelNoWrap);
                GUI.color = color;
            }
        }

        private static (CoherenceSync sync, Component component, Descriptor descriptor) ParseDefinitionName(string definitionName)
        {
            var match = DefinitionNameRegex.Match(definitionName);
            if (!match.Success)
            {
                return default;
            }

            var guid = match.Groups[1].Value;
            var sync = GetObjectFromGuid(guid) as CoherenceSync;
            if (!match.Groups[2].Success)
            {
                return (sync, default, default);
            }

            var identifier = match.Groups[2].Value;

            // identifier can be of length 32 (binding GUID) or up to 20 (localId as ulong)
            var isBinding = identifier.Length == 32;

            var component = isBinding
                ? GetObjectFromBinding(guid, identifier) as Component
                : GlobalObjectId.TryParse($"GlobalObjectId_V1-3-{guid}-{identifier}-0", out var globalObjectId)
                    ? GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId) as Component
                    : default;

            var binding = sync
                ? isBinding
                    ? sync.Bindings.FirstOrDefault(binding => binding.guid == identifier)
                    : default
                : default;

            var descriptor = binding?.Descriptor;

            return (sync, component, descriptor);
        }

        private static Object GetObjectFromGuid(string assetGuid)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (string.IsNullOrEmpty(assetPath))
            {
                return default;
            }

            var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (!gameObject)
            {
                return default;
            }

            return gameObject.TryGetComponent(out CoherenceSync sync) ? sync : gameObject;
        }

        private static Object GetObjectFromBinding(string assetGuid, string bindingGuid)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (string.IsNullOrEmpty(assetPath))
            {
                return default;
            }

            var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (!gameObject)
            {
                return default;
            }

            if (!gameObject.TryGetComponent(out CoherenceSync sync))
            {
                return gameObject;
            }

            var binding = sync.Bindings.FirstOrDefault(binding => binding.guid == bindingGuid);
            return binding?.unityComponent;
        }
    }
}
