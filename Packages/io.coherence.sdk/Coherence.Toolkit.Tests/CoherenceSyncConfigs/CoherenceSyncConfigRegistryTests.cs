namespace Coherence.Toolkit.Tests
{
    using Bindings;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using Coherence.Tests;

    public class CoherenceSyncConfigRegistryTests : CoherenceTest
    {
        private const string DefaultPrefabName = nameof(CoherenceSyncConfigRegistryTests);
        private const string DefaultPrefabPath = "Assets/" + DefaultPrefabName + ".prefab";
        private string prefabPath;
        private GameObject prefab;
        private CoherenceSync sync;
        private CoherenceSyncConfigRegistry registry;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            prefabPath = AssetDatabase.GenerateUniqueAssetPath(DefaultPrefabPath);
            prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
                ObjectFactory.CreateGameObject(DefaultPrefabName, typeof(CoherenceSync)),
                prefabPath, InteractionMode.AutomatedAction);
            sync = prefab.GetComponent<CoherenceSync>();
            registry = CoherenceSyncConfigRegistry.Instance;
        }

        [TearDown]
        public override void TearDown()
        {
            AssetDatabase.DeleteAsset(prefabPath);

            base.TearDown();
        }

        [Test]
        [Description("Verifies that created CoherenceSyncConfig objects are registered using the ID.GetHashCode().")]
        public void TestConfigHashCodeUsed()
        {
            // Arrange / Act - create a CoherenceSyncPrefab which automatically registers.
            var config = sync.CoherenceSyncConfig;
            var networkID = config.ID.GetHashCode();

            // Assert
            Assert.True(registry.GetFromNetworkId(networkID, out var testConfig));
            Assert.That(testConfig, Is.EqualTo(config));
        }

        [Test]
        [Description("Verifies that deleted configs are removed from the registry network IDs.")]
        public void TestDestroyedConfigsRemovedFromNetworkID()
        {
            // Arrange / Act - create a CoherenceSyncPrefab which automatically registers.
            var config = sync.CoherenceSyncConfig;
            var networkID = config.ID.GetHashCode();

            // Act
            AssetDatabase.DeleteAsset(prefabPath);

            // Assert
            Assert.False(registry.GetFromNetworkId(networkID, out var testConfig));
        }
    }
}
