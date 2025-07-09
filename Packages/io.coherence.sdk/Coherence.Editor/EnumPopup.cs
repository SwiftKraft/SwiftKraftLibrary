namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Toolkit;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    internal class EnumPopup<TEnum> where TEnum : Enum
    {
        public Object Target { get; }
        public string PropertyPath { get; }
        public Action OnUpdated { get; }
        public Action OnApply { get; }
        public Func<TEnum, bool> OnValidate { get; }
        public IReadOnlyDictionary<TEnum, GUIContent> Contents { get; }

        public EnumPopup(Object target, string propertyPath, IReadOnlyDictionary<TEnum, GUIContent> contents,
            Action onUpdated = null, Action onApply = null, Func<TEnum, bool> onValidate = null)
        {
            if (typeof(TEnum).GetCustomAttribute<FlagsAttribute>() != null)
            {
                throw new InvalidOperationException($"Bitfields ({nameof(FlagsAttribute)}) not supported.");
            }

            Target = target;
            PropertyPath = propertyPath;
            Contents = contents;
            OnUpdated = onUpdated;
            OnApply = onApply;
            OnValidate = onValidate;
        }

        public void Show()
        {
            var position = new Rect(Event.current.mousePosition, Vector2.zero);
            Show(position);
        }

        public void Show(Rect position)
        {
            var popup = new GenericPopup(OnMethodPopupGUI, GetMethodPopupSize);
            PopupWindow.Show(position, popup);
        }

        private void OnMethodPopupGUI()
        {
            using var serializedObject = new SerializedObject(Target);
            using var serializedProperty = serializedObject.FindProperty(PropertyPath);

            if (serializedProperty.serializedObject.UpdateIfRequiredOrScript())
            {
                OnUpdated?.Invoke();
            }

            foreach (var (enumValue, content) in Contents)
            {
                var intValue = Convert.ToInt32(enumValue);

                EditorGUI.BeginChangeCheck();
                var style = ContentUtils.GUIStyles.menuItem;
                var enabled = OnValidate?.Invoke(enumValue) ?? true;
                EditorGUI.BeginDisabledGroup(!enabled);
                var active = GUILayout.Toggle(serializedProperty.intValue == intValue, content, style);
                EditorGUI.EndDisabledGroup();
                if (EditorGUI.EndChangeCheck() && active)
                {
                    serializedProperty.intValue = intValue;
                }
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                OnApply?.Invoke();
            }
        }

        private Vector2 GetMethodPopupSize()
        {
            var maxWidth = 0f;

            foreach (var (_, guiContent) in Contents)
            {
                var width = ContentUtils.GUIStyles.menuItem.CalcSize(guiContent).x;

                if (width > maxWidth)
                {
                    maxWidth = width;
                }
            }

            maxWidth = Mathf.Ceil(maxWidth);

            var lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return new Vector2(maxWidth, lineHeight * Contents.Count);
        }
    }
}
