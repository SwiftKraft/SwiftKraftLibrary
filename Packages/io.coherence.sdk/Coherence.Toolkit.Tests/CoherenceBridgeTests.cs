namespace Coherence.Toolkit.Tests
{
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using Coherence.Runtime;
    using Coherence.Tests;
    using Coherence.Toolkit;

    public class CoherenceBridgeTests : CoherenceTest
    {
        private CoherenceBridge bridge;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            bridge = new GameObject().AddComponent<CoherenceBridge>();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            bridge.CloudService.Dispose();

            Object.DestroyImmediate(bridge.gameObject);
        }

        [Test]
        [Description("Ensures that a bridge CloudService is not null. See: https://github.com/coherence/unity/issues/7796")]
        public void CoherenceBridge_CloudService_NotNull()
        {
            Assert.NotNull(bridge.CloudService);
        }
    }
}
