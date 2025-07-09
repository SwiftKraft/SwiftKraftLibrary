// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Tests.Simulator
{
    using Coherence.Tests;
    using Editor.Toolkit;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    public class ProjectSimulatorSlugStoreTests : CoherenceTest
    {
        [Test]
        [Description("Value is set correctly in the store.")]
        public void Get_Returns_Value_Previously_Stored_Using_Set()
        {
            ProjectSimulatorSlugStore.Set(nameof(Get_Returns_Value_Previously_Stored_Using_Set),
                nameof(ProjectSimulatorSlugStoreTests));
            var value = ProjectSimulatorSlugStore.Get(nameof(Get_Returns_Value_Previously_Stored_Using_Set));
            Assert.That(value, Is.EqualTo(nameof(ProjectSimulatorSlugStoreTests)));
        }

        [Test]
        [Description("Disused keys are removed.")]
        public void Disused_Keys_Removed()
        {
            var keys = new[] { "a", "b", "c", "d" };
            foreach (var key in keys)
            {
                ProjectSimulatorSlugStore.Set(key, key);
            }

            foreach (var key in keys)
            {
                Assert.That(ProjectSimulatorSlugStore.Get(key), Is.EqualTo(key));
            }

            var keysToKeep = new List<string>(new []
            {
                "a",
                "c"
            });
            ProjectSimulatorSlugStore.KeepOnly(key => keysToKeep.Contains(key));

            Assert.That(ProjectSimulatorSlugStore.Get("a"), Is.EqualTo("a"));
            Assert.That(ProjectSimulatorSlugStore.Get("b"), Is.Null);
            Assert.That(ProjectSimulatorSlugStore.Get("c"), Is.EqualTo("c"));
            Assert.That(ProjectSimulatorSlugStore.Get("d"), Is.Null);
        }
    }
}
