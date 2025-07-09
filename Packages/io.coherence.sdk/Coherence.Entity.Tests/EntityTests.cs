// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Entities.Tests
{
    using NUnit.Framework;
    using System;
    using Entities;
    using Coherence.Tests;

    public class EntityTests : CoherenceTest
    {
        [TestCase]
        public void VersionConsts()
        {
            var expectedVersionBits = (ushort)Math.Log((double)Entity.MaxVersions, 2d);
            Assert.That(Entity.NumVersionBits, Is.EqualTo(expectedVersionBits));
        }
    }
}
