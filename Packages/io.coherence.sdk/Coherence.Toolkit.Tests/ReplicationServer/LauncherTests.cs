// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit.Tests.ReplicationServer
{
    using System.Globalization;
    using System.Threading;
    using Coherence.Tests;
    using Log;
    using NUnit.Framework;
    using Toolkit.ReplicationServer;

    public class LauncherTests : CoherenceTest
    {
        private CultureInfo cultureInfo;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            cultureInfo = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
        }

        [TearDown]
        public override void TearDown()
        {
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            base.TearDown();
        }

        [Test]
        [TestCase(LogLevel.Debug, LogTarget.File, LogFormat.Colored, "logfile.log")]
        [TestCase(LogLevel.Error, LogTarget.File, LogFormat.Colored, "logfile.log")]
        [TestCase(LogLevel.Info, LogTarget.File, LogFormat.Colored, "logfile.log")]
        [TestCase(LogLevel.Trace, LogTarget.File, LogFormat.Colored, "logfile.log")]
        [TestCase(LogLevel.Warning, LogTarget.File, LogFormat.Colored, "logfile.log")]
        [TestCase(LogLevel.Debug, LogTarget.Console, LogFormat.Colored, "logfile.log")]
        [TestCase(LogLevel.Debug, LogTarget.Console, LogFormat.JSON, "logfile.log")]
        [TestCase(LogLevel.Debug, LogTarget.Console, LogFormat.Plain, "logfile.log")]
        [Description("Verify that the log settings contains culture-independent characters")]
        public void LogTarget_Contains_InvariantText(LogLevel level, LogTarget target, LogFormat format, string path)
        {
            var logTargetConfig = new LogTargetConfig
            {
                Target = target,
                Format = format,
                LogLevel = level,
                FilePath = path
            };

            var expectedTarget = target.ToString().ToLowerInvariant();
            var expectedFormat = format.ToString().ToLowerInvariant();
            var expectedLevel = level.ToString().ToLowerInvariant();
            var commandLineArgument = Launcher.GenerateLogTargetArguments(new[]
            {
                logTargetConfig
            });

            Assert.That(commandLineArgument, Does.Contain($"{expectedTarget}"));
            Assert.That(commandLineArgument, Does.Contain($":{expectedFormat}"));
            Assert.That(commandLineArgument, Does.Contain($":{expectedLevel}"));
        }
    }
}
