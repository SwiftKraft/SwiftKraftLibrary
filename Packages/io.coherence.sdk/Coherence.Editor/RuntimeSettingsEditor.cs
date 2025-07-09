// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System.Reflection;
    using Coherence.Toolkit;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(RuntimeSettings))]
    internal class RuntimeSettingsEditor : BaseEditor, IOnAfterPreloaded
    {
        private string schemaID;
        private SerializedProperty organizationID;
        private SerializedProperty projectID;
        private SerializedProperty projectName;
        private SerializedProperty runtimeKey;
        private SerializedProperty schemas;
        private SerializedProperty advancedSettings;

        void IOnAfterPreloaded.OnAfterPreloaded()
        {
            var rs = target as RuntimeSettings;
            if (rs == null)
            {
                return;
            }

            rs.VersionInfo = AssetDatabase.LoadAssetAtPath<VersionInfo>(Paths.versionInfoPath);

            Postprocessor.UpdateRuntimeSettings();

            EditorUtility.SetDirty(rs);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            schemaID = TypeUtils.GetFieldValue<string>(serializedObject.targetObject, "schemaID",
                BindingFlags.Instance | BindingFlags.NonPublic);
            organizationID = serializedObject.FindProperty("organizationID");
            projectID = serializedObject.FindProperty("projectID");
            projectName = serializedObject.FindProperty("projectName");
            runtimeKey = serializedObject.FindProperty("runtimeKey");
            schemas = serializedObject.FindProperty("schemas");
            advancedSettings = serializedObject.FindProperty("advancedSettings");
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            organizationID.Dispose();
            projectID.Dispose();
            projectName.Dispose();
            runtimeKey.Dispose();
            schemas.Dispose();
            advancedSettings.Dispose();
        }

        protected override void OnGUI()
        {
            serializedObject.Update();

#if COHERENCE_USE_BAKED_SCHEMA_ID
            DrawSelectableLabel("Schema ID", ValueOrDefault(schemaID));
#else
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSelectableLabel("Schema ID", ValueOrDefault(schemaID));
                if (GUILayout.Button(Icons.GetContent("Coherence.Sync"), EditorStyles.miniButton,
                        GUILayout.ExpandWidth(false)))
                {
                    GUIUtility.keyboardControl = 0;
                    Postprocessor.UpdateRuntimeSettings();
                }

                if (BakeUtil.Outdated)
                {
                    GUILayout.Label(EditorGUIUtility.TrIconContent("Warning", "Outdated schemas, please bake again."));
                }
            }
#endif
            DrawSelectableLabel("Organization ID", ValueOrDefault(organizationID.stringValue));
            DrawSelectableLabel("Project ID", ValueOrDefault(projectID.stringValue));
            DrawSelectableLabel("Project Name", ValueOrDefault(projectName.stringValue));
            DrawSelectableLabel("Runtime Key", ValueOrDefault(runtimeKey.stringValue));

            DrawPropertiesExcluding(serializedObject, "m_Script", "schemaID", "uploadedSchema", "organizationID",
                "projectID", "projectName", "runtimeKey", "schemas", "advancedSettings");

            GUI.enabled = false;
            EditorGUILayout.PropertyField(schemas);
            GUI.enabled = true;

            if (RuntimeSettings.AdvancedSettings.Enabled)
            {
                EditorGUILayout.PropertyField(advancedSettings);
            }

            _ = serializedObject.ApplyModifiedProperties();
        }

        private static string ValueOrDefault(string text) => string.IsNullOrEmpty(text) ? "–" : text;

        private void DrawSelectableLabel(string prefix, string text)
        {
            _ = EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(prefix, EditorStyles.label);
            EditorGUILayout.SelectableLabel(text, EditorStyles.label,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();
        }
    }
}
