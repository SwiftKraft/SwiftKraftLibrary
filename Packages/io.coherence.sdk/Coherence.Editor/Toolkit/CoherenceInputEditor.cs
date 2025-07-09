// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using Coherence.Toolkit;
    using UnityEditor;
    using UnityEngine;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(CoherenceInput))]
    internal class CoherenceInputEditor : BaseEditor
    {
        static class GUIContents
        {
            public static readonly GUIContent noCoherenceSyncWarning = EditorGUIUtility.TrTextContentWithIcon(
                $"Can't find {nameof(CoherenceSync)} component. Please make sure that one is attached and enabled.",
                "Warning@2x");
        }

        private CoherenceSync coherenceSyncCached;
        private bool bakedToggleHighlighted;

        protected override void OnDisable()
        {
            base.OnDisable();
            UnhighlightBakedToggle();
        }

        protected override void OnGUI()
        {
            serializedObject.Update();

            if (!TryGetCoherenceSync(out _))
            {
                EditorGUILayout.LabelField(GUIContents.noCoherenceSyncWarning, ContentUtils.GUIStyles.miniLabel);
                return;
            }

            UnhighlightBakedToggle();

            DrawPropertiesExcluding(serializedObject, "m_Script", "fields");

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fields"));

                if (changed.changed)
                {
                    BakeUtil.CoherenceSyncSchemasDirty = true;
                }
            }

            _ = serializedObject.ApplyModifiedProperties();
        }

        private bool TryGetCoherenceSync(out CoherenceSync coherenceSync)
        {
            if (coherenceSyncCached)
            {
                coherenceSync = coherenceSyncCached;
                return true;
            }

            if (target is Component component)
            {
                coherenceSync = coherenceSyncCached = component.GetComponent<CoherenceSync>();
                return coherenceSync != null;
            }

            coherenceSync = null;
            return false;
        }

        private void UnhighlightBakedToggle()
        {
            if (!bakedToggleHighlighted)
            {
                return;
            }

            Highlighter.Stop();
            bakedToggleHighlighted = false;
        }
    }
}
