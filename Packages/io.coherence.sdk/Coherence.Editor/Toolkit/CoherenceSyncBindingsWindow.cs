// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Coherence.Toolkit;
    using Coherence.Toolkit.Bindings;
    using Log;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using Logger = Log.Logger;

    /// <summary>
    /// The Configuration window.
    /// </summary>
    internal class CoherenceSyncBindingsWindow : InspectComponentWindow<CoherenceSync>
    {
        private static readonly Type inspectorType = Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");

        public static CoherenceSyncBindingsWindow Instance { get; private set; }

        private static class FilterPopupSettings
        {
            public const float componentRowHeight = 19;
            public const float bottomPadding = 4;
            public const float separatorHeight = 4;
            public const float windowWidth = 200;
        }

        private class GUIStyles
        {
            public static readonly GUIStyle hoverButton = new GUIStyle(GUIStyle.none)
            {
                alignment = TextAnchor.MiddleCenter
            };

            public static readonly GUIStyle titleButton = new GUIStyle(EditorStyles.toolbarButton)
            {
                alignment = TextAnchor.MiddleLeft
            };

            public static readonly GUIStyle exposablePopupItem = new GUIStyle("ExposablePopupItem")
            {
                margin = new RectOffset(2, 0, 3, 0),
            };

            public static readonly GUIStyle menuItem = new GUIStyle("MenuItem")
            {
                imagePosition = ImagePosition.ImageLeft,
                fixedHeight = FilterPopupSettings.componentRowHeight,
            };

            public static readonly GUIStyle separator = new GUIStyle("sv_iconselector_sep");

            public static readonly GUIStyle menuItemButton = new GUIStyle(menuItem)
            {
                padding = new RectOffset(4, 4, 1, 2),
            };

            public static readonly GUIStyle iconButton = new GUIStyle(ContentUtils.GUIStyles.iconButton)
            {
                margin = new RectOffset(2, 2, 2, 2),
            };
        }

        private sealed class GUIContents
        {
            public static readonly GUIContent title = Icons.GetContentWithText("EditorWindow", "Configuration");
            public static readonly GUIContent warn = EditorGUIUtility.TrIconContent("Warning");

            private static GUIContent scopeVariables = GUIContent.none;
            private static GUIContent scopeMethods = GUIContent.none;
            private static GUIContent scopeComponents = GUIContent.none;

            public static void RecalcScopeContents(CoherenceSyncBindingsWindow window)
            {
                if (!window.Sync)
                {
                    return;
                }

                var (totalCount, invalidCount) = window.VariablesCount();
                scopeVariables = new GUIContent(totalCount == 0 ? "Variables" : $"Variables ({totalCount})",
                    invalidCount > 0 ? warn.image : null, "Select what variables to synchronize over the network.");
                (totalCount, invalidCount) = window.MethodsCount();
                scopeMethods = new GUIContent(totalCount == 0 ? "Methods" : $"Methods ({totalCount})",
                invalidCount > 0 ? warn.image : null, "Select what methods to be able to invoke over the network.");
                (totalCount, invalidCount) = window.ComponentActionsCount();
                scopeComponents = new GUIContent(totalCount == 0 ? "Components" : $"Components ({totalCount})",
                invalidCount > 0 ? warn.image : null, "Select what components to interact with over the network.");
            }

            public static GUIContent GetScopeContent(Scope scope)
            {
                return scope switch
                {
                    Scope.Variables => scopeVariables,
                    Scope.Methods => scopeMethods,
                    Scope.Components => scopeComponents,
                    _ => GUIContent.none,
                };
            }

            public static readonly GUIContent scopeInfoVariables =
                EditorGUIUtility.TrTextContent(
                    "Select variables to synchronize over the network and their interpolation settings.");

            public static readonly GUIContent scopeInfoMethods =
                EditorGUIUtility.TrTextContent("Select methods to expose over the network and their execution target.");

            public static readonly GUIContent scopeInfoComponents =
                EditorGUIUtility.TrTextContent("Select what happens to components on non-authoritative game objects.");

            public static readonly GUIContent selectAll = Icons.GetContent("Coherence.Select.All.Off", "Select All");

            public static readonly GUIContent
                selectAllHover = Icons.GetContent("Coherence.Select.All.On", "Select All");

            public static readonly GUIContent deselectAll =
                Icons.GetContent("Coherence.Select.None.Off", "Deselect All");

            public static readonly GUIContent deselectAllHover =
                Icons.GetContent("Coherence.Select.None.On", "Deselect All");

            public static readonly GUIContent hideComponent = EditorGUIUtility.TrIconContent("scenevis_hidden", "Hide");

            public static readonly GUIContent hideComponentHover =
                EditorGUIUtility.TrIconContent("scenevis_hidden_hover", "Hide");

            public static readonly GUIContent
                showComponent = EditorGUIUtility.TrIconContent("scenevis_visible", "Show");

            public static readonly GUIContent showComponentHover =
                EditorGUIUtility.TrIconContent("scenevis_visible_hover", "Show");

            public static readonly GUIContent revertOverrides =
                EditorGUIUtility.TrTextContent("Revert", "Revert all configuration changes made on this instance.");

            public static readonly GUIContent prune = EditorGUIUtility.TrTextContent("Prune",
                "Clear all leaked managed references. Reduces asset size (and memory it takes to load).\n\nWhen working with prefab variants, Unity leaks managed references (fields marked with [SerializeReference]). This can make your prefabs grow big and use more memory than necessary. Until Unity fixes this issue, we provide you with the ability to prune the leaked references on CoherenceSync prefab variants.");

            public static readonly GUIContent pruneNotAvailable = EditorGUIUtility.TrTextContent("Prune",
                "When working with prefab variants, Unity leaks managed references (fields marked with [SerializeReference]). This can make your prefabs grow big and use more memory than necessary. Until Unity fixes this issue, we provide you with the ability to prune the leaked references on CoherenceSync prefab variants. Pruning is only available on Unity 2021.2+.");

            public static readonly GUIContent openPrefabInIsolation =
                EditorGUIUtility.TrTextContentWithIcon("Open Prefab In Isolation", "Prefab Icon");

            public static readonly GUIContent openPrefabAsset = EditorGUIUtility.TrTextContentWithIcon("Open Prefab Asset", "Prefab Icon");

            public static readonly GUIContent prefabInstanceCannotConfigure =
                EditorGUIUtility.TrTextContentWithIcon(
                    "Prefab Instances cannot be configured. Open the Prefab Asset instead.", "console.infoicon.sml");

            public static readonly GUIContent runtimeEditingDisabled =
                EditorGUIUtility.TrTextContentWithIcon(
                    "Runtime editing disabled for baked entities. Exit playmode to configure", "console.infoicon.sml");

            public static readonly GUIContent componentsHidden =
                EditorGUIUtility.TrTextContentWithIcon("0 components hidden.", "scenevis_hidden");

            public static readonly GUIContent filterPopupButton =
                EditorGUIUtility.TrIconContent("scenevis_visible_hover",
                    "Select which components are visible on this view.");

            public static readonly GUIContent filterPopupButtonMixed =
                EditorGUIUtility.TrIconContent("scenevis_visible-mixed_hover",
                    "Select which components are visible on this view.");

            public static readonly GUIContent selectPrefabStage =
                EditorGUIUtility.TrTextContentWithIcon("Select in Prefab Stage", "Prefab Icon");

            public static readonly GUIContent selectPrefabStageDescription =
                EditorGUIUtility.TrTextContentWithIcon("The Prefab Asset cannot be edited directly while it is open in Prefab Stage", "console.infoicon.sml");

            public static readonly GUIContent coherenceSyncNotRoot = EditorGUIUtility.TrTextContent(
                $"{nameof(CoherenceSync)} should be at the root of the Prefab. If you need to sync any child values, first add {nameof(CoherenceSync)} to the root, then select the child and use the configuration window");

            public static readonly GUIContent prefabStageContextSupport =
                EditorGUIUtility.TrTextContentWithIcon(
                    "coherence can't configure prefabs in Context Mode yet. Open the Prefab in Isolation instead.", "console.infoicon.sml");

            public static readonly GUIContent prefabModeReadMode =
                EditorGUIUtility.TrTextContent("Read more about Prefab Mode");
        }

        public enum Scope
        {
            Variables,
            Methods,
            Components,
        }

        public enum Mode
        {
            Edit,
            View,
        }

        public enum HideMode
        {
            None,
            Hide,
            Fold,
        }

        public CoherenceSync Sync => Component;

        private bool hasInput;
        private Vector2 scrollPos;

        public Scope scope;
        public Mode mode;
        public HideMode hideMode = HideMode.Fold;
        public bool alwaysShowHoverButtons = true;

        public bool CanEdit => mode == Mode.Edit;

        private int hoverControl;
        private int hoverHeaderControl;

        private bool IsDebug => (Event.current.modifiers & EventModifiers.Alt) != 0;

        private int visibleComponentCount;
        private int maxVisibleComponentCount;
        private int invalidBindings;
        private int bindingsWithInputAuthPrediction;
        private GameObjectStatus gameObjectStatus;
        private int networkComponents;
        private HashSet<Component> uniqueComponentsBound = new HashSet<Component>();
        private bool hasInputComponent;

        private bool firstRun;

        private Rect popupRect;

        private ComponentActionsWindow componentActionsWindow;

        private static Logger Logger = Log.GetLogger<CoherenceSyncBindingsWindow>();

        private List<(int, string)> bindingsWithNullComponents = new();

        private GUIContent GetScopeContent(Scope scope)
        {
            return GUIContents.GetScopeContent(scope);
        }

        private bool InScope(Descriptor descriptor)
        {
            if (descriptor == null)
            {
                return true;
            }

            return (scope == Scope.Methods && descriptor.IsMethod)
                   || (scope == Scope.Variables && !descriptor.IsMethod);
        }

        private bool ShouldShow(Descriptor descriptor, Component component)
        {
            return InScope(descriptor) && (mode == Mode.Edit || Sync.HasBindingForDescriptor(descriptor, component));
        }

        public static CoherenceSyncBindingsWindow GetWindow()
        {
            var w = GetWindow<CoherenceSyncBindingsWindow>();
            var go = Selection.activeGameObject;
            if (go && go.TryGetComponent(out CoherenceSync sync))
            {
                w.Component = sync;
            }

            w.Refresh();
            //w.ShowAuxWindow(); // ignore: "Cannot reparent window to suggested parent. Window will not automatically close."

            return w;
        }

        public static CoherenceSyncBindingsWindow GetWindow(CoherenceSync sync)
        {
            Selection.activeObject = sync;
            var w = GetWindow<CoherenceSyncBindingsWindow>();
            w.Component = sync;
            w.Refresh();

            return w;
        }

        public static CoherenceSyncBindingsWindow GetWindowDocked(CoherenceSync sync)
        {
            if (inspectorType == null)
            {
                return GetWindow(sync);
            }

            Selection.activeObject = sync;
            var w = GetWindow<CoherenceSyncBindingsWindow>(inspectorType);
            w.Component = sync;
            w.Refresh();

            return w;
        }

        protected void Awake()
        {
            firstRun = true;
            Analytics.Capture(Analytics.Events.CoherenceSyncConfigure);
        }

        protected override void OnFocus()
        {
            if (Sync)
            {
                _ = EditorCache.UpdateBindings(Sync);
            }

            base.OnFocus();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            ObjectFactory.componentWasAdded += ComponentWasAdded;

            wantsMouseMove = true;
            minSize = new Vector2(280, 260);
            titleContent = GUIContents.title;

            if (!componentActionsWindow)
            {
                componentActionsWindow = CreateInstance<ComponentActionsWindow>();
            }

            if (Component)
            {
                componentActionsWindow.Refresh(Component);
            }

            if (!Instance)
            {
                Instance = this;
            }
        }


        protected override void OnDisable()
        {
            base.OnDisable();

            if (Instance == this)
            {
                Instance = null;
            }

            ObjectFactory.componentWasAdded -= ComponentWasAdded;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (componentActionsWindow)
            {
                DestroyImmediate(componentActionsWindow);
            }
        }

        public override void Refresh(bool forceNewSelection = false)
        {
            base.Refresh(forceNewSelection);

            hasInput = Component && Component.TryGetComponent(out CoherenceInput _);

            if (componentActionsWindow)
            {
                componentActionsWindow.Refresh(forceNewSelection);
            }

            GUIContents.RecalcScopeContents(this);

            if (Component == null)
            {
                return;
            }

            if (Sync != null)
            {
                gameObjectStatus = new GameObjectStatus(Sync.gameObject);
            }

            var entryInfo = CoherenceSyncConfigUtils.GetBindingInfo(Component).Value;

            uniqueComponentsBound = entryInfo.UniqueComponentsWithBindings;
            networkComponents = entryInfo.NetworkComponents;
            invalidBindings = entryInfo.InvalidBindings;
            bindingsWithInputAuthPrediction = entryInfo.BindingsWithInputAuthPrediction;
        }

        protected override void OnComponentsChanged()
        {
            base.OnComponentsChanged();
            ExpandComponentsBasedOnSearchFilter();
        }

        private void ComponentWasAdded(Component component)
        {
            OnFocus();
        }

        protected override void OnPrefabStageOpened(PrefabStage stage)
        {
            base.OnPrefabStageOpened(stage);
            Repaint();
        }

        protected override void OnPrefabStageClosing(PrefabStage stage)
        {
            base.OnPrefabStageClosing(stage);
            Repaint();
        }

        protected override void OnActiveSelectionChanged(CoherenceSync previousComponent, CoherenceSync newComponent)
        {
            base.OnActiveSelectionChanged(previousComponent, newComponent);
            Repaint();
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (!component || !context)
            {
                EditorGUILayout.LabelField("Select a Game Object with a CoherenceSync component attached.",
                    ContentUtils.GUIStyles.centeredStretchedLabel);
                return;
            }

            UpdateFlags();

            if (serializedGameObject.UpdateIfRequiredOrScript())
            {
                GUIContents.RecalcScopeContents(this);
            }

            if (serializedObject.UpdateIfRequiredOrScript())
            {
                GUIContents.RecalcScopeContents(this);
            }

            if (firstRun)
            {
                if (CanEdit)
                {
                    ExpandComponents();
                }
                else
                {
                    ExpandComponentsWithBindings();
                }

                firstRun = false;
            }

            visibleComponentCount = GetVisibleComponentCount();
            maxVisibleComponentCount = GetMaxVisibleComponentCount();

            DrawToolbar();

            if (Application.isPlaying)
            {
                _ = EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField(GUIContents.runtimeEditingDisabled, ContentUtils.GUIStyles.richMiniLabel);
                EditorGUILayout.EndVertical();
            }
            else if (!isAsset && gameObjectStatus is { IsInstanceInScene: true } or { IsNestedInstanceInsideAnotherPrefab: true })
            {
                GUILayout.Space(CoherenceHubLayout.SectionHorizontalSpacing);
                _ = EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField(GUIContents.prefabInstanceCannotConfigure, ContentUtils.GUIStyles.richMiniLabel);
                EditorGUILayout.EndVertical();
                DrawOpenPrefabButton();
                GUILayout.Space(CoherenceHubLayout.SectionSpacing);
            }
            else if (gameObjectStatus is { IsRootOfPrefabStageHierarchy: true, PrefabStageMode: PrefabStageMode.InContext })
            {
                GUILayout.Space(CoherenceHubLayout.SectionHorizontalSpacing);
                _ = EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField(GUIContents.prefabStageContextSupport, ContentUtils.GUIStyles.richMiniLabel);
                EditorGUILayout.EndVertical();

                OpenPrefabInIsolationButton();
                CoherenceHubLayout.DrawLink(GUIContents.prefabModeReadMode, ExternalLinks.UnityDocsPrefabMode);

                GUILayout.Space(CoherenceHubLayout.SectionSpacing);
            }
            else if (stage && stage.assetPath == AssetDatabase.GetAssetPath(Sync))
            {
                GUILayout.Space(CoherenceHubLayout.SectionHorizontalSpacing);
                _ = EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField(GUIContents.selectPrefabStageDescription, ContentUtils.GUIStyles.richMiniLabel);
                EditorGUILayout.EndVertical();

                DrawPrefabButton(GUIContents.selectPrefabStage);

                GUILayout.Space(CoherenceHubLayout.SectionSpacing);
            }

            _ = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Space(4);
            DrawSelector(EditorStyles.toolbarButton);
            EditorGUILayout.Space();

            if (OverrideActions.PruneAvailable)
            {
                if (OverrideActions.HasLeakedManagedIds(Sync))
                {
                    if (GUILayout.Button(GUIContents.prune, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                    {
                        _ = OverrideActions.PruneLeakedManagedIds(Sync);
                        GUIUtility.ExitGUI();
                    }
                }
            }
            else
            {
                if (PrefabUtility.IsPartOfVariantPrefab(Sync))
                {
                    EditorGUI.BeginDisabledGroup(true);
                    GUILayout.Label(GUIContents.pruneNotAvailable, EditorStyles.toolbarButton,
                        GUILayout.ExpandWidth(false));
                    EditorGUI.EndDisabledGroup();
                }
            }

            if (OverrideActions.HasOverrides(Sync))
            {
                if (GUILayout.Button(GUIContents.revertOverrides, EditorStyles.toolbarButton,
                        GUILayout.ExpandWidth(false)))
                {
                    OverrideActions.RevertOverrides(Sync, null, this);
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndHorizontal();

            ContentUtils.DrawCloneModeMessage();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            if (Sync.gameObject.transform.parent != null && Sync.CoherenceSyncConfig == null)
            {
                CoherenceHubLayout.DrawErrorArea(GUIContents.coherenceSyncNotRoot.text);
            }
            else
            {
                var readOnly = (CloneMode.Enabled && !CloneMode.AllowEdits) || stage && (stage.mode == PrefabStage.Mode.InContext || stage.assetPath == AssetDatabase.GetAssetPath(Sync));
                EditorGUI.BeginDisabledGroup(readOnly);
                DrawMainView();
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndScrollView();
            ApplyModifiedProperties(serializedObject);

            // NOTE don't apply GameObject's modified properties,
            // since it's not our responsibility
        }

        private void SetDirty(CoherenceSync sync)
        {
            EditorUtility.SetDirty(sync);
            BakeUtil.CoherenceSyncSchemasDirty = true;
        }

        private void ApplyModifiedProperties(SerializedObject obj)
        {
            if (obj != null && obj.ApplyModifiedProperties())
            {
                BakeUtil.CoherenceSyncSchemasDirty = true;
            }
        }

        public void DrawMainView()
        {
            if (scope == Scope.Components)
            {
                if (componentActionsWindow)
                {
                    componentActionsWindow.Context = Context;

                    DrawMessages();
                    if (GUI.enabled)
                    {
                        GUILayout.Label(GetScopeInfoContent(), ContentUtils.GUIStyles.centeredGreyMiniLabelWrap);
                    }
                    componentActionsWindow.DrawComponents();
                }
            }
            else
            {
                if (EditorUtility.scriptCompilationFailed)
                {
                    CoherenceHubLayout.DrawErrorArea(
                        "All compiler errors have to be fixed before you can change networked data!");
                }

                using var disabledScope = new EditorGUI.DisabledScope(EditorUtility.scriptCompilationFailed);
                DrawMessages();
                DrawEditModeInfo();
                DrawBindings();
                DrawVisibilityInfo();
            }
        }

        private GUIContent GetScopeInfoContent()
        {
            return scope switch
            {
                Scope.Variables => GUIContents.scopeInfoVariables,
                Scope.Methods => GUIContents.scopeInfoMethods,
                Scope.Components => GUIContents.scopeInfoComponents,
                _ => GUIContent.none,
            };
        }

        private bool IsRequired(Descriptor descriptor)
        {
            if (descriptor == null)
            {
                return false;
            }

            return descriptor.Required;
        }

        private void DrawOpenPrefabButton() => DrawPrefabButton(GUIContents.openPrefabAsset);
        private void OpenPrefabInIsolationButton() => DrawPrefabButton(GUIContents.openPrefabInIsolation);

        private void DrawPrefabButton(GUIContent label)
        {
            if (GUILayout.Button(label, GUILayout.Height(35f)))
            {
                PrefabUtils.OpenInIsolation(Sync.gameObject, gameObjectStatus, true);
            }
        }

        private void DrawMessages()
        {
            var canEnterPrefabMode = !(isInstance && !isAsset) && !inStage;
            if (GUI.enabled && canEnterPrefabMode && !Application.isPlaying)
            {
                DrawOpenPrefabButton();
            }

            if (networkComponents > BakeUtil.MaxUniqueComponentsBound)
            {
                CoherenceHubLayout.DrawErrorArea(BakeUtil.GetTooManyNetworkComponentsErrorMessage(networkComponents));
            }
        }

        private void DrawEditModeInfo()
        {
            if (!string.IsNullOrEmpty(searchString))
            {
                GUILayout.Label(EditorGUIUtility.TrTempContent($"Showing results for '{searchString}'."),
                    EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                if (GUI.enabled && (visibleComponentCount != 0 || hideMode == HideMode.Fold))
                {
                    GUILayout.Label(GetScopeInfoContent(), ContentUtils.GUIStyles.centeredGreyMiniLabelWrap);
                }
            }
        }

        private void DrawVisibilityInfo()
        {
            if (visibleComponentCount < maxVisibleComponentCount)
            {
                var diff = maxVisibleComponentCount - visibleComponentCount;
                GUIContents.componentsHidden.text = $"{diff} components hidden.";
                GUILayout.Label(GUIContents.componentsHidden, EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void ToggleAllFilteredBindingsFromComponent(Component component, bool selected)
        {
            if (!component)
            {
                return;
            }

            ApplyModifiedProperties(serializedObject);
            Undo.RecordObject(Sync,
                $"{(selected ? "Selected" : "Deselected")} All {ObjectNames.GetInspectorTitle(component)} Bindings");

            var hasProvider = EditorCache.GetBindingProviderForComponent(component, out DescriptorProvider provider);

            // custom bindings

            if (hasProvider)
            {
                var descriptors = EditorCache.GetComponentDescriptors(component);
                foreach (var descriptor in descriptors)
                {
                    if (descriptor == null)
                    {
                        continue;
                    }

                    if (!ShouldShow(descriptor, component))
                    {
                        continue;
                    }

                    if (IncludedInSearchFilter(descriptor))
                    {
                        if (selected)
                        {
                            _ = CoherenceSyncUtils.AddBinding(Sync, component, descriptor);
                        }
                        else if (!IsRequired(descriptor))
                        {
                            _ = CoherenceSyncUtils.RemoveBinding(Sync, component, descriptor);
                        }
                    }
                }
            }

            // delete serialized bindings that are no longer valid

            if (!selected)
            {
                for (int i = 0; i < Sync.Bindings.Count; i++)
                {
                    var binding = Sync.Bindings[i];

                    if (binding == null || binding.Descriptor == null)
                    {
                        continue;
                    }

                    if (binding.unityComponent != component)
                    {
                        continue;
                    }

                    if (!ShouldShow(binding.Descriptor, binding.UnityComponent))
                    {
                        continue;
                    }

                    if (!IncludedInSearchFilter(binding.Descriptor))
                    {
                        continue;
                    }

                    if (!IsRequired(binding.Descriptor))
                    {
                        Sync.Bindings.RemoveAt(i);
                        i--;
                    }
                }
            }

            _ = EditorCache.UpdateBindings(Sync); // reintroduce required, etc
            SetDirty(Sync);
            Undo.FlushUndoRecordObjects();
            if (serializedObject.UpdateIfRequiredOrScript())
            {
                GUIContents.RecalcScopeContents(this);
            }

            GUIUtility.ExitGUI();
        }

        private bool IncludedInSearchFilter(Descriptor descriptor, bool exactMatch = false)
        {
            if (descriptor == null)
            {
                return false;
            }

            if (SearchMatchesComponentName(descriptor.OwnerAssemblyQualifiedName))
            {
                return true;
            }

            return IncludedInSearchFilter(descriptor.Name, exactMatch);
        }

        private int GetVisibleComponentCount()
        {
            var c = 0;
            for (int i = 0; i < componentsProperty.arraySize; i++)
            {
                var baseComponentProperty = componentsProperty.GetArrayElementAtIndex(i);
                var componentProperty = baseComponentProperty.FindPropertyRelative("component");

                if (componentProperty.isExpanded)
                {
                    c++;
                }

                componentProperty.Dispose();
                baseComponentProperty.Dispose();
            }

            return c;
        }

        private int GetMaxVisibleComponentCount()
        {
            var c = 0;
            for (int i = 0; i < componentsProperty.arraySize; i++)
            {
                var baseComponentProperty = componentsProperty.GetArrayElementAtIndex(i);
                var componentProperty = baseComponentProperty.FindPropertyRelative("component");

                if (CanExpand(componentProperty))
                {
                    c++;
                }

                componentProperty.Dispose();
                baseComponentProperty.Dispose();
            }

            return c;
        }

        private bool CanExpand(SerializedProperty componentProperty)
        {
            return CanExpand(componentProperty, null);
        }

        private bool CanExpand(SerializedProperty componentProperty, params Type[] types)
        {
            if (componentProperty == null)
            {
                return false;
            }

            var component = componentProperty.objectReferenceValue as Component;

            if (!component)
            {
                // NOTE we will then need to collapse those components that go from null to "something we should not expand"
                return true;
            }

            return !TypeUtils.IsNonBindableType(component.GetType()) && HasBindings(componentProperty) &&
                   ComponentAssignableFromType(component, types);
        }

        private bool ComponentAssignableFromType(Component component, params Type[] types)
        {
            if (!component)
            {
                return false;
            }

            if (types == null || types.Length == 0)
            {
                return true;
            }

            var type = component.GetType();
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].IsAssignableFrom(type))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasBindings(SerializedProperty componentProperty)
        {
            var component = componentProperty.objectReferenceValue as Component;


            return EditorCache.GetComponentDescriptors(component).Count > 0;
        }

        private bool HasActiveBindings(SerializedProperty componentProperty)
        {
            var component = componentProperty.objectReferenceValue as Component;

            foreach (var binding in Sync.Bindings)
            {
                if (binding.unityComponent == component)
                {
                    if ((binding.IsMethod && scope == Scope.Methods)
                        || (!binding.IsMethod && scope == Scope.Variables))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void ExpandComponents()
        {
            if (componentsProperty == null)
            {
                return;
            }

            for (int i = 0; i < componentsProperty.arraySize; i++)
            {
                var baseComponentProperty = componentsProperty.GetArrayElementAtIndex(i);
                var componentProperty = baseComponentProperty.FindPropertyRelative("component");

                if (componentProperty != null)
                {
                    componentProperty.isExpanded = CanExpand(componentProperty);
                    componentProperty.Dispose();
                }

                baseComponentProperty.Dispose();
            }
        }

        public void ExpandComponentsBasedOnTypes(params Type[] types)
        {
            if (componentsProperty == null)
            {
                return;
            }

            for (int i = 0; i < componentsProperty.arraySize; i++)
            {
                var baseComponentProperty = componentsProperty.GetArrayElementAtIndex(i);
                var componentProperty = baseComponentProperty.FindPropertyRelative("component");

                if (componentProperty != null)
                {
                    componentProperty.isExpanded = CanExpand(componentProperty, types);
                    componentProperty.Dispose();
                }

                baseComponentProperty.Dispose();
            }
        }

        public void ExpandComponentsWithBindings()
        {
            if (componentsProperty == null)
            {
                return;
            }

            for (int i = 0; i < componentsProperty.arraySize; i++)
            {
                var baseComponentProperty = componentsProperty.GetArrayElementAtIndex(i);
                var componentProperty = baseComponentProperty.FindPropertyRelative("component");

                var component = componentProperty.objectReferenceValue as Component;

                componentProperty.isExpanded = false;

                if (HasActiveBindings(componentProperty))
                {
                    componentProperty.isExpanded = true;
                }

                componentProperty.Dispose();
                baseComponentProperty.Dispose();
            }
        }

        public void CollapseComponents()
        {
            if (componentsProperty == null)
            {
                return;
            }

            for (int i = 0; i < componentsProperty.arraySize; i++)
            {
                var baseComponentProperty = componentsProperty.GetArrayElementAtIndex(i);
                var componentProperty = baseComponentProperty.FindPropertyRelative("component");

                componentProperty.isExpanded = false;

                componentProperty.Dispose();
                baseComponentProperty.Dispose();
            }
        }

        public void DrawSelector()
        {
            DrawSelector(GUIStyles.exposablePopupItem);
        }

        public void DrawSelector(GUIStyle style)
        {
            for (int i = 0; i < 3; i++)
            {
                EditorGUI.BeginChangeCheck();
                _ = GUILayout.Toggle((int)scope == i, GetScopeContent((Scope)i), style, GUILayout.ExpandWidth(false));
                if (EditorGUI.EndChangeCheck())
                {
                    scope = (Scope)i;
                }
            }
        }

        private void DrawToolbar()
        {
            _ = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Space(4);

            if (Sync)
            {
                var content = ContentUtils.GetIcon(context);

                if (GUILayout.Button(content, GUIStyles.titleButton, GUILayout.Height(EditorGUIUtility.singleLineHeight),
                        GUILayout.ExpandWidth(true), GUILayout.MinWidth(120)))
                {
                    Selection.activeObject = context;
                    EditorGUIUtility.PingObject(context);

                    if (Event.current.alt)
                    {
                        if (inspectorType != null)
                        {
                            var w = GetWindow(inspectorType);
                            w.Show();

                            if (!docked)
                            {
                                Focus();
                            }
                        }
                    }
                }
            }

            if (scope == Scope.Components)
            {
                GUILayout.Space(1);

                EditorGUI.BeginChangeCheck();
                componentActionsWindow.searchString = ContentUtils.DrawSearchField(componentActionsWindow.searchString);
                if (EditorGUI.EndChangeCheck())
                {
                }
            }
            else
            {
                GUILayout.Space(1);

                EditorGUI.BeginChangeCheck();
                searchString = ContentUtils.DrawSearchField(searchString);
                if (EditorGUI.EndChangeCheck())
                {
                    ExpandComponentsBasedOnSearchFilter();
                }

                var content = visibleComponentCount == maxVisibleComponentCount
                    ? GUIContents.filterPopupButton
                    : GUIContents.filterPopupButtonMixed;
                if (EditorGUILayout.DropdownButton(content, FocusType.Passive, ContentUtils.GUIStyles.toolbarDropDown,
                        GUILayout.ExpandWidth(false)))
                {
                    var popup = new GenericPopup(OnPopupFilterGUI, GetPopupFilterSize);
                    PopupWindow.Show(popupRect, popup);
                    GUIUtility.ExitGUI();
                }

                if (Event.current.type == EventType.Repaint)
                {
                    popupRect = GUILayoutUtility.GetLastRect();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private Vector2 GetPopupFilterSize()
        {
            if (componentsProperty != null)
            {
                var items = componentsProperty.arraySize + 3;
                var separators = 1;
                var height = (items * FilterPopupSettings.componentRowHeight) +
                             (separators * FilterPopupSettings.separatorHeight) + FilterPopupSettings.bottomPadding;
                return new Vector2(FilterPopupSettings.windowWidth, height);
            }

            return new Vector2(FilterPopupSettings.windowWidth, 150);
        }

        private void OnPopupFilterGUI()
        {
            _ = EditorGUILayout.BeginVertical();
            DrawPopupFilterSelectors();
            DrawPopupFilterSeparator();
            DrawPopupFilterComponents();
            EditorGUILayout.EndVertical();
        }

        private void DrawPopupFilterSelectors()
        {
            EditorGUI.BeginDisabledGroup(visibleComponentCount == maxVisibleComponentCount);
            if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("Show All", "scenevis_visible_hover"),
                    GUIStyles.menuItemButton))
            {
                ExpandComponents();
                Repaint();
            }

            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button(
                    EditorGUIUtility.TrTextContentWithIcon("Show Selected",
                        "Show only components that have at least one item selected.", "scenevis_visible-mixed_hover"),
                    GUIStyles.menuItemButton))
            {
                ExpandComponentsWithBindings();
                Repaint();
            }

            EditorGUI.BeginDisabledGroup(visibleComponentCount == 0);
            if (GUILayout.Button(EditorGUIUtility.TrTextContentWithIcon("Hide All", "scenevis_hidden_hover"),
                    GUIStyles.menuItemButton))
            {
                CollapseComponents();
                Repaint();
            }

            EditorGUI.EndDisabledGroup();
        }

        private void DrawPopupFilterSeparator()
        {
            GUILayout.Space(FilterPopupSettings.separatorHeight);
            GUILayout.Label(GUIContent.none, GUIStyles.separator);
        }

        private void DrawPopupFilterComponents()
        {
            for (int i = 0; i < componentsProperty.arraySize; i++)
            {
                var baseComponentProperty = componentsProperty.GetArrayElementAtIndex(i);
                var componentProperty = baseComponentProperty.FindPropertyRelative("component");

                EditorGUI.BeginChangeCheck();
                EditorGUI.BeginDisabledGroup(!CanExpand(componentProperty));
                var expanded = GUILayout.Toggle(componentProperty.isExpanded,
                    GetComponentHeaderContent(componentProperty), GUIStyles.menuItem,
                    GUILayout.Width(FilterPopupSettings.windowWidth));
                EditorGUI.EndDisabledGroup();
                if (EditorGUI.EndChangeCheck())
                {
                    componentProperty.isExpanded = expanded;
                    Repaint();
                }

                componentProperty.Dispose();
                baseComponentProperty.Dispose();
            }
        }

        private void ExpandComponentsBasedOnSearchFilter()
        {
            for (int i = 0; i < componentsProperty.arraySize; i++)
            {
                var baseComponentProperty = componentsProperty.GetArrayElementAtIndex(i);
                var componentProperty = baseComponentProperty.FindPropertyRelative("component");

                componentProperty.isExpanded = ShouldExpand(componentProperty);

                componentProperty.Dispose();
                baseComponentProperty.Dispose();
            }
        }

#if LEGACY_BINDINGS_GLOBAL_SELECT_ALL
        private void SelectAllBaseOnSearchFilter()
        {
            ToggleAllBaseOnSearchFilter(true);
        }

        private void DeselectAllBaseOnSearchFilter()
        {
            ToggleAllBaseOnSearchFilter(false);
        }

        private void ToggleAllBaseOnSearchFilter(bool selected)
        {
            for (int i = 0; i < componentsProperty.arraySize; i++)
            {
                var baseComponentProperty = componentsProperty.GetArrayElementAtIndex(i);
                var componentProperty = baseComponentProperty.FindPropertyRelative("component");

                if (!componentProperty.isExpanded)
                {
                    componentProperty.Dispose();
                    baseComponentProperty.Dispose();
                    continue;
                }

                var component = componentProperty.objectReferenceValue as Component;

                ToggleAllFilteredBindingsFromComponent(component, selected);

                componentProperty.Dispose();
                baseComponentProperty.Dispose();
            }
        }
#endif

        private bool ShouldExpandMissing(SerializedProperty componentProperty)
        {
            using var fileIdProp = componentProperty.FindPropertyRelative("m_FileID");
            if (fileIdProp == null)
            {
                return false;
            }

            if (fileIdProp.intValue == 0)
            {
                return false;
            }

            var found = false;

            using var bindingsProperty = serializedObject.FindProperty("bindings");
            for (var j = 0; j < bindingsProperty.arraySize; j++)
            {
                using var bindingProperty = bindingsProperty.GetArrayElementAtIndex(j);
                using var componentFileIdProperty = bindingProperty.FindPropertyRelative("component.m_FileID");

                if (componentFileIdProperty == null)
                {
                    continue;
                }

                if (componentFileIdProperty.intValue != fileIdProp.intValue)
                {
                    continue;
                }

                if (!IncludedInSearchFilter(Sync.Bindings[j].Descriptor))
                {
                    continue;
                }

                found = true;
                break;
            }

            return found;
        }

        private bool SearchMatchesComponentName(string componentName)
        {
            if (IncludedInSearchFilter(componentName, exactMatch: true))
            {
                return true;
            }

            if (IncludedInSearchFilter(ObjectNames.GetInspectorTitle(component), exactMatch: true))
            {
                return true;
            }

            return false;
        }

        private bool ShouldExpand(SerializedProperty componentProperty)
        {
            if (string.IsNullOrEmpty(searchString))
            {
                return CanEdit ? CanExpand(componentProperty) : HasActiveBindings(componentProperty);
            }

            var targetComponent = componentProperty.objectReferenceValue as Component;

            if (!targetComponent)
            {
                return ShouldExpandMissing(componentProperty);
            }

            if (TypeUtils.IsNonBindableType(targetComponent.GetType()))
            {
                return false;
            }

            if (SearchMatchesComponentName(targetComponent.name))
            {
                return true;
            }

            var descriptors = EditorCache.GetComponentDescriptors(targetComponent);

            if (descriptors.Any(b =>
                    b.OwnerType == targetComponent.GetType() &&
                    ShouldShow(b, targetComponent) &&
                    IncludedInSearchFilter(b.Name)))
            {
                return true;
            }

            return Sync.Bindings.Any(b =>
                b.unityComponent == targetComponent &&
                ShouldShow(b.Descriptor, b.UnityComponent) &&
                !EditorCache.DescriptorExistsForBinding(descriptors, b) &&
                IncludedInSearchFilter(b.Name));
        }

        private GUIContent GetComponentHeaderContent(SerializedProperty componentProperty)
        {
            var targetComponent = componentProperty.objectReferenceValue as Component;

            if (!targetComponent)
            {
                return EditorGUIUtility.TrTextContentWithIcon(
                    IsDebug
                        ? $"Missing script ({componentProperty.FindPropertyRelative("m_FileID").intValue})"
                        : "Missing script", "Warning");
            }

            return EditorGUIUtility.TrTextContentWithIcon(
                ObjectNames.GetInspectorTitle(targetComponent), AssetPreview.GetMiniThumbnail(targetComponent));
        }

        public void DrawBindings()
        {
            if (PrefabUtils.IsInstance(Sync))
            {
                var userHelpMessage =
                    HasMissingComponents()
                        ? "At least one component has been removed from this prefab. To fix, click `Fix Prefab Instance` in the inspector."
                        : "The asset this prefab instance is linked to has invalid bindings. Invalid bindings can only be removed on the parent prefab asset. Click 'Open Prefab Asset' to fix invalid bindings.";

                var hasInvalidBindings = invalidBindings > 0
                    ? userHelpMessage
                    : string.Empty;

                if (!string.IsNullOrEmpty(hasInvalidBindings))
                {
                    var content = EditorGUIUtility.TrTextContentWithIcon(
                        hasInvalidBindings,
                        string.Empty, "Warning");
                    EditorGUILayout.LabelField(content, ContentUtils.GUIStyles.richMiniLabel);
                }
            }
            else
            {
                DrawInvalidBindingsMessage();
            }

            DrawBindingsWithInputAuthWarning();

            var canEdit = !Application.isPlaying && (!(isInstance && !isAsset) || Application.isPlaying);

            for (var i = 0; i < componentsProperty.arraySize; i++)
            {
                var baseComponentProperty = componentsProperty.GetArrayElementAtIndex(i);
                var componentProperty = baseComponentProperty.FindPropertyRelative("component");

                // on fold mode, let's keep drawing so that we see the header
                if (!componentProperty.isExpanded && (hideMode != HideMode.Fold || !CanExpand(componentProperty)))
                {
                    baseComponentProperty.Dispose();
                    componentProperty.Dispose();
                    continue;
                }

                var component = componentProperty.objectReferenceValue as Component;

                if (!component)
                {
                    continue;
                }

                var fullRect = EditorGUILayout.BeginHorizontal(GUI.skin.box);

                if (hideMode == HideMode.Hide && IsDebug)
                {
                    if (HoverButton(GUIContents.hideComponent, GUIContents.hideComponentHover))
                    {
                        componentProperty.isExpanded = false;
                    }
                }
                else if (hideMode == HideMode.Fold)
                {
                    var icon = componentProperty.isExpanded ? GUIContents.showComponent : GUIContents.hideComponent;
                    var iconHover = componentProperty.isExpanded
                        ? GUIContents.showComponentHover
                        : GUIContents.hideComponentHover;
                    if (HoverButton(icon, iconHover))
                    {
                        componentProperty.isExpanded = !componentProperty.isExpanded;
                    }
                }

                var header = GetComponentHeaderContent(componentProperty);
                var headerId = GUIUtility.GetControlID(header, FocusType.Passive);
                var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight,
                    EditorStyles.boldLabel);

                EditorGUI.BeginDisabledGroup(!componentProperty.isExpanded || !canEdit ||
                                             (component && TypeUtils.IsNonBindableType(component.GetType())));

                EditorGUI.LabelField(rect, header, EditorStyles.boldLabel);

                EditorGUILayout.Space();

                // handle header hover

                var headerHover = canEdit && (fullRect.Contains(Event.current.mousePosition) || alwaysShowHoverButtons);

                try
                {
                    if (Event.current.type == EventType.MouseMove)
                    {
                        if (headerHover)
                        {
                            if (hoverHeaderControl != headerId)
                            {
                                hoverHeaderControl = headerId;

                                if (mouseOverWindow != null)
                                {
                                    mouseOverWindow.Repaint();
                                }
                            }
                        }
                        else
                        {
                            if (hoverHeaderControl == headerId)
                            {
                                hoverHeaderControl = 0;

                                if (mouseOverWindow != null)
                                {
                                    mouseOverWindow.Repaint();
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // mouseOverWindow == true
                    // mouseOverWindow.Repaint -> NullReferenceException
                    // issue with native/managed implementation of EditorWindow?
                    // let's not bother the end user
                }

                if (Event.current.type != EventType.Repaint || (Event.current.type == EventType.Repaint && headerHover))
                {
                    if (CanEdit)
                    {
                        if (component &&
                            EditorCache.GetBindingProviderForComponent(component, out DescriptorProvider provider) &&
                            provider.AdditionalMenuItemData != null && provider.AdditionalMenuItemData.Length > 0)
                        {
                            foreach (var menuItemData in provider.AdditionalMenuItemData)
                            {
                                if (HoverButton(menuItemData.content, menuItemData.contentHover))
                                {
                                    var context = new MenuItemContext
                                    {
                                        sync = Sync,
                                        component = component,
                                        searchString = searchString,
                                    };

                                    ApplyModifiedProperties(serializedObject);
                                    menuItemData.function(context);
                                    if (serializedObject.UpdateIfRequiredOrScript())
                                    {
                                        GUIContents.RecalcScopeContents(this);
                                    }
                                }
                            }
                        }

                        if (HoverButton(GUIContents.selectAll, GUIContents.selectAllHover))
                        {
                            ToggleAllFilteredBindingsFromComponent(component, true);
                        }

                        if (HoverButton(GUIContents.deselectAll, GUIContents.deselectAllHover))
                        {
                            ToggleAllFilteredBindingsFromComponent(component, false);
                        }
                    }
                }

                EditorGUILayout.EndHorizontal();

                if (componentProperty.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    DrawComponent(componentProperty);
                    EditorGUI.indentLevel--;
                }

                EditorGUI.EndDisabledGroup();

                componentProperty.Dispose();
                baseComponentProperty.Dispose();
            }

            EditorGUI.BeginDisabledGroup(!canEdit);
            DrawBindingsWithNullComponents();
            EditorGUI.EndDisabledGroup();
        }

        private bool HoverButton(GUIContent normalContent, GUIContent hoverContent)
        {
            var style = GUIStyles.hoverButton;
            var r = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, style,
                GUILayout.Width(16));
            var hover = r.Contains(Event.current.mousePosition);
            var c = hover ? (GUI.enabled ? hoverContent : normalContent) : normalContent;
            int id = GUIUtility.GetControlID(c, FocusType.Passive);
            if (Event.current.type == EventType.MouseMove)
            {
                if (hover)
                {
                    if (hoverControl != id)
                    {
                        hoverControl = id;

                        if (mouseOverWindow)
                        {
                            mouseOverWindow.Repaint();
                        }
                    }
                }
                else
                {
                    if (hoverControl == id)
                    {
                        hoverControl = 0;

                        if (mouseOverWindow)
                        {
                            mouseOverWindow.Repaint();
                        }
                    }
                }
            }

            return GUI.Button(r, c, style);
        }

        private void DrawComponent(SerializedProperty componentProperty)
        {
            var component = componentProperty.objectReferenceValue as Component;

            DrawComponentBindings(component);
        }

        private void DrawComponentBindings(Component component)
        {
            if (!EditorCache.GetBindingProviderForComponent(component, out DescriptorProvider provider))
            {
                Logger.Error(Error.EditorBindingsWindowMissingBinding,
                    $"Missing Binding Provider for: {component}, expected to fallback to DefaultBindingProvider. " +
                    $"If needed you can implement your own {nameof(DescriptorProvider)}");
            }

            var drawsBindings = false;
            var descriptors = EditorCache.GetComponentDescriptors(component);

            // iterate through custom bindings currently
            // registered through the loaded provider
            for (var i = 0; i < descriptors.Count; i++)
            {
                var descriptor = descriptors[i];

                if (!ShouldShow(descriptor, component))
                {
                    continue;
                }

                if (!IncludedInSearchFilter(descriptor))
                {
                    continue;
                }

                drawsBindings = true;
                var (idx, binding) = Sync.IndexOfBindingForDescriptor(descriptor, component);
                var isBindingPresent = idx != -1;

                if (isBindingPresent && !CoherenceSyncUtils.IsBindingValid(Sync, idx, out _))
                {
                    continue;
                }

                _ = EditorGUILayout.BeginHorizontal();
                var controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
                using var serializedProperty = isBindingPresent
                    ? serializedObject.FindProperty($"bindings.Array.data[{idx}]")
                    : null;
                var controlLabel = isBindingPresent
                    ? EditorGUI.BeginProperty(controlRect, ContentUtils.GetContent(component, descriptor),
                        serializedProperty)
                    : ContentUtils.GetContent(component, descriptor);

                if (CanEdit)
                {
                    EditorGUI.BeginChangeCheck();
                    var isRequired = IsRequired(descriptor);
                    EditorGUI.BeginDisabledGroup(isRequired);
                    var selected = EditorGUI.ToggleLeft(controlRect, controlLabel, isBindingPresent,
                        ContentUtils.GUIStyles.richLabelNoWrap);
                    EditorGUI.EndDisabledGroup();
                    if (EditorGUI.EndChangeCheck())
                    {
                        // reference
                        if (selected)
                        {
                            if (networkComponents >= BakeUtil.MaxUniqueComponentsBound &&
                                !uniqueComponentsBound.Contains(component))
                            {
                                Logger.Error(Error.EditorBindingsWindowMaxComponents,
                                    $"You cannot create a binding for {component.GetType().Name}, this Prefab will create {networkComponents} " +
                                    "Network Components and binding an additional Component will put it over the limit.");
                                GUIUtility.ExitGUI();
                                return;
                            }

                            ApplyModifiedProperties(serializedObject);

                            _ = CoherenceSyncUtils.AddBinding(Sync, component, descriptor);
                        }
                        else
                        {
                            _ = CoherenceSyncUtils.RemoveBinding(Sync, component, descriptor);
                        }

                        SetDirty(Sync);
                        Undo.FlushUndoRecordObjects();
                        if (serializedObject.UpdateIfRequiredOrScript())
                        {
                            GUIContents.RecalcScopeContents(this);
                        }

                        GUIUtility.ExitGUI();
                        return;
                    }

                    EditorGUILayout.Space();

                    if (isBindingPresent && descriptor != null)
                    {
                        if (descriptor.IsMethod)
                        {
                            var routingIcons = ContentUtils.GUIContents.routingIcons;
                            var messageTarget = binding.routing;
                            var content = routingIcons.TryGetValue(messageTarget, out var icon)
                                ? icon
                                : GUIContent.none;

                            if (GUILayout.Button(content, GUIStyles.iconButton, GUILayout.ExpandWidth(false)))
                            {
                                var path = $"bindings.Array.data[{idx}].{nameof(Binding.routing)}";
                                var popup = new EnumPopup<MessageTarget>(Component, path,
                                    ContentUtils.GUIContents.routing, OnEnumPopupUpdate);
                                popup.Show();
                                GUIUtility.ExitGUI();
                            }
                        }
                        else
                        {
                            // binding != originalBinding
                            // originalBinding has "instance" data

                            // Interpolation settings _could_ be changed at editor runtime, but they don't do anything
                            // if set to "None". The tickets #6974 and #6975 propose fixes for this. For now,
                            // @2024-10-24, the interpolation settings remain locked at editor runtime. Prediction can
                            // be edited at runtime in the editor.
                            var interpolationSettingsPath = $"bindings.Array.data[{idx}].{nameof(Binding.interpolationSettings)}";
                            using var interpolationSettingsProperty = serializedObject.FindProperty(interpolationSettingsPath);
                            _ = EditorGUILayout.PropertyField(interpolationSettingsProperty, GUILayout.MinWidth(16));

                            GUI.enabled = true;
                            var predictionIcons = ContentUtils.GUIContents.predictionWithTooltip;
                            var predictionMode = binding.predictionMode;
                            var content = predictionIcons.ContainsKey(predictionMode)
                                ? predictionIcons[predictionMode]
                                : GUIContent.none;

                            if (predictionMode != PredictionMode.Never)
                            {
                                DrawPredictionLabel(content);
                            }

                            var width = ContentUtils.GUIStyles.iconButton.CalcSize(content).x;
                            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight,
                                ContentUtils.GUIStyles.iconButton, GUILayout.Width(width));
                            rect.y += 2;

                            if (GUI.Button(rect, content, ContentUtils.GUIStyles.iconButton))
                            {
                                var path = $"bindings.Array.data[{idx}].{nameof(Binding.predictionMode)}";
                                var popup = new EnumPopup<PredictionMode>(Component, path,
                                    ContentUtils.GUIContents.predictionWithLabel,
                                    OnEnumPopupUpdate, null, ValidatePredictionMode);
                                popup.Show();
                                GUIUtility.ExitGUI();
                            }
                        }

                        GUILayout.Space(4);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(ContentUtils.GetContent(component, descriptor),
                        ContentUtils.GUIStyles.richLabel);
                }

                if (isBindingPresent)
                {
                    OverrideActions.OpenContextMenu(controlRect, serializedProperty, Sync, isInstance);
                    EditorGUI.EndProperty();
                }

                EditorGUILayout.EndHorizontal();
            }

            // iterate through bindings currently serialized,
            // that are not registered anymore for this component
            for (var i = 0; i < Sync.Bindings.Count; i++)
            {
                var binding = Sync.Bindings[i];

                if (binding == null)
                {
                    continue;
                }

                if (binding.unityComponent != component)
                {
                    continue;
                }

                if (!ShouldShow(binding.Descriptor, binding.UnityComponent))
                {
                    continue;
                }

                if (!IncludedInSearchFilter(binding.Descriptor))
                {
                    continue;
                }

                if (CoherenceSyncUtils.IsBindingValid(Sync, i, out var invalidReason))
                {
                    continue;
                }

                drawsBindings = true;

                _ = EditorGUILayout.BeginHorizontal();

                var controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
                var path = $"bindings.Array.data[{i}]";
                var serializedProperty = serializedObject.FindProperty(path);
                var controlLabel = EditorGUI.BeginProperty(controlRect, ContentUtils.GetInvalidContent(binding.Descriptor, invalidReason), serializedProperty);

                if (CanEdit)
                {
                    EditorGUI.BeginChangeCheck();
                    var selected = EditorGUI.ToggleLeft(controlRect, controlLabel, true, ContentUtils.GUIStyles.richLabelNoWrap);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (!selected)
                        {
                            Undo.RecordObject(Sync, "Deselect Binding");
                            CoherenceSyncUtils.RemoveBinding(Sync, component, binding.Descriptor);
                        }

                        SetDirty(Sync);
                        Undo.FlushUndoRecordObjects();
                        if (serializedObject.UpdateIfRequiredOrScript())
                        {
                            GUIContents.RecalcScopeContents(this);
                        }

                        serializedProperty.Dispose();
                        GUIUtility.ExitGUI();
                        return;
                    }

                    {
                        var rect = GUILayoutUtility.GetLastRect();
                        var warnRect = rect;

                        var c = EditorGUIUtility.TrIconContent("Warning", invalidReason);
                        warnRect.x -= 3;
                        warnRect.width = 18;
                        GUI.Label(warnRect, c, EditorStyles.label);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(ContentUtils.GetInvalidContent(binding.Descriptor, invalidReason),
                        ContentUtils.GUIStyles.richLabelNoWrap);
                    {
                        var rect = GUILayoutUtility.GetLastRect();
                        var warnRect = rect;

                        var c = EditorGUIUtility.TrIconContent("Warning", invalidReason);
                        warnRect.x -= 3;
                        warnRect.width = 18;
                        GUI.Label(warnRect, c, EditorStyles.label);
                    }
                }

                OverrideActions.OpenContextMenu(controlRect, serializedProperty, Sync, isInstance);
                EditorGUI.EndProperty();

                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
                serializedProperty.Dispose();
            }

            if (!drawsBindings)
            {
                if (string.IsNullOrEmpty(searchString))
                {
                    var content = CanEdit ? "Nothing synchronizable." : "Nothing synchronized.";
                    EditorGUILayout.LabelField(content, ContentUtils.GUIStyles.miniLabelGrey);
                }
                else
                {
                    var content = CanEdit
                        ? $"Nothing synchronizable matching '{searchString}'."
                        : $"Nothing synchronized matching '{searchString}'.";
                    EditorGUILayout.LabelField(content, ContentUtils.GUIStyles.miniLabelGrey);
                }
            }

            EditorGUILayout.Space();
        }

        private void DrawPredictionLabel(GUIContent content)
        {
            var labelContent = new GUIContent(content.tooltip);
            var width = ContentUtils.GUIStyles.greyMiniLabelRight.CalcSize(labelContent).x;
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight,
                ContentUtils.GUIStyles.greyMiniLabelRight, GUILayout.Width(width));
            GUI.Label(rect, labelContent, ContentUtils.GUIStyles.greyMiniLabelRight);
        }

        private void DrawInvalidBindingsMessage()
        {
            if (invalidBindings == 0)
            {
                return;
            }

            NetworkObjectsInfoDrawer.DrawInvalidBindings(invalidBindings, () => Refresh(), Sync);
        }

        private void DrawBindingsWithInputAuthWarning()
        {
            NetworkObjectsInfoDrawer.DrawBindingsWithInputAuthorityPrediction(Sync, bindingsWithInputAuthPrediction,
                () => Refresh());
        }

        private bool HasMissingComponents()
        {
            var localInstanceHasMissingComponents = HasMissingComponents(Sync);

            var parentPrefabSync = PrefabUtility.GetCorrespondingObjectFromSource(Sync);
            if (parentPrefabSync == Sync)
            {
                return localInstanceHasMissingComponents;
            }

            return localInstanceHasMissingComponents && !HasMissingComponents(parentPrefabSync);
        }

        private bool HasMissingComponents(CoherenceSync sync)
        {
            foreach (var binding in sync.Bindings)
            {
                if (binding == null)
                {
                    continue;
                }

                if (!ShouldShow(binding.Descriptor, binding.UnityComponent))
                {
                    continue;
                }

                if (binding.Descriptor == null)
                {
                    continue;
                }

                if (binding.Descriptor.OwnerType == null || binding.UnityComponent == null)
                {
                    return true;
                }
            }

            return false;
        }

        private void DrawBindingsWithNullComponents()
        {
            bindingsWithNullComponents.Clear();

            for (var index = 0; index < Sync.Bindings.Count; index++)
            {
                var binding = Sync.Bindings[index];

                if (!binding.UnityComponent && !CoherenceSyncUtils.IsBindingValid(Sync, index, out var reason))
                {
                    bindingsWithNullComponents.Add((index, reason));
                }
            }

            if (bindingsWithNullComponents.Count == 0)
            {
                return;
            }

            _ = EditorGUILayout.BeginHorizontal(GUI.skin.box);
            var headerContent = EditorGUIUtility.TrTextContentWithIcon("Missing Script", "Warning");
            EditorGUILayout.LabelField(headerContent, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            for (var i = 0; i < bindingsWithNullComponents.Count; i++)
            {
                var (index, reason) = bindingsWithNullComponents[i];
                var binding = Sync.Bindings[index];

                if (!IncludedInSearchFilter(binding.Descriptor))
                {
                    continue;
                }

                var content = ContentUtils.GetInvalidContent(binding.Descriptor, reason);
                if (CanEdit)
                {
                    EditorGUI.BeginChangeCheck();
                    var selected = EditorGUILayout.ToggleLeft(content, true, ContentUtils.GUIStyles.richLabel);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (selected)
                        {
                            continue;
                        }

                        _ = CoherenceSyncUtils.RemoveBinding(Sync, binding.UnityComponent, binding.Descriptor);
                        GUIUtility.ExitGUI();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(content, ContentUtils.GUIStyles.richLabel);
                }
            }

            EditorGUI.indentLevel--;
        }

        private void OnEnumPopupUpdate()
        {
            GUIContents.RecalcScopeContents(this);
        }

        private bool ValidatePredictionMode(PredictionMode predictionMode) =>
            predictionMode != PredictionMode.InputAuthority || Sync.TryGetComponent(out CoherenceInput _);

        private (int totalCount, int invalidCount) VariablesCount()
            => CoherenceSyncUtils.GetVariableBindingsCount(Sync, Context);

        private (int totalCount, int invalidCount) MethodsCount()
            => CoherenceSyncUtils.GetMethodBindingsCount(Sync, Context);

        private (int totalCount, int invalidCount) ComponentActionsCount()
            => CoherenceSyncUtils.GetComponentActionsCount(Sync, Context);
    }
}
