// Copyright (c) coherence ApS.
// See the license file in the project root for more information.

namespace Coherence.Editor
{
    using Coherence.Toolkit;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Templating;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(ObjectProviderAttribute))]
    public class INetworkObjectDrawer : PropertyDrawer
    {
#if HAS_ADDRESSABLES
        private static readonly GUIContent addressableWarning = EditorGUIUtility.TrTextContentWithIcon($"The entity is marked as 'Addressable' but the provider used to load it is not '{nameof(AddressablesProvider)}'.", "Info");
#endif
        private static Dictionary<Type, List<Type>> objectProviderTypes = new Dictionary<Type, List<Type>>();
        internal static Dictionary<string, GUIContent> typeDisplayNames = new Dictionary<string, GUIContent>();
        private ObjectProviderAttribute castedAttribute;
        private Type interfaceType;

        private bool init;
        private bool hasValidationError;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUI.GetPropertyHeight(property, label, true);

            if (property.hasVisibleChildren)
            {
                height += EditorGUIUtility.standardVerticalSpacing;

                using var it = property.Copy();
                using var end = it.GetEndProperty();

                var enterChildren = true;
                while (it.NextVisible(enterChildren) && !SerializedProperty.EqualContents(it, end))
                {
                    enterChildren = false;
                    height += EditorGUIUtility.standardVerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(it);
                }

                if (hasValidationError)
                {
                    height += EditorGUIUtility.standardVerticalSpacing;
                    height += EditorGUIUtility.singleLineHeight;
                }
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init();

            var rootRect = position;
            rootRect.height = EditorGUIUtility.singleLineHeight;

            var prefixLabel = new GUIContent(castedAttribute.PrefixLabel, castedAttribute.Tooltip);
            prefixLabel = EditorGUI.BeginProperty(rootRect, prefixLabel, property);

            var controlRect = EditorGUI.PrefixLabel(rootRect, prefixLabel);

            var typeName = property.managedReferenceFullTypename.Contains(" ")
                ? property.managedReferenceFullTypename.Remove(0, property.managedReferenceFullTypename.IndexOf(" ")).Trim() : property.managedReferenceFullTypename.Trim();

            if (!typeDisplayNames.TryGetValue(typeName, out var displayName))
            {
                displayName = new GUIContent("Missing");
            }

            displayName = new GUIContent(displayName) { tooltip = string.Empty };

            var config = property.serializedObject.targetObject as CoherenceSyncConfig;
            if (EditorGUI.DropdownButton(controlRect, displayName, FocusType.Keyboard, CoherenceHubLayout.Styles.PopupNonFixedHeight))
            {
                _ = property.serializedObject.ApplyModifiedProperties();
                var typesPopup = new TypesPopup(objectProviderTypes[interfaceType].ToArray(), interfaceType, property, config);
                PopupWindow.Show(controlRect, typesPopup);
                GUIUtility.ExitGUI();
            }

            EditorGUI.EndProperty();

            if (property.hasVisibleChildren)
            {
                using var it = property.Copy();
                using var end = it.GetEndProperty();

                var rect = position;

                var indent = EditorGUI.indentLevel;
                var enterChildren = true;
                while (it.NextVisible(enterChildren) && !SerializedProperty.EqualContents(it, end))
                {
                    var h = EditorGUI.GetPropertyHeight(it, enterChildren);
                    rect.height = h;
                    rect.y += h + EditorGUIUtility.standardVerticalSpacing;
                    if (enterChildren)
                    {
                        EditorGUI.indentLevel++;
                    }
                    enterChildren = EditorGUI.PropertyField(rect, it);
                }

                if (property.managedReferenceValue is INetworkObjectProvider provider && !provider.Validate(config))
                {
                    hasValidationError = true;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    var iconContent = EditorGUIUtility.IconContent("console.warnicon.sml");
                    var buttonContent = new GUIContent("Fix Serialized Data", iconContent.image);

                    if (GUI.Button(rect, buttonContent))
                    {
                        provider.Initialize(config);
                    }
                }
                else
                {
                    hasValidationError = false;
                }

#if HAS_ADDRESSABLES
                if (!CoherenceSyncConfigUtils.ProviderIsAddressableOrCustom(config?.Provider) &&
                    CoherenceSyncConfigUtils.IsAddressable(config))
                {
                    EditorGUILayout.HelpBox(addressableWarning);
                }
#endif

                EditorGUI.indentLevel = indent;
            }
        }

        public static List<Type> GatherObjectProviders(Type type)
        {
            if (objectProviderTypes != null && objectProviderTypes.ContainsKey(type))
            {
                return objectProviderTypes[type];
            }

            var providers = new List<Type>();
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in allAssemblies)
            {
                var types = assembly.GetTypes().Where(t => type.IsAssignableFrom(t)
                                                           && t.IsClass && Attribute.GetCustomAttribute(t, typeof(ExcludeFromDropdownAttribute)) == null);
                providers.AddRange(types);

                foreach (var providerType in types)
                {
                    var attribute =
                        providerType.GetCustomAttribute(typeof(DisplayNameAttribute)) as DisplayNameAttribute;
                    var displayName = attribute != null ? attribute.Name : providerType.Name;
                    var tooltip = attribute != null ? attribute.Tooltip : string.Empty;

                    typeDisplayNames[providerType.FullName] = new GUIContent(displayName, tooltip);
                }
            }

            return providers;
        }

        private void Init()
        {
            if (init)
            {
                return;
            }

            init = true;
            castedAttribute = attribute as ObjectProviderAttribute;
            interfaceType = castedAttribute.Type;

            objectProviderTypes[interfaceType] = GatherObjectProviders(interfaceType);
        }

        public class TypesPopup : PopupWindowContent
        {
            private CoherenceSyncConfig entry;
            private Type interfaceType;
            private Type[] availableTypes;
            private SerializedProperty property;
            private SerializedObject serializedObject;

            private const int MaxResults = 50;

            public TypesPopup(Type[] availableTypes, Type interfaceType, SerializedProperty property, CoherenceSyncConfig entry)
            {
                serializedObject = new SerializedObject(property.serializedObject.targetObjects);

                this.property = serializedObject.FindProperty(property.propertyPath);
                this.entry = entry;
                this.interfaceType = interfaceType;
                this.availableTypes = availableTypes.OrderBy(p => p.Name).ToArray();
            }

            public override Vector2 GetWindowSize()
            {
                var itemHeight = 14f; // based on style's fixedHeight
                var height = itemHeight * (availableTypes.Length < MaxResults
                    ? availableTypes.Length + 1
                    : MaxResults + 1);
                height += 6f; // separator height
                return new Vector2(200f, height);
            }

            public override void OnClose()
            {
                property.Dispose();
                serializedObject.Dispose();
            }

            public override void OnGUI(Rect rect)
            {
                serializedObject.Update();

                for (int i = 0; i < availableTypes.Length; i++)
                {
                    if (i >= MaxResults)
                    {
                        break;
                    }

                    var type = availableTypes[i];

                    var style = Toolkit.ContentUtils.GUIStyles.miniMenuItem;

                    if (!typeDisplayNames.TryGetValue(type.FullName, out var content))
                    {
                        content = new GUIContent("Missing");
                    }

                    var size = style.CalcSize(content);

                    var controlRect = EditorGUILayout.GetControlRect(false, size.y, style);

                    EditorGUI.BeginChangeCheck();

                    var active = (!string.IsNullOrEmpty(property.managedReferenceFullTypename))
                                 && type.FullName == property.managedReferenceFullTypename.Remove(0, property.managedReferenceFullTypename.IndexOf(" ")).Trim();

                    active = GUI.Toggle(controlRect, active, content, style);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (active)
                        {
                            OnTypeSelected(type);
                        }

                        editorWindow.Close();
                        GUIUtility.ExitGUI();
                    }
                }

                if (Event.current.type == EventType.MouseMove)
                {
                    Event.current.Use();
                }

                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    editorWindow.Close();
                    GUIUtility.ExitGUI();
                }

                if ((Event.current.type == EventType.ValidateCommand || Event.current.type == EventType.ExecuteCommand) && Event.current.commandName == "Close")
                {
                    editorWindow.Close();
                    GUIUtility.ExitGUI();
                }

                DrawCreateImplementation(availableTypes.Length);

                _ = serializedObject.ApplyModifiedProperties();
            }

            private void DrawCreateImplementation(int index)
            {
                GUILayout.Space(4);
                GUILayout.Label(GUIContent.none, Toolkit.ContentUtils.GUIStyles.separator);

                var content = new GUIContent("Create Implementation...", $"Create a {interfaceType.Name} implementation from a template.");

                var style = Toolkit.ContentUtils.GUIStyles.miniMenuItem;
                var size = style.CalcSize(content);
                var rect = EditorGUILayout.GetControlRect(false, size.y, style);
                if (GUI.Button(rect, content, style))
                {
                    var defaultName = interfaceType == typeof(INetworkObjectProvider)
                        ? "CustomProvider"
                        : "CustomInstantiator";

                    var assetPath = EditorUtility.SaveFilePanelInProject("Select Assets Folder", defaultName, "cs", "Please enter a script name");

                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        if (File.Exists(assetPath))
                        {
                            return;
                        }

                        var templatePath = interfaceType == typeof(INetworkObjectProvider)
                            ? Paths.providerTemplatePath
                            : Paths.instantiatorTemplatePath;

                        var templateGenerator = new TemplateGenerator(templatePath, assetPath, defaultName);
                        templateGenerator.Generate();
                        _ = AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath));
                    }
                }
            }

            private void OnTypeSelected(Type type)
            {
                if (interfaceType == typeof(INetworkObjectProvider))
                {
                    _ = CoherenceSyncConfigUtils.CreateObjectProvider(entry, type);
                }
                else if (interfaceType == typeof(INetworkObjectInstantiator))
                {
                    CoherenceSyncConfigUtils.CreateObjectInstantiator(entry, type);
                }
            }
        }
    }
}
