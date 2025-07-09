// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Tests
{
    using NUnit.Framework;

    /// <summary>
    /// Unit tests for <see cref="TaskUtils"/>.
    /// </summary>
    public class TaskUtilsTests
    {
        [Test]
        public void Scheduler_Is_Not_Null() => Assert.That(TaskUtils.Scheduler, Is.Not.Null);
    }
}
