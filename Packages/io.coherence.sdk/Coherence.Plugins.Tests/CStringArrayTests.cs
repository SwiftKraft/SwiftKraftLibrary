// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Plugins.Tests
{
    using System;
    using Coherence.Tests;
    using NUnit.Framework;
    using Utils;

    public class CStringArrayTests : CoherenceTest
    {
        [Test(Description = "Converts string array to pointers and disposes allocated memory")]
        public void ConvertsStringArrayToPointers()
        {
            var source = new string[] { "foo", "bar", "baz" };

            using (var cStringArray = new CStringArray(source))
            {
                Assert.That(cStringArray.Ptr, Is.Not.EqualTo(IntPtr.Zero));
                Assert.That(cStringArray.Length, Is.EqualTo(source.Length));
            }
        }

        [Test(Description = "Calling dispose on CStringArray disposes allocated memory")]
        public void DisposesAllocatedMemory()
        {
            var source = new string[] { "foo", "bar", "baz" };

            var cStringArray = new CStringArray(source);
            cStringArray.Dispose();

            Assert.That(cStringArray.Ptr, Is.EqualTo(IntPtr.Zero));
            Assert.That(cStringArray.Length, Is.EqualTo(0));
        }
    }
}
