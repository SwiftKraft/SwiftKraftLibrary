namespace Coherence.Editor.Tests
{
    using Coherence.Toolkit;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using Coherence.Tests;

    [TestFixture]
    public class CoherenceSyncConfigAssetTests : CoherenceTest
    {
        private static readonly TestCaseData[] ComponentHandlingTestCases = ComponentTestUtils.TestCaseData;

        private const string DefaultAssetName = "Test";
        private const string DefaultAssetPath = "Assets/" + DefaultAssetName + ".asset";
        private CoherenceSyncConfig config;
        private string assetPath;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            assetPath = AssetDatabase.GenerateUniqueAssetPath(DefaultAssetPath);
            var instance = ScriptableObject.CreateInstance<CoherenceSyncConfig>();
            AssetDatabase.CreateAsset(instance, assetPath);
            config = AssetDatabase.LoadAssetAtPath<CoherenceSyncConfig>(assetPath);
        }

        [TearDown]
        public override void TearDown()
        {
            AssetDatabase.DeleteAsset(assetPath);
            config = default;

            base.TearDown();
        }

        [Test]
        [TestCaseSource(nameof(ComponentHandlingTestCases))]
        public void DestroyConfig_DeletesAsset(
            AddComponentDelegate addComponent,
            DestroyImmediateDelegate destroyImmediate)
        {
            destroyImmediate(config);
            Assert.IsFalse(AssetDatabase.LoadAssetAtPath<CoherenceSyncConfig>(DefaultAssetPath));
        }
    }
}
