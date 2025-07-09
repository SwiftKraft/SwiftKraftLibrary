// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System;
    using System.Linq;
    using Portal;
    using Toolkit;
    using UnityEngine.UIElements;

    public class CoherenceHub : EditorWindow, IAnyHubModule
    {
        private class HubSectionClickedProperties : Analytics.BaseProperties
        {
            public string section;
        }

        private static class GUIContents
        {
            public static readonly GUIContent bakeUpToDate = EditorGUIUtility.TrTextContentWithIcon("Bake", "Network code for this project is baked and ready to use.", Icons.GetPath("Coherence.Bake.Valid"));
            public static readonly GUIContent bakeOutdated = EditorGUIUtility.TrTextContentWithIcon("Bake Now", Icons.GetPath("Coherence.Bake.Warning"));

            public static readonly GUIContent cloudUpToDate = EditorGUIUtility.TrTextContentWithIcon("Upload Schema to Cloud", "The current schema is part of coherence Cloud. Check the 'Cloud' tab for further details.", Icons.GetPath("Coherence.Cloud.Valid"));
            public static readonly GUIContent cloudOutdated = EditorGUIUtility.TrTextContentWithIcon("Upload Schema to Cloud", Icons.GetPath("Coherence.Cloud.Warning"));
            public static readonly GUIContent cloudNotLoggedIn = EditorGUIUtility.TrTextContentWithIcon("Signup / Login",  Icons.GetPath("Coherence.Cloud"));
            public static readonly GUIContent cloudProjectNotSet = EditorGUIUtility.TrTextContentWithIcon("Select Project",  Icons.GetPath("Coherence.Cloud.Warning"));

            public static readonly GUIContent settings = EditorGUIUtility.TrIconContent("Settings",  "Settings");
        }

        [Serializable]
        public class VersionInfo
        {
            public string Sdk => RuntimeSettings.Instance.VersionInfo.Sdk;
            public string Engine => RuntimeSettings.Instance.VersionInfo.Engine;
            public string ProjectID => RuntimeSettings.Instance.ProjectID;
            public string SchemaID => RuntimeSettings.Instance.SchemaID;

            public override string ToString()
            {
                return Sdk;
            }

            public void CopyToClipBoard()
            {
                var versionData =
                    $"SDK: {Sdk}{Environment.NewLine}" +
                    $"Engine: {Engine}{Environment.NewLine}" +
                    $"Unity: {Application.unityVersion}{Environment.NewLine}" +
                    $"OS: {SystemInfo.operatingSystem}{Environment.NewLine}" +
                    $"ProjectID: {ProjectID}{Environment.NewLine}" +
                    $"SchemaID: {SchemaID}{Environment.NewLine}";

                GUIUtility.systemCopyBuffer = versionData;

                var window = focusedWindow;
                if (window)
                {
                    window.ShowNotification(new GUIContent("Copied to clipboard"));
                }
            }
        }

        public static Action OnHubGainedFocus;
        public static readonly VersionInfo Info = new();

        Log.Logger IAnyHubModule.Logger { get; set; }
        private const float OuterSpacerH = 8;
        private const float OuterSpacerV = 4;
        private const float MinWidth = 285;
        private const float MinHeight = 300;

        private const string TabIndexKey = "Coherence.Hub.TabIndex";

        private Vector2 scrollPos;
        private int tabIndex;
        private GUIContent[] ToolbarContent => GetDockedModules().Select(m => m.TitleContent).ToArray();
        private CoherenceHeader headerDrawer;

        internal static CoherenceHub Open() => GetWindow<CoherenceHub>();

        internal static CoherenceHub Open<T>() where T : HubModule
        {
            var hub = GetWindow<CoherenceHub>();
            var module = GetModule<T>();
            if (module)
            {
                hub.FocusModule(module);
            }

            return hub;
        }


        private void OnEnable()
        {
            minSize = new Vector2(MinWidth, MinHeight);
            _ = HubModuleManager.instance.Purge();
            titleContent = EditorGUIUtility.TrTextContentWithIcon("coherence Hub", Icons.GetPath("EditorWindow"));
            headerDrawer ??= new CoherenceHeader(this);

            // We want to receive MouseMove events in order to show on-hover popups without window flickering, since MouseMove events do not trigger repaints
            wantsMouseMove = true;
            HubModuleManager.instance.AssignAllModules(this);

            EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;
            tabIndex = EditorPrefs.GetInt(TabIndexKey, 0);
            CheckTabIndexBoundary();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= HandleOnPlayModeChanged;
            EditorPrefs.SetInt(TabIndexKey, tabIndex);
        }

        private void CheckTabIndexBoundary()
        {
            if (tabIndex >= GetDockedModules().Length)
            {
                tabIndex = 0;
            }
        }

        private void OnDestroy()
        {
            HubModuleManager.instance.ReleaseModules(this);
        }

        private void OnFocus()
        {
            OnHubGainedFocus?.Invoke();
        }

        private static void HandleOnPlayModeChanged(PlayModeStateChange obj)
        {
            // TODO RegisterModules();
        }

        internal static T FocusModule<T>() where T : HubModule
        {
            var module = GetModule<T>();
            if (!module)
            {
                return null;
            }

            if (module.IsDocked)
            {
                GetWindow<CoherenceHub>().FocusModule(module);
            }
            else
            {
                _ = module.OpenWindowWrapper(true);
            }

            return module;
        }

        internal bool FocusModule<T>(T module) where T : HubModule
        {
            var idx = Array.IndexOf(GetDockedModules(), module);
            var found = idx != -1;
            if (found)
            {
                tabIndex = idx;
                CreateGUI();
            }

            return found;
        }

        internal static void ResetTabSelection()
        {
            GetWindow<CoherenceHub>().tabIndex = 0;
        }

        private static T GetModule<T>() where T : HubModule
        {
            return HubModuleManager.instance.GetActiveModule<T>();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();

            var drawHeader = new IMGUIContainer(DrawHeader);
            SetVisualElementNoFlex(drawHeader);
            root.Add(drawHeader);

            var module = GetDockedModule();
            if (!module)
            {
                return;
            }

            var drawCloneModeMessage = new IMGUIContainer(() => ContentUtils.DrawCloneModeMessage());
            SetVisualElementNoFlex(drawCloneModeMessage);
            root.Add(drawCloneModeMessage);

            var uiToolkitElement = module.CreateGUI();
            if (uiToolkitElement != null)
            {
                root.Add(uiToolkitElement);
                return;
            }

            var scrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                viewDataKey = "hubScrollView",
            };

            var imguiModule = new IMGUIContainer(() => DrawModuleImgui(module));
            SetVisualElementNoFlex(imguiModule);
            SetVisualElementMargins(imguiModule, 8);

            scrollView.Add(imguiModule);
            root.Add(scrollView);
        }

        private static void SetVisualElementMargins(VisualElement visualElement, int margin) =>
            visualElement.style.marginTop = visualElement.style.marginBottom =
            visualElement.style.marginLeft = visualElement.style.marginRight = margin;

        private static void SetVisualElementNoFlex(VisualElement visualElement)
        {
            visualElement.style.flexGrow = 0;
            visualElement.style.flexShrink = 0;
        }

        private void DrawModuleImgui(HubModule module)
        {
            if (module.Help != null)
            {
                module.ShowHelpSections = CoherenceHubLayout.DrawHelpFoldout(module.ShowHelpSections, module.Help);
                EditorGUILayout.Space();
            }

            EditorGUI.BeginDisabledGroup(CloneMode.Enabled && !CloneMode.AllowEdits);
            module.OnGUI();
            EditorGUI.EndDisabledGroup();
        }

        private void DrawHeader()
        {
            headerDrawer.OnGUIWithLogin();

            var backgroundRect = EditorGUILayout.BeginHorizontal(GUIStyle.none);
            backgroundRect.xMax += CoherenceHubLayout.Styles.Grid.margin.right;
            backgroundRect.yMax += CoherenceHubLayout.Styles.Grid.margin.bottom;

            EditorGUI.DrawRect(backgroundRect, Color.black);
            GUILayout.Space(OuterSpacerH);
            EditorGUILayout.BeginVertical(GUIStyle.none);
            GUILayout.Space(OuterSpacerV);
            DrawTabs();
            GUILayout.Space(OuterSpacerV);
            EditorGUILayout.EndVertical();
            GUILayout.Space(OuterSpacerH);
            EditorGUILayout.EndHorizontal();

            DrawToolbar();
        }

        private void DrawToolbar()
        {
            _ = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginDisabledGroup(CloneMode.Enabled);
            if (GUILayout.Button(BakeUtil.Outdated ? GUIContents.bakeOutdated : GUIContents.bakeUpToDate, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
            {
                if (!BakeUtil.Bake())
                {
                    ShowNotification(new GUIContent("Baking failed"));
                }
            }
            if (PortalUtil.OrgAndProjectIsSet)
            {
                if (GUILayout.Button(PortalUtil.InSync ? GUIContents.cloudUpToDate : GUIContents.cloudOutdated, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    Schemas.UploadActive(InteractionMode.UserAction);
                }
            }
            else if (PortalLogin.IsLoggedIn)
            {
                if (GUILayout.Button(GUIContents.cloudProjectNotSet, EditorStyles.toolbarButton,
                        GUILayout.ExpandWidth(false)))
                {
                    FocusModule<CloudModule>();
                    ShowNotification(new GUIContent("Select organization and project"));
                }
            }
            else
            {
                if (GUILayout.Button(GUIContents.cloudNotLoggedIn, EditorStyles.toolbarButton,
                        GUILayout.ExpandWidth(false)))
                {
                    PortalLogin.Login(Repaint);
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            if (GUILayout.Button(GUIContents.settings, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
            {
                _ = SettingsService.OpenProjectSettings(Paths.projectSettingsWindowPath);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            EditorGUI.BeginChangeCheck();
            tabIndex = CoherenceHubLayout.DrawGrid(tabIndex, ToolbarContent);
            if (EditorGUI.EndChangeCheck())
            {
                var module = GetDockedModules().ElementAtOrDefault(tabIndex);
                if (module)
                {
                    Analytics.Capture(new Analytics.Event<HubSectionClickedProperties>(
                        Analytics.Events.HubSectionClicked,
                        new HubSectionClickedProperties
                        {
                            section = module.ModuleName,
                        }
                    ));

                    CreateGUI();
                }
            }
        }

        private HubModule GetDockedModule()
        {
            var dockedModules = GetDockedModules();
            if (tabIndex >= dockedModules.Length || tabIndex < 0)
            {
                tabIndex = 0;
            }

            return dockedModules[tabIndex];
        }

        private HubModule[] GetDockedModules() => HubModuleManager.instance.GetActiveModules(this);
    }
}
