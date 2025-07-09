// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Coherence.Tests;
    using NUnit.Framework;
    using Utils;

    /// <summary>
    /// Edit Mode unit tests for <see cref="Wait"/>.
    /// </summary>
    public sealed class WaitTests : CoherenceTest
    {
        [TestCase(0d), TestCase(0.01d)]
        public async Task Wait_For_Completes(double seconds) => await Wait.For(TimeSpan.FromSeconds(seconds));

        [TestCase(0d), TestCase(0.01d)]
        public async Task Wait_For_With_CancellationToken_Completes(double seconds) => await Wait.For(TimeSpan.FromSeconds(seconds), new CancellationTokenSource().Token);

        [Test]
        public async Task Wait_For_Can_Be_Canceled()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var waitFor = Wait.For(TimeSpan.FromSeconds(0.01d), cancellationTokenSource.Token);
            var completedSuccessfully = false;
            var awaiter = waitFor.GetAwaiter();
            waitFor.GetAwaiter().OnCompleted(() => { completedSuccessfully = awaiter.IsCompleted; });
            cancellationTokenSource.Cancel();
            await waitFor;
            Assert.That(!waitFor.IsCompleted);
            Assert.That(completedSuccessfully, Is.False);
        }
    }
}
