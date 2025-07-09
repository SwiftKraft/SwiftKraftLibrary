// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Log;
    using Portal;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEngine;
    using PackageInfo = UnityEditor.PackageManager.PackageInfo;
    using Logger = Log.Logger;
    using Object = UnityEngine.Object;

    [InitializeOnLoad]
    internal class Postprocessor : AssetPostprocessor
    {
        private class SdkInstallEventProperties : Analytics.BaseProperties
        {
            public string version;
        }

        private class SdkUpdateEventProperties : Analytics.BaseProperties
        {
            public string old_version;
            public string new_version;
        }

        private class SdkStartedEventProperties : Analytics.BaseProperties
        {
            public string source;
        }

        private static readonly Logger logger = Log.GetLogger<Postprocessor>();

        private const string oldLastSdkKey = "Coherence.LastInstallation";
        private const string lastSdkKey = "Coherence.Package.LastInstallation";
        private const string lastCodeGenKey = "Coherence.CodeGen.LastInstallation";
        private const string editorStartedKey = "Coherence.StartedEditor";

        private static readonly List<(Type singletonType, string assetPath)> preloadedSingletonDefinitions = new();

        private static Object[] cachedPreloadedAssets;

        static Postprocessor()
        {
            // preloaded assets are loaded by Unity on players (runtime), but not on editor
            // make sure all preloaded assets are preloaded at editor time
            cachedPreloadedAssets = PlayerSettings.GetPreloadedAssets();

            InitializePreloadedSingletonDefinitions();
            SendEditorStartedEvent();
            BakeUtil.OnBakeEnded += OnBakeEnded;
        }

        private static void InitializePreloadedSingletonDefinitions()
        {
            var types = TypeCache.GetTypesWithAttribute<PreloadedSingletonAttribute>();
            foreach (var type in types)
            {
                if (!typeof(PreloadedSingleton).IsAssignableFrom(type))
                {
                    Debug.LogWarning($"Type {type.FullName} that is tagged with {nameof(PreloadedSingletonAttribute)} doesn't inherit from {nameof(PreloadedSingleton)}. Skipping.");
                    continue;
                }

                var path = GetPathForPreloadedSingletonType(type);
                preloadedSingletonDefinitions.Add((type, path));
            }
        }

        private static string GetPathForPreloadedSingletonType(Type type)
        {
            return $"{Paths.projectAssetsPath}/{type.Name}.asset";
        }

        [DidReloadScripts(-1000)]
        private static void OnScriptsReloaded()
        {
            if (CloneMode.Enabled)
            {
                return;
            }

            if (EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += OnScriptsReloadedAndAssetDatabaseReady;
                return;
            }

            OnScriptsReloadedAndAssetDatabaseReady();
        }

        private static void OnScriptsReloadedAndAssetDatabaseReady()
        {
            EnsurePreloadedAssets();
            TrySdkVersionUpdate();
        }

        private static void OnBakeEnded()
        {
            if (EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += UpdateRuntimeSettings;
                return;
            }

            UpdateRuntimeSettings();
        }

        private static void SendEditorStartedEvent()
        {
            if (SessionState.GetBool(editorStartedKey, false))
            {
                return;
            }

            SessionState.SetBool(editorStartedKey, true);
            EditorApplication.delayCall += () => Analytics.Capture(
                new Analytics.Event<SdkStartedEventProperties>(Analytics.Events.EditorStarted,
                new SdkStartedEventProperties()
                {
                    source =
                        #if VSP
                            "asset_store",
                        #else
                            "registry",
                        #endif
                }));
        }

        private static bool IsSchema(string assetPath)
        {
            return Path.GetExtension(assetPath) == $".{Constants.schemaExtension}";
        }

        private static bool IsPreloadedAsset(string assetPath)
        {
            foreach (var def in preloadedSingletonDefinitions)
            {
                if (def.assetPath == assetPath)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNotNull(Object obj)
        {
            return obj != null;
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (CloneMode.Enabled)
            {
                return;
            }

            if (Array.Exists(deletedAssets, IsSchema))
            {
                Schemas.state = Schemas.SyncState.Unknown;
                Schemas.InvalidateSchemaCache();
                ProjectSettings.instance.PruneSchemas();
            }

            if (Array.Exists(importedAssets, IsSchema))
            {
                Schemas.state = Schemas.SyncState.Unknown;
                Schemas.InvalidateSchemaCache();
            }

            if (Array.Exists(deletedAssets, IsPreloadedAsset) || !AllPreloadedSingletonsCreated())
            {
                EnsurePreloadedAssets();
            }
        }

        private static bool AllPreloadedSingletonsCreated()
        {
            foreach (var (singletonType, _) in preloadedSingletonDefinitions)
            {
                if (!IsPreloadedSingletonCreated(singletonType))
                {
                    return false;
                }
            }

            return true;
        }

        /// <remarks>
        /// Requires <see cref="cachedPreloadedAssets"/> to be up-to-date.
        /// </remarks>
        private static bool IsPreloadedSingletonCreated(Type singletonType)
        {
            foreach (var asset in cachedPreloadedAssets)
            {
                if (!asset)
                {
                    continue;
                }

                if (asset.GetType() == singletonType)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void UpdateRuntimeSettings()
        {
            var instance = RuntimeSettings.Instance;
            Debug.Assert(instance);

            instance.schemas = GetSchemaAssets();
            instance.SchemaID = BakeUtil.SchemaID;

            EditorUtility.SetDirty(instance);
            AssetDatabase.SaveAssetIfDirty(instance);
        }

        internal static void EnsurePreloadedAssets()
        {
            cachedPreloadedAssets = PlayerSettings.GetPreloadedAssets();
            var createdAny = false;
            foreach (var def in preloadedSingletonDefinitions)
            {
                if (!TryCreatePreloadedAsset(ref cachedPreloadedAssets, def.singletonType, def.assetPath, out var scriptableObject))
                {
                    continue;
                }

                createdAny = true;

                var editor = Editor.CreateEditor(scriptableObject);
                if (editor is IOnAfterPreloaded onAfterPreloadedEditor)
                {
                    onAfterPreloadedEditor.OnAfterPreloaded();
                }

                Object.DestroyImmediate(editor);

                if (scriptableObject is IOnAfterPreloaded onAfterPreloaded)
                {
                    onAfterPreloaded.OnAfterPreloaded();
                }
            }

            if (createdAny)
            {
                cachedPreloadedAssets = Array.FindAll(cachedPreloadedAssets, IsNotNull);
                PlayerSettings.SetPreloadedAssets(cachedPreloadedAssets);
            }
        }

        private static SchemaAsset[] GetSchemaAssets() =>
            Paths.AllSchemas.Select(path =>
                {
                    if (!File.Exists(path))
                    {
                        return null;
                    }

                    var result = AssetDatabase.LoadAssetAtPath<SchemaAsset>(path);
                    if (result)
                    {
                        return result;
                    }

                    AssetUtils.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    result = AssetDatabase.LoadAssetAtPath<SchemaAsset>(path);
                    if (!result)
                    {
                        logger.Warning(Warning.EditorPostprocessorGetSchemaAssetsFailure,
                            $"Failed to load schema at {path}.");
                        return null;
                    }

                    return result;
                })
                .Where(schema => schema is not null)
                .ToArray();

        /// <returns>Whether a new asset is created and added to the preloaded asset.</returns>
        private static bool TryCreatePreloadedAsset(ref Object[] assets, Type type, string assetPath, out ScriptableObject asset)
        {
            Debug.Assert(typeof(PreloadedSingleton).IsAssignableFrom(type),
                "Trying to ensure preloaded singleton on a type that doesn't inherit from " +
                nameof(PreloadedSingleton));

            if (TryGet(assets, type, out var obj))
            {
                asset = (ScriptableObject)obj;
                return false;
            }

            // Try to find the singleton in memory - this can be the case when upgrading from older versions.
            asset = Object.FindFirstObjectByType(type) as PreloadedSingleton;

            // Even if the asset is not on memory, it could exist on the project.
            if (!asset)
            {
                asset = AssetDatabase.LoadAssetAtPath<PreloadedSingleton>(assetPath);
            }

            // If that's not the case, let's create it.
            if (!asset)
            {
                asset = ScriptableObject.CreateInstance(type);
            }

            Debug.Assert(asset, "Couldn't find or create singleton of type " + type.Name);

            if (asset && !AssetDatabase.Contains(asset))
            {
                try
                {
                    _ = Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? Paths.projectAssetsPath);
                    AssetDatabase.CreateAsset(asset, assetPath);
                }
                catch (Exception e)
                {
                    logger.Error(Error.EditorPostProcessorCreatePreloadedException,
                        $"Failed to create {assetPath}.\n{e}");
                    return default;
                }
            }

            Array.Resize(ref assets, assets.Length + 1);
            assets[^1] = asset;

            logger.Debug($"Created '{assetPath}', and referenced it as a Preloaded Asset.", ("context", asset));
            return true;
        }

        private static bool TryGet(IEnumerable<Object> list, Type type, out Object result)
        {
            foreach (var item in list)
            {
                if (!type.IsInstanceOfType(item))
                {
                    continue;
                }

                result = item;
                return true;
            }

            result = default;
            return false;
        }

        private static void TrySdkVersionUpdate()
        {
            var packageInfo = PackageInfo.FindForAssetPath(Paths.packageManifestPath);
            var versionInfo = AssetDatabase.LoadAssetAtPath<VersionInfo>(Paths.versionInfoPath);

            if (!versionInfo)
            {
                return;
            }

            if (packageInfo == null)
            {
                return;
            }

            if ((versionInfo.hideFlags & HideFlags.NotEditable) == 0)
            {
                using var so = new SerializedObject(versionInfo);
                so.FindProperty("sdk").stringValue = packageInfo.version;
                if (so.ApplyModifiedProperties())
                {
                    AssetDatabase.SaveAssetIfDirty(versionInfo);
                }
            }

            var codegen = EditorUserSettings.GetConfigValue(lastCodeGenKey);
            if (codegen != versionInfo.Engine)
            {
                logger.Debug($"New engine version {versionInfo.Engine} detected.");
                EditorUserSettings.SetConfigValue(lastCodeGenKey, versionInfo.Engine);
            }

            // read stored sdk version as of 0.9 and earlier
            var oldSdkVersion = EditorUserSettings.GetConfigValue(oldLastSdkKey);

            // if there's a value stored
            if (!string.IsNullOrEmpty(oldSdkVersion))
            {
                // remove it
                EditorUserSettings.SetConfigValue(oldLastSdkKey, null);

                // and assume we're on 0.9
                oldSdkVersion = "0.9";
            }
            else
            {
                oldSdkVersion = EditorUserSettings.GetConfigValue(lastSdkKey);
            }

            if (string.IsNullOrEmpty(oldSdkVersion))
            {
                WelcomeWindow.Open();
                Analytics.Capture(new Analytics.Event<SdkInstallEventProperties>(
                    Analytics.Events.SdkInstalled,
                    new SdkInstallEventProperties
                    {
                        version = versionInfo.Sdk,
                    }
                ));

                EditorUserSettings.SetConfigValue(lastSdkKey, versionInfo.Sdk);
            }
            else if (oldSdkVersion != versionInfo.Sdk)
            {
                logger.Info($"coherence was updated from {oldSdkVersion} to {versionInfo.Sdk}");

                Analytics.Capture(new Analytics.Event<SdkUpdateEventProperties>(
                    Analytics.Events.SdkUpdated,
                    new SdkUpdateEventProperties
                    {
                        old_version = oldSdkVersion,
                        new_version = versionInfo.Sdk,
                    }
                ));

                EditorUserSettings.SetConfigValue(lastSdkKey, versionInfo.Sdk);
                var (_, migrationMessages) = Migration.Migrate();

                var hub = CoherenceHub.Open();
                var module = CoherenceHub.FocusModule<LearnModule>();
                module.SetUpdateInfo(oldSdkVersion, versionInfo.Sdk, migrationMessages);
                hub.CreateGUI();
            }
        }
    }
}
