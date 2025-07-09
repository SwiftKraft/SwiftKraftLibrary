namespace Coherence.Editor.Tests
{
    using Coherence.Toolkit;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using Coherence.Tests;

    [TestFixture]
    public class CoherenceSyncPrefabTests : CoherenceTest
    {
        private const string DefaultPrefabName = nameof(CoherenceSyncPrefabTests);
        private const string DefaultPrefabPath = "Assets/" + DefaultPrefabName + ".prefab";
        private GameObject prefab;
        private GameObject instance;
        private string prefabPath;

        private static readonly TestCaseData[] ComponentHandlingTestCases = ComponentTestUtils.TestCaseData;

        private void SavePrefab()
        {
            PrefabUtility.SavePrefabAsset(prefab, out var savedSuccessfully);
            Assert.IsTrue(savedSuccessfully);
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            prefabPath = AssetDatabase.GenerateUniqueAssetPath(DefaultPrefabPath);
            prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(ObjectFactory.CreateGameObject(DefaultPrefabName),
                prefabPath, InteractionMode.AutomatedAction);
        }

        [TearDown]
        public override void TearDown()
        {
            if (instance)
            {
                Object.DestroyImmediate(instance, true);
                instance = default;
            }

            AssetDatabase.DeleteAsset(prefabPath);
            prefab = default;
            prefabPath = default;

            // Make sure Undo is cleared so no instances are leaked outside of the test scenes
            Undo.RevertAllInCurrentGroup();

            base.TearDown();
        }

        [Test]
        [TestCaseSource(nameof(ComponentHandlingTestCases))]
        public void AddCoherenceSyncInPrefab_CreatesConfig(
            AddComponentDelegate addComponent,
            DestroyImmediateDelegate destroyImmediate)
        {
            addComponent(prefab);
            SavePrefab();

            Assert.IsTrue(prefab.TryGetComponent(out CoherenceSync sync));
            Assert.IsTrue(CoherenceSyncConfigUtils.TryGetFromAsset(sync, out _));
            Assert.IsTrue(sync.CoherenceSyncConfig.IsLinked);
        }

        [Test]
        [TestCaseSource(nameof(ComponentHandlingTestCases))]
        public void DestroyCoherenceSyncInPrefab_DestroysConfig(
            AddComponentDelegate addComponent,
            DestroyImmediateDelegate destroyImmediate)
        {
            addComponent(prefab);
            SavePrefab();

            Assert.IsTrue(prefab.TryGetComponent(out CoherenceSync sync));
            SavePrefab();

            var config = sync.CoherenceSyncConfig;
            Assert.IsTrue(EditorUtility.IsPersistent(config));

            destroyImmediate(sync);
            SavePrefab();

            Assert.IsFalse(EditorUtility.IsPersistent(config));
            Assert.IsFalse(sync);
            Assert.IsFalse(CoherenceSyncConfigUtils.TryGetFromAsset(prefab, out _));
        }

        [Test]
        [TestCaseSource(nameof(ComponentHandlingTestCases))]
        public void AddCoherenceSyncOnPrefabInstance_DoesNotCreateConfig(
            AddComponentDelegate addComponent,
            DestroyImmediateDelegate destroyImmediate)
        {
            instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Assert.IsTrue(instance);
            addComponent(instance);

            Assert.IsTrue(instance.TryGetComponent(out CoherenceSync sync));
            Assert.IsFalse(sync.CoherenceSyncConfig);
            Assert.IsFalse(CoherenceSyncConfigUtils.TryGetFromAsset(sync, out _));
        }

        [Test]
        [TestCaseSource(nameof(ComponentHandlingTestCases))]
        public void ApplyPrefabInstanceWithCoherenceSync_CreatesConfig(
            AddComponentDelegate addComponent,
            DestroyImmediateDelegate destroyImmediate)
        {
            instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Assert.IsTrue(instance);
            addComponent(instance);

            Assert.IsTrue(instance.TryGetComponent(out CoherenceSync syncOnInstance));
            Assert.IsFalse(CoherenceSyncConfigUtils.TryGetFromAsset(syncOnInstance, out _));
            Assert.IsFalse(syncOnInstance.CoherenceSyncConfig);

            PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
            Assert.IsTrue(prefab.TryGetComponent(out CoherenceSync syncOnAsset));
            Assert.IsTrue(CoherenceSyncConfigUtils.TryGetFromAsset(syncOnAsset, out _));
            Assert.IsTrue(syncOnAsset.CoherenceSyncConfig);
            Assert.IsTrue(syncOnAsset.CoherenceSyncConfig.IsLinked);
        }

        [Test]
        [TestCaseSource(nameof(ComponentHandlingTestCases))]
        public void DeleteConfig_DestroysCoherenceSync(
            AddComponentDelegate addComponent,
            DestroyImmediateDelegate destroyImmediate)
        {
            addComponent(prefab);
            SavePrefab();

            Assert.IsTrue(prefab.TryGetComponent(out CoherenceSync sync));
            Assert.IsTrue(sync.CoherenceSyncConfig);
            Assert.IsTrue(sync.CoherenceSyncConfig.IsLinked);
            var configPath = AssetDatabase.GetAssetPath(sync.CoherenceSyncConfig);
            AssetDatabase.DeleteAsset(configPath);

            Assert.IsFalse(sync);
            Assert.IsFalse(CoherenceSyncConfigUtils.TryGetFromAsset(prefab, out _));
        }

        [Test]
        [TestCaseSource(nameof(ComponentHandlingTestCases))]
        public void DestroyConfig_DoesNotChangeCoherenceSync(
            AddComponentDelegate addComponent,
            DestroyImmediateDelegate destroyImmediate)
        {
            addComponent(prefab);
            SavePrefab();

            Assert.IsTrue(prefab.TryGetComponent(out CoherenceSync sync));

            var config = sync.CoherenceSyncConfig;
            Assert.IsTrue(config);
            Assert.IsTrue(config.IsLinked);

            var assetPath = AssetDatabase.GetAssetPath(config);
            destroyImmediate(config);
            AssetDatabase.DeleteAsset(assetPath);

            // we don't handle immediate destruction of CoherenceSyncConfig,
            // so we expect CoherenceSync to not get destroyed
            Assert.IsTrue(sync);
            Assert.IsFalse(CoherenceSyncConfigUtils.TryGetFromAsset(prefab, out _));
        }
    }
}
