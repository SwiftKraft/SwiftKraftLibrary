namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using CodeGen;
    using UnityEditor;
    using UnityEngine;

    public class AdvancedBakeWizard : ScriptableWizard
    {
        private bool noUnityReferences;
        private bool bakeUnityProject;

#if COHERENCE_HAS_RSL
        private bool extendedDefinition = true;
#else
        private bool extendedDefinition = false;
#endif

        private string outputDirectory = string.Empty;
        private List<string> schemaPaths = new();

        public static void Open()
        {
            DisplayWizard<AdvancedBakeWizard>("Advanced Bake - C# Protocol Code Generator", "Bake");
        }

        protected override bool DrawWizardGUI()
        {
            DrawTemplateConfigs();

            noUnityReferences =
                EditorGUILayout.Toggle(new GUIContent("No Unity References", "Pure C# code with no Unity references"),
                    noUnityReferences);

            bakeUnityProject =
                EditorGUILayout.Toggle(
                    new GUIContent("Bake Unity Project",
                        "Save Gathered.schema for your project and generate code for the RS, Toolkit and Gathered schemas."),
                    bakeUnityProject);

            extendedDefinition = EditorGUILayout.Toggle(new GUIContent("Extended Definition", "Required by the RSL"),
                extendedDefinition);

            EditorGUILayout.Separator();

            CoherenceHubLayout.DrawBoldLabel(new GUIContent("Output Directory"));
            CoherenceHubLayout.DrawDiskPath(outputDirectory, "Select a valid folder", SelectFolder, path =>
            {
                outputDirectory = path;
            });

            EditorGUILayout.Separator();

            DrawSchemas();

            EditorGUILayout.Separator();

            using var disabled = new EditorGUI.DisabledScope(!isValid);
            if (GUILayout.Button("Copy Headless Command To Clipboard"))
            {
                var command = GetCommand();
                GUIUtility.systemCopyBuffer = command;
                Debug.Log($"Copied {command} to clipboard");
            }

            isValid = Directory.Exists(outputDirectory) && (bakeUnityProject || schemaPaths.Count > 0);

            errorString = isValid ? string.Empty : "Missing output directory or schema file paths";

            return isValid;
        }

        private string GetCommand()
        {
            string exePath = string.Empty;
#if UNITY_EDITOR_WIN || UNITY_EDITOR_LINUX
            exePath = EditorApplication.applicationPath;
#elif UNITY_EDITOR_OSX
            exePath = $"{EditorApplication.applicationContentsPath}/MacOS/Unity";
#endif
            var method = bakeUnityProject
                ? "Coherence.Editor.BakeUtil.Bake"
                : "Coherence.Editor.CodeGenSelector.RunForExternalSchemas";

            var schemasStringBuilder = new StringBuilder();

            if (!bakeUnityProject && schemaPaths.Count > 0)
            {
                for (int i = 0; i < schemaPaths.Count; i++)
                {
                    schemasStringBuilder.Append(schemaPaths[i]);

                    if (i != schemaPaths.Count - 1)
                    {
                        schemasStringBuilder.Append(',');
                    }
                }
            }

            return
                $"\"{exePath}\" -projectPath \"{Path.GetDirectoryName(Application.dataPath)}\" -batchmode -nographics -quit -executeMethod {method} --output-dir \"{outputDirectory}\" {(noUnityReferences ? "--no-unity-refs" : string.Empty)} {(extendedDefinition ? "--extended-def" : string.Empty)} {(schemasStringBuilder.Length > 0 ? $"--schema \"{schemasStringBuilder}\"" : String.Empty)} -logFile \"{Path.GetFullPath(Paths.libraryRootPath + "/code-generation-log.txt")}\"";
        }

        private const string BakedFolder = "/baked"; // need this to prevent the PCG from deleting the contents of the root.

        private const string RSLDefinitionPath = "../replication-server-lite/replication-server-lite/Definition";
        private const string RSLTestsPath = "../replication-server-lite/server.tests/Definition";
        private const string CommonTestsPath = "../sdk/Coherence.Common.Tests";
        private const string HeadlessClientCloudTestPath = "../Scenarios/CloudTest";
        private const string HeadlessClientFPSPath = "../Scenarios/FPS";

        private void DrawTemplateConfigs()
        {
            using var scope = new EditorGUILayout.VerticalScope(CoherenceHubLayout.Styles.SectionBox);
            CoherenceHubLayout.DrawBoldLabel(new GUIContent("Template Configs"));

            DrawRslDefinitionTemplate();
            DrawRslTestsTemplate();
            DrawCommonTestsTemplate();
            DrawHeadlessClientCloudTestTemplate();
            DrawHeadlessClientFPSTemplate();
        }

        private void DrawHeadlessClientCloudTestTemplate()
        {
            if (!Directory.Exists(HeadlessClientCloudTestPath))
            {
                return;
            }

            if (!GUILayout.Button("Headless Client Cloud Test"))
            {
                return;
            }

            outputDirectory = Path.GetFullPath(HeadlessClientCloudTestPath + BakedFolder);
            Directory.CreateDirectory(outputDirectory);

            extendedDefinition = false;
            noUnityReferences = true;
            bakeUnityProject = true;
            schemaPaths.Clear();
        }

        private void DrawHeadlessClientFPSTemplate()
        {
            if (!Directory.Exists(HeadlessClientFPSPath))
            {
                return;
            }

            if (!GUILayout.Button("Headless Client FPS"))
            {
                return;
            }

            outputDirectory = Path.GetFullPath(HeadlessClientFPSPath + BakedFolder);
            Directory.CreateDirectory(outputDirectory);

            extendedDefinition = false;
            noUnityReferences = true;
            bakeUnityProject = true;
            schemaPaths.Clear();
        }

        private void DrawCommonTestsTemplate()
        {
            if (!Directory.Exists(CommonTestsPath))
            {
                return;
            }

            if (!GUILayout.Button("Common.Tests"))
            {
                return;
            }

            outputDirectory = Path.GetFullPath(CommonTestsPath + BakedFolder);
            Directory.CreateDirectory(outputDirectory);

            extendedDefinition = false;
            noUnityReferences = false;
            bakeUnityProject = false;
            schemaPaths.Clear();

            schemaPaths.Add(Path.GetFullPath(Paths.rsSchemaPath));
            schemaPaths.Add(Path.GetFullPath($"{CommonTestsPath}/test.schema"));
        }

        private void DrawRslDefinitionTemplate()
        {
            if (!Directory.Exists(RSLDefinitionPath))
            {
                return;
            }

            if (!GUILayout.Button("Replication Server Lite Definition"))
            {
                return;
            }

            outputDirectory = Path.GetFullPath(RSLDefinitionPath + BakedFolder);
            Directory.CreateDirectory(outputDirectory);

            extendedDefinition = true;
            noUnityReferences = true;
            bakeUnityProject = true;
            schemaPaths.Clear();
        }

        private void DrawRslTestsTemplate()
        {
            if (!Directory.Exists(RSLTestsPath))
            {
                return;
            }

            if (!GUILayout.Button("Replication Server Lite Tests"))
            {
                return;
            }

            outputDirectory = Path.GetFullPath(RSLTestsPath + BakedFolder);
            Directory.CreateDirectory(outputDirectory);

            extendedDefinition = true;
            noUnityReferences = true;
            bakeUnityProject = false;
            schemaPaths.Clear();

            schemaPaths.Add(Path.GetFullPath(Paths.rsSchemaPath));
            schemaPaths.Add(Path.GetFullPath($"{RSLTestsPath}/test.schema"));
        }

        private void DrawSchemas()
        {
            if (bakeUnityProject)
            {
                return;
            }

            using var scope = new EditorGUILayout.VerticalScope(CoherenceHubLayout.Styles.SectionBox);

            using (new EditorGUILayout.HorizontalScope())
            {
                CoherenceHubLayout.DrawBoldLabel(new GUIContent("Schemas"));
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Add From Disk"))
                {
                    var schemaFile = SelectSchemaFile();

                    if (File.Exists(schemaFile))
                    {
                        AddSchemaPath(schemaFile);
                    }
                }
            }

            EditorGUI.indentLevel++;

            for (int i = schemaPaths.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.LabelField(schemaPaths[i]);

                if (GUILayout.Button("Remove"))
                {
                    schemaPaths.RemoveAt(i);
                }
            }

            EditorGUI.indentLevel--;
        }

        private void AddSchemaPath(string path)
        {
            if (!schemaPaths.Contains(path))
            {
                schemaPaths.Add(path);
            }
        }

        private void OnWizardCreate()
        {
            if (bakeUnityProject)
            {
                _ = noUnityReferences ? BakeUtil.CustomBake(noUnityReferences, outputDirectory) : BakeUtil.Bake();

                return;
            }

            var codeGenData = new CodeGenData
            {
                SchemaDefinition = BakeUtil.GetSchemaDefinitionFromSchemaFiles(schemaPaths),
                EntitiesData = new EntitiesBakeData
                {
                    InputData = new Dictionary<string, string>(),
                    BehaviourData = new List<SyncedBehaviour>()
                },
                ExtendedDefinition = extendedDefinition,
                NoUnityReferences = noUnityReferences,
                OutputDirectory = outputDirectory
            };

            new CodeGenRunner().Run(codeGenData);
        }

        private string SelectFolder()
        {
            return EditorUtility.OpenFolderPanel("Select a valid folder", string.Empty, string.Empty);
        }

        private string SelectSchemaFile()
        {
            return EditorUtility.OpenFilePanel("Select a valid Schema file", string.Empty, "schema");
        }
    }
}
