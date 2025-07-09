// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.UI
{
    // at least in 2020 (and probably way further...) there is no single non-internal SplitterView element
    // For now, we use 2 VisualElements in a flexbox without a splitter to avoid dealing with this.
#if USE_SPLITTER
    using System.Reflection;
#endif
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Editor;
    using JetBrains.Annotations;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// An <see cref="EditorWindow"/> to pick and add one of the sample dialogs to the scene.
    /// </summary>
    internal class SampleDialogPickerWindow : EditorWindow
    {
        private static readonly GUIContent WindowTitle = new("coherence Samples");
        private static readonly string DefaultHeaderTitle = "Select a Sample to see the description";

        private const string KeyPrefix = "coherence-" + nameof(SampleDialogPickerWindow);

        private SampleDialogAsset m_LastSelectedDialog;

        private Label titleLabel;
        private VisualElement splitter;
        private GridView<SampleDialogAsset> gridView;
        private SampleDialogPreview dialogPreview;
        private Button addButton;

        private static readonly Vector2 DefaultWindowSize = new(800, 600);
        private static readonly Vector2 MinWindowSize = new(775, 240);
        private static readonly float SplitterDefaultRatio = 0.75f;

        internal ToolbarSearchField searchField;

        /// <summary>
        /// Parent in the hierarchy when instantiating the Dialog.
        /// </summary>
        public GameObject DialogParentGameObject;

        #region VisualSplitter reflection

#if USE_SPLITTER
        private static Type SplitterType;
        private static ConstructorInfo SplitterCtor;

        static SampleDialogPickerWindow()
        {
            SplitterType = typeof(EditorWindow).Assembly.GetType("UnityEditor.UIElements.VisualSplitter");
            SplitterCtor = SplitterType.GetConstructor(new Type[] { });
        }
#endif

        private VisualElement MakeSplitter()
        {
#if USE_SPLITTER
            var el = (VisualElement) SplitterCtor.Invoke(new object[] {});
#else
            var el = new VisualElement();
            el.AddToClassList("unity-visual-splitter");
#endif
            return el;
        }

        #endregion

        /// <summary>
        /// Shows or focuses the instance of the <see cref="SampleDialogPickerWindow"/>.
        /// </summary>
        /// <param name="parentGameObject">Parent in the hierarchy when instantiating the Dialog.</param>
        /// <returns>The instance of the window.</returns>
        public static SampleDialogPickerWindow ShowWindow(string searchText = null, GameObject parentGameObject = null)
        {
            // Get existing open window or if none, make a new one:
            var window = GetWindow<SampleDialogPickerWindow>(true);
            window.titleContent = WindowTitle;
            window.minSize = MinWindowSize;
            window.DialogParentGameObject = parentGameObject;
            window.searchField.value = searchText ?? string.Empty;
            window.Show();
            return window;
        }

        private void ValidatePosition()
        {
            const float tolerance = 0.0001f;
            if (Math.Abs(position.xMin) < tolerance && position.yMin < tolerance)
            {
                position = GetCenteredWindowPosition(DefaultWindowSize);
            }
        }

        private static Rect GetCenteredWindowPosition(Vector2 size)
        {
            var parentWindowPosition = EditorGUIUtility.GetMainWindowPosition();
            var pos = new Rect
            {
                x = 0,
                y = 0,
                width = Mathf.Min(size.x, parentWindowPosition.width * 0.90f),
                height = Mathf.Min(size.y, parentWindowPosition.height * 0.90f),
            };
            var w = (parentWindowPosition.width - pos.width) * 0.5f;
            var h = (parentWindowPosition.height - pos.height) * 0.5f;
            pos.x = parentWindowPosition.x + w;
            pos.y = parentWindowPosition.y + h;
            return pos;
        }

        [UsedImplicitly]
        private void OnEnable()
        {
            ValidatePosition();
            BuildUI();
        }

        [UsedImplicitly]
        private void OnDisable()
        {
#if USE_SPLITTER
            if (splitter != null)
            {
                EditorPrefs.SetFloat(GetKeyName(nameof(splitter)), gridView.resolvedStyle.flexGrow);
            }
#endif
        }

        private void BuildUI()
        {
            // Keyboard events need a focusable element to trigger
            rootVisualElement.focusable = true;
            rootVisualElement.RegisterCallback<KeyUpEvent>(e =>
            {
                switch (e.keyCode)
                {
                    case KeyCode.Escape when !docked:
                        Close();
                        break;
                }
            });

            // Load stylesheets
            rootVisualElement.AddToClassList(Styles.unityThemeVariables);
            var styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    Paths.sampleDialogPickerPath + "/SampleDialogPickerWindow.uss");
            rootVisualElement.styleSheets.Add(styleSheet);

            var headerContainer = new VisualElement();
            headerContainer.AddToClassList(Styles.sampleDialogPickerHeader);
            rootVisualElement.Add(headerContainer);
            {
                titleLabel = new Label(DefaultHeaderTitle);
                titleLabel.AddToClassList(Styles.dialogPickerHeaderLabel);
                headerContainer.Add(titleLabel);
                searchField = new ToolbarSearchField();
                searchField.AddToClassList(Styles.gridViewHeaderSearchField);
                searchField.RegisterValueChangedCallback(evt => gridView.FilterString = evt.newValue);
                headerContainer.Add(searchField);
            }

            splitter = MakeSplitter();
            splitter.AddToClassList(Styles.classMainContainer);
            rootVisualElement.Add(splitter);
            {
                var dialogs = AssetDatabase.FindAssets($"t:{typeof(SampleDialogAsset)}")
                    .Select(guid => AssetDatabase.LoadAssetAtPath<SampleDialogAsset>(AssetDatabase.GUIDToAssetPath(guid)))
                    .Where(asset => asset.Enabled)
                    .OrderBy(dialog => dialog.Priority);

                gridView = new GridView<SampleDialogAsset>
                {
                    Items = dialogs,
                };
                gridView.onSelectionChanged += OnTemplateListViewSelectionChanged;
                gridView.onItemsActivated += OnItemsActivated;
                splitter.Add(gridView);

                // Create a container for the template description (right side)
                dialogPreview = new SampleDialogPreview();
                splitter.Add(dialogPreview);

                EditorApplication.delayCall += () =>
                {
                    var leftRatio = EditorPrefs.GetFloat(GetKeyName(nameof(splitter)), SplitterDefaultRatio);
                    SetSplitterRatio(leftRatio);
                };
            }

            var footer = new VisualElement();
            footer.AddToClassList(Styles.sceneTemplateDialogFooter);
            rootVisualElement.Add(footer);
            {
                var buttonContainer = new VisualElement();
                buttonContainer.AddToClassList(Styles.classButtons);
                addButton = new Button(AddCurrentDialog)
                {
                    text = "Add To Scene",
                };
                addButton.AddToClassList(Styles.classElementSelected);
                buttonContainer.Add(addButton);
                var cancelButton = new Button(Close)
                {
                    text = "Cancel",
                };
                cancelButton.AddToClassList(Styles.classButton);
                buttonContainer.Add(cancelButton);
                footer.Add(buttonContainer);
            }

            gridView.SetSelection(gridView.Items.FirstOrDefault());
        }

        private void OnTemplateListViewSelectionChanged(IEnumerable<SampleDialogAsset> oldSelection,
            IEnumerable<SampleDialogAsset> newSelection)
        {
            var sampleDialog = newSelection.FirstOrDefault();

            if (sampleDialog != default)
            {
                switch (sampleDialog.SampleCategory)
                {
                    case SampleCategory.PackageSample:
                        addButton.text = !string.IsNullOrEmpty(sampleDialog.PrefabFileName)
                            ? "Add To Scene"
                            : "Import To Project";
                        break;
                    case SampleCategory.External:
                        addButton.text = "Open In Browser";
                        break;
                }

                dialogPreview.SampleDialog = sampleDialog;
                SetLastSelectedDialog(sampleDialog);
            }
        }

        private void OnItemsActivated(IEnumerable<SampleDialogAsset> items)
        {
            var sampleDialog = items.FirstOrDefault();
            if (sampleDialog != default)
            {
                AddDialog(sampleDialog);
            }
        }

        private void AddCurrentDialog()
        {
            var dialogToAdd = dialogPreview.SampleDialog;
            if (!dialogToAdd)
            {
                Debug.LogError("No dialog found to add.");
                return;
            }

            AddDialog(dialogToAdd);
        }

        private void AddDialog(SampleDialogAsset dialogAsset)
        {
            switch (dialogAsset.SampleCategory)
            {
                case SampleCategory.PackageSample:
                    UIUtils.ImportAndPingFromPackageSample(dialogAsset, DialogParentGameObject);
                    break;
                case SampleCategory.External:
                    Application.OpenURL(dialogAsset.ExternalUrl);
                    break;
            }
        }

        private void SetSplitterRatio(float leftRatio)
        {
            gridView.style.flexGrow = leftRatio;
            dialogPreview.style.flexGrow = 1f - leftRatio;
        }

        private static string GetKeyName(string name)
        {
            return $"{KeyPrefix}.{name}";
        }

        private void SetLastSelectedDialog(SampleDialogAsset sampleDialog)
        {
            m_LastSelectedDialog = sampleDialog;
            EditorPrefs.SetString(GetKeyName(nameof(m_LastSelectedDialog)), sampleDialog.PrefabFileName);
        }
    }
}
