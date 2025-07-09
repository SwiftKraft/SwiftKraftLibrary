namespace Coherence.Editor.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using Coherence.Tests;
    using Coherence.Toolkit;
    using NUnit.Framework;
    using Portal;
    using UnityEditor;
    using UnityEngine;

    public class BakeUtilTest : CoherenceTest
    {
        public override void TearDown()
        {
            ClearBakedData();
            AssetDatabase.Refresh();
        }

        [Test]
#if COHERENCE_SKIP_LONG_UNIT_TESTS
        [Ignore("Long running test")]
#endif
        public void Should_GenerateSchemaFileInAssets_When_GatheringSchema()
        {
            using (new TempPrefab(out var _))
            {
                Assert.That(BakeUtil.GenerateSchema(out var _, out var _), "Gather passed");
                Assert.That(File.Exists(Paths.gatherSchemaPath));
            }
        }

        [Test]
#if COHERENCE_SKIP_LONG_UNIT_TESTS
        [Ignore("Long running test")]
#endif
        public void Should_GenerateADifferentSchemaFileInAssets_When_GatheringSchemaWithDifferentNetworkSetup()
        {
            using (new TempPrefab(out var prefab))
            {
                var schemaId = BakeUtil.SchemaID;
                prefab.AddComponent<MeshRenderer>();
                CoherenceSyncUtils.AddBinding<MeshRenderer>(prefab, "enabled");
                BakeUtil.GenerateSchema(out var _, out var _);
                Assert.AreNotEqual(schemaId, BakeUtil.SchemaID);
            }
        }

        [TestCase(false), TestCase(true)]
#if COHERENCE_SKIP_LONG_UNIT_TESTS
        [Ignore("Long running test")]
#endif
        public void GenerateSchema_Should_Update_Schemas_State_Accordingly(bool changeSchema)
        {
            var schemasStateWas = Schemas.state;
            var localSchemaIdWas = Schemas.GetLocalSchemaID();
            using (new TempPrefab(out var prefab))
            {
                if (changeSchema)
                {
                    var meshRenderer = prefab.AddComponent<MeshRenderer>();
                    var descriptors = EditorCache.GetComponentDescriptors(meshRenderer);
                    const string memberName = "enabled";
                    var descriptor = descriptors.FirstOrDefault(d => d.Name == memberName);
                    var hadBinding = descriptor is not null;

                    if (hadBinding)
                    {
                        CoherenceSyncUtils.RemoveBinding<MeshRenderer>(prefab, memberName);
                    }
                    else
                    {
                        CoherenceSyncUtils.AddBinding<MeshRenderer>(prefab, memberName);
                    }
                }

                BakeUtil.GenerateSchema(out var _, out var _);

                var localSchemaId = Schemas.GetLocalSchemaID();
                if (changeSchema)
                {
                    var expectedSchemaState = schemasStateWas is Schemas.SyncState.InSync
                        ? Schemas.SyncState.OutOfSync
                        : Schemas.GetSyncStateForSchemaId(localSchemaId);

                    Assert.That(Schemas.state, Is.EqualTo(expectedSchemaState));
                    Assert.That(localSchemaIdWas, Is.Not.EqualTo(localSchemaId));
                }
                else
                {
                    var expectedSchemaState = !string.Equals(localSchemaId, localSchemaIdWas) && schemasStateWas is Schemas.SyncState.InSync
                        ? Schemas.SyncState.OutOfSync
                        : Schemas.GetSyncStateForSchemaId(localSchemaId);
                    Assert.That(Schemas.state, Is.EqualTo(expectedSchemaState));
                }

                Schemas.InvalidateSchemaCache();
                Schemas.state = schemasStateWas;
            }
        }

        private sealed class TempPrefab : IDisposable
        {
            private const string DefaultPrefabName = nameof(BakeUtilTest);
            private const string DefaultPrefabPath = "Assets/" + DefaultPrefabName + ".prefab";

            private AssetPath prefabPath;

            public TempPrefab(out GameObject prefab)
            {
                if (File.Exists(DefaultPrefabPath))
                {
                    AssetUtils.DeleteFile(DefaultPrefabPath);
                }

                ClearBakedData();

                var instance = ObjectFactory.CreateGameObject(DefaultPrefabName, typeof(CoherenceSync));
                prefabPath = AssetUtils.GenerateUniqueAssetPath(DefaultPrefabPath);

                prefab = AssetUtils.CreatePrefab
                (
                    instance,
                    ref prefabPath,
                    InteractionMode.AutomatedAction
                );

                _ = BakeUtil.GenerateSchema(out var _, out var _);
            }

            public void Dispose()
            {
                AssetUtils.DeleteFile(prefabPath);
            }
        }

        private static void ClearBakedData()
        {
            Schemas.InvalidateSchemaCache();

            try
            {
                if (File.Exists(Paths.gatherSchemaPath))
                {
                    File.Delete(Paths.gatherSchemaPath);
                    File.Delete(Paths.gatherSchemaPath + ".meta");
                }

                if (Directory.Exists(Paths.defaultSchemaBakePath))
                {
                    Directory.Delete(Paths.defaultSchemaBakePath, true);
                    File.Delete(Paths.defaultSchemaBakePath + ".meta");
                }

                if (Directory.Exists(BakeUtil.OutputFolder))
                {
                    Directory.Delete(BakeUtil.OutputFolder, true);
                    File.Delete(BakeUtil.OutputFolder + ".meta");
                }

                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
