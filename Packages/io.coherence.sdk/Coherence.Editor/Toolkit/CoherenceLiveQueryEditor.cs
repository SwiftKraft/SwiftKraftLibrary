// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using Coherence.Toolkit;
    using UnityEngine;
    using UnityEditor;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(CoherenceLiveQuery))]
    internal class CoherenceLiveQueryEditor : BaseEditor
    {
        private static class GUIContents
        {
            public static readonly GUIContent advanced = new("Advanced");
            public static readonly GUIContent inifinite = new("Infinite", "All entities within the scene are considered, independently of their position.");
            public static readonly GUIContent limited = new("Constrained", "Only consider entities contained within a cube volume.");
            public static readonly GUIContent extent = new("Extent", "Extent of the cube. It's always half the length of cube's edges.");
            public static readonly GUIContent helpTitle = new("What is a CoherenceLiveQuery");
            public static readonly GUIContent helpBody = new("Queries the Replication Server for entities that exist within an area of interest, bound by distance.");
        }

        private SerializedProperty extentProperty;
        private SerializedProperty extentUpdateThreshold;
        private SerializedProperty distanceUpdateThreshold;

        // For expanded properties, we use any serialized property that goes with its isExpanded property unused
        // The reason to rely on isExpanded instead of a bool field on this class is to benefit from
        // keeping state when the user switches between different CoherenceLiveQuery instances (changing selection or multi-editing)
        private bool HelpExpanded
        {
            get => extentUpdateThreshold.isExpanded;
            set => extentUpdateThreshold.isExpanded = value;
        }

        private bool AdvancedExpanded
        {
            get => extentProperty.isExpanded;
            set => extentProperty.isExpanded = value;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            extentProperty = serializedObject.FindProperty(CoherenceLiveQuery.Properties.Extent);
            extentUpdateThreshold = serializedObject.FindProperty(CoherenceLiveQuery.Properties.ExtentUpdateThreshold);
            distanceUpdateThreshold = serializedObject.FindProperty(CoherenceLiveQuery.Properties.DistanceUpdateThreshold);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            extentProperty.Dispose();
            extentUpdateThreshold.Dispose();
            distanceUpdateThreshold.Dispose();
        }

        protected override void OnGUI()
        {
            serializedObject.Update();
            DrawHelp();
            EditorGUILayout.Space();
            DrawAreaOfInterest();
            _ = serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            var liveQuery = target as CoherenceLiveQuery;
            if (!liveQuery || liveQuery.Extent <= 0)
            {
                return;
            }

            Handles.DrawWireCube(liveQuery.transform.position,2f * liveQuery.Extent * Vector3.one);
        }

        private void DrawHelp()
        {
            HelpExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(HelpExpanded, GUIContents.helpTitle);
            if (HelpExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(GUIContents.helpBody, ContentUtils.GUIStyles.wrappedLabel);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawAreaOfInterest()
        {
            EditorGUI.showMixedValue = extentProperty.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var infinite = EditorGUILayout.Toggle(GUIContents.inifinite, extentProperty.floatValue <= 0, EditorStyles.radioButton);
            if (EditorGUI.EndChangeCheck() && infinite)
            {
                extentProperty.floatValue = 0f;
            }
            EditorGUI.BeginChangeCheck();
            var constrained = EditorGUILayout.Toggle(GUIContents.limited, extentProperty.floatValue > 0, EditorStyles.radioButton);
            if (EditorGUI.EndChangeCheck() && constrained)
            {
                extentProperty.floatValue = 10f;
            }
            EditorGUI.showMixedValue = false;

            if (constrained)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(extentProperty, GUIContents.extent);
                AdvancedExpanded = EditorGUILayout.Foldout(AdvancedExpanded, GUIContents.advanced, true);
                if (AdvancedExpanded)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(distanceUpdateThreshold);
                    EditorGUILayout.PropertyField(extentUpdateThreshold);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}
