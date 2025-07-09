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
    using Coherence.Tests;

    public class WorldsServiceTests : CoherenceTest
    {
        private Mock<IAuthClientInternal> authClient = new();
        private Mock<IRuntimeSettings> runtimeSettings = new();

        private string worldsResponse =
            "[{\"id\":1173779553,\"project_id\":\"cbkehjjri4dbib3tu900\",\"name\":\"World1_EU\",\"schema_id\":\"826a89bad874101820a571e515e5b068292a7877\",\"ccu_target\":20,\"rs_size\":\"\",\"rs_version\":\"v0.43.0\",\"sdk_version\":\"0.10.9\",\"sim_size\":\"\",\"sim_slug\":\"\",\"sim_args\":\"\",\"rs_args\":\"\",\"rs_send_frequency\":20,\"rs_recv_frequency\":60,\"tags\":[],\"region\":\"eu\",\"created_at\":\"2023-03-02T06:34:25.384866Z\",\"updated_at\":\"2023-03-02T06:34:25.384866Z\",\"rs_resource\":{\"enabled\":false,\"cpu\":0,\"memory\":0,\"tier\":\"\",\"hard_memory\":0},\"sim_resource\":{\"enabled\":false,\"cpu\":0,\"memory\":0,\"tier\":\"\",\"hard_memory\":0},\"schema\":{\"id\":\"\",\"hashes\":null,\"project_id\":\"\",\"sdk_version\":\"\",\"commit\":\"\",\"timestamp\":\"0001-01-01T00:00:00Z\"},\"host\":{\"rsid\":\"ba4e5854-4712-fe53-838a-4c2e3797e78d\",\"ip\":\"18.196.114.164\",\"udp_port\":29165,\"web_port\":22475,\"sig_port\":31203,\"region\":\"eu\",\"job_id\":\"rsw-eu-cbkehjjri4dbib3tu900-1173779553\",\"max_ccu\":0},\"rs_status\":{\"exists\":false,\"running\":false,\"healthy\":false,\"error\":\"\",\"desired_status\":\"\"},\"pc_status\":{\"exists\":false,\"running\":false,\"healthy\":false,\"error\":\"\",\"desired_status\":\"\"},\"sim_status\":{\"exists\":false,\"running\":false,\"healthy\":false,\"error\":\"\",\"desired_status\":\"\"}},{\"id\":1307510269,\"project_id\":\"cbkehjjri4dbib3tu900\",\"name\":\"World2_US\",\"schema_id\":\"826a89bad874101820a571e515e5b068292a7877\",\"ccu_target\":20,\"rs_size\":\"\",\"rs_version\":\"v0.43.0\",\"sdk_version\":\"0.10.9\",\"sim_size\":\"\",\"sim_slug\":\"\",\"sim_args\":\"\",\"rs_args\":\"\",\"rs_send_frequency\":20,\"rs_recv_frequency\":60,\"tags\":[],\"region\":\"us\",\"created_at\":\"2023-03-02T06:34:44.57957Z\",\"updated_at\":\"2023-03-02T06:34:44.57957Z\",\"rs_resource\":{\"enabled\":false,\"cpu\":0,\"memory\":0,\"tier\":\"\",\"hard_memory\":0},\"sim_resource\":{\"enabled\":false,\"cpu\":0,\"memory\":0,\"tier\":\"\",\"hard_memory\":0},\"schema\":{\"id\":\"\",\"hashes\":null,\"project_id\":\"\",\"sdk_version\":\"\",\"commit\":\"\",\"timestamp\":\"0001-01-01T00:00:00Z\"},\"host\":{\"rsid\":\"1142c968-f2be-4dc4-9955-f4894ea9aa5d\",\"ip\":\"44.204.106.106\",\"udp_port\":21627,\"web_port\":20872,\"sig_port\":20112,\"region\":\"us\",\"job_id\":\"rsw-us-cbkehjjri4dbib3tu900-1307510269\",\"max_ccu\":0},\"rs_status\":{\"exists\":false,\"running\":false,\"healthy\":false,\"error\":\"\",\"desired_status\":\"\"},\"pc_status\":{\"exists\":false,\"running\":false,\"healthy\":false,\"error\":\"\",\"desired_status\":\"\"},\"sim_status\":{\"exists\":false,\"running\":false,\"healthy\":false,\"error\":\"\",\"desired_status\":\"\"}}]";

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            runtimeSettings.Setup(settings => settings.SchemaID).Returns(string.Empty);
            var versionInfoMock = new Mock<IVersionInfo>();
            versionInfoMock.Setup(vi => vi.Engine).Returns(string.Empty);
            runtimeSettings.Setup(settings => settings.VersionInfo).Returns(versionInfoMock.Object);
        }

        [Test]
        public void Should_ReturnNoWorlds_When_NoWorldsExist()
        {
            var requestMock = new Mock<IRequestFactoryInternal>();
            requestMock.Setup(factory => factory.SendRequest(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string,
                        string>>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<Action<RequestResponse<string>>>()))
                .Callback<string, string, string, string, Dictionary<string, string>, string, string,
                    Action<RequestResponse<string>>>(
                    (s, s1, s2, arg3, arg4, arg5, arg6, arg7) =>
                    {
                        var response = new RequestResponse<string>()
                        {
                            Result = string.Empty,
                            Status = RequestStatus.Success
                        };

                        arg7.Invoke(response);
                    });

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var worldService = new WorldsService(credentials, runtimeSettings.Object);

            bool receivedWorlds = false;

            worldService.FetchWorlds(list =>
            {
                if (list.Result.Count == 0)
                {
                    receivedWorlds = true;
                }
            });

            Assert.IsTrue(receivedWorlds);
        }

        [Test]
        public void Should_ReturnTwoWorlds_When_TwoWorldsExist()
        {
            var requestMock = new Mock<IRequestFactoryInternal>();
            requestMock.Setup(factory => factory.SendRequest(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string,
                        string>>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<Action<RequestResponse<string>>>()))
                .Callback<string, string, string, string, Dictionary<string, string>, string, string,
                    Action<RequestResponse<string>>>(
                    (s, s1, s2, arg3, arg4, arg5, arg6, arg7) =>
                    {
                        var response = new RequestResponse<string>()
                        {
                            Result = worldsResponse,
                            Status = RequestStatus.Success
                        };

                        arg7.Invoke(response);
                    });

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var worldService = new WorldsService(credentials, runtimeSettings.Object);

            bool receivedWorlds = false;

            worldService.FetchWorlds(list =>
            {
                if (list.Result.Count == 2)
                {
                    receivedWorlds = true;
                }
            });

            Assert.IsTrue(receivedWorlds);
        }

        [Test]
        public void Should_RethrowException_When_RequestHasException()
        {
            var requestMock = new Mock<IRequestFactoryInternal>();
            requestMock.Setup(factory => factory.SendRequest(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string,
                        string>>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<Action<RequestResponse<string>>>()))
                .Callback<string, string, string, string, Dictionary<string, string>, string, string,
                    Action<RequestResponse<string>>>(
                    (s, s1, s2, arg3, arg4, arg5, arg6, arg7) =>
                    {
                        var response = new RequestResponse<string>()
                        {
                            Exception = new RequestException(ErrorCode.Unknown),
                            Status = RequestStatus.Fail
                        };

                        arg7.Invoke(response);
                    });

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var worldService = new WorldsService(credentials, runtimeSettings.Object);

            bool caughtException = false;

            worldService.FetchWorlds(list =>
            {
                caughtException = list.Exception != null;
            });

            Assert.IsTrue(caughtException);
        }

        [Test]
        public async Task Should_ReturnTwoWorlds_When_TwoWorldsExistAsync()
        {
            var requestMock = new Mock<IRequestFactoryInternal>();
            requestMock.Setup(factory => factory.SendRequestAsync(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string,
                        string>>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<string>(worldsResponse));

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var worldService = new WorldsService(credentials, runtimeSettings.Object);

            var response = await worldService.FetchWorldsAsync();

            Assert.IsTrue(response.Count == 2);
        }

        [Test]
        public async Task Should_ReturnNoWorlds_When_NoWorldsExistAsync()
        {
            var requestMock = new Mock<IRequestFactoryInternal>();
            requestMock.Setup(factory => factory.SendRequestAsync(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string,
                        string>>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult<string>(string.Empty));

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var worldService = new WorldsService(credentials, runtimeSettings.Object);

            var response = await worldService.FetchWorldsAsync();

            Assert.IsTrue(response.Count == 0);
        }


        [Test]
        public async Task Should_RethrowException_When_RequestHasExceptionAsync()
        {
            var requestMock = new Mock<IRequestFactoryInternal>();
            requestMock.Setup(factory => factory.SendRequestAsync(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string,
                        string>>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string, string, Dictionary<string, string>, string, string>((s, s1, s2,
                    arg3, arg4, arg5, arg6) => throw new RequestException(ErrorCode.Unknown));

            var credentials = new CloudCredentialsPair(authClient.Object, requestMock.Object);
            using var worldService = new WorldsService(credentials, runtimeSettings.Object);

            bool caughtException = false;

            try
            {
                await worldService.FetchWorldsAsync();
            }
#pragma warning disable 0168
            catch (RequestException e)
#pragma warning restore 0168
            {
                caughtException = true;
            }
            finally
            {
                Assert.IsTrue(caughtException);
            }
        }

        [Test]
        public async Task Should_QueueCallbacks_When_RequestIsOnGoingAsync()
        {
            var mockRequestFactory = new MockRequestFactoryBuilder()
                .SendRequestAsyncReturns
                (
                    // first call
                    async () =>
                    {
                        await Task.Yield();
                        return worldsResponse;
                    },
                    // second call
                    () => Task.FromResult(worldsResponse)
                )
                .Build();

            var credentials = new CloudCredentialsPair(authClient.Object, mockRequestFactory);
            using var worldService = new WorldsService(credentials, runtimeSettings.Object);

            var fetchWorldsTask1 = worldService.FetchWorldsAsync();
            Assert.That(fetchWorldsTask1.IsCompletedSuccessfully, Is.False);

            var fetchWorldsTask2 = worldService.FetchWorldsAsync();
            Assert.That(fetchWorldsTask2.IsCompleted, Is.False);

            await Task.WhenAll(fetchWorldsTask1, fetchWorldsTask2);

            Assert.That(fetchWorldsTask1.IsCompletedSuccessfully, Is.True);
            Assert.That(fetchWorldsTask2.IsCompletedSuccessfully, Is.True);
        }
    }
}
