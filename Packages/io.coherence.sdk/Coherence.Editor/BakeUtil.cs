// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Log;
    using Portal;
    using Serializer;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEditor.Compilation;
    using UnityEngine;
    using Logger = Log.Logger;
#if COHERENCE_USE_BAKED_SCHEMA_ID
    using System.Reflection;
#endif

    [InitializeOnLoad]
    public static class BakeUtil
    {
        public const string bakeOnEnterPlayModeKey = "Coherence.BakeOnEnterPlayMode";
        public const string bakeOnBuildKey = "Coherence.BakeOnBuild";
        public const string coherenceSyncSchemasDirtyKey = "Coherence.CoherenceSyncSchemasDirty";

        public static event Action OnBakeStarted;
        public static event Action OnBakeEnded;
        public static event Action OnSchemasSetDirty;

        private static string PostBakeCompilationActionsKey => "io.coherence.postbakecompilation";

        private const string executeAfterCompilationKey = "Coherence.BakeUtil.ExecuteAfterCompilation";

        private static readonly Logger logger = Log.GetLogger(typeof(BakeUtil));

        static BakeUtil()
        {
#if COHERENCE_USE_BAKED_SCHEMA_ID
            var defType = TypeCache.GetTypesDerivedFrom<ProtocolDef.IDefinition>()
                .FirstOrDefault(t => t.FullName == "Coherence.Generated.Definition");

            var bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var fieldInfo = defType?.GetField("schemaId", bindingFlags);
            var schemaId = (string)fieldInfo?.GetValue(null);

            HasSchemaID = !string.IsNullOrEmpty(schemaId);
            SchemaID = schemaId;
#endif

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            TryExecuteAfterCompilation();
        }

        private static void TryExecuteAfterCompilation()
        {
            var typeName = SessionState.GetString(executeAfterCompilationKey, null);
            if (!string.IsNullOrEmpty(typeName))
            {
                SessionState.EraseString(executeAfterCompilationKey);

                var t = Type.GetType(typeName);
                if (t != null)
                {
                    try
                    {
                        var instance = (IExecuteAfterCompilation)Activator.CreateInstance(t);
                        instance.OnAfterComplation();
                    }
                    catch (Exception e)
                    {
                        logger.Error(Error.EditorBakeUtilAfterCompilationException, e.Message);
                    }
                }
                else
                {
                    logger.Error(Error.EditorBakeUtilMissingType, ("type", typeName));
                }
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            switch (stateChange)
            {
                case PlayModeStateChange.EnteredEditMode:
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    if (IsBakingInProcess())
                    {
                        EditorApplication.ExitPlaymode();
                        return;
                    }

                    if (BakeOnEnterPlayMode)
                    {
                        if (Outdated)
                        {
                            _ = Bake();
                        }
                    }

                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    break;
            }
        }

        public static bool BakeOnEnterPlayMode
        {
            get => UserSettings.GetBool(bakeOnEnterPlayModeKey, false);
            set => UserSettings.SetBool(bakeOnEnterPlayModeKey, value);
        }

        public static bool BakeOnBuild
        {
            get => UserSettings.GetBool(bakeOnBuildKey, false);
            set => UserSettings.SetBool(bakeOnBuildKey, value);
        }

        /// <summary>
        ///     Determines if the active schemas have changed since the last bake performed.
        ///     <note>It only takes into account schemas, it won't consider the gather manifest (necessary for a complete bake).</note>
        /// </summary>
        public static bool ActiveSchemasChanged => ProjectSettings.instance.ActiveSchemasChanged;

        /// <summary>
        ///     Determines if the baked files are outdated, based on missing files and changes on schema files.
        ///     If they are, a <see cref="BakeAsync"></see> operation is recommended.
        /// </summary>
        public static bool Outdated => !HasSchemaID ||
                                       ActiveSchemasChanged ||
                                       CoherenceSyncSchemaOutdated ||
                                       !HasBaked;

        /// <summary>
        ///     Determines if the gathered CoherenceSync schema is outdated.
        /// </summary>
        public static bool CoherenceSyncSchemaOutdated => !GatheredSchemaExists || CoherenceSyncSchemasDirty;

        /// <summary>
        /// Determines if there's valid bake data.
        /// </summary>
        public static bool HasBaked => Directory.Exists(Paths.defaultSchemaBakePath);

        /// <summary>
        ///     Gets the path (relative to the project path) where the baked files are stored.
        /// </summary>
        public static string OutputFolder => ProjectSettings.instance.GetSchemaBakeFolderPath();

        /// <summary>
        ///     Gets the current SchemaID for this project. The SchemaID is a hash computed from the contents of all the active
        ///     schemas.
        /// </summary>
        public static string SchemaID
#if COHERENCE_USE_BAKED_SCHEMA_ID
        {
            get;
            internal set;
        }
#else
            => Schemas.GetLocalSchemaID();
#endif

        /// <summary>
        ///     Gets the current short SchemaID (5 chars) for this project. The SchemaID is a hash computed from the contents of
        ///     all the active schemas.
        /// </summary>
        public static string SchemaIDShort => SchemaID.Substring(0, 5);

        public static bool HasSchemaID
#if COHERENCE_USE_BAKED_SCHEMA_ID
        {
            get;
            internal set;
        }
#else
            => true;
#endif

        /// <summary>
        ///     Maximum number of different components that can be created for a particular network entity.
        ///     In Unity, the number of different components that will be created for a network entity is given by:
        ///     (Number of different Unity Components with bindings) + (Asset Id Component) + (Unique Id Component) + (Persistent
        ///     Component) + (PreserveChildren Component)
        ///     The sum of all these different Components cannot surpass 31.
        /// </summary>
        public const int MaxUniqueComponentsBound = (1 << Serialize.NUM_BITS_FOR_COMPONENT_COUNT) - 1; //31;

        private static bool GatheredSchemaExists => File.Exists(Paths.gatherSchemaPath);

        public static bool CoherenceSyncSchemasDirty
        {
            get => UserSettings.GetBool(coherenceSyncSchemasDirtyKey, false);
            internal set
            {
                var wasDirty = UserSettings.GetBool(coherenceSyncSchemasDirtyKey);
                UserSettings.SetBool(coherenceSyncSchemasDirtyKey, value);
                if (!wasDirty && value)
                {
                    OnSchemasSetDirty?.Invoke();
                }
            }
        }

        private static bool bakingInProcess;

        /// <summary>
        ///     Bake coherence data synchronously.
        ///     1. Save Schema file for your project
        ///     2. Generate optimized code for your project
        /// </summary>
        /// <remarks>
        ///     The end of this method will force a recompilation.
        /// </remarks>
        /// <returns>
        ///     True when the Schema file and the generated code was created successfully, false otherwise.
        /// </returns>
        public static bool Bake()
        {
            if (CloneMode.Enabled)
            {
                return false;
            }

            var task = GenerateSchemaAndBakedCode(false);

            task.Wait();

            return task.Result;
        }

        /// <summary>
        ///     Bake coherence data asynchronously with no return.
        ///     1. Save Schema file for your project
        ///     2. Generate optimized code for your project
        /// </summary>
        /// <remarks>
        ///     The end of this method will force a recompilation.
        /// </remarks>
        public async static void BakeAsyncNoReturn()
        {
            _ = await BakeAsync();
        }

        /// <summary>
        ///     Bake coherence data asynchronously.
        ///     1. Save Schema file for your project
        ///     2. Generate optimized code for your project
        /// </summary>
        /// <remarks>
        ///     The end of this method will force a recompilation.
        /// </remarks>
        /// <returns>
        ///     True when the Schema file and the generated code was created successfully, false otherwise.
        /// </returns>
        public async static Task<bool> BakeAsync()
        {
            return await GenerateSchemaAndBakedCode(true);
        }

        internal static bool CustomBake(bool noUnityReferences, string bakePath)
        {
            var task = GenerateSchemaAndBakedCode(false, noUnityReferences, bakePath);

            task.Wait();

            return task.Result;
        }

        internal static string GetTooManyNetworkComponentsErrorMessage(int networkComponents)
        {
            return
                $"This Prefab will create {networkComponents} Network Components at runtime, this is limited to {MaxUniqueComponentsBound} due to serialization limitations.\n" +
                "Amount of Network Components is given by the amount of Unity Components with synced variables. Additionally, a Network Component will be created if the object is Persistent, Unique or if it uses the Preserve Children option.";
        }

        internal static bool GenerateSchema(out SchemaDefinition schemaDef, out EntitiesBakeData entitiesData)
        {
            schemaDef = null;
            entitiesData = default;
            var localSchemaIdWas = Schemas.GetLocalSchemaID();
            var schemasStateWas = Schemas.state;
            var success = GatherCoherenceSyncSchema(out schemaDef, out entitiesData);

            Schemas.InvalidateSchemaCache();
            var localSchemaId = Schemas.GetLocalSchemaID();
            if (!string.Equals(localSchemaId, localSchemaIdWas))
            {
                Schemas.state =
                    // If the local schema used to be in sync with remote, and it has changed, then we know that it is now out of sync.
                    schemasStateWas is Schemas.SyncState.InSync ? Schemas.SyncState.OutOfSync
                    // Otherwise, try to figure out new sync state as best as we can based on current cached remote state
                    : Schemas.GetSyncStateForSchemaId(localSchemaId);
            }
            else
            {
                Schemas.state = schemasStateWas;
            }

            return success;
        }

        internal static SchemaDefinition GetSchemaDefinitionFromSchemaFiles(IEnumerable<string> schemaPaths)
        {
            var schemas = new List<string>();

            foreach (var schemaPath in schemaPaths)
            {
                if (!File.Exists(schemaPath))
                {
                    logger.Error(Error.EditorBakeUtilSchemaNotFound, ("path", schemaPath));
                    continue;
                }

                schemas.Add(File.ReadAllText(schemaPath));
            }

            if (schemas.Count == 0)
            {
                throw new ArgumentException("External Schemas not found. " +
                                            "Make sure you specify them correctly with the --schema argument, " +
                                            "eg --schema \"../myPath/mySchema.schema,../myPath/mySchema2.schema\"");
            }

            var combinedSchemaText = string.Join("\n", schemas);
            return SchemaReader.Read(combinedSchemaText);
        }

        private static async Task<bool> GenerateSchemaAndBakedCode(bool asyncCodeGen, bool noUnityReferences = false,
            string bakePath = null)
        {
            if (ShouldCompilationErrorsStopBaking())
            {
                return false;
            }

            if (IsBakingInProcess())
            {
                return false;
            }

            OnBakeStartActions();

            try
            {
                if (!GenerateSchema(out SchemaDefinition schemaDef, out EntitiesBakeData entitiesData))
                {
                    return false;
                }

                if (asyncCodeGen)
                {
                    return await CodeGenSelector.RunForProjectSchemasAsync(schemaDef, entitiesData, noUnityReferences,
                        bakePath);
                }
                else
                {
                    return CodeGenSelector.RunForProjectSchemas(schemaDef, entitiesData, noUnityReferences, bakePath);
                }
            }
            catch (Exception e)
            {
                logger.Error(Error.EditorBakeUtilGenerateSchemeException, e.Message);
                return false;
            }
            finally
            {
                OnBakeEndedActions();
            }
        }

        private static bool IsBakingInProcess()
        {
            if (!bakingInProcess)
            {
                return false;
            }

            logger.Warning(Warning.EditorBakeUtilInProgress);
            return true;
        }

        private static void OnBakeStartActions()
        {
            try
            {
                bakingInProcess = true;
                OnBakeStarted?.Invoke();
            }
            catch (Exception e)
            {
                logger.Error(Error.EditorBakeUtilStartException, e.Message);
            }
        }

        private static void OnBakeEndedActions()
        {
            try
            {
                Schemas.UpdateSyncState();
                bakingInProcess = false;
                OnBakeEnded?.Invoke();
                SessionState.SetBool(PostBakeCompilationActionsKey, true);
            }
            catch (Exception e)
            {
                logger.Error(Error.EditorBakeUtilEndedException, e.Message);
            }
        }

        private static bool ShouldCompilationErrorsStopBaking()
        {
            if (!EditorUtility.scriptCompilationFailed)
            {
                return false;
            }

            Watchdog.GetCompilationErrorsInBakedCodeViaReflectionWithFallback(out var errorsInBakedCodeAssets);

            logger.Error(Error.EditorBakeUtilCompilerErrors);

            if (!errorsInBakedCodeAssets || Application.isBatchMode || !EditorUtility.DisplayDialog(
                    "coherence watchdog",
                    "Detected compilation errors on baked scripts. To preserve the integrity of the baked scripts and generated Schema, baking should always be done without compiler errors.",
                    "Delete Baked Scripts and Diagnose"))
            {
                return true;
            }

            if (CodeGenSelector.Clear())
            {
                CompilationPipeline.RequestScriptCompilation();
            }

            return true;
        }

        private static bool GatherCoherenceSyncSchema(out SchemaDefinition schemaDef, out EntitiesBakeData entitiesData)
        {
            return SchemaCreator.GatherSyncBehavioursAndEmit(out schemaDef, out entitiesData);
        }

        [DidReloadScripts]
        private static void OnBakeComplete()
        {
            var performActions = SessionState.GetBool(PostBakeCompilationActionsKey, false);

            if (!performActions)
            {
                return;
            }

            SessionState.SetBool(PostBakeCompilationActionsKey, false);

            if (PortalUtil.UploadAfterBake)
            {
                _ = Schemas.UploadActive();
            }
        }
    }
}
