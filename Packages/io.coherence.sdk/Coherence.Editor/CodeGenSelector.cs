// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using CodeGen;
    using Log;
    using UnityEditor;
    using UnityEngine;
    using Logger = Log.Logger;

    public static class CodeGenSelector
    {
        public delegate void BakeCompleteHandler();

        public static event BakeCompleteHandler OnBakeComplete;

        private static readonly Logger logger = Log.GetLogger(typeof(CodeGenSelector));

        private class CodeGenProperties : Analytics.BaseProperties
        {
            public string strategy;
            public bool success;
        }

        public static void RunForExternalSchemas()
        {
            var codeGenData = new CodeGenData
            {
                EntitiesData = new EntitiesBakeData
                {
                    InputData = new Dictionary<string, string>(),
                    BehaviourData = new List<SyncedBehaviour>(),
                },
            };

            CodeGenDataCliArgsOverrides(codeGenData, true);

            new CodeGenRunner().Run(codeGenData);
        }

        internal static async Task<bool> RunForProjectSchemasAsync(SchemaDefinition gatheredSchemaDefinition,
            EntitiesBakeData entitiesData, bool noUnityReferences, string bakePath)
        {
            var defaultAssetsBakePath = string.IsNullOrEmpty(bakePath) ? Paths.defaultSchemaBakePath : bakePath;

            var success = await RunProjectSchemasCodeGenAsync(gatheredSchemaDefinition, entitiesData, defaultAssetsBakePath,
                noUnityReferences);

            OnCodeGenCompleted(success);

            return success;
        }

        internal static bool RunForProjectSchemas(SchemaDefinition gatheredSchemaDefinition,
            EntitiesBakeData entitiesData, bool noUnityReferences, string bakePath)
        {
            var defaultAssetsBakePath = string.IsNullOrEmpty(bakePath) ? Paths.defaultSchemaBakePath : bakePath;

            var success = RunProjectSchemasCodeGen(gatheredSchemaDefinition, entitiesData, defaultAssetsBakePath,
                noUnityReferences);

            OnCodeGenCompleted(success);

            return success;
        }

        internal static bool Clear(bool warn = false)
        {
            try
            {
                if (warn &&
                    !EditorUtility.DisplayDialog("Delete Baked Data",
                        "Are you sure you want to delete all the generated baked files?", "Ok", "Cancel"))
                {
                    return false;
                }

                if (AssetDatabase.MoveAssetToTrash(Paths.defaultSchemaBakePath))
                {
                    AssetDatabase.Refresh();
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        private static void OnCodeGenCompleted(bool success)
        {
            if (success)
            {
                _ = ProjectSettings.instance.RehashActiveSchemas();
                try
                {
                    OnBakeComplete?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            Analytics.Capture(new Analytics.Event<CodeGenProperties>(
                Analytics.Events.Bake,
                new CodeGenProperties
                {
                    strategy = "Assets",
                    success = success,
                }
            ));
        }

        private static bool RunProjectSchemasCodeGen(SchemaDefinition schemaDefinition, EntitiesBakeData entitiesData,
            string bakePath, bool noUnityReferences)
        {
            var codeGenData =
                GetCodeGenDataForProjectSchemas(schemaDefinition, entitiesData, bakePath, noUnityReferences);

            if (!new CodeGenRunner().Run(codeGenData))
            {
                return false;
            }

            return true;
        }

        private static async Task<bool> RunProjectSchemasCodeGenAsync(SchemaDefinition schemaDefinition,
            EntitiesBakeData entitiesData, string bakePath, bool noUnityReferences)
        {
            var codeGenData =
                GetCodeGenDataForProjectSchemas(schemaDefinition, entitiesData, bakePath, noUnityReferences);

            if (!await Task.Run(() => new CodeGenRunner().Run(codeGenData)))
            {
                return false;
            }

            return true;
        }

        private static CodeGenData GetCodeGenDataForProjectSchemas(SchemaDefinition schemaDefinition,
            EntitiesBakeData entitiesData, string bakePath, bool noUnityReferences)
        {
            var combinedSchema = GetCombinedProjectSchemas(schemaDefinition);

            var codeGenData = new CodeGenData
            {
                SchemaDefinition = combinedSchema,
                EntitiesData = entitiesData,
                OutputDirectory = bakePath,
                ExtendedDefinition = false,
                BakeToolkitImplementations = true,
                NoUnityReferences = noUnityReferences,
            };

            CodeGenDataCliArgsOverrides(codeGenData, false);

#if COHERENCE_HAS_RSL
            codeGenData.ExtendedDefinition = true;
#endif

            return codeGenData;
        }

        private static SchemaDefinition GetCombinedProjectSchemas(SchemaDefinition userSchema)
        {
            var toolkitSchema = AssetDatabase.LoadAssetAtPath<SchemaAsset>(Paths.toolkitSchemaPath);
            var engineSchema = AssetDatabase.LoadAssetAtPath<SchemaAsset>(Paths.rsSchemaPath);

            var combinedSchema = new SchemaDefinition();
            combinedSchema.ComponentDefinitions.AddRange(engineSchema.SchemaDefinition.ComponentDefinitions);
            combinedSchema.ComponentDefinitions.AddRange(toolkitSchema.SchemaDefinition.ComponentDefinitions);
            combinedSchema.ComponentDefinitions.AddRange(userSchema.ComponentDefinitions);

            combinedSchema.CommandDefinitions.AddRange(engineSchema.SchemaDefinition.CommandDefinitions);
            combinedSchema.CommandDefinitions.AddRange(toolkitSchema.SchemaDefinition.CommandDefinitions);
            combinedSchema.CommandDefinitions.AddRange(userSchema.CommandDefinitions);

            combinedSchema.InputDefinitions.AddRange(engineSchema.SchemaDefinition.InputDefinitions);
            combinedSchema.InputDefinitions.AddRange(toolkitSchema.SchemaDefinition.InputDefinitions);
            combinedSchema.InputDefinitions.AddRange(userSchema.InputDefinitions);

            combinedSchema.ArchetypeDefinitions.AddRange(engineSchema.SchemaDefinition.ArchetypeDefinitions);
            combinedSchema.ArchetypeDefinitions.AddRange(toolkitSchema.SchemaDefinition.ArchetypeDefinitions);
            combinedSchema.ArchetypeDefinitions.AddRange(userSchema.ArchetypeDefinitions);
            combinedSchema.SchemaId = BakeUtil.SchemaID;

            return combinedSchema;
        }

        private static void CodeGenDataCliArgsOverrides(CodeGenData codeGenData, bool allowSchemaOverrides)
        {
            var args = Environment.GetCommandLineArgs();

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "--no-unity-refs":
                        codeGenData.NoUnityReferences = true;
                        break;
                    case "--extended-def":
                        codeGenData.ExtendedDefinition = true;
                        break;
                    case "--output-dir" when i + 1 <= args.Length - 1:
                        codeGenData.OutputDirectory = args[i + 1];
                        i++;
                        break;
                    case "--output-dir":
                        throw new ArgumentException("No output dir found");
                    case "--schema" when i + 1 <= args.Length - 1:
                        if (!allowSchemaOverrides)
                        {
                            logger.Error(Error.EditorCodegenSelectorSchemaOverride,
                                "Trying to override the Schemas we're generating code for, but we're baking project Schemas." +
                                "If you wish to override the Schemas, use the method CodeGenSelector.? instead.");
                            break;
                        }

                        var schemaPaths = args[i + 1].Split(',');

                        codeGenData.SchemaDefinition = BakeUtil.GetSchemaDefinitionFromSchemaFiles(schemaPaths);
                        break;
                    case "--schema":
                        throw new ArgumentException("No Schema file paths found");
                }
            }
        }
    }
}
