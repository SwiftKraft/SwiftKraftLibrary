// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System;
    using System.Threading.Tasks;
    using Coherence.Tests;
    using NUnit.Framework;
    using Utils;

    /// <summary>
    /// Unit tests for <see cref="TimeSpanExtensions"/>.
    /// </summary>
    public sealed class TimeSpanExtensionsTests : CoherenceTest
    {
        [TestCase(0d), TestCase(0.01d)]
        public async Task Await_TimeSpan_Completes(double seconds) => await TimeSpan.FromSeconds(seconds);
    }
}
