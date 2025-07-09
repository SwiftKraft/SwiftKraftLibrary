// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using System.Threading.Tasks;
    using Cloud;
    using Coherence.Tests;
    using Editor.ReplicationServer;
    using NUnit.Framework;
    using Toolkit.ReplicationServer;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;

    // Disabled on other platforms because the tests fail on TeamCity.
    [TestFixture, UnityPlatform(RuntimePlatform.WindowsEditor)]
    [Ignore("Disabled. coherence/unity#8167")]
    public class ReplicationServerUtilsTests : CoherenceTest
    {
        private const int PingTimeOutSecs = 5;
        private static readonly ReplicationServerConfig replicationServerConfig = EditorLauncher.CreateLocalRoomsConfig();
        private static IReplicationServer replicationServer;

        private static TestCaseData[] testCases =
        {
            new TestCaseData("localhost", replicationServerConfig.APIPort, true)
                .SetName("Valid Port and Host")
                .SetDescription($"localhost:{replicationServerConfig.APIPort}"),
            new TestCaseData("localhost", 9999, false)
                .SetName("Invalid Port")
                .SetDescription("localhost:9999"),
            new TestCaseData("192.168.0.0", replicationServerConfig.APIPort, false)
                .SetName("Invalid Host")
                .SetDescription($"foobar:{replicationServerConfig.APIPort}"),
            new TestCaseData("192.168.0.0", 9999, false)
                .SetName("Invalid Port and Host")
                .SetDescription("192.168.0.0:9999"),
        };

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
#if UNITY_2022_1_OR_NEWER
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
#endif
            ReplicationServerUtils.Timeout = 1;
            replicationServer = Launcher.Create(replicationServerConfig);
            replicationServer.Start();
        }

        public override void OneTimeTearDown()
        {
            base.OneTimeTearDown();
            replicationServer.Stop();
        }

        [Test, TestCaseSource(nameof(testCases))]
        public async Task PingHttpServer_Ends(string host, int port, bool shouldSucceed)
        {
            var endTime = EditorApplication.timeSinceStartup + PingTimeOutSecs;
            var done = false;
            var success = false;

            do
            {
                ReplicationServerUtils.PingHttpServer(host, port, ok =>
                {
                    done = true;
                    success = ok;
                });

                await Task.Yield();
            } while (!done || EditorApplication.timeSinceStartup < endTime);

            Assert.IsTrue(success == shouldSucceed);
        }

        [Test, Timeout(1500), TestCaseSource(nameof(testCases))]
        public async Task PingHttpServerAsync_Ends(string host, int port, bool shouldSucceed)
        {
            var success = await ReplicationServerUtils.PingHttpServerAsync(host, port);
            Assert.IsTrue(success == shouldSucceed);
        }
    }
}
