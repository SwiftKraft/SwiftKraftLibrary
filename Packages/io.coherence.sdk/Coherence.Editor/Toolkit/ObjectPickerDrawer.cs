// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using System.IO;
    using System.Linq;
    using Coherence.Toolkit;
    using UnityEditor;
    using UnityEngine;
#if COHERENCE_OBJECT_PICKER_PRESETS
    using UnityEditor.Presets;
#endif

    [CustomPropertyDrawer(typeof(ObjectPickerAttribute))]
    internal class ObjectPickerDrawer : PropertyDrawer
    {
#if COHERENCE_OBJECT_PICKER_EDITOR
        private static Editor editor;
#endif
        private static Object[] cachedAssets;
        private static Rect lastPopupRect;

        protected SerializedProperty cachedProperty;
        protected string path;
        protected Object[] objs;

        public virtual bool DisplayNoneOption => true;

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                _ = EditorGUI.PropertyField(rect, property, label);
                return;
            }

            _ = EditorGUI.BeginProperty(rect, label, property);

            float buttonWidth = 16;
            var pickerRect = new Rect(rect.xMax - rect.height, rect.y, rect.height, rect.height);

            var e = Event.current;
            var obj = property.objectReferenceValue;

            if (GUI.Button(pickerRect, GetIconContent(property), ContentUtils.GUIStyles.iconButton))
            {
                var r = new Rect(Event.current.mousePosition, Vector2.zero);
                objs = property.serializedObject.targetObjects;
                path = property.propertyPath;
                cachedProperty = new SerializedObject(objs).FindProperty(path);
                ShowPopup(r);
            }

            if (obj)
            {
                var labelStyle = ContentUtils.GUIStyles.greyMiniLabelRight;
                var interpolationName = obj ? obj.name : InterpolationPickerAttribute.None;
                var labelRect = new Rect(rect.x, rect.y - 1, rect.width - buttonWidth - 2, rect.height);
                var labelContent = EditorGUIUtility.TrTempContent(interpolationName);

                if (labelRect.width >= labelStyle.CalcSize(labelContent).x)
                {
                    GUI.Label(labelRect, labelContent, ContentUtils.GUIStyles.greyMiniLabelRight);
                }
                else
                {
                    GUI.Label(labelRect, "...", ContentUtils.GUIStyles.greyMiniLabelRight);
                }

                if (e.type == EventType.MouseDown &&
                    labelRect.Contains(e.mousePosition) &&
                    obj)
                {
                    if (e.clickCount == 1)
                    {
                        EditorGUIUtility.PingObject(property.objectReferenceValue);
                    }
                    else if (e.clickCount == 2)
                    {
                        Selection.activeObject = property.objectReferenceValue;
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        protected virtual GUIContent GetIconContent(SerializedProperty property)
        {
            return EditorGUIUtility.TrIconContent("_Popup");
        }

        private void ShowPopup(Rect rect)
        {
            var popup = new GenericPopup(OnPopupGUI, GetPopupSize, OnPopupOpen, OnPopupClose);
            PopupWindow.Show(rect, popup);
            GUIUtility.ExitGUI();
        }

        private void ShowPopupFromCache()
        {
            var popup = new GenericPopup(OnPopupGUI, GetPopupSize, OnPopupOpen, OnPopupClose);
            var r = lastPopupRect;
            PopupWindow.Show(lastPopupRect, popup);
            GUIUtility.ExitGUI();
        }

        private void RefreshAssets()
        {
            var guids = AssetDatabase.FindAssets("t:" +
                                                 fieldInfo.FieldType
                                                     .Name); // TODO might need type's full name for non Unity assets?
            cachedAssets = guids
                .Select(guid => AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
        }

        private void OnPopupOpen()
        {
            UpdateReference();
            RefreshAssets();
        }

#if COHERENCE_OBJECT_PICKER_PRESETS
        private void DrawPresetsButton()
        {
            var obj = cachedProperty.objectReferenceValue;
            var c = EditorGUIUtility.TrIconContent("Preset.Context");
            var disabled = !obj ||
                !new PresetType(obj).IsValid() ||
                (obj.hideFlags & HideFlags.NotEditable) != 0;

            EditorGUI.BeginDisabledGroup(disabled);
            if (GUILayout.Button(c, ContentUtils.GUIStyles.iconButton))
            {
                var receiver = ScriptableObject.CreateInstance<PopupPresetSelectorReceiver>();
                receiver.Init(obj);
                PresetSelector.ShowSelector(obj, null, true, receiver);
            }
            EditorGUI.EndDisabledGroup();
        }
#endif

        private void DrawInspectButton()
        {
            var obj = cachedProperty.objectReferenceValue;
            var c = EditorGUIUtility.TrIconContent("UnityEditor.InspectorWindow");
            EditorGUI.BeginDisabledGroup(!obj);
            if (GUILayout.Button(c, ContentUtils.GUIStyles.iconButton))
            {
                Selection.activeObject = obj;
            }

            EditorGUI.EndDisabledGroup();
        }

        private void OnPopupGUI()
        {
            _ = cachedProperty.serializedObject.UpdateIfRequiredOrScript();

            var obj = cachedProperty.objectReferenceValue;
            _ = EditorGUILayout.BeginHorizontal();
            GUILayout.Label(obj ? $"Using '{obj.name}'" : "Using none", ContentUtils.GUIStyles.miniLabelGrey,
                GUILayout.ExpandWidth(true));

            EditorGUILayout.Space();

            DrawInspectButton();
#if COHERENCE_OBJECT_PICKER_PRESETS
            DrawPresetsButton();
#endif
            EditorGUILayout.EndHorizontal();

            DrawSelector();

#if COHERENCE_OBJECT_PICKER_EDITOR
            EditorGUI.BeginDisabledGroup(!obj || (obj.hideFlags & HideFlags.NotEditable) != 0);
            if (editor)
            {
                editor.OnInspectorGUI();
            }
            EditorGUI.EndDisabledGroup();
#endif

            _ = cachedProperty.serializedObject.ApplyModifiedProperties();
        }

        private void DrawSelector()
        {
            var obj = cachedProperty.objectReferenceValue;

            _ = EditorGUILayout.BeginVertical();
            EditorGUI.BeginChangeCheck();
            if (DisplayNoneOption)
            {
                var selNone = GUILayout.Toggle(!obj, ObjectPickerAttribute.None, ContentUtils.GUIStyles.miniMenuItem);
                if (EditorGUI.EndChangeCheck())
                {
                    if (selNone)
                    {
                        UpdateReference(null);
                        GenericPopup.Repaint();
                    }
                }
            }

            if (cachedAssets != null && cachedAssets.Length > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label(GUIContent.none, ContentUtils.GUIStyles.separator);
                foreach (var asset in cachedAssets)
                {
                    if (!asset)
                    {
                        continue;
                    }

                    EditorGUI.BeginChangeCheck();
                    var sel = GUILayout.Toggle(asset == obj, asset.name, ContentUtils.GUIStyles.miniMenuItem);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (sel)
                        {
                            UpdateReference(asset);
                            GenericPopup.Repaint();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }

            if (typeof(ScriptableObject).IsAssignableFrom(fieldInfo.FieldType))
            {
                GUILayout.Space(4);
                GUILayout.Label(GUIContent.none, ContentUtils.GUIStyles.separator);
                if (GUILayout.Button("Create new...", ContentUtils.GUIStyles.miniMenuItem))
                {
                    GenericPopup.SendCloseCommand();
                    var asset = CreateAsset();
                    UpdateReference(asset ? asset : obj);
                    ShowPopupFromCache();
                }
            }

            GUILayout.EndVertical();
        }

        private void UpdateReference(Object obj)
        {
            if (obj)
            {
                cachedProperty.serializedObject.Dispose();
                cachedProperty.Dispose();
                cachedProperty = new SerializedObject(objs).FindProperty(path);
            }

            if (cachedProperty.objectReferenceValue != obj)
            {
                OnReferenceChanged(cachedProperty.objectReferenceValue, obj);
            }

            cachedProperty.serializedObject.Update();
            cachedProperty.objectReferenceValue = obj;
            _ = cachedProperty.serializedObject.ApplyModifiedProperties();
            UpdateReference();
        }

        protected virtual void OnReferenceChanged(Object oldReference, Object newReference)
        {
        }

        private void UpdateReference()
        {
#if COHERENCE_OBJECT_PICKER_EDITOR
            if (editor)
            {
                Object.DestroyImmediate(editor);
                editor = null;
            }

            if (cachedProperty.objectReferenceValue)
            {
                editor = Editor.CreateEditor(cachedProperty.objectReferenceValue);
            }
#endif
        }

        private void OnPopupClose()
        {
            cachedProperty.serializedObject.Dispose();
            cachedProperty.Dispose();

#if COHERENCE_OBJECT_PICKER_EDITOR
            if (editor)
            {
                Object.DestroyImmediate(editor);
            }
#endif
        }

        private Vector2 GetPopupSize()
        {
            var width = 240;
            var headerHeight = ContentUtils.GUIStyles.miniLabelGrey.CalcHeight(GUIContent.none, width);
            var singleLineHeight = ContentUtils.GUIStyles.miniMenuItem.CalcHeight(GUIContent.none, width);
            var assetCount = cachedAssets != null ? cachedAssets.Length : 0;
            var assetsHeight = assetCount * singleLineHeight;
            var separatorHeight = 8;
            var height = headerHeight + (1 * singleLineHeight) + separatorHeight + assetsHeight;
            if (!DisplayNoneOption)
            {
                height -= singleLineHeight;
            }

            if (typeof(ScriptableObject).IsAssignableFrom(fieldInfo.FieldType))
            {
                height += separatorHeight + singleLineHeight;
            }

            return new Vector2(width, height);
        }

        public Object CreateAsset()
        {
            var uniquePath =
                AssetDatabase.GenerateUniqueAssetPath(
                    $"{Paths.projectAssetsPath}/New {fieldInfo.FieldType.Name}.asset");
            var path = Path.GetDirectoryName(uniquePath);
            var filename = Path.GetFileName(uniquePath);
            var savePath = EditorUtility.SaveFilePanelInProject("Create new asset", filename, "asset",
                "Select where to save the asset", path);
            if (!string.IsNullOrEmpty(savePath))
            {
                var newSettings = CreateInstance();
                AssetDatabase.CreateAsset(newSettings, savePath);

                return newSettings;
            }

            return null;
        }

        protected virtual Object CreateInstance()
        {
            return ScriptableObject.CreateInstance(fieldInfo.FieldType);
        }
    }
}
