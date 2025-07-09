// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Toolkit
{
    using System;
    using Interpolation;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(InterpolationSettings))]
    [CanEditMultipleObjects]
    internal class InterpolationSettingsEditor : Editor
    {
        private static class GUIContents
        {
            public static readonly GUIContent interpolationType = EditorGUIUtility.TrTextContent("Interpolation Type");

            public static readonly GUIContent smoothing = EditorGUIUtility.TrTextContent("Smoothing");
            public static readonly GUIContent latency = EditorGUIUtility.TrTextContent("Latency");

            public static readonly GUIContent maxDistance = EditorGUIUtility.TrTextContent("Teleport Distance",
                "Maximum distance between data points before the component teleports to the next value without interpolating.");

            public static readonly GUIContent maxOvershootAllowed = EditorGUIUtility.TrTextContent("Max Overshoot Allowed",
                "Maximum number of samples to proceed into extrapolation after overshooting the final sample in the buffer.");
        }

        private class SerializedProperties : IDisposable
        {
            public SerializedProperty interpolationType;
            public SerializedProperty smoothing;
            public SerializedProperty latency;
            public SerializedProperty maxDistance;
            public SerializedProperty maxOvershootAllowed;

            public SerializedProperties(SerializedObject so)
            {
                interpolationType = so.FindProperty(nameof(InterpolationSettings.interpolator));
                smoothing = so.FindProperty(nameof(InterpolationSettings.smoothing));
                latency = so.FindProperty(nameof(InterpolationSettings.latencySettings));
                maxDistance = so.FindProperty(nameof(InterpolationSettings.maxDistance));
                maxOvershootAllowed = so.FindProperty(nameof(InterpolationSettings.maxOvershootAllowed));
            }

            public void Dispose()
            {
                interpolationType.Dispose();
                smoothing.Dispose();
                latency.Dispose();
                maxDistance.Dispose();
                maxOvershootAllowed.Dispose();
            }
        }

        private SerializedProperties serializedProperties;

        public override void OnInspectorGUI()
        {
            if (serializedObject.targetObject == null)
            {
                return;
            }

            serializedObject.Update();

            _ = EditorGUILayout.PropertyField(serializedProperties.interpolationType, GUIContents.interpolationType);

            using (new EditorGUI.DisabledScope(IsSettingsDisabled))
            {
                ContentUtils.DrawSection(serializedProperties.smoothing, GUIContents.smoothing);
                ContentUtils.DrawSection(serializedProperties.latency, GUIContents.latency);

                _ = EditorGUILayout.PropertyField(serializedProperties.maxDistance, GUIContents.maxDistance);
                _ = EditorGUILayout.PropertyField(serializedProperties.maxOvershootAllowed, GUIContents.maxOvershootAllowed);
            }

            _ = serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            if (target != null)
            {
                serializedProperties = new SerializedProperties(serializedObject);
            }
        }

        private void OnDisable()
        {
            serializedProperties?.Dispose();
        }

        private bool IsSettingsDisabled =>
            string.IsNullOrEmpty(serializedProperties.interpolationType.managedReferenceFullTypename) ||
            serializedProperties.interpolationType.managedReferenceFullTypename ==
            "Coherence.Interpolation Coherence.Interpolation.Interpolator";
    }
}
