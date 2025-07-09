// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System.Collections.Generic;
    using Cloud;
    using Coherence.Tests;
    using NUnit.Framework;

    public class CloudUniqueIdPoolTests : CoherenceTest
    {
        private static readonly string ProjectId = "proj-unittests";

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            CloudUniqueIdPool.RemoveProjectPool(ProjectId);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            CloudUniqueIdPool.RemoveProjectPool(ProjectId);
        }

        [Test(Description="Get a unique id from the pool")]
        public void Get_ReturnsUniqueId()
        {
            var uniqueId = CloudUniqueIdPool.Get(ProjectId);

            Assert.That(uniqueId, Is.Not.Null);
        }

        [Test(Description="Verifies that unique ids are recycled")]
        public void Get_AfterRelease_ReturnsSameUniqueId()
        {
            var uniqueId = CloudUniqueIdPool.Get(ProjectId);
            CloudUniqueIdPool.Release(ProjectId, uniqueId);

            var newUniqueId = CloudUniqueIdPool.Get(ProjectId);

            Assert.That(uniqueId, Is.EqualTo(newUniqueId));
        }

        [Test(Description="Verifies that unique ids are recycled, and new ones are created when needed")]
        public void Get_ReturnsNewUniqueId_WhenPoolEmpty()
        {
            List<CloudUniqueId> uniqueIds = new ();
            uniqueIds.Add(CloudUniqueIdPool.Get(ProjectId));
            uniqueIds.Add(CloudUniqueIdPool.Get(ProjectId));

            foreach (var uid in uniqueIds)
            {
                CloudUniqueIdPool.Release(ProjectId, uid);
            }

            for (var i = 0; i < uniqueIds.Count; ++i)
            {
                var recycledId = CloudUniqueIdPool.Get(ProjectId);
                Assert.That(uniqueIds, Has.Exactly(1).Matches<CloudUniqueId>(x => x == recycledId));
            }

            var newId = CloudUniqueIdPool.Get(ProjectId);
            Assert.That(uniqueIds, Has.None.Matches<CloudUniqueId>(x => x == newId));
        }
    }
}
