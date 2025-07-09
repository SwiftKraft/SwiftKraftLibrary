// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using Common;
    using System;
    using UnityEditor;
    using Log;
    using Logger = Log.Logger;
    using System.Collections.Generic;
    using System.Linq;
    using Object = UnityEngine.Object;

    public static class Migration
    {
        private static readonly Logger logger = Log.GetLogger(typeof(Migration));
        private static List<IDataMigrator> activeMigrators = new();

        public static (bool, HashSet<string>) Migrate(Object obj = null)
        {
            if (CloneMode.Enabled)
            {
                return (false, new HashSet<string>());
            }

            var currentVersion = GetActiveMigrators();

            if (!currentVersion.HasValue)
            {
                return (false, new HashSet<string>());
            }

            var targetAssetVersion = $"{currentVersion.Value.Major}{currentVersion.Value.Minor}";

            return obj == null
                ? MigrateAllTargetObjects(targetAssetVersion)
                : MigrateSpecificObject(obj, targetAssetVersion);
        }

        private static (bool, HashSet<string>) MigrateSpecificObject(Object obj, string targetAssetVersion)
        {
            if (!AssetNeedsUpdate(targetAssetVersion, obj))
            {
                return (false, new HashSet<string>());
            }

            var migratedAny = false;
            var migrationErrors = false;

            foreach (var migrator in activeMigrators)
            {
                try
                {
                    if (migrator.RequiresMigration(obj))
                    {
                        migratedAny |= migrator.MigrateObject(obj);
                    }
                }
                catch (Exception e)
                {
                    logger.Error(Error.EditorMigratorSpecificObjectException,
                        $"{e.Message}\n\nStackTrace: {e.StackTrace}");
                    migrationErrors = true;
                }
            }

            if (migrationErrors)
            {
                PrintMigrationErrorsMessage();
            }

            if (migratedAny)
            {
                UpdateAssetVersion(targetAssetVersion, obj);
            }

            return (migratedAny, new HashSet<string>());
        }

        private static void PrintMigrationErrorsMessage()
        {
            logger.Error(Error.EditorMigratorGenericErrors);
        }

        private static (bool, HashSet<string>) MigrateAllTargetObjects(string targetAssetVersion)
        {
            var migratedAny = false;
            var migrationErrors = false;
            var successMessages = new HashSet<string>();

            var migratorsForObject = GetTargetObjectsAndInitMigrators(targetAssetVersion);

            try
            {
                foreach (var (asset, dataMigrators) in migratorsForObject)
                {
                    var migrated = false;

                    foreach (var migrator in dataMigrators)
                    {
                        var result = migrator.MigrateObject(asset);
                        migrated |= result;

                        if (!result)
                        {
                            continue;
                        }

                        successMessages.Add(migrator.MigrationMessage);
                    }

                    if (migrated)
                    {
                        UpdateAssetVersion(targetAssetVersion, asset);
                    }

                    migratedAny |= migrated;
                }
            }
            catch (Exception e)
            {
                logger.Error(Error.EditorMigratorAllTargetsException,
                    $"{e.Message}\n\nStackTrace: {e.StackTrace}");
                migrationErrors = true;
            }

            if (migratedAny && !migrationErrors)
            {
                AssetDatabase.StartAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.StopAssetEditing();
            }
            else if (migrationErrors)
            {
                PrintMigrationErrorsMessage();
            }
            else
            {
                logger.Info("coherence Assets are up to date.");
            }

            return (migratedAny, successMessages);
        }

        private static Dictionary<Object, List<IDataMigrator>> GetTargetObjectsAndInitMigrators(string targetAssetVersion)
        {
            var migratorsForObject = new Dictionary<Object, List<IDataMigrator>>();

            foreach (var migrator in activeMigrators)
            {
                migrator.Initialize();

                foreach (var unityObj in migrator.GetMigrationTargets())
                {
                    if (!AssetNeedsUpdate(targetAssetVersion, unityObj) || !migrator.RequiresMigration(unityObj))
                    {
                        continue;
                    }

                    if (!migratorsForObject.TryGetValue(unityObj, out var migrators))
                    {
                        migrators = new List<IDataMigrator>();
                        migratorsForObject[unityObj] = migrators;
                    }

                    migrators.Add(migrator);
                }
            }

            return migratorsForObject;
        }

        private static void UpdateAssetVersion(string targetAssetVersion, Object obj)
        {
            using var so = new SerializedObject(obj);
            using var assetVersionProperty = so.FindProperty("assetVersion");

            if (assetVersionProperty == null)
            {
                return;
            }

            assetVersionProperty.stringValue = targetAssetVersion;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool AssetNeedsUpdate(string targetAssetVersion, Object unityObj)
        {
            var assetVersionProperty = new SerializedObject(unityObj).FindProperty("assetVersion");
            var assetVersion = assetVersionProperty?.stringValue;

            var checkMigration = assetVersionProperty == null || assetVersion != targetAssetVersion;
            return checkMigration;
        }

        private static SemVersion? GetActiveMigrators()
        {
            var versionInfo = AssetDatabase.LoadAssetAtPath<VersionInfo>(Paths.versionInfoPath);

            if (versionInfo == null)
            {
                return null;
            }

            var currentVersion = SemVersion.Parse(versionInfo.Sdk);

            var migratorInterface = typeof(IDataMigrator);

            var migrators = migratorInterface.Assembly.GetTypes()
                .Where(t => migratorInterface.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            activeMigrators = new List<IDataMigrator>();

            foreach (var migrator in migrators)
            {
                var migratorInstance = (IDataMigrator)Activator.CreateInstance(migrator);

                if (currentVersion.CompareTo(migratorInstance.MaxSupportedVersion) == -1)
                {
                    activeMigrators.Add(migratorInstance);
                }
                else
                {
                    logger.Error(Error.EditorMigratorActiveMigratorsException,
                        $"Data Migrator of type {migrator.Name} has Max Supported Version {migratorInstance.MaxSupportedVersion.ToString()} " +
                        "and won't execute, consider removing it or reevaluating its Max Supported Version.");
                }
            }

            activeMigrators.Sort((x, y) => x.Order.CompareTo(y.Order));

            return currentVersion;
        }
    }

}

