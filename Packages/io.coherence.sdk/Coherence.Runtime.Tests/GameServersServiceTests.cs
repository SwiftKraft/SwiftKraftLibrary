// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Runtime.Tests
{
    using Cloud;
    using Common;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Moq.Language.Flow;
    using Coherence.Tests;

    [TestFixture]
    public class GameServersServiceTests : CoherenceTest
    {
        private readonly Mock<IAuthClientInternal> authClient = new();
        private readonly Mock<IRuntimeSettings> runtimeSettings = new();

        private const string TestGameServerJson = @"
        {
            ""id"": 33568963,
            ""region"": ""eu"",
            ""slug"": ""ref-gamesrv4"",
            ""tag"": ""test"",
            ""kv"": {""foo"": ""bar""},
            ""size"": ""unity.8x"",
            ""max_players"": 10,
            ""connected_players"": 1,
            ""suspended"": true,
            ""ip"": ""127.0.0.1"",
            ""port"": 42050,
            ""created_at"": 1703165531,
            ""last_started_at"": 1703165531
        }
        ";

        [Test]
        public async Task DeployAsync_Should_ReturnIdObject_When_GivenCorrectOptions()
        {
            var (requestMock, service) = NewTestService();

            MockAsyncResponse(requestMock, Task.FromResult(@"{""id"": 123, ""secret"": ""secret""}"));

            var gameServer = await service.DeployAsync(new GameServerDeployOptions());
            Assert.AreEqual(gameServer.Id, 123);
            Assert.AreEqual(gameServer.Secret, "secret");
        }

        [Test]
        public void DeployAsync_Should_ThrowException_When_ServerRespondsWithError()
        {
            var (requestMock, service) = NewTestService();

            var ex = new RequestException(ErrorCode.Unknown, 400, "Bad Request");
            MockAsyncException(requestMock, ex);

            Assert.ThrowsAsync<RequestException>(async () =>
            {
                await service.DeployAsync(new GameServerDeployOptions());
            });
        }

        [Test]
        public async Task ListAsync_Should_ReturnGameServers_When_GivenCorrectOptions()
        {
            var (requestMock, service) = NewTestService();

            MockAsyncResponse(requestMock, Task.FromResult($@"
            [
                {TestGameServerJson},
                {TestGameServerJson}
            ]
            "));

            var gameServers = await service.ListAsync(new GameServerListOptions());
            Assert.AreEqual(gameServers.Count, 2);
            ValidateTestGameServer(gameServers[0]);
        }

        [Test]
        public void ListAsync_Should_ThrowException_When_ServerRespondsWithError()
        {
            var (requestMock, service) = NewTestService();

            var ex = new RequestException(ErrorCode.Unknown, 500, "Internal Server Error");
            MockAsyncException(requestMock, ex);

            Assert.ThrowsAsync<RequestException>(async () =>
            {
                await service.ListAsync(new GameServerListOptions());
            });
        }

        [Test]
        public async Task MatchAsync_Should_ReturnGameServer_When_ThereIsAMatch()
        {
            var (requestMock, service) = NewTestService();

            MockAsyncResponse(requestMock, Task.FromResult($"{{\"gameserver\": {TestGameServerJson}}}"));

            var optionalGameServer = await service.MatchAsync(new GameServerMatchOptions());
            Assert.NotNull(optionalGameServer.GameServerData);
            ValidateTestGameServer(optionalGameServer.GameServerData.Value);
        }

        [Test]
        public async Task MatchAsync_Should_ReturnEmptyGameServerData_When_ThereIsNoMatch()
        {
            var (requestMock, service) = NewTestService();

            MockAsyncResponse(requestMock, Task.FromResult(@"{""gameserver"": null}"));

            var optionalGameServer = await service.MatchAsync(new GameServerMatchOptions());
            Assert.Null(optionalGameServer.GameServerData);
        }

        [Test]
        public void MatchAsync_Should_ThrowException_When_ServerRespondsWithError()
        {
            var (requestMock, service) = NewTestService();

            var ex = new RequestException(ErrorCode.Unknown, 500, "Internal Server Error");
            MockAsyncException(requestMock, ex);

            Assert.ThrowsAsync<RequestException>(async () =>
            {
                await service.MatchAsync(new GameServerMatchOptions());
            });
        }

        [Test]
        public async Task GetAsync_Should_ReturnGameServer_When_ItExists()
        {
            var (requestMock, service) = NewTestService();

            MockAsyncResponse(requestMock, Task.FromResult(TestGameServerJson));

            var gameServer = await service.GetAsync(33568963);
            ValidateTestGameServer(gameServer);
        }

        [Test]
        public void GetAsync_Should_RaiseException_When_ItDoesntExist()
        {
            var (requestMock, service) = NewTestService();

            var ex = new RequestException(ErrorCode.Unknown, 404, "Not Found");
            MockAsyncException(requestMock, ex);

            Assert.ThrowsAsync<RequestException>(async () =>
            {
                await service.GetAsync(0);
            });
        }

        [Test]
        public void SuspendAsync_Should_Succeed_When_ItExists()
        {
            var (requestMock, service) = NewTestService();

            MockAsyncResponse(requestMock, Task.FromResult(""));

            Assert.DoesNotThrowAsync(async () =>
            {
                await service.SuspendAsync(1);
            });
        }

        [Test]
        public void SuspendAsync_Should_ThrowException_When_ItDoesntExist()
        {
            var (requestMock, service) = NewTestService();

            var ex = new RequestException(ErrorCode.Unknown, 404, "Not Found");
            MockAsyncException(requestMock, ex);

            Assert.ThrowsAsync<RequestException>(async () =>
            {
                await service.SuspendAsync(0);
            });
        }

        [Test]
        public void ResumeAsync_Should_Succeed_When_ItExists()
        {
            var (requestMock, service) = NewTestService();

            MockAsyncResponse(requestMock, Task.FromResult(""));

            Assert.DoesNotThrowAsync(async () =>
            {
                await service.ResumeAsync(1);
            });
        }

        [Test]
        public void ResumeAsync_Should_ThrowException_When_ItDoesntExist()
        {
            var (requestMock, service) = NewTestService();

            var ex = new RequestException(ErrorCode.Unknown, 404, "Not Found");
            MockAsyncException(requestMock, ex);

            Assert.ThrowsAsync<RequestException>(async () =>
            {
                await service.ResumeAsync(0);
            });
        }

        [Test]
        public void DeleteAsync_Should_Succeed_When_ItExists()
        {
            var (requestMock, service) = NewTestService();

            MockAsyncResponse(requestMock, Task.FromResult(""));

            Assert.DoesNotThrowAsync(async () =>
            {
                await service.DeleteAsync(1);
            });
        }

        [Test]
        public void DeleteAsync_Should_ThrowException_When_ItDoesntExist()
        {
            var (requestMock, service) = NewTestService();

            var ex = new RequestException(ErrorCode.Unknown, 404, "Not Found");
            MockAsyncException(requestMock, ex);

            Assert.ThrowsAsync<RequestException>(async () =>
            {
                await service.DeleteAsync(0);
            });
        }

        private void ValidateTestGameServer(GameServerData server)
        {
            Assert.AreEqual(server.Id, 33568963);
            Assert.AreEqual(server.Region, "eu");
            Assert.AreEqual(server.Slug, "ref-gamesrv4");
            Assert.AreEqual(server.Tag, "test");
            Assert.AreEqual(server.KV, new Dictionary<string, string>
            {
                { "foo", "bar" },
            });
            Assert.AreEqual(server.Size, "unity.8x");
            Assert.AreEqual(server.MaxPlayers, 10);
            Assert.AreEqual(server.ConnectedPlayers, 1);
            Assert.AreEqual(server.Suspended, true);
            Assert.AreEqual(server.Ip, "127.0.0.1");
            Assert.AreEqual(server.Port, 42050);
            Assert.AreEqual(server.CreatedAt, 1703165531);
            Assert.AreEqual(server.LastStartedAt, 1703165531);
        }

        //
        // Test helpers
        //

        private (Mock<IRequestFactoryInternal>, IGameServersService) NewTestService()
        {
            var requestMock = new Mock<IRequestFactoryInternal>(MockBehavior.Strict);
            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            var service = new GameServersService(credentials, runtimeSettings.Object);
            return (requestMock, service);
        }

        private static void MockAsyncResponse(Mock<IRequestFactoryInternal> mock, Task<string> response)
        {
            SetupAsyncMockRequest(mock).Returns(response);
        }

        private static void MockAsyncException(Mock<IRequestFactoryInternal> mock, Exception ex)
        {
            SetupAsyncMockRequest(mock).Throws(ex);
        }

        private static ISetup<IRequestFactoryInternal, Task<string>> SetupAsyncMockRequest(Mock<IRequestFactoryInternal> mock)
        {
            return mock.Setup(factory => factory.SendRequestAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>())
            );
        }
    }
}
