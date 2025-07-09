namespace Coherence.Toolkit.Tests
{
    using NUnit.Framework;
    using UnityEngine;
    using UnityEditor;
    using Coherence.Tests;
    using Coherence.Toolkit;
    using System.Linq;
    using System.Threading.Tasks;

#if HAS_ADDRESSABLES
    public class AddressablesProviderTests : CoherenceTest
    {
        private const string PrefabName = "BasicAddressables";
        private CoherenceSyncConfig config;
        private AddressablesProvider provider;
        private const int timeoutMS = 1000;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            logger = logger.With<AddressablesProviderTests>();

            var registry = CoherenceSyncConfigRegistry.Instance;
            foreach (var conf in registry)
            {
                if (conf.name == PrefabName)
                {
                    config = conf;
                    break;
                }
            }

            provider = new AddressablesProvider();
            provider.Initialize(config);

            // make sure the assets aren't loaded.
            Assert.False(provider.IsAssetLoaded);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            Resources.UnloadUnusedAssets();
        }

        [Test, Timeout(timeoutMS)]
        [Description("Validates that an addressable can be loaded")]
        public void Test_LoadOneInstanceSync()
        {
            var sync = provider.LoadAsset("Sync");

            Assert.NotNull(sync);

            provider.Release(sync);
        }

        [Test, Timeout(timeoutMS)]
        [Description("Validates that an addressable can be loaded async.")]
        public async Task Test_LoadOneInstanceAsync()
        {
            ICoherenceSync sync1 = null;

            provider.LoadAsset("Sync1", (sync) =>
            {
                sync1 = sync;
                Assert.NotNull(sync1, "Sync1");
            });

            await WaitWhile(() => sync1 != null);

            provider.Release(sync1);
        }

        [Test, Timeout(timeoutMS)]
        [Description("Validates that two instances of an addressable can be loaded")]
        public void Test_LoadTwoInstancesSync()
        {
            var sync1 = provider.LoadAsset("Sync1");
            Assert.NotNull(sync1);

            var sync2 = provider.LoadAsset("Sync2");
            Assert.NotNull(sync2);

            provider.Release(sync1);
            provider.Release(sync2);
        }

        [Test, Timeout(timeoutMS)]
        [Description("Validates that more than one addressable of the same kind can be loaded in the same frame.\n" +
            "issue #6489")]
        public async Task Test_LoadTwoInstancesAsync()
        {
            ICoherenceSync sync1 = null;
            ICoherenceSync sync2 = null;

            provider.LoadAsset("Sync1", (sync) =>
            {
                sync1 = sync;
                Assert.NotNull(sync1, "Sync1");
            });

            provider.LoadAsset("Sync2", (sync) =>
            {
                sync2 = sync;
                Assert.NotNull(sync2, "Sync2");
            });

            await WaitWhile(() => sync1 != null && sync2 != null);

            provider.Release(sync1);
            provider.Release(sync2);
        }
    }
#endif // HAS_ADDRESSABLES
}
