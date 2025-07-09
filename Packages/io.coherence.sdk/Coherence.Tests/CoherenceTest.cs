// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Tests
{
    using System;
    using System.Threading.Tasks;
    using Common;
    using Common.Tests;
    using Log;
    using NUnit.Framework;
    using Logger = Log.Logger;

    public class CoherenceTest
    {
        public TestLogger logger;

        [SetUp]
        virtual public void SetUp()
        {
            logger = new TestLogger(typeof(CoherenceTest));

            logger.Info($"{GetType().Name}.{TestContext.CurrentContext.Test.Name} - SetUp");
            IDisposableInternal.SetCurrentInitializationContext(TestContext.CurrentContext.Test.Name);
        }

        [TearDown]
        virtual public void TearDown()
        {
            Assert.That(logger.GetLogLevelCount(LogLevel.Warning), Is.Zero, "Test failed with untracked warnings.");
            Assert.That(logger.GetLogLevelCount(LogLevel.Error), Is.Zero, "Test failed with untracked errors.");

            logger.Info($"{GetType().Name}.{TestContext.CurrentContext.Test.Name} - TearDown");
            logger.Dispose();
            IDisposableInternal.SetCurrentInitializationContext(null);
        }

        [OneTimeSetUp]
        virtual public void OneTimeSetUp()
        {
        }

        [OneTimeTearDown]
        virtual public void OneTimeTearDown()
        {
            // Make sure that warnings from leaked resources get logged at this point.
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public class TimeoutException : Exception { }

        public async Task WaitWhile(Func<bool> condition, int frequency = 25, int timeout = -1)
        {
            var waitTask = Task.Run(async () =>
            {
                while (condition()) await Task.Delay(frequency);
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
            {
                throw new TimeoutException();
            }
        }
    }
}
